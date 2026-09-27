using System;
using UnityEngine;
namespace ChemLab.Game {
// A closed, consistently wound triangle mesh. Plane clipping keeps the actual
// rounded/irregular surface; no bounding-box substitution for displaced volume.
// Coordinates and volume use the input mesh's units, not assumed SI units.
public sealed class SubmergedSolidShape {
    public struct Sample { public double Volume; public Vector3 Centroid; }
    readonly Vector3[] points;
    readonly int[] faces;
    readonly Vector3[] polygon=new Vector3[4];
    readonly Vector3 reference;
    public float Bottom {get;}
    public float Top {get;}
    public double TotalVolume {get;}
    public SubmergedSolidShape(Vector3[] vertices,int[] triangles,Matrix4x4 transform) {
        if(vertices==null||vertices.Length<4||triangles==null||triangles.Length<12||triangles.Length%3!=0)
            throw new ArgumentException("A closed triangular solid mesh is required");
        points=new Vector3[vertices.Length];faces=(int[])triangles.Clone();
        float bottom=float.PositiveInfinity,top=float.NegativeInfinity;var bounds=new Bounds();
        for(int i=0;i<points.Length;i++){
            var p=transform.MultiplyPoint3x4(vertices[i]);
            if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new ArgumentException("Non-finite solid geometry");
            points[i]=p;bottom=Mathf.Min(bottom,p.y);top=Mathf.Max(top,p.y);
            if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);
        }
        foreach(int index in faces)if(index<0||index>=points.Length)throw new ArgumentException("Invalid triangle index");
        Bottom=bottom;Top=top;reference=bounds.center;
        TotalVolume=Measure(Top).Volume;
        if(!(TotalVolume>1e-15))throw new ArgumentException("Solid has zero volume");
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    public Sample Measure(float surfaceY) {
        if(!Finite(surfaceY))throw new ArgumentOutOfRangeException(nameof(surfaceY));
        if(surfaceY<=Bottom)return new Sample();
        // The cap lies on the plane through origin, so its tetrahedral volume
        // and first moment are exactly zero. Only clipped original faces are needed.
        var origin=new Vector3(reference.x,Mathf.Min(surfaceY,Top),reference.z);
        double volume=0,mx=0,my=0,mz=0;
        for(int face=0;face<faces.Length;face+=3){
            int count=0;var previous=points[faces[face+2]];bool wasInside=previous.y<=surfaceY;
            for(int edge=0;edge<3;edge++){
                var current=points[faces[face+edge]];bool inside=current.y<=surfaceY;
                if(inside!=wasInside){
                    float t=(surfaceY-previous.y)/(current.y-previous.y);
                    polygon[count++]=previous+(current-previous)*t;
                }
                if(inside)polygon[count++]=current;
                previous=current;wasInside=inside;
            }
            for(int i=1;i+1<count;i++){
                var a=polygon[0]-origin;var b=polygon[i]-origin;var c=polygon[i+1]-origin;
                double dv=((double)a.x*((double)b.y*c.z-(double)b.z*c.y)
                          +(double)a.y*((double)b.z*c.x-(double)b.x*c.z)
                          +(double)a.z*((double)b.x*c.y-(double)b.y*c.x))/6;
                volume+=dv;mx+=dv*(a.x+b.x+c.x)/4;my+=dv*(a.y+b.y+c.y)/4;mz+=dv*(a.z+b.z+c.z)/4;
            }
        }
        if(Math.Abs(volume)<1e-18)return new Sample();
        return new Sample{Volume=Math.Abs(volume),Centroid=origin+new Vector3((float)(mx/volume),(float)(my/volume),(float)(mz/volume))};
    }
    // Finds the plane for rho_solid/rho_liquid of the mesh under water.
    // Used to initialize equilibrium, not as a replacement for dynamic forces.
    public float PlaneForFraction(double fraction) {
        if(double.IsNaN(fraction)||double.IsInfinity(fraction)||fraction<0||fraction>1)
            throw new ArgumentOutOfRangeException(nameof(fraction));
        if(fraction==0)return Bottom;if(fraction==1)return Top;
        float lo=Bottom,hi=Top;
        for(int i=0;i<32;i++){
            float mid=(lo+hi)*.5f;if(mid==lo||mid==hi)break;
            if(Measure(mid).Volume<TotalVolume*fraction)lo=mid;else hi=mid;
        }
        return (lo+hi)*.5f;
    }
}
}
