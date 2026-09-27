using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Game;
using ChemLab.Chem;
namespace ChemLab.Game {
public static class SolidPackingAudit {
    public static void Run(){
        Lab.EnsureReady();var lines=new List<string>();int passed=0,failed=0;
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
        var corners=new List<Vector3>();foreach(float x in new[]{-.5f,.5f})foreach(float y in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})corners.Add(new Vector3(x,y,z));
        var cube=new SolidPackingBox(corners.ToArray(),Vector3.one,Quaternion.identity);
        Check("overlapping cubes",cube.Overlaps(Vector3.zero,cube,Vector3.one*.5f));
        Check("separated cubes",!cube.Overlaps(Vector3.zero,cube,Vector3.right*1.1f));
        var touching=cube.TouchingOffset(cube,Vector3.right);
        Check("contact candidate preserves support spacing",!cube.Overlaps(touching,cube,Vector3.zero)&&Mathf.Abs(touching.x-1.000002f)<1e-7f);
        Check("first swept support",cube.Sweep(Vector3.up*3,cube,Vector3.zero,Vector3.down*4,out float hit)&&Mathf.Abs(hit-.5f)<1e-6f);
        Check("sweep misses side obstacle",!cube.Sweep(new Vector3(2,3,0),cube,Vector3.zero,Vector3.down*4,out _));
        var rotation=Quaternion.Euler(0,45,0);var thin=new SolidPackingBox(corners.ToArray(),new Vector3(2,.2f,.1f),rotation);
        bool adjacentSeparated=true;for(int candidate=0;candidate<8;candidate++)adjacentSeparated&=!thin.Overlaps(thin.AdjacentOffset(cube,candidate),cube,Vector3.zero);
        Check("shape-aligned candidates retain separating support planes",adjacentSeparated);
        Check("rotated thin boxes separated despite overlapping AABBs",!thin.Overlaps(Vector3.zero,thin,rotation*(Vector3.forward*.3f)));
        var random=new System.Random(918);bool cachedSame=true;
        for(int pairIndex=0;pairIndex<20;pairIndex++){
            float Next()=>(float)random.NextDouble();
            var a=new SolidPackingBox(corners.ToArray(),new Vector3(.1f+Next(),.1f+Next(),.1f+Next()),Quaternion.Euler(Next()*360,Next()*360,Next()*360));
            var b=new SolidPackingBox(corners.ToArray(),new Vector3(.1f+Next(),.1f+Next(),.1f+Next()),Quaternion.Euler(Next()*360,Next()*360,Next()*360));
            var cached=a.Prepare(b);
            for(int sample=0;sample<100;sample++){
                var p=new Vector3(Next()*2-1,Next()*2-1,Next()*2-1);var q=new Vector3(Next()*2-1,Next()*2-1,Next()*2-1);var motion=new Vector3(Next()*2-1,Next()*2-1,Next()*2-1);
                cachedSame&=a.Overlaps(p,b,q)==cached.Overlaps(p,q);
                bool reference=a.Sweep(p,b,q,motion,out float referenceHit),actual=cached.Sweep(p,q,motion,out float cachedHit);
                cachedSame&=reference==actual&&Mathf.Abs(referenceHit-cachedHit)<1e-7f;
            }
        }
        Check("cached axes match uncached reference for 2000 overlap and sweep cases",cachedSame);
        var tightCube=cube.PrepareTight(cube);
        Check("original support cube overlap",tightCube.Overlaps(Vector3.zero,Vector3.one*.5f));
        Check("original support cube separation",!tightCube.Overlaps(Vector3.zero,Vector3.right*1.1f));
        Check("original support continuous first contact with 0.01mm allowance",tightCube.Sweep(Vector3.up*3,Vector3.zero,Vector3.down*4,out float tightHit)&&Mathf.Abs(tightHit-(3-1.0001f)/4)<1e-6f);
        foreach(var kind in new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.Bottle}){
            var model=new Vessel(kind,Vector3.zero){Temp=0,Water=50};model.Undissolved[WaterIce.Id]=9;
            var seeds=new List<SolidBodySeed>();var timer=System.Diagnostics.Stopwatch.StartNew();var mesh=SolidGeometry.Build(model,seeds);timer.Stop();
            var boundary=new SolidVesselBoundary(kind);var contacts=new List<SolidContactShape>();var poses=new List<Vector3>();
            int inside=0;double mass=0,volume=0;float overlap=0;bool contained=true;
            try{
                foreach(var seed in seeds){mass+=seed.MassGrams;volume+=seed.VolumeWorldCubed;
                    if(seed.IsSpilled)continue;inside++;contained&=boundary.Contains(seed.LocalVertices,seed.Center);
                    contacts.Add(new SolidContactShape(seed.LocalVertices,seed.Triangles));poses.Add(seed.Center);
                }
                for(int i=0;i<contacts.Count;i++)for(int j=i+1;j<contacts.Count;j++)if(contacts[i].Penetration(poses[i],contacts[j],poses[j],out _,out float depth))overlap=Mathf.Max(overlap,depth);
                lines.Add($"METRIC {kind} seed={model.Id} total={seeds.Count} inside={inside} spilled={seeds.Count-inside} buildMs={timer.Elapsed.TotalMilliseconds:G7} overlap={overlap:G9}");
                Check(kind+" inventory count and mass",seeds.Count==160&&Math.Abs(mass-9*ChemData.Sub[WaterIce.Id].Mass)<1e-8);
                Check(kind+" original solid volume",Math.Abs(volume*1000-model.SolidVolume())<1e-7);
                Check(kind+" actual vertices stay inside",contained);
                Check(kind+" independently cooked original shapes do not overlap",overlap<=.00001f);
                var alternateSeeds=new List<SolidBodySeed>();
                var alternateMesh=SolidGeometry.BuildForPackingAudit(model,alternateSeeds,781);
                try{
                    bool identical=seeds.Count==alternateSeeds.Count,changedPosition=false;
                    for(int i=0;i<Math.Min(seeds.Count,alternateSeeds.Count);i++){
                        var a=seeds[i];var b=alternateSeeds[i];
                        identical&=a.SubstanceId==b.SubstanceId&&a.MassGrams==b.MassGrams&&a.VolumeWorldCubed==b.VolumeWorldCubed&&a.LocalVertices.Length==b.LocalVertices.Length&&a.Triangles.Length==b.Triangles.Length;
                        for(int k=0;k<Math.Min(a.LocalVertices.Length,b.LocalVertices.Length);k++)identical&=a.LocalVertices[k].Equals(b.LocalVertices[k]);
                        for(int k=0;k<Math.Min(a.Triangles.Length,b.Triangles.Length);k++)identical&=a.Triangles[k]==b.Triangles[k];
                        changedPosition|=!a.Center.Equals(b.Center);
                    }
                    var aColors=mesh.colors;var bColors=alternateMesh.colors;identical&=aColors.Length==bColors.Length;
                    for(int i=0;i<Math.Min(aColors.Length,bColors.Length);i++)identical&=aColors[i].Equals(bColors[i]);
                    Check(kind+" different search preserves every original vertex, triangle, mass, volume and colour",identical);
                    Check(kind+" alternate search actually changes placement",changedPosition);
                }finally{UnityEngine.Object.DestroyImmediate(alternateMesh);}
            }finally{foreach(var contact in contacts)contact.Dispose();UnityEngine.Object.DestroyImmediate(mesh);}
        }
        lines.Add($"TOTAL {passed} passed, {failed} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-packing-audit.txt",lines);
        if(failed>0)throw new Exception("Solid packing audit failed");
    }
}
}
