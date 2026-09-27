using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Game;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class SolidBodyStateAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind))){
            var vessel=new Vessel(kind,Vector3.zero){Temp=0};vessel.Undissolved[WaterIce.Id]=.05;
            var seeds=new List<SolidBodySeed>();var mesh=SolidGeometry.Build(vessel,seeds);var old=SolidGeometry.Build(vessel);
            var vertices=mesh.vertices;var original=old.vertices;bool same=vertices.Length==original.Length;
            if(same)for(int i=0;i<vertices.Length;i++)same&=vertices[i]==original[i];
            Check(kind+" visible geometry unchanged by body extraction",same);
            double mass=0,volume=0;bool match=true;
            foreach(var seed in seeds){
                mass+=seed.MassGrams;volume+=seed.VolumeWorldCubed;
                for(int i=0;i<seed.VertexCount;i++)match&=Vector3.Distance(vertices[seed.FirstVertex+i],seed.Center+seed.LocalVertices[seed.Triangles[i]])<1e-7;
                var shape=seed.ImmersionShape();match&=Math.Abs(shape.TotalVolume-seed.VolumeWorldCubed)<seed.VolumeWorldCubed*2e-6;
            }
            Check(kind+" body descriptors match rendered vertices and volume",seeds.Count>0&&match);
            Check(kind+" body masses preserve inventory",Math.Abs(mass-.05*ChemData.Sub[WaterIce.Id].Mass)<1e-10);
            Check(kind+" descriptors preserve solid volume",Math.Abs(volume*1000-vessel.SolidVolume())<1e-8);
            UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(old);
        }
        foreach(string id in new[]{"zn","cu","feoh3"}){
            var mixed=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=0};mixed.Precipitates["caco3"]=.001;
            if(id=="feoh3")mixed.Precipitates[id]=.001;else mixed.Undissolved[id]=.001;
            var seeds=new List<SolidBodySeed>();var mesh=SolidGeometry.Build(mixed,seeds);var vertices=mesh.vertices;
            lines.Add($"METRIC mixed={id} bodies={seeds.Count} sourceForm={ChemData.Sub[id].Form} bedForm={ChemData.Sub["caco3"].Form}");
            bool correct=seeds.Count>0;double mass=0,bedMass=0;int bedCount=0;
            foreach(var seed in seeds){
                if(seed.IsStatic){bedCount++;bedMass+=seed.MassGrams;correct&=seed.SubstanceId=="caco3"&&seed.IsPrecipitate;continue;}
                mass+=seed.MassGrams;correct&=seed.SubstanceId==id&&seed.IsPrecipitate==(id=="feoh3");
                for(int i=0;i<seed.VertexCount;i++)correct&=Vector3.Distance(vertices[seed.FirstVertex+i],seed.Center+seed.LocalVertices[seed.Triangles[i]])<1e-7;
            }
            Check(id+" moving grains/flocs map correctly after static powder bed",correct&&Math.Abs(mass-.001*ChemData.Sub[id].Mass)<1e-10);
            Check(id+" static bed separately preserves powder inventory",bedCount==1&&Math.Abs(bedMass-.001*ChemData.Sub["caco3"].Mass)<1e-10);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        var irregular=new SubmergedSolidShape(IceChunkShape.Vertices,IceChunkShape.Faces,Matrix4x4.Scale(Vector3.one*.1f));
        foreach(double density in new[]{.5,.917,1d,2d}){
            double mass=irregular.TotalVolume*1000*density;
            var body=new SolidTranslationState(irregular,mass,new Vector3(0,-1,0));
            body.Step(.05f,0,1,0);
            double expected=98.1*(1/density-1)*.05;
            Check("immersed acceleration density "+density,Math.Abs(body.Velocity.y-expected)<1e-5);
            var p=body.Position;var v=body.Velocity;var center=body.BuoyancyCenter;double wet=body.DisplacedWorldVolume;
            body.Step(.1f,0,1,20,true);
            Check("pause freezes complete body state "+density,body.Position==p&&body.Velocity==v&&body.BuoyancyCenter==center&&body.DisplacedWorldVolume==wet);
        }
        foreach(float dt in new[]{.05f,.01f,.005f}){
            var floating=new SolidTranslationState(irregular,irregular.TotalVolume*1000*.917,new Vector3(0,-.2f,0));
            int n=Mathf.RoundToInt(8/dt);for(int i=0;i<n;i++)floating.Step(dt,0,1,30);
            float expected=-irregular.PlaneForFraction(.917);
            lines.Add($"METRIC dt={dt} y={floating.Position.y:G9} expected={expected:G9} velocity={floating.Velocity.y:G9}");
            Check("ice rises to actual mesh equilibrium dt="+dt,Math.Abs(floating.Position.y-expected)<.0001&&Math.Abs(floating.Velocity.y)<.001);
            Check("floating displacement balances weight dt="+dt,Math.Abs(floating.DisplacedWorldVolume/irregular.TotalVolume-.917)<.001);
            for(int i=0;i<n;i++)floating.Step(dt,.1f,1,30);
            Check("floating follows raised liquid surface dt="+dt,Math.Abs(floating.Position.y-(expected+.1f))<.0001);
        }
        var dry=new SolidTranslationState(irregular,1,Vector3.zero);dry.Step(.05f,-10,0,0);
        Check("dry gravity respects 100mm world unit",Math.Abs(dry.Position.y+.5*98.1*.05*.05)<1e-6&&Math.Abs(dry.Velocity.y+98.1*.05)<1e-5);
        bool rejected=false;try{dry.Step(.01f,0,double.NaN,1);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check("invalid fluid density rejected",rejected);
        foreach(float drag in new[]{0f,.00001f,1f,10000f}){
            var damped=new SolidTranslationState(irregular,1,Vector3.zero){Velocity=new Vector3(2,0,0)};
            damped.Step(.05f,-10,0,drag);
            double expectedVelocity=2*Math.Exp(-drag*.05);
            double expectedX=drag==0?.1:2*(1-Math.Exp(-drag*.05))/drag;
            Check("linear drag analytic horizontal decay "+drag,Math.Abs(damped.Velocity.x-expectedVelocity)<1e-6&&Math.Abs(damped.Position.x-expectedX)<1e-6);
        }
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-body-state-audit.txt",lines);
        if(fail>0)throw new Exception("Solid body state audit failed: "+fail);
        Debug.Log("Solid body state audit passed: "+pass);
    }
}
}
