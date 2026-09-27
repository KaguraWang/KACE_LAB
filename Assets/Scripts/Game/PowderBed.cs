using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Sub-pixel grains are represented by a continuous, volume-calibrated powder bed.
// Individual coarse octahedra are intentionally not used for powders.
public static class PowderBed {
    const int Segments=128;
    static readonly double AreaFactor=Segments*Math.Sin(2*Math.PI/Segments)/2;
    static double SegmentVolume(Vector2 a,Vector2 b) =>
        AreaFactor*(b.y-a.y)*(a.x*a.x+(double)a.x*b.x+b.x*b.x)/3;
    static float Height(List<Vector2> profile,double volume) {
        if(volume<=0)return profile[0].y;
        for(int i=1;i<profile.Count;i++){
            var a=profile[i-1];var b=profile[i];double segment=SegmentVolume(a,b);
            if(volume>segment){volume-=segment;continue;}
            float lo=0,hi=1;
            for(int j=0;j<30;j++){
                float f=(lo+hi)*.5f;
                if(SegmentVolume(a,Vector2.Lerp(a,b,f))<volume)lo=f;else hi=f;
            }
            return Mathf.Lerp(a.y,b.y,(lo+hi)*.5f);
        }
        return profile[profile.Count-1].y;
    }
    public static Mesh Build(VesselKind kind,double volumeMl,double belowMl,List<SolidBodySeed> seeds=null) {
        if(double.IsNaN(volumeMl)||double.IsInfinity(volumeMl)||volumeMl<=0||
           double.IsNaN(belowMl)||double.IsInfinity(belowMl))return new Mesh{name="Empty powder"};
        var g=VesselMesh.Get(kind);var outer=VesselMesh.Profile(g.Type,g.R,g.H);
        float wall=Mathf.Max(.012f,g.R*.052f);
        var profile=new List<Vector2>();
        for(int i=0;i<outer.Count;i++){
            if(i>0){
                var a=outer[i-1];var b=outer[i];float threshold=wall+.0005f;
                if((a.x-threshold)*(b.x-threshold)<0)
                    profile.Add(new Vector2(.0005f,Mathf.Lerp(a.y,b.y,(threshold-a.x)/(b.x-a.x))));
            }
            profile.Add(new Vector2(Mathf.Max(.0005f,outer[i].x-wall),outer[i].y));
        }
        double capacity=0;
        for(int i=1;i<profile.Count;i++)capacity+=SegmentVolume(profile[i-1],profile[i]);
        double start=Math.Max(0,belowMl)/1000,end=start+volumeMl/1000;
        var parts=new List<CombineInstance>();
        if(start<capacity){
            // Solve against polygonal frusta, keeping every sidewall on the cavity.
            // Stretching Y after tessellation would penetrate tapered glass.
            float bottom=Height(profile,start),top=Height(profile,Math.Min(capacity,end));
            var loop=new List<Vector2>{new Vector2(0,bottom),
                new Vector2(VesselMesh.RadiusAt(profile,bottom/g.H),bottom)};
            foreach(var p in profile)if(p.y>bottom&&p.y<top)loop.Add(p);
            float rt=VesselMesh.RadiusAt(profile,top/g.H);
            loop.Add(new Vector2(rt,top));
            for(int i=1;i<=24;i++)loop.Add(new Vector2(rt*(1-i/24f),top));
            var insideMesh=VesselMesh.RevolveLoop(loop,Segments);
            if(seeds!=null)seeds.Add(new SolidBodySeed{IsStatic=true,Center=Vector3.zero,
                LocalVertices=insideMesh.vertices,Triangles=insideMesh.triangles,
                VolumeWorldCubed=Math.Min(capacity,end)-start});
            parts.Add(new CombineInstance{mesh=insideMesh,transform=Matrix4x4.identity});
        }
        if(end>capacity){
            // Legacy excess rests on the tabletop in consecutive shallow annuli.
            // Preserve its volume without a vertical tower or overlapping layers.
            const float thickness=.025f;
            double inner=g.R+.03,baseSquared=inner*inner;
            float r0=(float)Math.Sqrt(baseSquared+Math.Max(0,start-capacity)/(AreaFactor*thickness));
            float r1=(float)Math.Sqrt(baseSquared+(end-capacity)/(AreaFactor*thickness));
            var loop=new List<Vector2>{new Vector2(r0,0),new Vector2(r1,0),
                new Vector2(r1,thickness),new Vector2(r0,thickness)};
            parts.Add(new CombineInstance{mesh=VesselMesh.RevolveLoop(loop,Segments),transform=Matrix4x4.identity});
        }
        var mesh=new Mesh{name="Bounded volume-calibrated powder",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.CombineMeshes(parts.ToArray());mesh.RecalculateBounds();
        foreach(var part in parts){
            if(Application.isPlaying)UnityEngine.Object.Destroy(part.mesh);
            else UnityEngine.Object.DestroyImmediate(part.mesh);
        }
        return mesh;
    }
}
}
