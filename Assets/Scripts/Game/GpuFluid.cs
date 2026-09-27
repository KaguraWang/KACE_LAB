using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// GPU-resident solver and 3D density reconstruction. No particle readback or CPU mesh
// reconstruction in the interactive frame loop.
public sealed class GpuFluid:IDisposable {
    ComputeShader cs;ComputeBuffer p,x,v,d,l,boundaryContribution,boundaryPlane,profile,field,cellHead,cellNext,neighborVisits,occupiedBounds;RenderTexture volume,surfaceVolume;
    readonly int[] solve=new int[5];int clear,splat,resolve,clearCells,indexCells,cellCount,n,gx=48,gy,gz=48;
    int viscosity,commitViscosity,viscosityWeights;
    float spacing,h,height,radius;double accumulator,ml=-1,samplingMl=-1;
    VesselKind configuredKind;bool configured;
    Vector3 previousPosition,previousVelocity;bool initialized;
    struct SolidGpuBody {public Vector4 CenterFirst,MinCount,MaxRadius;}
    ComputeBuffer solidBodies,solidTriangles;
    ComputeBuffer solidBodyNodes,solidTriangleNodes,solidTriangleRanges;
    SolidMeshBvh solidBvh;
    Material renderMaterial;
    SolidGpuBody[] solidUpload=Array.Empty<SolidGpuBody>();
    public int SolidObstacleCount=>solidUpload.Length;
    public bool Supported=>SystemInfo.supportsComputeShaders && SystemInfo.supports3DRenderTextures;
    public int Count=>n;
    public int DispatchCount {get;private set;}
    public int FieldBuildCount {get;private set;}
    public double LastStepCpuMilliseconds {get;private set;}
    public double MassVolumeMl=>ml;
    public double PendingSimulationSeconds=>accumulator;
    public FluidVisualSnapshot CaptureVisualSnapshot()=>cs==null||renderMaterial==null?null:
        new FluidVisualSnapshot(renderMaterial,profile,occupiedBounds,solidBodies,solidTriangles,
            solidBodyNodes,solidTriangleNodes,solidTriangleRanges);
    public bool AllPairsReferenceForTest {get;set;}
    public void Configure(VesselKind kind,double amount,MeshFilter mf,Material material){
        if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
        if(configured&&kind==configuredKind&&amount==ml)return;
        // Small inventory changes retain moving particles, but must update their
        // physical volume and reconstruction weight. Compare against the last
        // reseed, not the last call, so repeated small doses eventually resample.
        if(configured&&kind==configuredKind&&cs!=null&&n>0&&amount>0.05&&Math.Abs(amount-samplingMl)<Math.Max(0.05,amount*0.015)){
            ml=amount;SetParticleVolume();Field();return;
        }
        double pending=configured&&kind==configuredKind&&cs!=null&&amount>0.05?accumulator:0;
        Dispose();ml=samplingMl=amount;configuredKind=kind;configured=true;if(amount<=0.05)return;
        if(!Supported){Debug.LogError("3D liquid requires a compute-capable GPU. CPU fallback disabled.");mf.sharedMesh=VesselMesh.BuildLiquid(kind,VesselMesh.FillFractionFor(kind,amount));return;}
        cs=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("Fluid3D"));
        var seed=new Fluid3D();seed.Configure(kind,amount);var points=seed.Positions;n=points.Length;spacing=seed.Spacing;h=spacing*1.8f;height=seed.Height;radius=seed.Radius;
        cs.SetFloat("VesselFloor",seed.Floor);
        gy=Mathf.Clamp(Mathf.CeilToInt(height/(radius*2)*gx),32,128);
        p=new ComputeBuffer(n,12);x=new ComputeBuffer(n,12);v=new ComputeBuffer(n,12);d=new ComputeBuffer(n,12);l=new ComputeBuffer(n,4);profile=new ComputeBuffer(513,4);field=new ComputeBuffer(gx*gy*gz,4);
        boundaryContribution=new ComputeBuffer(n,16);
        boundaryPlane=new ComputeBuffer(n,16);
        occupiedBounds=new ComputeBuffer(6,4);
        int cx=Mathf.CeilToInt(radius*2/h)+1,cy=Mathf.CeilToInt((height+.02f)/h)+1,cz=cx;cellCount=cx*cy*cz;
        cellHead=new ComputeBuffer(cellCount,4);cellNext=new ComputeBuffer(n,4);neighborVisits=new ComputeBuffer(n,4);
        cs.SetInts("CellGrid",cx,cy,cz);cs.SetInt("CellCount",cellCount);cs.SetInt("AuditAllPairs",AllPairsReferenceForTest?1:0);
        p.SetData(points);x.SetData(points);v.SetData(new Vector3[n]);var radii=new float[513];for(int i=0;i<513;i++)radii[i]=seed.InnerRadius(i/512f*height);profile.SetData(radii);
        volume=new RenderTexture(gx,gy,0,RenderTextureFormat.RFloat){dimension=UnityEngine.Rendering.TextureDimension.Tex3D,volumeDepth=gz,enableRandomWrite=true,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};volume.Create();
        surfaceVolume=new RenderTexture(volume.descriptor){filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};surfaceVolume.Create();
        string[] names={"Predict","Density","Correct","ApplyCorrection","Velocity"};for(int i=0;i<5;i++)solve[i]=cs.FindKernel(names[i]);clear=cs.FindKernel("ClearField");splat=cs.FindKernel("Splat");resolve=cs.FindKernel("ResolveField");
        clearCells=cs.FindKernel("ClearCells");indexCells=cs.FindKernel("IndexCells");
        viscosity=cs.FindKernel("Viscosity");commitViscosity=cs.FindKernel("CommitViscosity");viscosityWeights=cs.FindKernel("ViscosityWeights");cs.SetFloat("ViscosityStrength",.03f);
        cs.SetTexture(resolve,"SurfaceVolume",surfaceVolume);
        ConfigureSolids(Array.Empty<SolidBodySeed>());
        foreach(int k in new[]{solve[0],solve[1],solve[2],solve[3],solve[4],viscosity,commitViscosity,viscosityWeights,clear,splat,resolve,clearCells,indexCells}){
            cs.SetBuffer(k,"P",p);cs.SetBuffer(k,"X",x);cs.SetBuffer(k,"V",v);cs.SetBuffer(k,"D",d);cs.SetBuffer(k,"L",l);cs.SetBuffer(k,"Profile",profile);cs.SetBuffer(k,"Field",field);cs.SetTexture(k,"Volume",volume);
            cs.SetBuffer(k,"CellHead",cellHead);cs.SetBuffer(k,"CellNext",cellNext);cs.SetBuffer(k,"NeighborVisits",neighborVisits);
            cs.SetBuffer(k,"BoundaryContribution",boundaryContribution);
            cs.SetBuffer(k,"BoundaryPlane",boundaryPlane);
        }
        var min=new Vector3(-radius,-0.01f,-radius);var size=new Vector3(radius*2,height+0.02f,radius*2);
        cs.SetBuffer(clear,"OccupiedBounds",occupiedBounds);cs.SetBuffer(splat,"OccupiedBounds",occupiedBounds);
        cs.SetInt("Count",n);cs.SetInts("Grid",gx,gy,gz);cs.SetFloat("Spacing",spacing);cs.SetFloat("H",h);cs.SetFloat("H2",h*h);cs.SetFloat("Poly",315f/(64*Mathf.PI*Mathf.Pow(h,9)));cs.SetFloat("Spiky",-45f/(Mathf.PI*Mathf.Pow(h,6)));cs.SetFloat("VesselHeight",height);
        SetParticleVolume();cs.SetVector("GridMin",min);cs.SetVector("GridSize",size);
        material.shader=Shader.Find("ChemLab/FluidVolume");renderMaterial=material;BindSolidRendering();material.SetTexture("_FluidDensity",surfaceVolume);material.SetVector("_FluidMin",min);material.SetVector("_FluidSize",size);material.SetVector("_FluidTexel",new Vector3(1f/gx,1f/gy,1f/gz));
        material.SetBuffer("_FluidOccupiedBounds",occupiedBounds);
        material.SetBuffer("_FluidProfile",profile);material.SetFloat("_FluidHeight",height);
        var old=mf.sharedMesh;mf.sharedMesh=Box(min,size);if(old!=null)UnityEngine.Object.Destroy(old);
        Field();initialized=false;accumulator=pending;
    }
    void SetParticleVolume(){
        float kernel=spacing*2.15f,particleVolume=(float)(ml/1000/n);
        cs.SetFloat("ParticleVolume",particleVolume);cs.SetFloat("Kernel",kernel);
        cs.SetFloat("Weight",315f/(64*Mathf.PI*kernel*kernel*kernel)*particleVolume);
    }
    public void Step(Vector3 position,Quaternion rotation,float dt,bool stir,float bubble){
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentOutOfRangeException(nameof(dt));
        LastStepCpuMilliseconds=0;if(cs==null)return;
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        if(!initialized){previousPosition=position;initialized=true;}
        var vel=(position-previousPosition)/Mathf.Max(dt,0.001f);var accel=Quaternion.Inverse(rotation)*Vector3.ClampMagnitude((vel-previousVelocity)/Mathf.Max(dt,0.001f),18);previousPosition=position;previousVelocity=vel;
        cs.SetVector("Acceleration",accel);cs.SetVector("Gravity",Quaternion.Inverse(rotation)*new Vector3(0,-(float)VesselMesh.GravityWorldUnitsPerSecondSquared,0));cs.SetFloat("Stir",stir?1:0);cs.SetFloat("Bubble",bubble);
        // Cover this frame's complete experiment time at the unchanged 90 Hz
        // solver step. A constant three-tick budget silently capped 60 FPS at 2x.
        // Retain fractional ticks and allow at least three ticks for legacy debt.
        accumulator+=dt;const double step=1.0/90;cs.SetFloat("Dt",(float)step);int count=0;
        double budget=Math.Max(3,Math.Ceiling(dt/step));
        bool advanced=false;
        while(accumulator>=step&&count++<budget){
            accumulator-=step;cs.Dispatch(solve[0],(n+127)/128,1,1);
            for(int i=0;i<4;i++){
                // Corrections can cross cell boundaries: rebuild before EVERY density iteration.
                if(!AllPairsReferenceForTest){cs.Dispatch(clearCells,(cellCount+255)/256,1,1);cs.Dispatch(indexCells,(n+127)/128,1,1);}
                cs.Dispatch(solve[1],(n+127)/128,1,1);cs.Dispatch(solve[2],(n+127)/128,1,1);cs.Dispatch(solve[3],(n+127)/128,1,1);
            }
            // Last correction may cross a hash cell. Viscosity needs the final
            // positions, not the neighbor index from before that correction.
            if(!AllPairsReferenceForTest){cs.Dispatch(clearCells,(cellCount+255)/256,1,1);cs.Dispatch(indexCells,(n+127)/128,1,1);}
            cs.Dispatch(solve[4],(n+127)/128,1,1);
            cs.Dispatch(viscosity,(n+127)/128,1,1);cs.Dispatch(commitViscosity,(n+127)/128,1,1);DispatchCount++;advanced=true;
        }
        // Between fixed solver ticks the particles and density field are unchanged.
        if(advanced)Field();
        LastStepCpuMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
    }
    public void ConfigureSolids(IReadOnlyList<SolidBodySeed> seeds){
        if(cs==null)return;
        solidBodies?.Release();solidTriangles?.Release();
        solidBodyNodes?.Release();solidTriangleNodes?.Release();solidTriangleRanges?.Release();
        solidUpload=new SolidGpuBody[seeds.Count];var triangles=new List<Vector3>();
        for(int i=0;i<seeds.Count;i++){
            var seed=seeds[i];var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);var max=-min;float radius=0;
            foreach(var vertex in seed.LocalVertices){min=Vector3.Min(min,vertex);max=Vector3.Max(max,vertex);radius=Mathf.Max(radius,vertex.magnitude);}
            solidUpload[i]=new SolidGpuBody{CenterFirst=new Vector4(seed.Center.x,seed.Center.y,seed.Center.z,triangles.Count),MinCount=new Vector4(min.x,min.y,min.z,seed.Triangles.Length/3),MaxRadius=new Vector4(max.x,max.y,max.z,radius)};
            foreach(int index in seed.Triangles)triangles.Add(seed.LocalVertices[index]);
        }
        solidBodies=new ComputeBuffer(Math.Max(1,seeds.Count),48);solidTriangles=new ComputeBuffer(Math.Max(1,triangles.Count),12);
        solidBodies.SetData(seeds.Count>0?solidUpload:new SolidGpuBody[1]);solidTriangles.SetData(triangles.Count>0?triangles.ToArray():new Vector3[1]);
        solidBvh=new SolidMeshBvh(seeds);
        solidBodyNodes=new ComputeBuffer(Math.Max(1,solidBvh.Bodies.Length),32);solidTriangleNodes=new ComputeBuffer(Math.Max(1,solidBvh.Triangles.Length),32);solidTriangleRanges=new ComputeBuffer(Math.Max(1,seeds.Count),8);
        solidBodyNodes.SetData(solidBvh.Bodies.Length>0?solidBvh.Bodies:new SolidMeshBvh.Node[1]);solidTriangleNodes.SetData(solidBvh.Triangles.Length>0?solidBvh.Triangles:new SolidMeshBvh.Node[1]);solidTriangleRanges.SetData(seeds.Count>0?solidBvh.TriangleRanges:new Vector2Int[1]);
        cs.SetInt("SolidCount",seeds.Count);
        cs.SetInt("SolidBodyNodeCount",solidBvh.Bodies.Length);
        foreach(int kernel in new[]{solve[0],solve[1],solve[3],resolve}){
            cs.SetBuffer(kernel,"SolidBodies",solidBodies);cs.SetBuffer(kernel,"SolidTriangles",solidTriangles);
            cs.SetBuffer(kernel,"SolidBodyNodes",solidBodyNodes);cs.SetBuffer(kernel,"SolidTriangleNodes",solidTriangleNodes);cs.SetBuffer(kernel,"SolidTriangleRanges",solidTriangleRanges);
        }
        BindSolidRendering();
    }
    void BindSolidRendering(){
        if(renderMaterial==null)return;
        renderMaterial.SetInt("SolidCount",solidUpload.Length);renderMaterial.SetBuffer("SolidBodies",solidBodies);renderMaterial.SetBuffer("SolidTriangles",solidTriangles);
        renderMaterial.SetInt("SolidBodyNodeCount",solidBvh.Bodies.Length);renderMaterial.SetBuffer("SolidBodyNodes",solidBodyNodes);renderMaterial.SetBuffer("SolidTriangleNodes",solidTriangleNodes);renderMaterial.SetBuffer("SolidTriangleRanges",solidTriangleRanges);
    }
    public void UpdateSolidPositions(Vector3[] positions){
        if(positions==null||positions.Length!=solidUpload.Length)throw new ArgumentException("Solid pose count does not match geometry");
        if(cs==null||positions.Length==0)return;
        bool changed=false;
        for(int i=0;i<positions.Length;i++){
            var p=positions[i];var old=solidUpload[i].CenterFirst;
            if(p.x==old.x&&p.y==old.y&&p.z==old.z)continue;
            solidUpload[i].CenterFirst=new Vector4(p.x,p.y,p.z,old.w);changed=true;
        }
        if(changed){solidBodies.SetData(solidUpload);solidBvh.RefitBodies(positions);solidBodyNodes.SetData(solidBvh.Bodies);}
    }
    void Field(){cs.Dispatch(clear,(gx*gy*gz+255)/256,1,1);cs.Dispatch(splat,(n+127)/128,1,1);cs.Dispatch(resolve,(gx+3)/4,(gy+3)/4,(gz+3)/4);FieldBuildCount++;}
    // Explicit audit only; never called by production updates.
    public Vector3[] ReadbackForTest(){var data=new Vector3[n];p?.GetData(data);return data;}
    public Vector4[] BoundaryContributionsForTest(){var data=new Vector4[n];boundaryContribution?.GetData(data);return data;}
    public Vector3[] ViscosityForTest(Vector3[] positions,Vector3[] velocities,float strength,bool allPairs){
        if(cs==null||positions.Length!=n||velocities.Length!=n)throw new ArgumentException("Configured particle arrays required");
        var oldX=new Vector3[n];var oldV=new Vector3[n];var oldD=new Vector3[n];var oldL=new float[n];x.GetData(oldX);v.GetData(oldV);d.GetData(oldD);l.GetData(oldL);
        try{
            x.SetData(positions);v.SetData(velocities);cs.SetFloat("ViscosityStrength",strength);cs.SetInt("AuditAllPairs",allPairs?1:0);
            if(!allPairs){cs.Dispatch(clearCells,(cellCount+255)/256,1,1);cs.Dispatch(indexCells,(n+127)/128,1,1);}
            cs.Dispatch(viscosityWeights,(n+127)/128,1,1);cs.Dispatch(viscosity,(n+127)/128,1,1);cs.Dispatch(commitViscosity,(n+127)/128,1,1);
            var result=new Vector3[n];v.GetData(result);return result;
        }finally{x.SetData(oldX);v.SetData(oldV);d.SetData(oldD);l.SetData(oldL);cs.SetFloat("ViscosityStrength",.03f);cs.SetInt("AuditAllPairs",AllPairsReferenceForTest?1:0);}
    }
    public uint[] ClassifySolidPointsForTest(Vector3[] points){
        if(cs==null||points==null)throw new ArgumentException("Configured GPU and points required");
        if(points.Length==0)return Array.Empty<uint>();
        var audit=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("SolidBvhAudit"));
        var input=new ComputeBuffer(points.Length,12);var output=new ComputeBuffer(points.Length,4);
        try{
            int kernel=audit.FindKernel("Classify");input.SetData(points);
            audit.SetInt("SolidCount",solidUpload.Length);audit.SetInt("SolidBodyNodeCount",solidBvh.Bodies.Length);audit.SetInt("AuditPointCount",points.Length);
            audit.SetBuffer(kernel,"SolidBodies",solidBodies);audit.SetBuffer(kernel,"SolidTriangles",solidTriangles);audit.SetBuffer(kernel,"SolidBodyNodes",solidBodyNodes);audit.SetBuffer(kernel,"SolidTriangleNodes",solidTriangleNodes);audit.SetBuffer(kernel,"SolidTriangleRanges",solidTriangleRanges);
            audit.SetBuffer(kernel,"AuditPoints",input);audit.SetBuffer(kernel,"AuditInside",output);audit.Dispatch(kernel,(points.Length+63)/64,1,1);
            var result=new uint[points.Length];output.GetData(result);return result;
        }finally{input.Release();output.Release();UnityEngine.Object.Destroy(audit);}
    }
    public Vector3[] ProjectSolidPointsForTest(Vector3[] points,float margin,out Vector3[] reference){
        if(cs==null||points==null||points.Length==0)throw new ArgumentException("Configured GPU and nonempty points required");
        var audit=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("SolidBvhAudit"));
        var input=new ComputeBuffer(points.Length,12);var output=new ComputeBuffer(points.Length,12);var linear=new ComputeBuffer(points.Length,12);
        try{
            int kernel=audit.FindKernel("ProjectAudit");input.SetData(points);
            audit.SetInt("SolidCount",solidUpload.Length);audit.SetInt("SolidBodyNodeCount",solidBvh.Bodies.Length);audit.SetInt("AuditPointCount",points.Length);audit.SetFloat("AuditMargin",margin);
            audit.SetBuffer(kernel,"SolidBodies",solidBodies);audit.SetBuffer(kernel,"SolidTriangles",solidTriangles);audit.SetBuffer(kernel,"SolidBodyNodes",solidBodyNodes);audit.SetBuffer(kernel,"SolidTriangleNodes",solidTriangleNodes);audit.SetBuffer(kernel,"SolidTriangleRanges",solidTriangleRanges);
            audit.SetBuffer(kernel,"AuditPoints",input);audit.SetBuffer(kernel,"AuditProjected",output);audit.SetBuffer(kernel,"AuditReference",linear);audit.Dispatch(kernel,(points.Length+63)/64,1,1);
            var result=new Vector3[points.Length];reference=new Vector3[points.Length];output.GetData(result);linear.GetData(reference);return result;
        }finally{input.Release();output.Release();linear.Release();UnityEngine.Object.Destroy(audit);}
    }
    public Vector4[] BoundarySamplesForTest(Vector3[] points,float support,out Vector4[] nearest){
        var audit=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("SolidBvhAudit"));
        var input=new ComputeBuffer(points.Length,12);var output=new ComputeBuffer(points.Length,16);var distance=new ComputeBuffer(points.Length,16);
        try{
            int kernel=audit.FindKernel("BoundaryAudit");input.SetData(points);
            audit.SetInt("SolidCount",solidUpload.Length);audit.SetInt("SolidBodyNodeCount",solidBvh.Bodies.Length);audit.SetInt("AuditPointCount",points.Length);audit.SetFloat("AuditMargin",support);
            audit.SetBuffer(kernel,"SolidBodies",solidBodies);audit.SetBuffer(kernel,"SolidTriangles",solidTriangles);audit.SetBuffer(kernel,"SolidBodyNodes",solidBodyNodes);audit.SetBuffer(kernel,"SolidTriangleNodes",solidTriangleNodes);audit.SetBuffer(kernel,"SolidTriangleRanges",solidTriangleRanges);
            audit.SetBuffer(kernel,"AuditPoints",input);audit.SetBuffer(kernel,"AuditBoundary",output);audit.SetBuffer(kernel,"AuditNearest",distance);audit.Dispatch(kernel,(points.Length+63)/64,1,1);
            var result=new Vector4[points.Length];nearest=new Vector4[points.Length];output.GetData(result);distance.GetData(nearest);return result;
        }finally{input.Release();output.Release();distance.Release();UnityEngine.Object.Destroy(audit);}
    }
    // Audit the real Predict kernel using uniforms set by Step, without density
    // constraints or wall contacts. Restore all particle buffers afterwards.
    public void ReadPredictionForTest(Vector3 initialPosition,float dt,out Vector3 predicted,out Vector3 velocity){
        if(cs==null||n==0||!(dt>0)||float.IsInfinity(dt))throw new ArgumentException("Configured GPU fluid and positive finite audit step required");
        var oldP=new Vector3[n];var oldX=new Vector3[n];var oldV=new Vector3[n];p.GetData(oldP);x.GetData(oldX);v.GetData(oldV);
        try{
            var points=new Vector3[n];for(int i=0;i<n;i++)points[i]=initialPosition;
            p.SetData(points);v.SetData(new Vector3[n]);cs.SetFloat("Dt",dt);cs.Dispatch(solve[0],(n+127)/128,1,1);
            x.GetData(points);predicted=points[0];v.GetData(points);velocity=points[0];
        }finally{p.SetData(oldP);x.SetData(oldX);v.SetData(oldV);cs.SetFloat("Dt",1f/90);}
    }
    public float[] ReadDensityForTest(out Vector3Int grid,out Vector3 min,out Vector3 size,bool interpolationField=false){
        grid=new Vector3Int(gx,gy,gz);min=new Vector3(-radius,-.01f,-radius);size=new Vector3(radius*2,height+.02f,radius*2);
        var request=UnityEngine.Rendering.AsyncGPUReadback.Request(interpolationField?surfaceVolume:volume,0);request.WaitForCompletion();
        if(request.hasError)throw new InvalidOperationException("GPU density audit readback failed");
        // Texture readback may expose each depth slice as a separate layer.
        // Copy all reported layers rather than assuming GetData(0) is the volume.
        int elementsPerLayer=request.layerDataSize/sizeof(float);
        var data=new float[elementsPerLayer*request.layerCount];
        for(int layer=0;layer<request.layerCount;layer++){
            var source=request.GetData<float>(layer);
            for(int i=0;i<source.Length;i++)data[layer*elementsPerLayer+i]=source[i];
        }
        if(data.Length!=gx*gy*gz)throw new InvalidOperationException($"GPU density dimensions differ: {data.Length} vs {gx*gy*gz}, layers={request.layerCount}");
        return data;
    }
    public ulong NeighborVisitsForTest(){var data=new uint[n];neighborVisits.GetData(data);ulong total=0;foreach(uint value in data)total+=value;return total;}
    public void Dispose(){configured=false;n=0;initialized=false;accumulator=0;if(renderMaterial!=null)renderMaterial.SetInt("SolidCount",0);renderMaterial=null;foreach(var b in new[]{p,x,v,d,l,boundaryContribution,boundaryPlane,profile,field,cellHead,cellNext,neighborVisits,occupiedBounds,solidBodies,solidTriangles,solidBodyNodes,solidTriangleNodes,solidTriangleRanges})b?.Release();p=x=v=d=l=boundaryContribution=boundaryPlane=profile=field=cellHead=cellNext=neighborVisits=occupiedBounds=solidBodies=solidTriangles=solidBodyNodes=solidTriangleNodes=solidTriangleRanges=null;solidBvh=null;solidUpload=Array.Empty<SolidGpuBody>();if(volume!=null){volume.Release();UnityEngine.Object.Destroy(volume);}if(surfaceVolume!=null){surfaceVolume.Release();UnityEngine.Object.Destroy(surfaceVolume);}if(cs!=null)UnityEngine.Object.Destroy(cs);cs=null;volume=surfaceVolume=null;}
    static Mesh Box(Vector3 min,Vector3 size){
        var p=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(1,1,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(1,1,1),new Vector3(0,1,1)};
        for(int i=0;i<8;i++)p[i]=min+Vector3.Scale(p[i],size);
        var mesh=new Mesh{name="GPU fluid raymarch bounds"};mesh.vertices=p;mesh.triangles=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4};mesh.RecalculateBounds();return mesh;
    }
}
}
