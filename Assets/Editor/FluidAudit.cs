using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class FluidAudit {
    public static void Run(){
        Lab.EnsureReady();var lines=new List<string>();int fail=0;
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)fail++;}
        foreach(var kind in new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.RoundBottom,VesselKind.Tube}) {
            var fluid=new Fluid3D();float amount=kind==VesselKind.Tube?20:100;
            fluid.Configure(kind,amount);int n=fluid.Positions.Length;
            var clock=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<45;i++)fluid.Step(Vector3.zero,Quaternion.identity,1f/90,false,0);
            fluid.Impulse(new Vector3(0.6f,0.3f,0.2f));
            for(int i=0;i<45;i++)fluid.Step(Vector3.zero,Quaternion.identity,1f/90,true,0);
            Check("3D particles move and preserve count "+kind,n>50&&fluid.Positions.Length==n&&fluid.KineticEnergy>0);
            Check("particle mass conserved "+kind,Math.Abs(fluid.MassVolumeMl-amount)<0.001);
            bool inside=true,finite=true;foreach(var p in fluid.Positions){inside&=fluid.BoundaryDistance(p)>=-0.001f;finite&=!float.IsNaN(p.x+p.y+p.z);}
            Check("finite particles inside vessel "+kind,finite&&inside);
            lines.Add("METRIC "+kind+" particles="+n+" solver_ms_per_step="+clock.Elapsed.TotalMilliseconds/90+" compression="+fluid.MaxCompression);
            var mesh=new Mesh();var surface=new FluidSurface3D();clock.Restart();surface.Rebuild(fluid,mesh);
            Check("connected volume reconstruction "+kind,mesh.vertexCount>0&&Math.Abs(surface.MeshVolumeMl-amount)<amount*0.18);
            lines.Add("METRIC "+kind+" mesh_volume_ml="+surface.MeshVolumeMl+" reconstruction_ms="+clock.Elapsed.TotalMilliseconds);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        var overflow=new Vessel(VesselKind.Flask,Vector3.zero);overflow.Undissolved["zn"]=8;
        var solid=SolidGeometry.Build(overflow);Check("overfilled solid cannot form sky tower",solid.bounds.max.y<=VesselMesh.Get(VesselKind.Flask).H+0.2f);
        UnityEngine.Object.DestroyImmediate(solid);
        lines.Add("TOTAL failures="+fail);Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/fluid-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
