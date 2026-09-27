using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class RuntimeVesselStorageAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        var endpoints=new Dictionary<string,List<double[]>>();
        foreach(string id in new[]{"h2o","ch3oh","c2h5oh"})foreach(bool reflux in new[]{false,true})foreach(float step in new[]{1f,.2f,.05f}){
            Lab.Reset();ApparatusSet.Clear();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            var model=PureLiquidThermalModel.For(id);double molar=ChemData.Sub[id].Mass;
            s.Water=r.Water=0;s.Temp=(float)(model==null?99:model.BoilingC-1);r.Temp=25;
            if(id=="h2o"){s.Water=50;r.Water=10;}else{s.Solutes[id]=50/molar;r.Solutes[id]=10/molar;}
            double M(Vessel v)=>id=="h2o"?v.Water:(v.Solutes.TryGetValue(id,out var n)?n*molar:0);
            double H(Vessel v)=>M(v)*(model==null?4.18*v.Temp:model.H(v.Temp));
            var rig=new DistillationAssembly(s){Cooling=true};rig.Configure(reflux,r,out _);rig.Start(out _);
            Check("default vessel inertia "+id+" "+reflux+" "+step,rig.UseVesselThermalStorage);
            double before=H(s)+H(r);
            double Total()=>H(s)+H(r)+rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ+rig.CondenserStoredEnergyJ+rig.CondenserAmbientEnergyJ+rig.CoolantEnergyJ+rig.EscapedEnergyJ;
            for(int i=0;i<(int)Math.Round(120/step);i++)Lab.Tick(step);
            string key=id+" reflux="+reflux;
            string label=key+" step="+step;
            if(!endpoints.ContainsKey(key))endpoints[key]=new List<double[]>();
            endpoints[key].Add(new[]{s.GlassTemperatureC,r.GlassTemperatureC,rig.CollectedMl+rig.ReturnedMl});
            lines.Add($"METRIC {label} residualJ={Total()-before-rig.HeaterEnergyJ:G12} sourceWallC={s.GlassTemperatureC:G12} receiverWallC={r.GlassTemperatureC:G12}");
            Check(label+" transfers with glass heating",rig.Running&&s.GlassTemperatureC>25&&rig.VesselStoredEnergyJ>0&&(rig.CollectedMl+rig.ReturnedMl)>0);
            Check(label+" total energy",Math.Abs(Total()-before-rig.HeaterEnergyJ)<.02);
            Check(label+" mass",Math.Abs(M(s)+M(r)+rig.EscapedGrams-60)<1e-8);
            double sourceWall=s.GlassTemperatureC,receiverWall=r.GlassTemperatureC,stored=rig.VesselStoredEnergyJ;
            using(var pour=new PourTransaction(s,r)){Lab.Tick(10);}
            Check(label+" paused walls",s.GlassTemperatureC==sourceWall&&r.GlassTemperatureC==receiverWall&&rig.VesselStoredEnergyJ==stored);
            rig.Stop();double stoppedTotal=Total(),heater=rig.HeaterEnergyJ;
            Lab.Tick(1);
            Check(label+" stopped residual heat conserved",Math.Abs(Total()-stoppedTotal)<.02&&rig.HeaterEnergyJ==heater);
            double wallAfterStop=s.GlassTemperatureC;rig.Detach();
            var next=new DistillationAssembly(s);next.Configure(reflux,r,out _);
            Check(label+" reconnect retains physical wall state",s.GlassTemperatureC==wallAfterStop&&next.VesselStoredEnergyJ==0);
            next.Detach();
        }
        foreach(var pair in endpoints)for(int component=0;component<3;component++){
            var e=pair.Value;double coarse=Math.Abs(e[0][component]-e[1][component]),fine=Math.Abs(e[1][component]-e[2][component]);
            lines.Add($"CONVERGENCE {pair.Key} component={component} coarse={coarse:G12} fine={fine:G12}");
            Check(pair.Key+" convergence "+component,fine<coarse*.4+1e-5);
        }
        foreach(string invalid in new[]{"wall_nan","ceramic","unknown_liquid","negative_contact","mismatched_liquid","invalid_heater"}){
            Lab.Reset();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(invalid=="ceramic"?VesselKind.Crucible:VesselKind.Flask);
            s.Water=50;s.Temp=80;r.Water=10;r.Temp=25;
            if(invalid=="wall_nan")r.GlassTemperatureC=double.NaN;
            if(invalid=="unknown_liquid")r.Solutes["nacl"]=.01;
            if(invalid=="mismatched_liquid"){r.Water=0;r.Solutes["c2h5oh"]=.1;}
            var rig=new DistillationAssembly(s){Cooling=true,UseVesselThermalStorage=true};rig.Configure(false,r,out _);rig.Start(out _);
            if(invalid=="negative_contact")rig.VesselContactWPerK=-1;
            if(invalid=="invalid_heater")rig.HeaterPowerWatts=double.NaN;
            rig.Tick(1);
            Check(invalid+" rejected",!rig.Running);
            Check(invalid+" no partial source commit",s.Temp==80&&s.GlassTemperatureC==25&&s.Water==50&&rig.VesselStoredEnergyJ==0&&rig.VesselAmbientEnergyJ==0&&rig.HeaterEnergyJ==0);
            rig.Detach();
        }
        foreach(string id in new[]{"ch3oh","c2h5oh"}){
            Lab.Reset();var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            s.Water=r.Water=0;s.Temp=25;s.GlassTemperatureC=60;r.Temp=25;r.Solutes[id]=.1;
            var rig=new DistillationAssembly(s){Cooling=true};rig.Configure(false,r,out _);
            Lab.Tick(1);
            Check(id+" dry source retains passive wall cooling",s.Water==0&&s.GlassTemperatureC<60&&s.GlassTemperatureC>25&&rig.HeaterEnergyJ==0);
            Check(id+" dry source wall budget",Math.Abs(rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ)<1e-8);
            rig.Detach();
        }
        Lab.Reset();{
            var s=Lab.AddVessel(VesselKind.RoundBottom);var r=Lab.AddVessel(VesselKind.Flask);
            s.Water=10;r.Water=0;s.Temp=r.Temp=25;r.GlassTemperatureC=80;
            var rig=new DistillationAssembly(s){Cooling=true,UseVesselThermalStorage=true};rig.Configure(false,r,out _);
            rig.Tick(1);
            Check("empty receiver wall cools without inventing liquid",r.Water==0&&r.GlassTemperatureC<80&&r.GlassTemperatureC>25);
            Check("empty receiver environmental energy",Math.Abs(rig.VesselStoredEnergyJ+rig.VesselAmbientEnergyJ)<1e-8);
            rig.Detach();
        }
        Lab.Reset();lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/runtime-vessel-storage-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
