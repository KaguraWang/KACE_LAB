using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class RuntimeWallBoilingAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        foreach(string id in new[]{"ch3oh","c2h5oh"})foreach(bool reflux in new[]{false,true})foreach(double power in new[]{0d,250})foreach(double mass in new[]{.01,10d}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            var model=PureLiquidThermalModel.For(id);double molar=ChemData.Sub[id].Mass;
            s.Water=r.Water=0;s.Temp=(float)(model.BoilingC-.001);s.GlassTemperatureC=100;r.Temp=25;
            s.Solutes[id]=mass/molar;
            double M(Vessel v)=>v.Solutes.TryGetValue(id,out var n)?n*molar:0;
            double H(Vessel v)=>M(v)*model.H(v.Temp);
            var rig=new DistillationAssembly(s){Cooling=true,HeaterPowerWatts=power};rig.Configure(reflux,r,out _);rig.Start(out _);
            double before=H(s)+H(r);rig.Tick(1);
            string label=$"{id} reflux={reflux} power={power} mass={mass}";
            double total=H(s)+H(r)+rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ+rig.CondenserStoredEnergyJ+rig.CondenserAmbientEnergyJ+rig.CoolantEnergyJ+rig.EscapedEnergyJ;
            lines.Add($"METRIC {label} residual={total-before-rig.HeaterEnergyJ:G12} transferred={rig.CollectedMl+rig.ReturnedMl:G12} escaped={rig.EscapedGrams:G12}");
            Check(label+" wall vapor delivered",rig.HasThermalResult&&(rig.CollectedMl+rig.ReturnedMl)>0&&s.GlassTemperatureC<100);
            Check(label+" mass",Math.Abs(M(s)+M(r)+rig.EscapedGrams-mass)<1e-8);
            Check(label+" energy",Math.Abs(total-before-rig.HeaterEnergyJ)<.002);
            Check(label+" condenser advances once",Math.Abs(rig.SimulatedCondenserSeconds-1)<1e-12);
            if(power==0)Check(label+" no fictitious heater energy",rig.HeaterEnergyJ==0);
            // Full initial dry-out must preserve the original alcohol identity.
            if(mass==.01&&!reflux)Check(label+" dry source still delivers alcohol",M(s)<1e-10&&M(r)>0&&r.Water==0);
            rig.Detach();
        }
        foreach(string id in new[]{"ch3oh","c2h5oh"})foreach(string mode in new[]{"stopped_source","hot_receiver","full_receiver","failed_condenser"}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            var model=PureLiquidThermalModel.For(id);double molar=ChemData.Sub[id].Mass;
            s.Water=r.Water=0;s.Temp=r.Temp=(float)(model.BoilingC-.001);s.Solutes[id]=1/molar;r.Solutes[id]=1/molar;
            s.GlassTemperatureC=mode=="hot_receiver"?25:100;r.GlassTemperatureC=mode=="hot_receiver"?100:25;
            if(mode=="full_receiver")r.Solutes[id]=r.Capacity*PhaseProperties.Density(id,25)/molar;
            double M(Vessel v)=>v.Solutes.TryGetValue(id,out var n)?n*molar:0;
            double H(Vessel v)=>M(v)*model.H(v.Temp);
            var rig=new DistillationAssembly(s){Cooling=true,HeaterPowerWatts=0};rig.Configure(false,r,out _);
            if(mode!="stopped_source")rig.Start(out _);
            // Finite input that overflows the condenser's doubled contact K:
            // a failed downstream solve must still account for withdrawn vapor.
            if(mode=="failed_condenser")rig.ConductanceWPerK=double.MaxValue;
            double before=H(s)+H(r),mass=M(s)+M(r);rig.Tick(1);
            double total=H(s)+H(r)+rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ+rig.CondenserStoredEnergyJ+rig.CondenserAmbientEnergyJ+rig.CoolantEnergyJ+rig.EscapedEnergyJ;
            string label=id+" "+mode;
            Check(label+" residual vapor vents",rig.EscapedGrams>0&&rig.HeaterEnergyJ==0);
            Check(label+" inventory preserved",Math.Abs(M(s)+M(r)+rig.EscapedGrams-mass)<1e-8&&r.Volume()<=r.Capacity+1e-8);
            Check(label+" total energy",Math.Abs(total-before)<.002);
            rig.Detach();
        }
        var endpoints=new Dictionary<string,List<double[]>>();
        foreach(string id in new[]{"ch3oh","c2h5oh"})foreach(bool reflux in new[]{false,true})foreach(float step in new[]{1f,.2f,.05f}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            var model=PureLiquidThermalModel.For(id);double molar=ChemData.Sub[id].Mass;
            s.Water=r.Water=0;s.Temp=(float)(model.BoilingC-.001);s.GlassTemperatureC=100;r.Temp=25;s.Solutes[id]=10/molar;
            double M(Vessel v)=>v.Solutes.TryGetValue(id,out var n)?n*molar:0;
            double H(Vessel v)=>M(v)*model.H(v.Temp);
            var rig=new DistillationAssembly(s){Cooling=true,HeaterPowerWatts=0};rig.Configure(reflux,r,out _);rig.Start(out _);
            double before=H(s)+H(r);int count=(int)Math.Round(30/step);
            for(int i=0;i<count;i++)Lab.Tick(step);
            double total=H(s)+H(r)+rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ+rig.CondenserStoredEnergyJ+rig.CondenserAmbientEnergyJ+rig.CoolantEnergyJ+rig.EscapedEnergyJ;
            string label=$"main loop {id} reflux={reflux} step={step}";
            Check(label+" residual heat transported",rig.CollectedMl+rig.ReturnedMl>0&&rig.HeaterEnergyJ==0);
            Check(label+" energy",Math.Abs(total-before)<.002);
            Check(label+" mass",Math.Abs(M(s)+M(r)+rig.EscapedGrams-10)<1e-8);
            Check(label+" one condenser interval per tick",Math.Abs(rig.SimulatedCondenserSeconds-count*(double)step)<1e-7);
            string key=id+" reflux="+reflux;
            if(!endpoints.ContainsKey(key))endpoints[key]=new List<double[]>();
            endpoints[key].Add(new[]{s.GlassTemperatureC,(double)s.Temp,rig.CollectedMl+rig.ReturnedMl});
            double wall=s.GlassTemperatureC,seconds=rig.SimulatedCondenserSeconds,escaped=rig.EscapedGrams;
            using(var pour=new PourTransaction(s,r)){Lab.Tick(10);}
            Check(label+" pause freezes wall vapor and clock",s.GlassTemperatureC==wall&&rig.SimulatedCondenserSeconds==seconds&&rig.EscapedGrams==escaped);
            rig.Detach();
        }
        foreach(var pair in endpoints)for(int component=0;component<3;component++){
            var e=pair.Value;double coarse=Math.Abs(e[0][component]-e[1][component]),fine=Math.Abs(e[1][component]-e[2][component]);
            lines.Add($"CONVERGENCE {pair.Key} component={component} coarse={coarse:G12} fine={fine:G12}");
            Check(pair.Key+" coupled phase convergence "+component,fine<coarse*.4+1e-5);
        }
        Lab.Reset();lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/runtime-wall-boiling-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
