using UnityEngine;
using System.Collections.Generic;
namespace ChemLab.Game {
// Closed subdivided, irregular grain, bounded by unit radius. The measured volume
// is used by SolidGeometry, so added surface detail never invents material mass.
public static class GrainShape {
    public static readonly Vector3[] Vertices;
    public static readonly int[] Faces;
    public static readonly double Volume;
    static GrainShape(){
        float t=(1+Mathf.Sqrt(5))/2;
        var points=new List<Vector3>{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
        for(int i=0;i<points.Count;i++)points[i]=points[i].normalized;
        int[] baseFaces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        var edges=new Dictionary<long,int>();var refined=new List<int>();
        int Midpoint(int a,int b){
            long key=((long)System.Math.Min(a,b)<<32)|(uint)System.Math.Max(a,b);
            if(edges.TryGetValue(key,out int existing))return existing;
            int index=points.Count;points.Add((points[a]+points[b]).normalized);edges[key]=index;return index;
        }
        for(int i=0;i<baseFaces.Length;i+=3){
            int a=baseFaces[i],b=baseFaces[i+1],c=baseFaces[i+2],ab=Midpoint(a,b),bc=Midpoint(b,c),ca=Midpoint(c,a);
            refined.AddRange(new[]{a,ab,ca,b,bc,ab,c,ca,bc,ab,bc,ca});
        }
        // Fixed seed makes mesh generation reproducible without touching Unity's
        // global RNG. Radial variation breaks the repeated gemstone silhouette.
        var random=new System.Random(7319);
        for(int i=0;i<points.Count;i++)points[i]*=.87f+.13f*(float)random.NextDouble();
        Vertices=points.ToArray();Faces=refined.ToArray();
        double volume=0;for(int i=0;i<Faces.Length;i+=3)volume+=Vector3.Dot(Vertices[Faces[i]],Vector3.Cross(Vertices[Faces[i+1]],Vertices[Faces[i+2]]))/6.0;
        Volume=System.Math.Abs(volume);
    }
}
}
