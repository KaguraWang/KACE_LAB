using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class RuntimeStorageAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        foreach(string id in new[]{"h2o","ch3oh","c2h5oh"})foreach(bool initialized in new[]{false,true}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            s.Water=r.Water=0;s.Temp=r.Temp=25;
            if(id=="h2o")s.Water=50;else s.Solutes[id]=50/ChemData.Sub[id].Mass;
            var rig=new DistillationAssembly(s){Cooling=true,UseVesselThermalStorage=false};rig.Configure(false,r,out _);rig.Start(out _);
            if(initialized)rig.Tick(.1f);
            double temperature=s.Temp,energy=rig.HeaterEnergyJ,stored=rig.CondenserStoredEnergyJ,coolant=rig.CoolantEnergyJ,ambient=rig.CondenserAmbientEnergyJ;
            double water=s.Water,moles=s.Get(id),unused=rig.UnusedHeaterEnergyJ;
            rig.CondenserLengthMeters=initialized?.4:double.NaN;
            rig.Tick(1);
            Check($"{id} geometry rejection initialized={initialized}",!rig.Running&&rig.Notice.Contains("尺寸"));
            Check($"{id} geometry rejection atomic initialized={initialized}",s.Temp==temperature&&s.Water==water&&s.Get(id)==moles&&
                rig.HeaterEnergyJ==energy&&rig.UnusedHeaterEnergyJ==unused&&rig.CondenserStoredEnergyJ==stored&&rig.CoolantEnergyJ==coolant&&rig.CondenserAmbientEnergyJ==ambient&&rig.CollectedMl==0);
            rig.Detach();
        }
        var endpoints=new Dictionary<string,List<double[]>>();
        foreach(string id in new[]{"h2o","ch3oh","c2h5oh"})foreach(bool reflux in new[]{false,true})foreach(double flow in new[]{0,10})foreach(float step in
            id=="c2h5oh"&&reflux&&flow==10?new[]{1f,.2f,.05f,.0125f}:new[]{1f,.2f,.05f}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            var model=PureLiquidThermalModel.For(id);double mass=ChemData.Sub[id].Mass;
            if(id=="h2o"){s.Water=50;s.Temp=99;r.Water=10;r.Temp=25;}
            else{s.Water=r.Water=0;s.Solutes[id]=50/mass;r.Solutes[id]=10/mass;s.Temp=(float)(model.BoilingC-1);r.Temp=25;}
            double M(Vessel v)=>id=="h2o"?v.Water:(v.Solutes.TryGetValue(id,out var n)?n*mass:0);
            double H(Vessel v)=>M(v)*(id=="h2o"?4.18*v.Temp:model.H(v.Temp));
            var rig=new DistillationAssembly(s){Cooling=true,UseVesselThermalStorage=false,WaterFlowGramsPerSecond=flow};rig.Configure(reflux,r,out _);rig.Start(out _);
            Check("default transient enabled "+id+" "+reflux+" "+flow,rig.UseTransientCondenser);
            double before=H(s)+H(r);for(int n=0;n<(int)Math.Round(120/step);n++)Lab.Tick(step);
            string label=$"{id} reflux={reflux} flow={flow} step={step}";
            Check(label+" runs with wall storage",rig.Running&&rig.CondenserWallC>15&&rig.CondenserStoredEnergyJ>0);
            double residual=H(s)+H(r)+rig.CoolantEnergyJ+rig.CondenserStoredEnergyJ+rig.CondenserAmbientEnergyJ+rig.EscapedEnergyJ-before-rig.HeaterEnergyJ;
            lines.Add($"METRIC {label} energyResidualJ={residual:G9}");
            lines.Add($"ENDPOINT {label} wall={rig.CondenserWallC:G12} water={rig.CondenserWaterC:G12} collected={rig.CollectedMl:G12} returned={rig.ReturnedMl:G12}");
            string key=$"{id} reflux={reflux} flow={flow}";
            if(!endpoints.ContainsKey(key))endpoints[key]=new List<double[]>();
            endpoints[key].Add(new[]{rig.CondenserWallC,rig.CondenserWaterC,rig.CollectedMl,rig.ReturnedMl});
            Check(label+" energy",Math.Abs(residual)<.02);
            Check(label+" mass",Math.Abs(M(s)+M(r)+rig.EscapedGrams-60)<1e-8);
            double wall=rig.CondenserWallC,stored=rig.CondenserStoredEnergyJ;
            using(var transaction=new PourTransaction(s,r)){rig.Tick(10);}
            Check(label+" pause preserves storage",rig.CondenserWallC==wall&&rig.CondenserStoredEnergyJ==stored);
            rig.Stop();double hot=rig.HeaterEnergyJ,m=M(s)+M(r),sum=rig.CondenserStoredEnergyJ+rig.CoolantEnergyJ+rig.CondenserAmbientEnergyJ;
            rig.Tick(1);
            Check(label+" stopped residual heat balance",rig.HeaterEnergyJ==hot&&M(s)+M(r)==m&&Math.Abs(rig.CondenserStoredEnergyJ+rig.CoolantEnergyJ+rig.CondenserAmbientEnergyJ-sum)<1e-7);
            rig.Detach();
        }
        foreach(var pair in endpoints)for(int component=0;component<4;component++){
            var values=pair.Value;int last=values.Count-1;
            double coarse=Math.Abs(values[last-2][component]-values[last-1][component]),fine=Math.Abs(values[last-1][component]-values[last][component]);
            lines.Add($"CONVERGENCE {pair.Key} component={component} coarseDelta={coarse:G9} fineDelta={fine:G9}");
            Check(pair.Key+" timestep convergence component="+component,fine<coarse*.4+1e-5);
        }
        Lab.Reset();lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/runtime-storage-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
