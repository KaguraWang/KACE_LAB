using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class VesselShellAudit {
    static double MeshVolume(Mesh mesh){
        var v=mesh.vertices;var t=mesh.triangles;double sum=0;
        for(int i=0;i<t.Length;i+=3){
            var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];
            sum+=((double)a.x*((double)b.y*c.z-(double)b.z*c.y)+
                  (double)a.y*((double)b.z*c.x-(double)b.x*c.z)+
                  (double)a.z*((double)b.x*c.y-(double)b.y*c.x))/6;
        }
        return Math.Abs(sum)*1000;
    }
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind))){
            double exact=VesselMesh.ShellVolumeMl(kind);
            double area=VesselMesh.OuterSurfaceAreaM2(kind);
            Check(kind+" positive finite exterior area",area>0&&!double.IsInfinity(area)&&!double.IsNaN(area));
            Check(kind+" positive finite shell",exact>0&&!double.IsInfinity(exact)&&!double.IsNaN(exact));
            lines.Add($"METRIC {kind} shellMl={exact:G12}");
            double lastError=double.PositiveInfinity;
            foreach(int segments in new[]{32,64,128}){
                var mesh=VesselMesh.BuildGlass(kind,segments);
                double measured=MeshVolume(mesh);
                // Regular polygon area relative to the axisymmetric circle.
                double polygonFactor=segments*Math.Sin(2*Math.PI/segments)/(2*Math.PI);
                Check(kind+" rendered shell matches integral n="+segments,Math.Abs(measured-exact*polygonFactor)<exact*2e-5);
                double error=Math.Abs(measured-exact);
                Check(kind+" mesh convergence n="+segments,error<lastError);
                lastError=error;UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        // Different common vessels must not collapse to one nominal-capacity
        // heat capacity. No material constants are introduced by this audit.
        Check("beaker/flask distinct shell volumes",Math.Abs(VesselMesh.ShellVolumeMl(VesselKind.Beaker)-VesselMesh.ShellVolumeMl(VesselKind.Flask))>1);
        var geo=VesselMesh.Get(VesselKind.Beaker);double r0=geo.R*.94,r1=geo.R,h=geo.H;
        double frustumArea=(Math.PI*r0*r0+Math.PI*(r0+r1)*Math.Sqrt(h*h+(r1-r0)*(r1-r0)))*.01;
        Check("beaker exterior area analytic frustum",Math.Abs(VesselMesh.OuterSurfaceAreaM2(VesselKind.Beaker)-frustumArea)<1e-8);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/vessel-shell-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
