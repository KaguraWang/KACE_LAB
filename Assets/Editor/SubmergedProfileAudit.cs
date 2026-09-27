using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class SubmergedProfileAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        foreach(bool ice in new[]{false,true})foreach(float angle in new[]{0f,17f,63f}){
            var shape=new SubmergedSolidShape(ice?IceChunkShape.Vertices:GrainShape.Vertices,ice?IceChunkShape.Faces:GrainShape.Faces,
                Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(angle,31,angle*.4f),new Vector3(.07f,.09f,.11f)));
            var timer=System.Diagnostics.Stopwatch.StartNew();var profile=new SubmergedSolidProfile(shape);timer.Stop();
            double maxVolume=0,maxMoment=0,previous=0;bool monotonic=true,inside=true;
            for(int i=0;i<=1000;i++){
                float y=Mathf.Lerp(shape.Bottom,shape.Top,i/1000f);var exact=shape.Measure(y);var cached=profile.Measure(y);
                maxVolume=Math.Max(maxVolume,Math.Abs(exact.Volume-cached.Volume)/shape.TotalVolume);
                var em=exact.Centroid*(float)exact.Volume;var cm=cached.Centroid*(float)cached.Volume;
                maxMoment=Math.Max(maxMoment,(em-cm).magnitude/(shape.TotalVolume*(shape.Top-shape.Bottom)));
                monotonic&=cached.Volume>=previous-1e-14&&cached.Volume<=shape.TotalVolume+1e-14;previous=cached.Volume;
                if(cached.Volume>0)inside&=cached.Centroid.y>=shape.Bottom-1e-7&&cached.Centroid.y<=y+1e-7;
            }
            string name=$"ice={ice} angle={angle}";
            lines.Add($"METRIC {name} relativeVolume={maxVolume:G9} normalizedMoment={maxMoment:G9} buildMs={timer.Elapsed.TotalMilliseconds:G6}");
            Check(name+" cached volume within 0.02 percent of exact mesh",maxVolume<.0002);
            Check(name+" cached buoyancy moment within 0.02 percent",maxMoment<.0002);
            Check(name+" monotonic bounded and exact empty/full",monotonic&&profile.Measure(shape.Bottom-1).Volume==0&&profile.Measure(shape.Top+1).Volume==shape.TotalVolume);
            Check(name+" cached force point stays submerged",inside);
            double mass=shape.TotalVolume*1000*.917;
            var exactBody=new SolidTranslationState(shape,mass,new Vector3(0,-.2f,0),false);
            var cachedBody=new SolidTranslationState(shape,mass,new Vector3(0,-.2f,0));double maxPath=0;
            for(int i=0;i<480;i++){
                exactBody.Step(1f/60,0,1,30);cachedBody.Step(1f/60,0,1,30);
                maxPath=Math.Max(maxPath,Vector3.Distance(exactBody.Position,cachedBody.Position));
            }
            Check(name+" entire rising trajectory stays within 0.02 mm",maxPath<.0002);
            // Warm both paths before recording comparative CPU timing. No hard
            // wall-clock threshold: the user's other local workloads may compete.
            double checksum=0;timer.Restart();for(int i=0;i<20000;i++)checksum+=profile.Measure(Mathf.Lerp(shape.Bottom,shape.Top,(i%1001)/1000f)).Volume;timer.Stop();double cacheMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();for(int i=0;i<20000;i++)checksum+=shape.Measure(Mathf.Lerp(shape.Bottom,shape.Top,(i%1001)/1000f)).Volume;timer.Stop();
            lines.Add($"METRIC {name} queries=20000 cachedMs={cacheMs:G6} exactMs={timer.Elapsed.TotalMilliseconds:G6} checksum={checksum:G9}");
        }
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/submerged-profile-audit.txt",lines);
        if(fail>0)throw new Exception("Submerged profile audit failed: "+fail);
        Debug.Log("Submerged profile audit passed: "+pass);
    }
}
}
