using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChemLab.Chem;
namespace ChemLab.Game {
// Native contact island per vessel. No renderers or chemistry objects are cloned.
// Translation only for now, matching the cached fixed-orientation immersion data.
public sealed class NativeSolidWorld:IDisposable {
    static int serial;
    readonly Scene scene;
    readonly PhysicsScene physics;
    readonly Rigidbody[] bodies;
    readonly Collider[] colliders;
    readonly Bounds[] localBounds;
    readonly SolidBodySeed[] seeds;
    readonly SolidTranslationState[] states;
    readonly List<GameObject> hosts=new List<GameObject>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly PhysicMaterial material;
    // Normalize small contact geometry inside the isolated physics scene.
    // Visible meshes, hydrostatics and mass remain in laboratory units. The
    // original scale is retained only as an explicit diagnostic comparison.
    readonly float simulationScale=Array.IndexOf(Environment.GetCommandLineArgs(),"-nativeSolidUnitScale")>=0?1f:10f;
    bool disposed;
    double accumulated;
    public double SimulatedSeconds {get;private set;}
    public int WallTriangleCount {get;private set;}
    public float MaximumCollisionSkin {get;private set;}
    public bool ResidualContactsEnabledForTest {get;}=Array.IndexOf(Environment.GetCommandLineArgs(),"-solidResidualContacts")>=0;
    double residualQueryMs,residualProjectionMs;
    long forceTicks,simulationTicks,wallTicks;
    int measuredSteps;
    public void ResetStepTimings(){forceTicks=simulationTicks=wallTicks=0;measuredSteps=0;}
    public NativeSolidWorld(VesselKind kind,SolidBodySeed[] seeds,SolidTranslationState[] states){
        this.seeds=seeds;this.states=states;bodies=new Rigidbody[seeds.Length];
        colliders=new Collider[seeds.Length];localBounds=new Bounds[seeds.Length];
        scene=SceneManager.CreateScene("ChemLab Native Solids "+(++serial),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        physics=scene.GetPhysicsScene();
        material=new PhysicMaterial("Solid contact no friction"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicMaterialCombine.Minimum,bounceCombine=PhysicMaterialCombine.Minimum};
        try{
            var wall=VesselMesh.BuildCollisionGlass(kind);meshes.Add(wall);AddCollider("Vessel wall",wall,false,Vector3.zero,1);
            WallTriangleCount=wall.triangles.Length/3;
            // The render shell closes its bottom annulus, not the central cavity.
            // Supply the same impermeable minimum floor used by SolidVesselBoundary.
            // The curved side mesh still constrains round-bottom vessels above it.
            var geo=VesselMesh.Get(kind);
            float floor=Mathf.Max(Mathf.Max(.012f,geo.R*.052f),kind==VesselKind.Cylinder?.04f:0);
            var floorHost=new GameObject("Vessel solid floor"){hideFlags=HideFlags.HideAndDontSave,layer=2};hosts.Add(floorHost);
            SceneManager.MoveGameObjectToScene(floorHost,scene);
            floorHost.transform.localScale=Vector3.one*simulationScale;
            var floorCollider=floorHost.AddComponent<BoxCollider>();
            floorCollider.center=new Vector3(0,floor-.05f,0);floorCollider.size=new Vector3(geo.R*2,.1f,geo.R*2);
            floorCollider.sharedMaterial=material;floorCollider.contactOffset=.0001f*simulationScale;
            for(int i=0;i<seeds.Length;i++){
                var seed=seeds[i];if(seed.IsSpilled)continue;
                if(seed.IsStatic){var bed=new Mesh{name="Native powder bed",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};bed.vertices=seed.LocalVertices;bed.triangles=seed.Triangles;bed.RecalculateBounds();meshes.Add(bed);AddCollider("Powder bed",bed,false,seed.Center,1);continue;}
                if(states[i]==null)continue;
                float radius=0;foreach(var p in seed.LocalVertices)radius=Mathf.Max(radius,p.magnitude);
                var vertices=new Vector3[seed.LocalVertices.Length];for(int v=0;v<vertices.Length;v++)vertices[v]=seed.LocalVertices[v]/radius;
                var mesh=new Mesh{name="Native normalized solid"};mesh.vertices=vertices;mesh.triangles=seed.Triangles;mesh.RecalculateBounds();meshes.Add(mesh);
                // Conservative collision proxy only: at most 0.2 mm or 2.5% of
                // body radius. Render geometry, hydrostatic volume and mass keep
                // their original dimensions. Independent original-shape audits
                // must still pass; proxy penetration is reported separately.
                float skin=Mathf.Min(.002f,radius*.025f),collisionRadius=radius+skin;
                MaximumCollisionSkin=Mathf.Max(MaximumCollisionSkin,skin);
                var host=AddCollider("Solid body",mesh,true,states[i].Position,collisionRadius);
                colliders[i]=host.GetComponent<Collider>();
                localBounds[i]=new Bounds(mesh.bounds.center*collisionRadius,mesh.bounds.size*collisionRadius);
                var body=host.AddComponent<Rigidbody>();bodies[i]=body;
                body.useGravity=false;body.mass=(float)seed.MassGrams;body.drag=0;body.angularDrag=0;
                body.constraints=RigidbodyConstraints.FreezeRotation;body.solverIterations=32;body.solverVelocityIterations=8;
                body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.maxDepenetrationVelocity=5*simulationScale;
                body.sleepThreshold=0;body.velocity=states[i].Velocity*simulationScale;
            }
        }catch{Dispose();throw;}
    }
    GameObject AddCollider(string name,Mesh mesh,bool convex,Vector3 position,float scale){
        var host=new GameObject(name){hideFlags=HideFlags.HideAndDontSave,layer=2};hosts.Add(host);
        SceneManager.MoveGameObjectToScene(host,scene);host.transform.position=position*simulationScale;host.transform.localScale=Vector3.one*(scale*simulationScale);
        var collider=host.AddComponent<MeshCollider>();collider.sharedMaterial=material;
        collider.contactOffset=.0001f*simulationScale;
        collider.convex=convex;collider.sharedMesh=mesh;
        return host;
    }
    public void Step(float dt,float surface,double liquidDensity,float drag){
        if(disposed)throw new ObjectDisposedException(nameof(NativeSolidWorld));
        if(!(dt>0)||dt>.25f||float.IsNaN(dt)||double.IsNaN(liquidDensity)||double.IsInfinity(liquidDensity)||liquidDensity<0||float.IsNaN(drag)||float.IsInfinity(drag)||drag<0)throw new ArgumentException("Invalid native solid step");
        accumulated+=dt;const float fixedStep=1f/240;
        while(accumulated>=fixedStep){accumulated-=fixedStep;StepFixed(fixedStep,surface,liquidDensity,drag);SimulatedSeconds+=fixedStep;}
    }
    void StepFixed(float dt,float surface,double liquidDensity,float drag){
        long started=System.Diagnostics.Stopwatch.GetTimestamp();measuredSteps++;
        double decay=Math.Exp(-drag*dt),response=drag<1e-6?dt:(1-decay)/drag;
        for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){
            var state=states[i];var body=bodies[i];state.UpdateHydrostatics(surface);
            if(!(body.position/simulationScale).Equals(state.Position))body.position=state.Position*simulationScale;
            double acceleration=VesselMesh.GravityWorldUnitsPerSecondSquared*(liquidDensity*1000*state.DisplacedWorldVolume/seeds[i].MassGrams-1);
            body.velocity=(state.Velocity*(float)decay+Vector3.up*(float)(acceleration*response))*simulationScale;
        }
        forceTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        started=System.Diagnostics.Stopwatch.GetTimestamp();physics.Simulate(dt);
        simulationTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        started=System.Diagnostics.Stopwatch.GetTimestamp();
        for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){
            var body=bodies[i];var state=states[i];
            state.AcceptProjectedPose(body.position/simulationScale,body.velocity/simulationScale);
            if(!(body.position/simulationScale).Equals(state.Position))body.position=state.Position*simulationScale;
            if(!(body.velocity/simulationScale).Equals(state.Velocity))body.velocity=state.Velocity*simulationScale;
            state.UpdateHydrostatics(surface);
        }
        wallTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
    }
    public float MaximumPenetrationForTest(){
        float max=0;
        for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)for(int j=i+1;j<bodies.Length;j++)if(bodies[j]!=null){
            var a=bodies[i].GetComponent<Collider>();var b=bodies[j].GetComponent<Collider>();
            if(Physics.ComputePenetration(a,bodies[i].position,bodies[i].rotation,b,bodies[j].position,bodies[j].rotation,out _,out float depth))max=Mathf.Max(max,depth/simulationScale);
        }
        return max;
    }
    // End-of-frame split-position contact solve. Geometry and mass are unchanged;
    // wall projection may consume a correction, so refresh contacts each sweep.
    public void ResolveResidualContacts(){
        if(disposed)throw new ObjectDisposedException(nameof(NativeSolidWorld));
        if(!ResidualContactsEnabledForTest)return;
        long queryTicks=0,projectionTicks=0;
        for(int sweep=0;sweep<8;sweep++){
            float largest=0;
            for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)
            for(int j=i+1;j<bodies.Length;j++)if(bodies[j]!=null){
                var a=states[i];var b=states[j];
                var ba=localBounds[i];ba.center+=a.Position;
                var bb=localBounds[j];bb.center+=b.Position;
                if(!ba.Intersects(bb))continue;
                long started=System.Diagnostics.Stopwatch.GetTimestamp();
                bool hit=Physics.ComputePenetration(colliders[i],a.Position*simulationScale,Quaternion.identity,colliders[j],b.Position*simulationScale,Quaternion.identity,out var normal,out float depth);
                depth/=simulationScale;
                queryTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
                if(!hit||depth<=.00001f)continue;
                largest=Mathf.Max(largest,depth);
                float wa=(float)(seeds[j].MassGrams/(seeds[i].MassGrams+seeds[j].MassGrams)),wb=1-wa;
                var correction=normal*(depth+.000002f);
                var va=a.Velocity;var vb=b.Velocity;
                float incoming=Vector3.Dot(va-vb,normal);
                if(incoming<0){va-=normal*(incoming*wa);vb+=normal*(incoming*wb);}
                started=System.Diagnostics.Stopwatch.GetTimestamp();
                a.AcceptProjectedPose(a.Position+correction*wa,va);
                b.AcceptProjectedPose(b.Position-correction*wb,vb);
                projectionTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
            }
            if(largest<=.00001f)break;
        }
        residualQueryMs=queryTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
        residualProjectionMs=projectionTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
        for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){
            if(!(bodies[i].position/simulationScale).Equals(states[i].Position))bodies[i].position=states[i].Position*simulationScale;
            if(!(bodies[i].velocity/simulationScale).Equals(states[i].Velocity))bodies[i].velocity=states[i].Velocity*simulationScale;
        }
    }
    // Diagnostic only: identify whether the residual belongs to one jammed pair
    // or a broad stack convergence failure; never relax the acceptance threshold.
    public string ContactReportForTest(){
        int count=0,worstA=-1,worstB=-1;float max=0;Vector3 normal=Vector3.zero;
        for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)for(int j=i+1;j<bodies.Length;j++)if(bodies[j]!=null){
            var a=bodies[i].GetComponent<Collider>();var b=bodies[j].GetComponent<Collider>();
            if(Physics.ComputePenetration(a,bodies[i].position,bodies[i].rotation,b,bodies[j].position,bodies[j].rotation,out var n,out float depth)&&depth>0){
                depth/=simulationScale;count++;if(depth>max){max=depth;worstA=i;worstB=j;normal=n;}
            }
        }
        double milliseconds=1000.0/System.Diagnostics.Stopwatch.Frequency;
        string report=$"scale={simulationScale} steps={measuredSteps} forceMs={forceTicks*milliseconds:G7} simulateMs={simulationTicks*milliseconds:G7} wallMs={wallTicks*milliseconds:G7} wallTriangles={WallTriangleCount} maxCollisionSkinWorld={MaximumCollisionSkin:G7} residualQueryMs={residualQueryMs:G7} residualProjectionMs={residualProjectionMs:G7} pairs={count} max={max:G9} simulatedSeconds={SimulatedSeconds:G9}";
        if(worstA<0)return report;
        foreach(int index in new[]{worstA,worstB}){
            var body=bodies[index];var seed=seeds[index];
            report+=$" body[{index}] substance={seed.SubstanceId} massG={seed.MassGrams:G9} position={(body.position/simulationScale).ToString("F7")} velocity={(body.velocity/simulationScale).ToString("F7")} displaced={states[index].DisplacedWorldVolume:G9}";
        }
        return report+$" normal={normal.ToString("F7")}";
    }
    public void Dispose(){
        if(disposed)return;disposed=true;
        foreach(var host in hosts)if(host!=null){host.SetActive(false);UnityEngine.Object.Destroy(host);}
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
    }
}
}
