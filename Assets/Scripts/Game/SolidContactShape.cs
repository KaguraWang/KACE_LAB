using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ChemLab.Game {
// A convex contact proxy cooked from the visible solid vertices in an isolated
// physics scene. Disabling MeshCollider removes the native shape needed by the
// penetration query, so isolation is by physics scene, not enabled=false.
// It never participates in scene picking, trigger callbacks or automatic physics.
// Buoyancy/mass still use the original irregular mesh, not its convex hull.
public sealed class SolidContactShape:IDisposable {
    readonly GameObject host;
    readonly Mesh mesh;
    readonly MeshCollider collider;
    bool disposed;
    readonly Vector3 minimum,maximum;
    static Scene queryScene;
    static int liveShapes;
    static int sceneSerial;
    public float Radius {get;}
    public bool ParticipatesInSceneQueries=>host!=null&&host.scene.GetPhysicsScene()==Physics.defaultPhysicsScene;
    public SolidContactShape(Vector3[] vertices,int[] triangles){
        if(vertices==null||vertices.Length<4||triangles==null)throw new ArgumentException("Solid contact mesh required");
        float radius=0;foreach(var p in vertices)radius=Mathf.Max(radius,p.magnitude);
        if(!(radius>1e-8f))throw new ArgumentException("Solid contact mesh has no extent");
        Radius=radius;
        minimum=maximum=vertices[0];
        foreach(var p in vertices){minimum=Vector3.Min(minimum,p);maximum=Vector3.Max(maximum,p);}
        // Normalize before PhysX welding/cooking to retain sub-millimetre grains.
        var normalized=new Vector3[vertices.Length];for(int i=0;i<vertices.Length;i++)normalized[i]=vertices[i]/radius;
        mesh=new Mesh{name="Solid contact hull source",hideFlags=HideFlags.HideAndDontSave};
        mesh.vertices=normalized;mesh.triangles=triangles;mesh.RecalculateBounds();
        host=new GameObject("Solid contact query only"){hideFlags=HideFlags.HideAndDontSave,layer=2};
        if(!queryScene.IsValid()||!queryScene.isLoaded)queryScene=SceneManager.CreateScene("ChemLab Solid Contact Queries "+(++sceneSerial),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        SceneManager.MoveGameObjectToScene(host,queryScene);liveShapes++;
        host.transform.localScale=Vector3.one*radius;
        collider=host.AddComponent<MeshCollider>();
        collider.convex=true;collider.sharedMesh=mesh;
    }
    public bool Penetration(Vector3 position,SolidContactShape other,Vector3 otherPosition,out Vector3 normal,out float depth){
        normal=Vector3.zero;depth=0;
        var aMin=position+minimum;var aMax=position+maximum;
        var bMin=otherPosition+other.minimum;var bMax=otherPosition+other.maximum;
        if(aMin.x>bMax.x||bMin.x>aMax.x||aMin.y>bMax.y||bMin.y>aMax.y||aMin.z>bMax.z||bMin.z>aMax.z)return false;
        if((position-otherPosition).sqrMagnitude>(Radius+other.Radius)*(Radius+other.Radius))return false;
        return Physics.ComputePenetration(collider,position,Quaternion.identity,other.collider,otherPosition,Quaternion.identity,out normal,out depth)&&depth>1e-7f;
    }
    public void Dispose(){
        if(disposed)return;disposed=true;
        collider.enabled=false;
        if(Application.isPlaying){UnityEngine.Object.Destroy(host);UnityEngine.Object.Destroy(mesh);}
        else{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(mesh);}
        if(--liveShapes==0&&queryScene.IsValid()){SceneManager.UnloadSceneAsync(queryScene);queryScene=default;}
    }
}
public static class SolidPairContact {
    public struct Constraint {
        public SolidTranslationState A,B;
        public Vector3 Normal,InitialDelta;
        public float Depth;
        public double MassA,MassB;
        public bool HasProjection;
        public Vector3 LastProjectedA,LastProjectedB;
    }
    public static bool BuildConstraint(SolidContactShape a,SolidTranslationState stateA,double massA,SolidContactShape b,SolidTranslationState stateB,double massB,out Constraint constraint){
        constraint=default;long started=System.Diagnostics.Stopwatch.GetTimestamp();QueryCalls++;
        bool hit=a.Penetration(stateA.Position,b,stateB.Position,out var normal,out float depth);
        QueryTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        if(!hit)return false;Overlaps++;
        constraint=new Constraint{A=stateA,B=stateB,Normal=normal,InitialDelta=stateA.Position-stateB.Position,Depth=depth,MassA=massA,MassB=massB};return true;
    }
    // A contact Jacobian is held only for this 1/240-second substep; positions
    // are reprojected iteratively, while native narrow-phase runs once, not 8x.
    public static bool ProjectConstraint(ref Constraint c){
        float depth=c.Depth-Vector3.Dot(c.Normal,c.A.Position-c.B.Position-c.InitialDelta);
        if(depth<=1e-7f)return false;
        double invA=1/c.MassA,invB=1/c.MassB,sum=invA+invB;
        bool moved=false;
        // A wall-blocked projection cannot improve until another contact moves
        // either endpoint. Still solve its velocity constraint below.
        if(!c.HasProjection||!c.LastProjectedA.Equals(c.A.Position)||!c.LastProjectedB.Equals(c.B.Position)){
            long started=System.Diagnostics.Stopwatch.GetTimestamp();var beforeA=c.A.Position;var beforeB=c.B.Position;
            c.A.ApplyContactCorrection(c.Normal*((depth+2e-6f)*(float)(invA/sum)));
            c.B.ApplyContactCorrection(-c.Normal*((depth+2e-6f)*(float)(invB/sum)));
            CorrectionTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
            moved=!beforeA.Equals(c.A.Position)||!beforeB.Equals(c.B.Position);
            c.HasProjection=!moved;c.LastProjectedA=c.A.Position;c.LastProjectedB=c.B.Position;
        }
        float relative=Vector3.Dot(c.A.Velocity-c.B.Velocity,c.Normal);
        if(relative<0){double impulse=-relative/sum;c.A.Velocity+=c.Normal*(float)(impulse*invA);c.B.Velocity-=c.Normal*(float)(impulse*invB);}
        return moved;
    }
    public static long QueryTicks,CorrectionTicks,QueryCalls,Overlaps;
    public static void ResetDiagnostics(){QueryTicks=CorrectionTicks=QueryCalls=Overlaps=0;}
    // Perfectly inelastic normal response: no energy is introduced; tangential
    // velocity is preserved. Angular response is handled separately in future.
    public static bool Resolve(SolidContactShape a,SolidTranslationState stateA,double massA,
                               SolidContactShape b,SolidTranslationState stateB,double massB){
        if(!(massA>0)||!(massB>0)||double.IsInfinity(massA)||double.IsInfinity(massB))throw new ArgumentOutOfRangeException("Solid contact mass");
        long started=System.Diagnostics.Stopwatch.GetTimestamp();QueryCalls++;
        bool overlap=a.Penetration(stateA.Position,b,stateB.Position,out var normal,out float depth);
        QueryTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        if(!overlap)return false;
        Overlaps++;started=System.Diagnostics.Stopwatch.GetTimestamp();
        double invA=1/massA,invB=1/massB,sum=invA+invB;
        stateA.ApplyContactCorrection(normal*((depth+2e-6f)*(float)(invA/sum)));
        stateB.ApplyContactCorrection(-normal*((depth+2e-6f)*(float)(invB/sum)));
        CorrectionTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        float relative=Vector3.Dot(stateA.Velocity-stateB.Velocity,normal);
        if(relative<0){
            double impulse=-relative/sum;
            stateA.Velocity+=normal*(float)(impulse*invA);
            stateB.Velocity-=normal*(float)(impulse*invB);
        }
        return true;
    }
}
}
