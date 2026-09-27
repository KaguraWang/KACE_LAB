using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Collections;
namespace ChemLab.Game {
public sealed class SolidPairContactAuditRunner:MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidPackingTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-solidContactTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-solidGpuTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-solidViewTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-solidViewFrames")>=0){Application.runInBackground=true;new GameObject("Solid contact audit").AddComponent<SolidPairContactAuditRunner>();}}
    IEnumerator Start(){
        yield return new WaitForSeconds(2);
        int exitCode=0;
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidViewFrames")>=0){
            var frames=SolidViewAudit.RunFrames();
            while(true){bool next=false;try{next=frames.MoveNext();}catch(Exception e){Debug.LogException(e);exitCode=1;}
                if(!next)break;yield return frames.Current;}
            Application.Quit(exitCode);yield break;
        }
        try{if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidPackingTest")>=0)SolidPackingAudit.Run();else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidViewTest")>=0)SolidViewAudit.Run();else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidGpuTest")>=0)GpuSolidObstacleAudit.Run();else SolidPairContactAudit.Run();}catch(Exception e){Debug.LogException(e);exitCode=1;}
        yield return null;
        Application.Quit(exitCode);
    }
}
public static class SolidPairContactAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        foreach(bool ice in new[]{false,true})foreach(float scale in new[]{.003f,.03f,.1f}){
            var source=ice?IceChunkShape.Vertices:GrainShape.Vertices;var faces=ice?IceChunkShape.Faces:GrainShape.Faces;
            var vertices=new Vector3[source.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=source[i]*scale;
            var shape=new SubmergedSolidShape(vertices,faces,Matrix4x4.identity);
            using(var a=new SolidContactShape(vertices,faces))using(var b=new SolidContactShape(vertices,faces)){
                string label=$"ice={ice} scale={scale}";
                Check(label+" isolated contact proxies cannot intercept picks",!a.ParticipatesInSceneQueries&&!b.ParticipatesInSceneQueries);
                Check(label+" isolated proxies compute real overlap",a.Penetration(Vector3.zero,b,Vector3.right*scale*.5f,out var normal,out float depth)&&depth>0);
                Check(label+" separated solids do not contact",!a.Penetration(Vector3.zero,b,Vector3.right*scale*4,out _,out _));
                var sa=new SolidTranslationState(shape,1,Vector3.zero){Velocity=Vector3.right};
                var sb=new SolidTranslationState(shape,3,Vector3.right*scale*.5f){Velocity=Vector3.left};
                var momentum=sa.Velocity+sb.Velocity*3;double energy=sa.Velocity.sqrMagnitude+sb.Velocity.sqrMagnitude*3;
                bool changed=SolidPairContact.Resolve(a,sa,1,b,sb,3);
                Check(label+" pair separates without modifying geometry",changed&&!a.Penetration(sa.Position,b,sb.Position,out _,out _));
                Check(label+" normal impulse preserves linear momentum",Vector3.Distance(momentum,sa.Velocity+sb.Velocity*3)<1e-6);
                Check(label+" normal impulse never adds kinetic energy",sa.Velocity.sqrMagnitude+sb.Velocity.sqrMagnitude*3<=energy+1e-6);
                Check(label+" mass weighted position correction preserves center of mass",Vector3.Distance(sa.Position+sb.Position*3,Vector3.right*scale*1.5f)<1e-6);
                lines.Add($"METRIC {label} depth={depth:G9} normal={normal}");
            }
        }
        // Native integration must keep laboratory acceleration and displacement
        // when its internal coordinate scale changes. These bodies have no
        // contacts, so the expected damped, semi-implicit fixed steps are known.
        foreach(bool ice in new[]{false,true})foreach(float size in new[]{.003f,.03f,.1f})foreach(double density in new[]{0d,.917,2d}){
            var source=ice?IceChunkShape.Vertices:GrainShape.Vertices;var faces=ice?IceChunkShape.Faces:GrainShape.Faces;
            var vertices=new Vector3[source.Length];for(int i=0;i<vertices.Length;i++)vertices[i]=source[i]*size;
            var shape=new SubmergedSolidShape(vertices,faces,Matrix4x4.identity);
            double solidDensity=density==0?2:density,liquidDensity=density==0?0:1;
            var seed=new SolidBodySeed{LocalVertices=vertices,Triangles=faces,Center=new Vector3(0,.35f,0),MassGrams=shape.TotalVolume*1000*solidDensity};
            var state=new SolidTranslationState(shape,seed.MassGrams,seed.Center){Velocity=new Vector3(.2f,0,.1f)};
            var expectedPosition=state.Position;var expectedVelocity=state.Velocity;const float dt=1f/240,drag=5;
            double decay=Math.Exp(-drag*dt),response=(1-decay)/drag;
            float acceleration=(float)(98.1*(liquidDensity/solidDensity-1));
            using(var world=new NativeSolidWorld(Chem.VesselKind.Beaker,new[]{seed},new[]{state})){
                for(int step=0;step<10;step++){
                    expectedVelocity=expectedVelocity*(float)decay+Vector3.up*(float)(acceleration*response);
                    expectedPosition+=expectedVelocity*dt;world.Step(dt,.65f,liquidDensity,drag);
                }
                Check($"native physical units ice={ice} size={size} density={density}",Vector3.Distance(state.Position,expectedPosition)<1e-5f&&Vector3.Distance(state.Velocity,expectedVelocity)<1e-5f&&Math.Abs(world.SimulatedSeconds-10d*dt)<1e-8);
            }
        }
        Chem.Lab.EnsureReady();
        var vessel=new Chem.Vessel(Chem.VesselKind.Beaker,Vector3.zero){Temp=0};vessel.Undissolved[Chem.WaterIce.Id]=.05;
        var seeds=new List<SolidBodySeed>();var batchMesh=SolidGeometry.Build(vessel,seeds);
        // Deliberately overlapped initial stack, not merely already separated seeds.
        for(int i=0;i<seeds.Count;i++)seeds[i].Center=new Vector3(0,.2f+i*.007f,0);
        var originalUv=batchMesh.uv;var originalNormals=batchMesh.normals;
        using(var batch=new SolidBodyBatch(vessel.Kind,batchMesh,seeds)){
            var boundary=new SolidVesselBoundary(vessel.Kind);bool contained=true;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            for(int frame=0;frame<300;frame++){
                batch.Step(1f/60,.5f,1,30);
                for(int i=0;i<batch.Count;i++)contained&=boundary.Contains(seeds[i].LocalVertices,batch.State(i).Position);
            }
            timer.Stop();batch.UpdateMesh();
            Check("persistent ice batch remains inside vessel",contained);
            Check("persistent ice batch separates initially overlapping bodies",batch.MaximumPenetrationForTest()<5e-5f);
            var points=batchMesh.vertices;bool follows=true;double displacement=0,mass=0;
            for(int i=0;i<batch.Count;i++){
                var seed=seeds[i];displacement+=batch.State(i).DisplacedWorldVolume;mass+=seed.MassGrams;
                for(int v=0;v<seed.VertexCount;v++)follows&=Vector3.Distance(points[seed.FirstVertex+v],batch.State(i).Position+seed.LocalVertices[seed.Triangles[v]])<1e-7;
            }
            Check("persistent visible mesh follows every actual body pose",follows);
            Check("floating ice batch displacement balances inventory mass",Math.Abs(displacement*1000-mass)<mass*.002);
            var uv=batchMesh.uv;var normals=batchMesh.normals;bool appearance=uv.Length==originalUv.Length&&normals.Length==originalNormals.Length;
            for(int i=0;i<uv.Length;i++)appearance&=uv[i]==originalUv[i]&&normals[i]==originalNormals[i];
            Check("persistent mesh retains approved surface UV and normals",appearance);
            var frozen=new Vector3[batch.Count];for(int i=0;i<batch.Count;i++)frozen[i]=batch.State(i).Position;
            batch.Step(.1f,.8f,1,30,true);batch.UpdateMesh();bool paused=true;
            for(int i=0;i<batch.Count;i++)paused&=batch.State(i).Position==frozen[i];
            Check("persistent batch pause freezes physics and mesh",paused);
            var level=new SolidLiquidLevel(vessel.Kind);var bodies=new SolidTranslationState[batch.Count];
            for(int i=0;i<batch.Count;i++)bodies[i]=batch.State(i);
            bool inventory=true;var volumeTimer=System.Diagnostics.Stopwatch.StartNew();
            for(int frame=0;frame<300;frame++){
                batch.StepWithLiquidVolume(1f/60,100,1,30);
                inventory&=Math.Abs(level.AvailableMl(batch.LiquidSurfaceY,bodies)-100)<.0001&&batch.ExcessLiquidMl==0;
            }
            volumeTimer.Stop();
            Check("moving solids close liquid volume every sampled frame",inventory);
            double displacedFinal=0;for(int i=0;i<batch.Count;i++)displacedFinal+=bodies[i].DisplacedWorldVolume;
            Check("volume-coupled floating ice retains buoyancy mass balance",Math.Abs(displacedFinal*1000-mass)<mass*.002);
            float emptyLevel=level.Solve(100,Array.Empty<SolidTranslationState>(),out _);
            Check("solid displacement raises level without creating liquid",batch.LiquidSurfaceY>emptyLevel&&Math.Abs(level.CavityVolumeMl(batch.LiquidSurfaceY)-100-displacedFinal*1000)<.0001);
            float top=level.Solve(1000,bodies,out double overflow);
            Check("overcapacity is explicit and not lost",top==level.Height&&Math.Abs(overflow+level.AvailableMl(top,bodies)-1000)<1e-8);
            Check("dry container has no spurious liquid",level.Solve(0,bodies,out double dryExcess)==level.Floor&&dryExcess==0);
            float frozenLevel=batch.LiquidSurfaceY;batch.StepWithLiquidVolume(.1f,200,1,30,true);
            Check("volume coupled pause freezes surface and bodies",batch.LiquidSurfaceY==frozenLevel);
            lines.Add($"METRIC volumeCoupled bodies={batch.Count} simulatedSeconds=5 cpuMs={volumeTimer.Elapsed.TotalMilliseconds:G6} level={batch.LiquidSurfaceY:G9}");
            lines.Add($"METRIC batch bodies={batch.Count} simulatedSeconds=5 cpuMs={timer.Elapsed.TotalMilliseconds:G6} residualOverlap={batch.MaximumPenetrationForTest():G9}");
        }
        UnityEngine.Object.Destroy(batchMesh);
        var meltingVessel=new Chem.Vessel(Chem.VesselKind.Beaker,Vector3.zero){Temp=0};
        meltingVessel.Undissolved[Chem.WaterIce.Id]=.8;
        var initialSeeds=new List<SolidBodySeed>();var initialMesh=SolidGeometry.Build(meltingVessel,initialSeeds);
        using(var initial=new SolidBodyBatch(meltingVessel.Kind,initialMesh,initialSeeds)){
            for(int i=0;i<initial.Count;i++)if(initial.State(i)!=null)initial.State(i).Velocity=new Vector3(.01f*i,.02f,-.03f);
            meltingVessel.Undissolved[Chem.WaterIce.Id]=.4;
            var meltSeeds=new List<SolidBodySeed>();var meltMesh=SolidGeometry.Build(meltingVessel,meltSeeds,initial.Seeds);
            using(var melted=new SolidBodyBatch(meltingVessel.Kind,meltMesh,meltSeeds,initial)){
                bool pose=true,velocity=true,shape=true,visible=true;double massAfter=0,volumeAfter=0;var points=meltMesh.vertices;
                Check("melting retains chunk count across old mL recount threshold",initial.Count>8&&melted.Count==initial.Count);
                for(int i=0;i<melted.Count;i++){
                    var a=initial.State(i);var b=melted.State(i);if(a==null||b==null){pose=false;continue;}
                    pose&=a.Position==b.Position&&meltSeeds[i].Center==b.Position;velocity&=a.Velocity==b.Velocity;
                    var seed=meltSeeds[i];massAfter+=seed.MassGrams;volumeAfter+=seed.ImmersionShape().TotalVolume;
                    for(int v=0;v<seed.LocalVertices.Length;v++)shape&=Vector3.Distance(seed.LocalVertices[v],initialSeeds[i].LocalVertices[v]*Mathf.Pow(.5f,1f/3))<1e-6;
                    for(int v=0;v<seed.VertexCount;v++)visible&=Vector3.Distance(points[seed.FirstVertex+v],b.Position+seed.LocalVertices[seed.Triangles[v]])<1e-7;
                }
                Check("melting retains every surviving center",pose);
                Check("melting retains every surviving velocity",velocity);
                Check("melting shrinks original irregular shape without rerandomization",shape);
                Check("remapped visible mesh matches persistent shape and pose",visible);
                double expectedMass=.4*Chem.ChemData.Sub[Chem.WaterIce.Id].Mass;
                double expectedVolume=expectedMass/Chem.PhaseProperties.Density(Chem.WaterIce.Id,0)/1000;
                Check("remapped bodies match remaining chemical mass and volume",Math.Abs(massAfter-expectedMass)<1e-9&&Math.Abs(volumeAfter-expectedVolume)<expectedVolume*1e-5);
                bool oldIntact=true;for(int i=0;i<initial.Count;i++)oldIntact&=initialSeeds[i].VolumeWorldCubed==meltSeeds[i].VolumeWorldCubed*2;
                Check("remapping never mutates previous batch geometry inventory",oldIntact);
            }
            UnityEngine.Object.Destroy(meltMesh);
        }
        UnityEngine.Object.Destroy(initialMesh);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-pair-contact-audit.txt",lines);
        if(fail>0)throw new Exception("Solid pair contact audit failed: "+fail);
        Debug.Log("Solid pair contact audit passed: "+pass);
    }
}
}
