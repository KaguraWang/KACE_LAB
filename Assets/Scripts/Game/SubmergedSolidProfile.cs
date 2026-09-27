using System;
using UnityEngine;
namespace ChemLab.Game {
// Immutable lookup of the exact rendered mesh integrals. Runtime lookup is O(1)
// and allocation-free; first moments are interpolated, never bare centroids.
// Rebuild only when shape/orientation changes, not when it translates.
public sealed class SubmergedSolidProfile {
    readonly double[] volumes;
    readonly Vector3[] moments;
    readonly float bottom,top;
    readonly double total;
    public SubmergedSolidProfile(SubmergedSolidShape shape,int intervals=256){
        if(shape==null||intervals<16||intervals>1024)throw new ArgumentOutOfRangeException(nameof(intervals));
        bottom=shape.Bottom;top=shape.Top;total=shape.TotalVolume;
        volumes=new double[intervals+1];moments=new Vector3[intervals+1];
        for(int i=0;i<=intervals;i++){
            var sample=shape.Measure(Mathf.Lerp(bottom,top,(float)i/intervals));
            volumes[i]=sample.Volume;moments[i]=sample.Centroid*(float)sample.Volume;
        }
    }
    public SubmergedSolidShape.Sample Measure(float height){
        if(float.IsNaN(height)||float.IsInfinity(height))throw new ArgumentOutOfRangeException(nameof(height));
        if(height<=bottom)return new SubmergedSolidShape.Sample();
        int n=volumes.Length-1;
        if(height>=top)return new SubmergedSolidShape.Sample{Volume=total,Centroid=moments[n]/(float)volumes[n]};
        float q=(height-bottom)/(top-bottom)*n;int index=Mathf.Min(n-1,(int)q);float f=q-index;
        double volume=volumes[index]+(volumes[index+1]-volumes[index])*f;
        var moment=Vector3.LerpUnclamped(moments[index],moments[index+1],f);
        if(volume<=1e-18)return new SubmergedSolidShape.Sample();
        var centroid=moment/(float)volume;
        // In the first tiny slice interpolation must not put the force point
        // above the actual water plane.
        centroid.y=Mathf.Clamp(centroid.y,bottom,height);
        return new SubmergedSolidShape.Sample{Volume=volume,Centroid=centroid};
    }
}
}
