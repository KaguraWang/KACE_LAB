using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
public static class SolidViewAudit {
    public static System.Collections.IEnumerator RunFrames(){
        var lines=new List<string>();var lab=UnityEngine.Object.FindObjectOfType<LabBootstrap>();
        bool enabled=lab.enabled;lab.enabled=false; // Isolate view cost from chemical inventory changes.
        bool fixedFrames=Array.IndexOf(Environment.GetCommandLineArgs(),"-fixedSolidFrames")>=0;
        bool drainGpu=Array.IndexOf(Environment.GetCommandLineArgs(),"-solidGpuDrain")>=0;
        bool frameTiming=Array.IndexOf(Environment.GetCommandLineArgs(),"-solidFrameTiming")>=0;
        bool strictPerformance=Array.IndexOf(Environment.GetCommandLineArgs(),"-strictPerformance")>=0;
        var timing=new FrameTiming[1];
        SolidProfilerAudit profiler=Array.IndexOf(Environment.GetCommandLineArgs(),"-solidProfilerAudit")>=0?new SolidProfilerAudit():null;
        int frameCount=Array.IndexOf(Environment.GetCommandLineArgs(),"-longSolidFrames")>=0?600:150;
        lines.Add("METRIC physics frame timing="+(fixedFrames?"controlled 1/60s; comparison only":"actual Time.deltaTime; runtime acceptance"));
        lines.Add("METRIC requested frame count="+frameCount);
        lines.Add("METRIC synchronous GPU drain="+drainGpu+" (diagnostic only, never runtime acceptance)");
        var hiddenViews=new List<GameObject>();
        foreach(var existing in UnityEngine.Object.FindObjectsOfType<VesselView>())if(existing.gameObject.activeSelf){hiddenViews.Add(existing.gameObject);existing.gameObject.SetActive(false);}
        var cam=lab.Cam;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;
        bool oldCameraEnabled=cam.enabled;
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-solidSingleRender")>=0)cam.enabled=false;
        int renderCount=0;Camera.CameraCallback countRender=c=>{if(c==cam)renderCount++;};Camera.onPreRender+=countRender;
        var oldTarget=cam.targetTexture;var target=new RenderTexture(1280,800,24);target.Create();cam.targetTexture=target;
        Vessel model=null;
        try{
            foreach(int requested in new[]{8,160}){
                int firstRender=renderCount;
                Debug.Log($"[SolidFrames] start requested={requested}");
                model=new Vessel(VesselKind.Beaker,new Vector3(8,0,0)){Water=50,Temp=0};
                model.Undissolved[WaterIce.Id]=requested==8?.05:9;
                Lab.Vessels.Add(model);lab.SyncViews();Debug.Log($"[SolidFrames] static view ready requested={requested}");VesselView view=null;
                foreach(var item in UnityEngine.Object.FindObjectsOfType<VesselView>())if(item.Model==model)view=item;
                var timer=System.Diagnostics.Stopwatch.StartNew();view.EnableSolidDynamics(Array.IndexOf(Environment.GetCommandLineArgs(),"-nativeSolid")>=0);timer.Stop();
                bool omitLiquidRendering=Array.IndexOf(Environment.GetCommandLineArgs(),"-solidNoLiquidRender")>=0;
                if(omitLiquidRendering)foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial!=null&&renderer.sharedMaterial.shader.name=="ChemLab/FluidVolume")renderer.forceRenderingOff=true;
                lines.Add("METRIC omit liquid rendering="+omitLiquidRendering+" (diagnostic only when true; GPU simulation remains active)");
                if(fixedFrames)view.enabled=false; // Explicit stepping below, never double-advance.
                lines.Add($"METRIC requested={requested} total={view.SolidBodies.Count} active={view.Fluid.SolidObstacleCount} initializationMs={timer.Elapsed.TotalMilliseconds:G7}");
                Debug.Log("[SolidFrames] "+lines[lines.Count-1]);File.WriteAllLines("Logs/solid-view-frame-progress.txt",lines);
                cam.transform.position=model.Pos+new Vector3(1.7f,1.6f,-2.4f);cam.transform.LookAt(model.Pos+new Vector3(0,.5f,0));
                var times=new List<double>();var drainTimes=new List<double>();bool aborted=false;var phaseTime=System.Diagnostics.Stopwatch.StartNew();
                var gpuTimes=new List<double>();ulong lastTimestamp=0;
                for(int frame=0;frame<frameCount;frame++){
                    if(fixedFrames)view.AdvanceContents(1f/60);
                    if(frame<6||frame%30==0)Debug.Log($"[SolidFrames] requested={requested} before render frame={frame}");
                    double precedingSolidMs=view.LastSolidCpuMilliseconds;int precedingGc=GC.CollectionCount(0);
                    var renderTimer=System.Diagnostics.Stopwatch.StartNew();
                    cam.Render(); // Hidden/minimized windows can skip backbuffer rendering.
                    renderTimer.Stop();
                    if(frameTiming)FrameTimingManager.CaptureFrameTimings();
                    double drainMs=0;
                    if(drainGpu){
                        var drainTimer=System.Diagnostics.Stopwatch.StartNew();view.Fluid.ReadbackForTest();drainTimer.Stop();drainMs=drainTimer.Elapsed.TotalMilliseconds;
                        if(frame>=30)drainTimes.Add(drainMs);
                    }
                    yield return null;
                    double ms=Time.unscaledDeltaTime*1000;
                    if(frame>=30&&ms>100)profiler?.Report(requested,frame,ms);
                    if(frameTiming&&FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].frameStartTimestamp!=lastTimestamp){
                        var sample=timing[0];lastTimestamp=sample.frameStartTimestamp;
                        if(frame>=30&&sample.gpuFrameTime>0)gpuTimes.Add(sample.gpuFrameTime);
                        if(frame>=30&&(sample.cpuFrameTime>33.4||sample.gpuFrameTime>33.4))Debug.Log($"[SolidFrames] engine requested={requested} timestamp={lastTimestamp} cpuMs={sample.cpuFrameTime:G7} mainMs={sample.cpuMainThreadFrameTime:G7} renderMs={sample.cpuRenderThreadFrameTime:G7} presentWaitMs={sample.cpuMainThreadPresentWaitTime:G7} gpuMs={sample.gpuFrameTime:G7}");
                    }
                    if(frame>=30&&ms>33.4)Debug.Log($"[SolidFrames] slow requested={requested} frame={frame} intervalMs={ms:G7} precedingSolidMs={precedingSolidMs:G7} explicitRenderMs={renderTimer.Elapsed.TotalMilliseconds:G7} drainMs={drainMs:G7} currentSolidMs={view.LastSolidCpuMilliseconds:G7} gc0Delta={GC.CollectionCount(0)-precedingGc}");
                    if(frame<6||frame%30==0)Debug.Log($"[SolidFrames] requested={requested} after update frame={frame} ms={ms:G7} solidCpuMs={view.LastSolidCpuMilliseconds:G7} fluidSubmitMs={view.Fluid.LastStepCpuMilliseconds:G7}");
                    if(frame<6||frame%30==0)Debug.Log($"[SolidFrames] phases integrationMs={view.SolidBodies.IntegrationMilliseconds:G7} contactMs={view.SolidBodies.ContactMilliseconds:G7} levelMs={view.SolidBodies.LevelMilliseconds:G7}");
                    if(frame<6||frame%30==0)Debug.Log($"[SolidFrames] contact queryMs={SolidPairContact.QueryTicks*1000.0/System.Diagnostics.Stopwatch.Frequency:G7} correctionMs={SolidPairContact.CorrectionTicks*1000.0/System.Diagnostics.Stopwatch.Frequency:G7} queries={SolidPairContact.QueryCalls} overlaps={SolidPairContact.Overlaps}");
                    if(frame>=30)times.Add(ms);
                    if((strictPerformance&&((frame>0&&ms>1000)||phaseTime.Elapsed.TotalSeconds>30))||phaseTime.Elapsed.TotalSeconds>300){aborted=true;lines.Add("FAIL solid fixture safety budget exceeded; fixture stopped");break;}
                }
                cam.Render();var previousActive=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
                int nonBlack=0;foreach(var pixel in image.GetPixels32())if(pixel.r>10||pixel.g>10||pixel.b>10)nonBlack++;
                lines.Add((nonBlack>10000?"PASS ":"FAIL ")+"offscreen render has visible scene pixels: "+nonBlack);
                File.WriteAllBytes(Path.GetFullPath($"Logs/solid-view-{requested}-offscreen.png"),image.EncodeToPNG());
                UnityEngine.Object.Destroy(image);RenderTexture.active=previousActive;yield return null;
                times.Sort();
                lines.Add($"METRIC requested={requested} actualCameraRenders={renderCount-firstRender} automaticCameraEnabled={cam.enabled}");
                if(frameTiming){gpuTimes.Sort();lines.Add(gpuTimes.Count==0?"METRIC engine GPU timing unavailable":$"METRIC engine GPU samples={gpuTimes.Count} p95Ms={gpuTimes[(int)(gpuTimes.Count*.95)]:G7} maxMs={gpuTimes[gpuTimes.Count-1]:G7}");}
                if(drainTimes.Count>0){drainTimes.Sort();lines.Add($"METRIC diagnostic GPU drain p95Ms={drainTimes[(int)(drainTimes.Count*.95)]:G7} maxMs={drainTimes[drainTimes.Count-1]:G7}");}
                float overlap=view.SolidBodies.MaximumPenetrationForTest();
                lines.Add($"METRIC nativeColliderOverlap={view.SolidBodies.NativePenetrationForTest():G9} proxyOverlap={overlap:G9}");
                lines.Add("METRIC contactDetails "+view.SolidBodies.NativeContactReportForTest());
                int outside=view.SolidBodies.BoundaryViolationsForTest();
                lines.Add((outside==0?"PASS ":"FAIL ")+"solid bodies outside cavity (0.02mm tolerance): "+outside);
                lines.Add((overlap<=.0002f?"PASS ":"FAIL ")+"solid penetration below 0.02mm: "+overlap);
                if(times.Count>0){double p95=times[Math.Min(times.Count-1,(int)(times.Count*.95))];
                    lines.Add($"METRIC requested={requested} samples={times.Count} p95Ms={p95:G7} maxMs={times[times.Count-1]:G7} particles={view.Fluid.Count} residualOverlap={view.SolidBodies.MaximumPenetrationForTest():G7}");
                    lines.Add((p95<33.4?"PASS ":strictPerformance?"FAIL ":"PERF ")+"frame p95 below 33.4ms");
                    lines.Add((times[times.Count-1]<250?"PASS ":strictPerformance?"FAIL ":"PERF ")+"maximum frame stall below 250ms");
                }
                lines.Add($"METRIC requested={requested} aborted={aborted} resolution={Screen.width}x{Screen.height} GPU={SystemInfo.graphicsDeviceName}");
                File.WriteAllLines("Logs/solid-view-frame-progress.txt",lines);
                Lab.Vessels.Remove(model);model=null;lab.SyncViews();yield return null;
            }
        }finally{
            Camera.onPreRender-=countRender;cam.enabled=oldCameraEnabled;
            profiler?.Dispose();
            if(model!=null){Lab.Vessels.Remove(model);lab.SyncViews();}
            lab.enabled=enabled;cam.transform.SetPositionAndRotation(oldPosition,oldRotation);
            foreach(var item in hiddenViews)if(item!=null)item.SetActive(true);
            cam.targetTexture=oldTarget;target.Release();UnityEngine.Object.Destroy(target);
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-view-frame-audit.txt",lines);
        }
        foreach(var line in lines)if(line.StartsWith("FAIL "))throw new Exception("Solid frame audit failed; see solid-view-frame-audit.txt");
    }
    public static void Run(){
        var lines=new List<string>();int pass=0,fail=0;
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        var lab=UnityEngine.Object.FindObjectOfType<LabBootstrap>();
        var model=new Vessel(VesselKind.Beaker,new Vector3(0,0,0)){Water=100,Temp=0};
        model.Undissolved[WaterIce.Id]=.05;Lab.Vessels.Add(model);lab.SyncViews();
        VesselView view=null;foreach(var candidate in UnityEngine.Object.FindObjectsOfType<VesselView>())if(candidate.Model==model)view=candidate;
        try{
            view.EnableSolidDynamics(Array.IndexOf(Environment.GetCommandLineArgs(),"-nativeSolid")>=0);
            Check("real vessel view owns dynamic solids and GPU obstacles",view.SolidBodies.Count==8&&view.Fluid.SolidObstacleCount==8);
            var smallDosePoses=view.Fluid.ReadbackForTest();int smallDoseCount=view.Fluid.Count;
            model.Water=100.001;view.Refresh();
            Check("real view forwards sub-fill-threshold dose to GPU",view.Fluid.MassVolumeMl==model.Volume()&&view.Fluid.Count==smallDoseCount&&view.Fluid.SolidObstacleCount==8);
            var afterSmallDose=view.Fluid.ReadbackForTest();bool dosePreservedPoses=afterSmallDose.Length==smallDosePoses.Length;
            for(int i=0;i<afterSmallDose.Length&&i<smallDosePoses.Length;i++)dosePreservedPoses&=afterSmallDose[i]==smallDosePoses[i];
            Check("small dose retains actual GPU particle poses",dosePreservedPoses);
            model.Water=100;view.Refresh();
            Check("real view forwards sub-fill-threshold withdrawal to GPU",view.Fluid.MassVolumeMl==100&&view.Fluid.SolidObstacleCount==8);
            float start=view.SolidBodies.State(0).Position.y;
            for(int frame=0;frame<180;frame++)view.AdvanceContents(1f/60);
            Check("real view advances floating solids without losing liquid",view.SolidBodies.State(0).Position.y>start&&model.Water==100&&view.Fluid.MassVolumeMl==100);
            var positions=new Vector3[8];for(int i=0;i<8;i++)positions[i]=view.SolidBodies.State(i).Position;
            model.Undissolved[WaterIce.Id]=.025;view.Refresh();bool retained=true;
            for(int i=0;i<8;i++)retained&=positions[i]==view.SolidBodies.State(i).Position;
            Check("real view melt refresh preserves positions and rebinds obstacles",retained&&view.Fluid.SolidObstacleCount==8);
            int dispatch=view.Fluid.DispatchCount;
            var target=new Vessel(VesselKind.Beaker,Vector3.zero);
            using(var transaction=new PourTransaction(model,target)){
                view.AdvanceContents(.1f);
                bool frozen=view.Fluid.DispatchCount==dispatch;
                for(int i=0;i<8;i++)frozen&=positions[i]==view.SolidBodies.State(i).Position;
                Check("staged pour freezes both GPU and solid view advancement",frozen);
            }
            model.Undissolved.Clear();view.Refresh();
            Check("complete melting releases dynamic bodies and clears GPU obstacles",view.SolidBodies==null&&view.Fluid.SolidObstacleCount==0);
            model.Precipitates["caco3"]=.2;model.Undissolved["zn"]=.002;view.Refresh();
            float bedTop=0;int beds=0;double bedVolume=0;
            foreach(var seed in view.SolidBodies.Seeds)if(seed.IsStatic){beds++;bedVolume+=seed.ImmersionShape().TotalVolume;foreach(var p in seed.LocalVertices)bedTop=Mathf.Max(bedTop,p.y);}
            Check("powder bed enters GPU as actual static geometry",beds==1&&view.Fluid.SolidObstacleCount==view.SolidBodies.Count);
            double expectedBed=.2*ChemData.Sub["caco3"].Mass/PhaseProperties.Density("caco3",0)/1000;
            Check("static powder collision mesh retains actual material volume",Math.Abs(bedVolume-expectedBed)<expectedBed*1e-5);
            bool supported=true;
            for(int frame=0;frame<30;frame++){
                view.AdvanceContents(1f/60);
                for(int i=0;i<view.SolidBodies.Count;i++)if(view.SolidBodies.State(i)!=null)
                    foreach(var p in view.SolidBodies.Seeds[i].LocalVertices)supported&=p.y+view.SolidBodies.State(i).Position.y>=bedTop-2e-5;
            }
            Check("dense grains cannot sink through powder support",supported);
        }finally{Lab.Vessels.Remove(model);lab.SyncViews();}
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-view-audit.txt",lines);
        if(fail>0)throw new Exception("Solid view audit failed: "+fail);
        Debug.Log("Solid view audit passed: "+pass);
    }
}
}
