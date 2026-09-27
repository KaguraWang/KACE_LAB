using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Quiescent volume closure, using the same 513-radius wall profile as GpuFluid.
// Bodies must be non-overlapping and contained. No inventory is changed here.
public sealed class SolidLiquidLevel {
    readonly float[] radius=new float[513];
    readonly double[] cumulative=new double[513];
    readonly float height,floor;
    readonly double floorVolume;
    readonly List<SubmergedSolidProfile> beds=new List<SubmergedSolidProfile>();
    public void AddStaticBed(SolidBodySeed seed){
        if(!seed.IsStatic||seed.Center!=Vector3.zero)throw new ArgumentException("Static bed must use vessel-local mesh coordinates");
        beds.Add(new SubmergedSolidProfile(seed.ImmersionShape()));
    }
    public float Height=>height;
    public float Floor=>floor;
    public SolidLiquidLevel(VesselKind kind){
        var g=VesselMesh.Get(kind);height=g.H;floor=kind==VesselKind.Cylinder?.04f:0;
        var profile=VesselMesh.Profile(g.Type,g.R,g.H);float wall=Mathf.Max(.012f,g.R*.052f);
        for(int i=0;i<=512;i++)radius[i]=Mathf.Max(.0005f,VesselMesh.RadiusAt(profile,i/512f)-wall);
        for(int i=1;i<=512;i++)cumulative[i]=cumulative[i-1]+Frustum(radius[i-1],radius[i],height/512.0);
        floorVolume=RawVolume(floor);
    }
    static double Frustum(double a,double b,double h)=>Math.PI*h*(a*a+a*b+b*b)/3;
    double RawVolume(float y){
        double q=Math.Max(0,Math.Min(512,(double)y/height*512));int index=Math.Min(511,(int)q);
        double f=q-index,r=radius[index]+(radius[index+1]-radius[index])*f;
        return cumulative[index]+Frustum(radius[index],r,f*height/512.0);
    }
    public double CavityVolumeMl(float y)=>Math.Max(0,RawVolume(y)-floorVolume)*1000;
    public double AvailableMl(float y,IReadOnlyList<SolidTranslationState> bodies){
        double displaced=0;for(int i=0;i<bodies.Count;i++)if(bodies[i]!=null)
            displaced+=bodies[i].VolumeBelow(y)-bodies[i].VolumeBelow(floor);
        foreach(var bed in beds)displaced+=bed.Measure(y).Volume-bed.Measure(floor).Volume;
        return Math.Max(0,CavityVolumeMl(y)-1000*displaced);
    }
    // Excess is reported explicitly, never silently discarded from chemistry.
    public float Solve(double liquidMl,IReadOnlyList<SolidTranslationState> bodies,out double excessMl){
        if(double.IsNaN(liquidMl)||double.IsInfinity(liquidMl)||liquidMl<0||bodies==null)throw new ArgumentException("Finite nonnegative liquid inventory and body list required");
        double capacity=AvailableMl(height,bodies);excessMl=Math.Max(0,liquidMl-capacity);
        if(liquidMl==0)return floor;if(liquidMl>=capacity)return height;
        float low=floor,high=height;
        for(int i=0;i<28;i++){float mid=(low+high)*.5f;if(AvailableMl(mid,bodies)<liquidMl)low=mid;else high=mid;}
        return (low+high)*.5f;
    }
}
}
