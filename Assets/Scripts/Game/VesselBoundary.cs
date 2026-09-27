using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
public static class VesselBoundary {
    public static void Configure(Material m, VesselKind kind) {
        var g=VesselMesh.Get(kind); var p=VesselMesh.Profile(g.Type,g.R,g.H);
        float wall=Mathf.Max(0.012f,g.R*0.052f);var radii=new float[32];
        for(int i=0;i<32;i++) radii[i]=Mathf.Max(0,VesselMesh.RadiusAt(p,i/31f)-wall);
        m.SetFloatArray("_Profile",radii);m.SetFloat("_VesselHeight",g.H);m.SetFloat("_ClipContainer",1);
    }
}
}
