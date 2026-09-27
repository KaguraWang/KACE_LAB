using System;
using System.Collections.Generic;
using System.IO;
using ChemLab.Chem;
using ChemLab.Game;
using UnityEngine;
using UnityEditor;
namespace ChemLab.Editor {
public static class SolidAudit {
    static double Tetrahedron(Vector3 a,Vector3 b,Vector3 c) =>
        ((double)a.x*((double)b.y*c.z-(double)b.z*c.y)+
         (double)a.y*((double)b.z*c.x-(double)b.x*c.z)+
         (double)a.z*((double)b.x*c.y-(double)b.y*c.x))/6;
    public static void Run(){
        Lab.EnsureReady();int checks=0,fail=0;var lines=new List<string>();
        void Check(string name,bool value){checks++;if(!value)fail++;lines.Add((value?"PASS ":"FAIL ")+name);}
        var edgeUse=new Dictionary<long,int>();bool outward=true,bounded=true;
        for(int i=0;i<GrainShape.Faces.Length;i+=3){
            int a=GrainShape.Faces[i],b=GrainShape.Faces[i+1],c=GrainShape.Faces[i+2];
            outward&=Vector3.Dot(GrainShape.Vertices[a],Vector3.Cross(GrainShape.Vertices[b],GrainShape.Vertices[c]))>0;
            foreach(var pair in new[]{(a,b),(b,c),(c,a)}){
                long key=((long)Math.Min(pair.Item1,pair.Item2)<<32)|(uint)Math.Max(pair.Item1,pair.Item2);
                edgeUse.TryGetValue(key,out int count);edgeUse[key]=count+1;
            }
        }
        bool closed=true;foreach(int count in edgeUse.Values)closed&=count==2;
        foreach(var point in GrainShape.Vertices)bounded&=point.magnitude<=1&&point.magnitude>.8f;
        Check("refined grain closed manifold",closed&&GrainShape.Faces.Length==240&&GrainShape.Vertices.Length==42);
        Check("refined grain outward bounded positive volume",outward&&bounded&&GrainShape.Volume>0);
        Check("ice rounded cube has positive volume",IceChunkShape.Volume>0&&IceChunkShape.Faces.Length>100);
        lines.Add("METRIC ice unit volume="+IceChunkShape.Volume);
        foreach(var kind in new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.RoundBottom})foreach(double grams in new[]{.1,5,10,20}){
            var ice=new Vessel(kind,Vector3.zero){Temp=0};ice.Undissolved[WaterIce.Id]=grams/ChemData.Sub[WaterIce.Id].Mass;
            var mesh=SolidGeometry.Build(ice);var points=mesh.vertices;var faces=mesh.triangles;var g=VesselMesh.Get(kind);var profile=VesselMesh.Profile(g.Type,g.R,g.H);
            double volume=0;bool inside=true,gloss=true;
            for(int i=0;i<faces.Length;i+=3)volume+=Tetrahedron(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
            foreach(var p in points)inside&=p.y>=0&&p.y<=g.H&&new Vector2(p.x,p.z).magnitude<=VesselMesh.RadiusAt(profile,p.y/g.H)-Mathf.Max(.012f,g.R*.052f)+1e-5;
            foreach(var uv in mesh.uv)gloss&=uv.y>.9f;
            Check($"ice {kind} {grams}g exact mesh volume",Math.Abs(Math.Abs(volume)-ice.SolidVolume()/1000)<ice.SolidVolume()/1000*.002);
            Check($"ice {kind} {grams}g cavity and dielectric traits",inside&&gloss);
            if(kind==VesselKind.Beaker){
                var chunks=new List<Bounds>();int stride=IceChunkShape.Faces.Length;
                for(int start=0;start<points.Length;start+=stride){
                    var bounds=new Bounds(points[start],Vector3.zero);
                    for(int n=1;n<stride;n++)bounds.Encapsulate(points[start+n]);
                    chunks.Add(bounds);
                }
                bool supported=true;
                for(int n=0;n<chunks.Count;n++){
                    var b=chunks[n];bool touches=Mathf.Abs(b.min.y-Mathf.Max(.012f,g.R*.052f))<1e-5f;
                    for(int k=0;k<n;k++){
                        var a=chunks[k];touches|=Mathf.Abs(b.min.y-a.max.y)<1e-5f&&b.min.x<a.max.x&&b.max.x>a.min.x&&b.min.z<a.max.z&&b.max.z>a.min.z;
                    }
                    supported&=touches;
                }
                Check($"ice {kind} {grams}g floor or chunk support",supported);
            }
            if(!inside)lines.Add($"METRIC ice {kind} {grams}g bounds={mesh.bounds} vesselRadius={g.R} height={g.H} capacity={ice.InteriorCapacityMl}");
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind)))foreach(string id in new[]{"zn","cu","caco3","starch","feoh3"}){
            if(!ChemData.Sub.ContainsKey(id))continue;
            var vessel=new Vessel(kind,Vector3.zero);
            if(id=="feoh3")vessel.Precipitates[id]=0.002;else vessel.Undissolved[id]=0.002;
            var mesh=SolidGeometry.Build(vessel);var points=mesh.vertices;var faces=mesh.triangles;
            double volume=0;bool finite=true;
            for(int i=0;i<faces.Length;i+=3)volume+=Tetrahedron(points[faces[i]]-mesh.bounds.center,points[faces[i+1]]-mesh.bounds.center,points[faces[i+2]]-mesh.bounds.center);
            foreach(var point in points)finite&=!float.IsNaN(point.sqrMagnitude)&&!float.IsInfinity(point.sqrMagnitude);
            double expected=SolidGeometry.VolumeMl(vessel)/1000;
            Check(kind+" "+id+" mass-volume",expected>0&&Math.Abs(Math.Abs(volume)-expected)<expected*0.002);
            Check(kind+" "+id+" finite/no sky column",finite&&mesh.bounds.min.y>=-0.00001f&&mesh.bounds.max.y<VesselMesh.Get(kind).H+0.2f);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind)))foreach(double below in new[]{0,.3,1.2})foreach(double fraction in new[]{.0001,.4,.99,1.5}){
            double capacity=VesselMesh.InnerCapacity(kind)*1000;
            var mesh=PowderBed.Build(kind,capacity*fraction,capacity*below);
            var g=VesselMesh.Get(kind);var profile=VesselMesh.Profile(g.Type,g.R,g.H);
            double volume=0;var points=mesh.vertices;var faces=mesh.triangles;bool inside=true;
            for(int i=0;i<faces.Length;i+=3)volume+=Tetrahedron(points[faces[i]]-mesh.bounds.center,points[faces[i+1]]-mesh.bounds.center,points[faces[i+2]]-mesh.bounds.center);
            foreach(var p in points){
                float radius=new Vector2(p.x,p.z).magnitude;
                // Overflow may rest outside the vessel, but never hover above it.
                inside &= p.y>=-1e-5f&&p.y<=g.H+1e-5f;
                if(radius<g.R)inside &= radius<=Mathf.Max(.0005f,VesselMesh.RadiusAt(profile,p.y/g.H)-Mathf.Max(.012f,g.R*.052f))+.00001f;
            }
            Check(kind+" layered powder "+below+"+"+fraction+" bounded",inside);
            // Mesh coordinates are float32: a micrometre-thin layer high in a
            // pipette quantizes its two absolute Y coordinates. Permit at most
            // 0.00001 mL absolute error for these tiny doses (10 nanolitres).
            double tolerance=Math.Max(capacity*fraction/1000*.002,1e-8);
            Check(kind+" layered powder "+below+"+"+fraction+" volume",Math.Abs(Math.Abs(volume)-capacity*fraction/1000)<tolerance);
            if(Math.Abs(Math.Abs(volume)-capacity*fraction/1000)>=tolerance)
                lines.Add("METRIC measured="+Math.Abs(volume).ToString("G17")+" expected="+(capacity*fraction/1000).ToString("G17"));
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        foreach(var kind in new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.RoundBottom})foreach(bool powderFirst in new[]{false,true}){
            var vessel=new Vessel(kind,Vector3.zero);
            double powderMl=VesselMesh.InnerCapacity(kind)*1000*.35;
            double powderMol=powderMl*PhaseProperties.Density("starch",25)/ChemData.Sub["starch"].Mass;
            if(powderFirst)vessel.Undissolved["starch"]=powderMol;
            vessel.Undissolved["cu"]=.01;
            if(!powderFirst)vessel.Undissolved["starch"]=powderMol;
            var bed=PowderBed.Build(kind,powderMl,0);float top=bed.bounds.max.y;
            var mesh=SolidGeometry.Build(vessel);var points=mesh.vertices;var traits=mesh.uv;
            bool clear=true;float lowestGrain=float.PositiveInfinity;int grainVertices=0;double volume=0;var faces=mesh.triangles;
            for(int i=0;i<points.Length;i++)if(traits[i].y>.1f){grainVertices++;clear &= points[i].y>=top-1e-5f;lowestGrain=Mathf.Min(lowestGrain,points[i].y);}
            for(int i=0;i<faces.Length;i+=3)volume+=Tetrahedron(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);
            Check(kind+" mixed powder order "+powderFirst+" grains above bed",grainVertices>0&&clear);
            Check(kind+" mixed powder order "+powderFirst+" grain touches bed",Mathf.Abs(lowestGrain-top)<1e-5f);
            double expected=vessel.SolidVolume()/1000;
            Check(kind+" mixed powder order "+powderFirst+" volume",Math.Abs(Math.Abs(volume)-expected)<expected*.002);
            UnityEngine.Object.DestroyImmediate(bed);UnityEngine.Object.DestroyImmediate(mesh);
        }
        lines.Add("TOTAL "+(checks-fail)+" passed, "+fail+" failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
