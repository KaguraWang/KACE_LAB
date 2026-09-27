using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
namespace ChemLab.Game {
// Opt-in diagnostic only. Inclusive marker durations overlap and must not be
// summed into a frame time. Never enabled by the ordinary laboratory loop.
public sealed class SolidProfilerAudit:IDisposable {
    readonly List<string> names=new List<string>();
    readonly List<ProfilerRecorder> recorders=new List<ProfilerRecorder>();
    readonly List<KeyValuePair<string,long>> samples=new List<KeyValuePair<string,long>>();
    public SolidProfilerAudit(){
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        var inventory=new List<string>();var seen=new HashSet<string>();
        foreach(var handle in handles){
            var description=ProfilerRecorderHandle.GetDescription(handle);string name=description.Name;
            inventory.Add(description.Category+" | "+name+" | "+description.UnitType);
            if(description.UnitType!=ProfilerMarkerDataUnit.TimeNanoseconds)continue;
            if(!(name.Contains("Update")||name.Contains("Wait")||name.Contains("Physics")||name.Contains("Render")||name.Contains("Gfx")||name.Contains("GC")||name.Contains("Mesh")||name.Contains("Buffer")||name.Contains("Camera")||name.Contains("PlayerLoop")||name.Contains("Behaviour")||name.Contains("Job")||name.Contains("Thread")))continue;
            string key=description.Category+"/"+name;if(!seen.Add(key))continue;
            var recorder=ProfilerRecorder.StartNew(description.Category,name,1);
            if(!recorder.Valid){recorder.Dispose();continue;}
            names.Add(key);recorders.Add(recorder);
        }
        inventory.Sort();File.WriteAllLines("Logs/solid-profiler-markers.txt",inventory);
        Debug.Log($"[SolidProfiler] development={Debug.isDebugBuild} available={handles.Count} active={recorders.Count}");
    }
    public void Report(int requested,int frame,double intervalMs){
        samples.Clear();
        for(int i=0;i<recorders.Count;i++){long value=recorders[i].LastValue;if(value>100000)samples.Add(new KeyValuePair<string,long>(names[i],value));}
        samples.Sort((a,b)=>b.Value.CompareTo(a.Value));
        var text=new StringBuilder($"[SolidProfiler] requested={requested} frame={frame} intervalMs={intervalMs:G7} inclusive markers:");
        for(int i=0;i<Math.Min(20,samples.Count);i++)text.Append("\n").Append(samples[i].Key).Append(" ms=").Append((samples[i].Value/1e6).ToString("G7"));
        Debug.Log(text.ToString());
    }
    public void Dispose(){foreach(var recorder in recorders)recorder.Dispose();recorders.Clear();}
}
}
