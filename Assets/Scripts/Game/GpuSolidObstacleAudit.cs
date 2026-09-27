using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
public static class GpuSolidObstacleAudit {
    sealed class CpuSolid {
        readonly SolidBodySeed seed;readonly Bounds bounds;
        public CpuSolid(SolidBodySeed seed){this.seed=seed;var box=new Bounds(seed.LocalVertices[0],Vector3.zero);foreach(var p in seed.LocalVertices)box.Encapsulate(p);bounds=box;}
        // Independent CPU solid-angle winding test, not GPU ray parity copied
        // back into C#. Full meshes retain their original irregular surface.
        public bool Inside(Vector3 point,Vector3 center){
            var p=point-center;if(!bounds.Contains(p))return false;double winding=0;
            for(int t=0;t<seed.Triangles.Length;t+=3){
                var a=seed.LocalVertices[seed.Triangles[t]]-p;var b=seed.LocalVertices[seed.Triangles[t+1]]-p;var c=seed.LocalVertices[seed.Triangles[t+2]]-p;
                double la=a.magnitude,lb=b.magnitude,lc=c.magnitude;
                double numerator=(double)a.x*((double)b.y*c.z-(double)b.z*c.y)+(double)a.y*((double)b.z*c.x-(double)b.x*c.z)+(double)a.z*((double)b.x*c.y-(double)b.y*c.x);
                double denominator=la*lb*lc+Vector3.Dot(a,b)*lc+Vector3.Dot(b,c)*la+Vector3.Dot(c,a)*lb;
                winding+=2*Math.Atan2(numerator,denominator);
            }
            return Math.Abs(winding)>6;
        }
    }
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        var go=new GameObject("GPU solid audit");var filter=go.AddComponent<MeshFilter>();var material=new Material(Shader.Find("ChemLab/FluidVolume"));
        var solver=new GpuFluid();
        try{
            foreach(double rate in new[]{.5,1d,2d,3d}){
                solver.Dispose();solver.Configure(VesselKind.Tube,1,filter,material);
                double supplied=0;int start=solver.DispatchCount;double maxDebt=0;
                for(int frame=0;frame<120;frame++){
                    float dt=(float)(rate/60);supplied+=dt;
                    solver.Step(Vector3.zero,Quaternion.identity,dt,false,0);
                    maxDebt=Math.Max(maxDebt,solver.PendingSimulationSeconds);
                }
                double consumed=(solver.DispatchCount-start)/90d;
                Check("rate "+rate+" actual GPU ticks consume supplied time",Math.Abs(consumed+solver.PendingSimulationSeconds-supplied)<1e-6);
                Check("rate "+rate+" never accumulates more than one fixed tick",maxDebt<1d/90+1e-8);
                lines.Add("METRIC rate="+rate+" supplied="+supplied+" simulated="+consumed+" maxDebt="+maxDebt);
                int paused=solver.DispatchCount;
                solver.Step(Vector3.zero,Quaternion.identity,0,false,0);
                Check("rate "+rate+" pause cannot advance pending whole ticks",solver.DispatchCount==paused);
                solver.Step(Vector3.zero,Quaternion.identity,1f/60,false,0);
                Check("rate "+rate+" returns to normal without backlog",solver.PendingSimulationSeconds<1d/90+1e-8);
            }
            solver.Dispose();
            solver.Configure(VesselKind.Beaker,100,filter,material);
            var viscosityPositions=solver.ReadbackForTest();var viscosityVelocities=new Vector3[solver.Count];
            var viscosityRandom=new System.Random(819);
            for(int pattern=0;pattern<3;pattern++){
                var points=(Vector3[])viscosityPositions.Clone();
                for(int i=0;i<points.Length;i++){
                    if(pattern==2)points[i]=new Vector3(0,.1f,0);
                    viscosityVelocities[i]=pattern==0?new Vector3(.7f,-.4f,.2f):new Vector3((float)viscosityRandom.NextDouble()*2-1,(float)viscosityRandom.NextDouble()*2-1,(float)viscosityRandom.NextDouble()*2-1);
                }
                foreach(float strength in new[]{0f,.03f,1f}){
                    var actual=solver.ViscosityForTest(points,viscosityVelocities,strength,false);
                    var referenceViscosity=solver.ViscosityForTest(points,viscosityVelocities,strength,true);
                    double beforeEnergy=0,afterEnergy=0,bx=0,by=0,bz=0,ax=0,ay=0,az=0;float maximumDifference=0,maximumChange=0;
                    for(int i=0;i<actual.Length;i++){
                        var a=actual[i];var b=viscosityVelocities[i];beforeEnergy+=b.sqrMagnitude;afterEnergy+=a.sqrMagnitude;
                        bx+=b.x;by+=b.y;bz+=b.z;ax+=a.x;ay+=a.y;az+=a.z;
                        maximumDifference=Mathf.Max(maximumDifference,Vector3.Distance(a,referenceViscosity[i]));maximumChange=Mathf.Max(maximumChange,Vector3.Distance(a,b));
                    }
                    double momentumError=Math.Sqrt((ax-bx)*(ax-bx)+(ay-by)*(ay-by)+(az-bz)*(az-bz));
                    Check($"GPU viscosity hash equals all-pairs pattern={pattern} strength={strength}",maximumDifference<2e-5f);
                    Check($"GPU viscosity preserves momentum and dissipates energy pattern={pattern} strength={strength}",momentumError/actual.Length<1e-6&&afterEnergy<=beforeEnergy+1e-4);
                    if(pattern==0||strength==0)Check($"GPU viscosity preserves bulk motion or disabled state pattern={pattern} strength={strength}",maximumChange<1e-6f);
                    else Check($"GPU viscosity actually damps relative motion pattern={pattern} strength={strength}",afterEnergy<beforeEnergy-.001);
                    lines.Add($"METRIC viscosity pattern={pattern} strength={strength} maxReferenceDifference={maximumDifference:G7} meanMomentumError={momentumError/actual.Length:G7} energyBefore={beforeEnergy:G8} energyAfter={afterEnergy:G8}");
                }
            }
            int originalCount=solver.Count,originalFieldBuilds=solver.FieldBuildCount;var originalMesh=filter.sharedMesh;
            var initialDensity=solver.ReadDensityForTest(out _,out _,out _);double densityBefore=0;foreach(float value in initialDensity)densityBefore+=value;
            solver.Configure(VesselKind.Beaker,100.5,filter,material);
            var addedDensity=solver.ReadDensityForTest(out _,out _,out _);double densityAfter=0;foreach(float value in addedDensity)densityAfter+=value;
            Check("sub-threshold addition updates exact GPU inventory without rebuilding particle buffers",solver.MassVolumeMl==100.5&&solver.Count==originalCount&&filter.sharedMesh==originalMesh&&solver.FieldBuildCount==originalFieldBuilds+1);
            Check("sub-threshold addition updates actual GPU density weight",densityBefore>0&&Math.Abs(densityAfter/densityBefore-1.005)<.0005);
            solver.Configure(VesselKind.Beaker,100.25,filter,material);
            Check("sub-threshold withdrawal updates exact GPU inventory",solver.MassVolumeMl==100.25&&solver.Count==originalCount&&filter.sharedMesh==originalMesh);
            solver.Configure(VesselKind.Beaker,101,filter,material);solver.Configure(VesselKind.Beaker,102,filter,material);
            Check("cumulative small additions eventually rebuild sampling",solver.MassVolumeMl==102&&filter.sharedMesh!=originalMesh);
            solver.Configure(VesselKind.Beaker,100,filter,material);
            int elapsedStart=solver.DispatchCount;bool completeFrame=false;
            for(int frame=0;frame<3;frame++){
                int before=solver.DispatchCount;solver.Step(Vector3.zero,Quaternion.identity,frame==0?.1f:0,false,0);
                if(frame==0)completeFrame=solver.DispatchCount-before==9;
            }
            Check("slow frame retains all 100ms for nine fixed GPU ticks",solver.DispatchCount-elapsedStart==9);
            Check("slow frame finishes all nine fixed ticks in its own update",completeFrame);
            int resampleStart=solver.DispatchCount;
            solver.Step(Vector3.zero,Quaternion.identity,.005f,false,0);double debt=solver.PendingSimulationSeconds;
            solver.Configure(VesselKind.Beaker,102,filter,material);
            Check("inventory resampling preserves fractional simulation time",debt>.004&&debt<1d/90&&solver.PendingSimulationSeconds==debt);
            solver.Step(Vector3.zero,Quaternion.identity,.095f,false,0);
            Check("resampled solver consumes the full combined 100ms",solver.DispatchCount-resampleStart==9&&solver.PendingSimulationSeconds<1e-7);
            bool invalidRejected=true;
            foreach(float invalid in new[]{-.1f,float.NaN,float.PositiveInfinity}){
                int before=solver.DispatchCount;double pending=solver.PendingSimulationSeconds;
                try{solver.Step(Vector3.zero,Quaternion.identity,invalid,false,0);invalidRejected=false;}
                catch(ArgumentOutOfRangeException){invalidRejected&=solver.DispatchCount==before&&solver.PendingSimulationSeconds==pending;}
            }
            Check("invalid elapsed time leaves GPU simulation unchanged",invalidRejected);
            solver.Configure(VesselKind.Beaker,102,filter,material);solver.Configure(VesselKind.Beaker,100,filter,material);
            foreach(var rotation in new[]{Quaternion.identity,Quaternion.Euler(0,0,90)}){
                solver.Step(Vector3.zero,rotation,0,false,0);
                var origin=new Vector3(0,.5f,0);const float dt=.001f;
                solver.ReadPredictionForTest(origin,dt,out var predicted,out var velocity);
                var expected=Quaternion.Inverse(rotation)*new Vector3(0,-98.1f,0);
                Check("GPU gravity uses 100mm world units at rotation "+rotation.eulerAngles,Vector3.Distance(velocity,expected*dt)<2e-7f&&Vector3.Distance(predicted,origin+expected*(dt*dt))<2e-7f);
            }
            // Independent analytic box distances and numerical integration of
            // the normalized poly6 half-space, not the shader's polynomial.
            var boxVertices=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            for(int i=0;i<boxVertices.Length;i++)boxVertices[i]*=.2f;
            var boxFaces=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2,0,1,5,0,5,4};
            var boundaryBox=new SolidBodySeed{LocalVertices=boxVertices,Triangles=boxFaces,Center=new Vector3(.05f,.25f,0)};
            solver.ConfigureSolids(new[]{boundaryBox});
            var directions=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            var distances=new[]{.001f,.02f,.05f,.1f,.2f};var boundaryPoints=new List<Vector3>();
            foreach(var direction in directions)foreach(float distance in distances)boundaryPoints.Add(boundaryBox.Center+direction*(.2f+distance));
            for(int moved=0;moved<2;moved++){
                var translation=moved==0?Vector3.zero:new Vector3(.12f,.07f,-.04f);
                solver.UpdateSolidPositions(new[]{boundaryBox.Center+translation});
                var queries=boundaryPoints.ToArray();for(int i=0;i<queries.Length;i++)queries[i]+=translation;
                var contributions=solver.BoundarySamplesForTest(queries,.1f,out var nearest);
                bool geometry=true,kernel=true,gradient=true;float maxNormalError=0,maxGradientError=0,maxDistanceError=0;
                for(int i=0;i<queries.Length;i++){
                    double start=Math.Min(1,distances[i%distances.Length]/.1),sum=0,step=(1-start)/1000;
                    for(int k=0;k<=1000;k++){double u=start+k*step;sum+=(k==0||k==1000?1:k%2==0?2:4)*Math.Pow(1-u*u,4);}
                    double fraction=315.0/256*sum*step/3;
                    var normal=directions[i/distances.Length];float expectedDistance=Mathf.Min(.1f,distances[i%distances.Length]);
                    geometry&=Mathf.Abs(nearest[i].w-expectedDistance)<1e-6f;
                    maxDistanceError=Mathf.Max(maxDistanceError,Mathf.Abs(nearest[i].w-expectedDistance));
                    if(expectedDistance<.0999f)maxNormalError=Mathf.Max(maxNormalError,Vector3.Distance((Vector3)nearest[i],normal));
                    if(expectedDistance<.0999f)geometry&=Vector3.Distance((Vector3)nearest[i],normal)<1e-5f;
                    kernel&=Math.Abs(contributions[i].w-fraction)<2e-6;
                    double derivative=-315.0/256/.1*Math.Pow(1-start*start,4);
                    gradient&=Vector3.Distance((Vector3)contributions[i],normal*(float)derivative)<.0001f;
                    maxGradientError=Mathf.Max(maxGradientError,Vector3.Distance((Vector3)contributions[i],normal*(float)derivative));
                }
                Check("GPU nearest original box surface matches analytic distances before/after refit "+moved,geometry);
                Check("GPU boundary kernel matches independent numerical half-space integral "+moved,kernel);
                Check("GPU boundary gradient points away from solid pressure "+moved,gradient);
                lines.Add($"METRIC boundary moved={moved} maxDistanceError={maxDistanceError:G9} maxNormalError={maxNormalError:G9} maxGradientError={maxGradientError:G9}");
            }
            var seeds=new List<SolidBodySeed>();
            foreach(bool ice in new[]{true,false}){
                var source=ice?IceChunkShape.Vertices:GrainShape.Vertices;var faces=ice?IceChunkShape.Faces:GrainShape.Faces;
                var vertices=new Vector3[source.Length];for(int i=0;i<source.Length;i++)vertices[i]=Quaternion.Euler(7,23,12)*source[i]*(ice?.16f:.1f);
                seeds.Add(new SolidBodySeed{LocalVertices=vertices,Triangles=faces,Center=new Vector3(ice?-.12f:.12f,.15f,0)});
            }
            var many=new List<SolidBodySeed>();var manyCpu=new List<CpuSolid>();var manyPoses=new Vector3[33];
            for(int body=0;body<33;body++){
                var original=seeds[body%2];var copy=new SolidBodySeed{LocalVertices=original.LocalVertices,Triangles=original.Triangles,Center=new Vector3((body%5-2)*.3f,.2f+(body/25)*.3f,((body/5)%5-2)*.3f)};
                many.Add(copy);manyCpu.Add(new CpuSolid(copy));manyPoses[body]=copy.Center;
            }
            solver.ConfigureSolids(many);
            for(int moved=0;moved<2;moved++){
                if(moved>0){for(int body=0;body<manyPoses.Length;body++)manyPoses[body]+=new Vector3(Mathf.Sin(body)*.11f,.07f,Mathf.Cos(body)*.08f);solver.UpdateSolidPositions(manyPoses);}
                var random=new System.Random(407);var points=new Vector3[33*24];
                for(int sample=0;sample<points.Length;sample++)points[sample]=manyPoses[sample/24]+new Vector3((float)random.NextDouble()*.26f-.13f,(float)random.NextDouble()*.26f-.13f,(float)random.NextDouble()*.26f-.13f);
                var classified=solver.ClassifySolidPointsForTest(points);bool same=true;int inside=0;
                for(int sample=0;sample<points.Length;sample++){bool expected=false;for(int body=0;body<manyCpu.Count;body++)if(manyCpu[body].Inside(points[sample],manyPoses[body])){expected=true;break;}same&=(classified[sample]!=0)==expected;if(expected)inside++;}
                Check("GPU 33-body BVH matches independent winding before/after refit "+moved,same&&inside>20&&inside<points.Length-20);
                lines.Add($"METRIC BVH moved={moved} points={points.Length} interior={inside}");
                foreach(float margin in new[]{0f,.005f,.02f}){
                    var projected=solver.ProjectSolidPointsForTest(points,margin,out var linear);float maximumDifference=0;
                    for(int sample=0;sample<points.Length;sample++)maximumDifference=Mathf.Max(maximumDifference,Vector3.Distance(projected[sample],linear[sample]));
                    Check($"GPU BVH projection matches original triangle scan moved={moved} margin={margin}",maximumDifference<2e-6f);
                    lines.Add($"METRIC projection moved={moved} margin={margin} maximumDifference={maximumDifference:G9}");
                }
            }
            var cpu=new[]{new CpuSolid(seeds[0]),new CpuSolid(seeds[1])};var poses=new[]{seeds[0].Center,seeds[1].Center};
            int initialInside=0;foreach(var point in solver.ReadbackForTest())for(int i=0;i<cpu.Length;i++)if(cpu[i].Inside(point,poses[i]))initialInside++;
            Check("GPU obstacle fixture initially intersects seeded liquid",initialInside>0);
            int count=solver.Count;solver.ConfigureSolids(seeds);
            Check("GPU receives both actual solid meshes",solver.SolidObstacleCount==2);
            var reference=new Fluid3D();reference.Configure(VesselKind.Beaker,100);
            int maxInside=0;bool bounded=true,finite=true;
            for(int frame=0;frame<60;frame++){
                poses[0]=seeds[0].Center+new Vector3(Mathf.Sin(frame*.11f)*.018f,Mathf.Sin(frame*.07f)*.02f,0);
                poses[1]=seeds[1].Center+new Vector3(0,0,Mathf.Sin(frame*.13f)*.02f);
                solver.UpdateSolidPositions(poses);solver.Step(Vector3.zero,Quaternion.identity,1f/60,frame%2==0,.2f);
                if(frame%5!=0&&frame!=59)continue;
                int inside=0;foreach(var point in solver.ReadbackForTest()){
                    finite&=!float.IsNaN(point.sqrMagnitude)&&!float.IsInfinity(point.sqrMagnitude);
                    bounded&=reference.BoundaryDistance(point)>=-1e-5f;
                    for(int i=0;i<cpu.Length;i++)if(cpu[i].Inside(point,poses[i]))inside++;
                }
                maxInside=Math.Max(maxInside,inside);
            }
            Check("GPU particles stay outside moving actual mesh interiors",maxInside==0);
            Check("GPU solid collisions retain vessel boundaries and finite coordinates",bounded&&finite);
            Check("GPU solid collisions preserve liquid inventory and particle count",solver.Count==count&&Math.Abs(solver.MassVolumeMl-100)<1e-9);
            int boundaryAffected=0;bool finiteBoundary=true;
            foreach(var contribution in solver.BoundaryContributionsForTest()){
                finiteBoundary&=!float.IsNaN(contribution.sqrMagnitude)&&!float.IsInfinity(contribution.sqrMagnitude)&&contribution.w>=0&&contribution.w<=.500001f;
                if(contribution.w>1e-5f&&((Vector3)contribution).sqrMagnitude>1e-8f)boundaryAffected++;
            }
            Check("actual GPU density iterations use finite solid boundary gradients",finiteBoundary&&boundaryAffected>0);
            lines.Add($"METRIC actualBoundaryAffectedParticles={boundaryAffected}");
            var density=solver.ReadDensityForTest(out var grid,out var min,out var size);int masked=0;bool empty=true;
            for(int z=0;z<grid.z;z++)for(int y=0;y<grid.y;y++)for(int x=0;x<grid.x;x++){
                var p=min+Vector3.Scale(new Vector3((float)x/(grid.x-1),(float)y/(grid.y-1),(float)z/(grid.z-1)),size);
                for(int i=0;i<cpu.Length;i++)if(cpu[i].Inside(p,poses[i])){masked++;empty&=density[(z*grid.y+y)*grid.x+x]==0;break;}
            }
            Check("GPU reconstructed density is zero inside solid mesh sample sites",masked>0&&empty);
            var interpolation=solver.ReadDensityForTest(out _,out _,out _,true);
            bool preservesLiquid=true;int continuedKernel=0;
            for(int i=0;i<density.Length;i++){
                if(density[i]>0)preservesLiquid&=interpolation[i]==density[i];
                if(density[i]==0&&interpolation[i]>0)continuedKernel++;
            }
            Check("interpolation field preserves all nonzero physical density samples",preservesLiquid);
            Check("interpolation retains kernels across solid voxel boundaries before exact clipping",continuedKernel>0);
            lines.Add($"METRIC continuedKernelSamples={continuedKernel}");
            float Interpolate(float[] values,Vector3 point){
                var q=Vector3.Scale(point-min,new Vector3((grid.x-1)/size.x,(grid.y-1)/size.y,(grid.z-1)/size.z));
                int x=Mathf.Clamp(Mathf.FloorToInt(q.x),0,grid.x-2),y=Mathf.Clamp(Mathf.FloorToInt(q.y),0,grid.y-2),z=Mathf.Clamp(Mathf.FloorToInt(q.z),0,grid.z-2);
                float u=Mathf.Clamp01(q.x-x),v=Mathf.Clamp01(q.y-y),w=Mathf.Clamp01(q.z-z);
                float At(int dx,int dy,int dz)=>values[((z+dz)*grid.y+y+dy)*grid.x+x+dx];
                return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(At(0,0,0),At(1,0,0),u),Mathf.Lerp(At(0,1,0),At(1,1,0),u),v),Mathf.Lerp(Mathf.Lerp(At(0,0,1),At(1,0,1),u),Mathf.Lerp(At(0,1,1),At(1,1,1),u),v),w);
            }
            var interfaceRandom=new System.Random(3817);int restoredSamples=0,externalSamples=0;bool monotonic=true;
            for(int sample=0;sample<4096;sample++){
                float Next()=>(float)interfaceRandom.NextDouble()*.3f-.15f;
                var point=poses[sample%poses.Length]+new Vector3(Next(),Next(),Next());bool inside=false;
                for(int body=0;body<cpu.Length;body++)inside|=cpu[body].Inside(point,poses[body]);
                if(inside||reference.BoundaryDistance(point)<0)continue;
                externalSamples++;float oldValue=Interpolate(density,point),newValue=Interpolate(interpolation,point);
                monotonic&=newValue+1e-6f>=oldValue;
                if(oldValue<=.42f&&newValue>.42f)restoredSamples++;
            }
            Check("continuous interpolation removes artificial exterior holes without eroding other liquid",externalSamples>100&&restoredSamples>0&&monotonic);
            lines.Add($"METRIC exteriorSamples={externalSamples} restoredThresholdSamples={restoredSamples}");
            var cameraObject=new GameObject("Solid interface audit camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var target=new RenderTexture(48,48,24,RenderTextureFormat.ARGBFloat);target.Create();camera.targetTexture=target;
            var pixels=new Texture2D(48,48,TextureFormat.RGBAFloat,false,true);var previousTarget=RenderTexture.active;
            int surfaceHits=0,insideSurface=0;float largestExitCorrection=0,surfaceDifference=0,colorDifference=0;
            Color[] Capture(){camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,48,48),0,0);pixels.Apply();return pixels.GetPixels();}
            float Difference(Color[] a,Color[] b){float maximum=0;for(int i=0;i<a.Length;i++)maximum=Mathf.Max(maximum,Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b),Mathf.Abs(a[i].a-b[i].a));return maximum;}
            try{
                go.layer=29;go.transform.position=new Vector3(10000,0,10000);go.AddComponent<MeshRenderer>().sharedMaterial=material;
                material.SetFloat("_AuditSurface",1);camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                camera.nearClipPlane=.0001f;camera.farClipPlane=3;camera.fieldOfView=55;camera.aspect=1;
                // Look outward from inside each solid: the first liquid surface
                // must be outside its actual mesh, including interpolated cells.
                for(int solid=0;solid<poses.Length;solid++)foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.up}){
                    camera.transform.position=go.transform.position+poses[solid];camera.transform.rotation=Quaternion.LookRotation(direction,Mathf.Abs(direction.y)>.9f?Vector3.forward:Vector3.up);
                    material.SetFloat("_AuditDisableOccupiedBounds",0);var optimized=Capture();
                    material.SetFloat("_AuditDisableOccupiedBounds",1);surfaceDifference=Mathf.Max(surfaceDifference,Difference(optimized,Capture()));
                    material.SetFloat("_AuditSurface",0);var originalColor=Capture();
                    material.SetFloat("_AuditDisableOccupiedBounds",0);colorDifference=Mathf.Max(colorDifference,Difference(originalColor,Capture()));material.SetFloat("_AuditSurface",1);
                    foreach(var pixel in optimized)if(pixel.a>.99f){
                        surfaceHits++;var point=new Vector3(pixel.r,pixel.g,pixel.b);
                        for(int body=0;body<cpu.Length;body++)if(cpu[body].Inside(point,poses[body])){
                            insideSurface++;var ray=(point-poses[solid]).normalized;float lo=0,hi=.1f;
                            if(!cpu[body].Inside(point+ray*hi,poses[body])){for(int iteration=0;iteration<24;iteration++){float mid=(lo+hi)*.5f;if(cpu[body].Inside(point+ray*mid,poses[body]))lo=mid;else hi=mid;}largestExitCorrection=Mathf.Max(largestExitCorrection,hi);}else largestExitCorrection=Mathf.Max(largestExitCorrection,.1f);
                            break;
                        }
                    }
                }
            }finally{material.SetFloat("_AuditSurface",0);material.SetFloat("_AuditDisableOccupiedBounds",0);RenderTexture.active=previousTarget;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(cameraObject);}
            Check("occupied bounds preserve original raymarch hit image",surfaceDifference<1e-6f);
            Check("occupied bounds preserve original liquid color and optical depth image",colorDifference<1e-6f);
            lines.Add($"METRIC occupied bounds surfaceDifference={surfaceDifference:G9} colorDifference={colorDifference:G9}");
            lines.Add($"METRIC renderedInterfaceHits={surfaceHits} renderedHitsInsideSolid={insideSurface} largestRayExitCorrectionWorld={largestExitCorrection:G9}");
            Check("actual raymarched liquid interface stays outside original solid interiors",surfaceHits>100&&insideSurface==0);
            solver.ConfigureSolids(Array.Empty<SolidBodySeed>());solver.Step(Vector3.zero,Quaternion.identity,1f/60,false,0);
            Check("removing solids clears GPU obstacle state without losing liquid",solver.SolidObstacleCount==0&&solver.Count==count&&Math.Abs(solver.MassVolumeMl-100)<1e-9);
            bool clearedBoundary=true;foreach(var contribution in solver.BoundaryContributionsForTest())clearedBoundary&=contribution==Vector4.zero;
            Check("removing solids clears actual GPU boundary contributions",clearedBoundary);
            solver.Dispose();Check("disposed GPU solver reports no live particles",solver.Count==0);
            int disposedBuilds=solver.FieldBuildCount;solver.Configure(VesselKind.Beaker,100,filter,material);
            Check("disposed GPU solver can reconfigure the same liquid amount",solver.Count>0&&solver.FieldBuildCount>disposedBuilds);
            lines.Add($"METRIC initialInteriorParticles={initialInside} maximumRemainingInterior={maxInside} maskedVoxels={masked} particles={count} GPU={SystemInfo.graphicsDeviceName}");
        }finally{solver.Dispose();if(filter.sharedMesh!=null)UnityEngine.Object.Destroy(filter.sharedMesh);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(go);}
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/gpu-solid-obstacle-audit.txt",lines);
        if(fail>0)throw new Exception("GPU solid obstacle audit failed: "+fail);
        Debug.Log("GPU solid obstacle audit passed: "+pass);
    }
}
}
