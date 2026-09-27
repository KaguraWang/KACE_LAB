using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class WaterCondenserAudit {
    static void AuditPause(Action<string,bool> check){
        double H(Vessel v)=>v.Water*ThermalMixing.WaterCpJPerGramK*v.Temp;
        foreach(bool reflux in new[]{false,true})foreach(float temperature in new[]{25f,100f})foreach(bool commit in new[]{false,true}){
            Lab.Reset();ApparatusSet.Clear();
            string label=$"pause/resume reflux={reflux} T={temperature} commit={commit}";
            var source=Lab.AddVessel(VesselKind.RoundBottom);source.Water=50;source.Temp=temperature;
            var receiver=Lab.AddVessel(VesselKind.Flask);receiver.Water=10;receiver.Temp=25;
            var pourTarget=Lab.AddVessel(VesselKind.Beaker);pourTarget.Water=10;pourTarget.Temp=25;
            var unrelated=Lab.AddVessel(VesselKind.Beaker);unrelated.Water=20;unrelated.Temp=25;
            var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.Lit=true;ApparatusSet.Attach(unrelated,burner);
            var rig=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true,WaterFlowGramsPerSecond=.005};
            rig.Configure(reflux,receiver,out _);rig.Start(out _);rig.Tick(.1);
            double originalWater=source.Water;float originalTemp=source.Temp;
            double heat=rig.HeaterEnergyJ,cooling=rig.CoolantEnergyJ,escaped=rig.EscapedWaterGrams,escapedHeat=rig.EscapedEnergyJ;
            double collected=rig.CollectedMl,returned=rig.ReturnedMl,unused=rig.UnusedHeaterEnergyJ;
            float clock=Lab.Clock;
            using(var transaction=new PourTransaction(source,pourTarget)){
                check(label+" preview accepted",transaction.Preview(.2,0,out _));
                double water=source.Water,targetWater=pourTarget.Water,receiverWater=receiver.Water;
                float targetTemp=pourTarget.Temp;
                for(int i=0;i<50;i++){Lab.Tick(.1f);ApparatusSet.Tick(.1f);rig.Tick(.1);}
                check(label+" all thermal ledgers frozen",rig.HeaterEnergyJ==heat&&rig.CoolantEnergyJ==cooling&&rig.EscapedWaterGrams==escaped&&rig.EscapedEnergyJ==escapedHeat&&rig.CollectedMl==collected&&rig.ReturnedMl==returned&&rig.UnusedHeaterEnergyJ==unused);
                check(label+" inventories temperatures and clock frozen",source.Water==water&&pourTarget.Water==targetWater&&receiver.Water==receiverWater&&source.Temp==originalTemp&&pourTarget.Temp==targetTemp&&Lab.Clock==clock);
                check(label+" unrelated burner also frozen",unrelated.Temp==25);
                if(commit)transaction.Commit();else transaction.Undo();
            }
            check(label+" pause released and run state preserved",!PourTransaction.IsActive&&rig.Running);
            check(label+" transfer or undo respected",Math.Abs(source.Water-originalWater*(commit?.8:1))<1e-9);
            double initial=H(source)+H(receiver),mass=source.Water+receiver.Water;
            heat=rig.HeaterEnergyJ;cooling=rig.CoolantEnergyJ;escaped=rig.EscapedWaterGrams;escapedHeat=rig.EscapedEnergyJ;
            rig.Tick(.1);
            double residual=H(source)+H(receiver)-initial+rig.CoolantEnergyJ-cooling+rig.EscapedEnergyJ-escapedHeat-(rig.HeaterEnergyJ-heat);
            check(label+" resumed exactly one step of supply",Math.Abs(rig.HeaterEnergyJ-heat-25)<1e-8);
            check(label+" resumed energy and mass conservation",Math.Abs(residual)<.005&&Math.Abs(source.Water+receiver.Water+rig.EscapedWaterGrams-escaped-mass)<1e-8);
            rig.Detach();
        }
        Lab.Reset();ApparatusSet.Clear();
    }
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        double H(Vessel v)=>v.Water*ThermalMixing.WaterCpJPerGramK*v.Temp;
        foreach(bool reflux in new[]{false,true})foreach(double coolant in new[]{0d,.005,1,10}){
            var source=new Vessel(VesselKind.RoundBottom,Vector3.zero){Water=50,Temp=100};
            var receiver=new Vessel(VesselKind.Flask,Vector3.right){Water=10,Temp=25};
            var rig=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true,WaterFlowGramsPerSecond=coolant};
            rig.Configure(reflux,receiver,out _);rig.Start(out _);
            double initial=H(source)+H(receiver),mass=source.Water+receiver.Water;
            rig.Tick(1);
            double residual=H(source)+H(receiver)+rig.CoolantEnergyJ+rig.EscapedEnergyJ-initial-rig.HeaterEnergyJ;
            lines.Add($"METRIC reflux={reflux} coolant={coolant} energyResidualJ={residual:G9}");
            Check($"mass conservation reflux={reflux} coolant={coolant}",Math.Abs(source.Water+receiver.Water+rig.EscapedWaterGrams-mass)<1e-8);
            Check($"energy conservation reflux={reflux} coolant={coolant}",Math.Abs(residual)<.005);
            Check($"heater drives vapor generation reflux={reflux} coolant={coolant}",Math.Abs(rig.HeaterEnergyJ-250)<1e-8);
            if(coolant==0)Check("zero water flow cannot silently capture vapor",rig.EscapedWaterGrams>0&&rig.LastThermalResult.CondensedKgPerSecond==0&&rig.CoolantEnergyJ==0);
            if(coolant==10)Check("adequate cooling retains all generated vapor",rig.EscapedWaterGrams<1e-10&&rig.LastThermalResult.CondensedKgPerSecond>0);
            rig.Detach();
        }
        var warming=new Vessel(VesselKind.RoundBottom,Vector3.zero){Water=50,Temp=25};
        var target=new Vessel(VesselKind.Flask,Vector3.right);
        var warmRig=new DistillationAssembly(warming){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};warmRig.Configure(false,target,out _);warmRig.Start(out _);warmRig.Tick(1);
        Check("sub-boiling heater power raises sensible heat without distillate",Math.Abs(warming.Temp-(25+250/(50*4.18)))<1e-5&&target.Water==0&&warmRig.HeaterEnergyJ==250);
        warming.Temp=40;warming.Water=60;warmRig.Tick(1);
        Check("thermal precision cache respects external source temperature and mass edits",Math.Abs(warming.Temp-(40+250/(60*4.18)))<1e-5);
        warming.Temp=100;warmRig.HeaterPowerWatts=0;warmRig.Tick(1);
        Check("zero power cannot create steam at boiling point",target.Water==0&&!warmRig.HasThermalResult);
        warmRig.HeaterPowerWatts=250;target.Water=target.Capacity-.001;warmRig.Tick(1);
        Check("receiver capacity stops heater without deleting water",Math.Abs(target.Water-target.Capacity)<1e-8&&!warmRig.Running&&warmRig.UnusedHeaterEnergyJ>200);
        warmRig.Detach();
        var externalSource=new Vessel(VesselKind.RoundBottom,Vector3.zero){Water=50,Temp=100};
        var externalReceiver=new Vessel(VesselKind.Flask,Vector3.right){Water=10,Temp=25};
        var externalRig=new DistillationAssembly(externalSource){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};externalRig.Configure(false,externalReceiver,out _);externalRig.Start(out _);externalRig.Tick(.2);
        externalReceiver.Water=20;externalReceiver.Temp=60;double firstCollected=externalRig.CollectedMl;externalRig.Tick(.2);
        double added=externalRig.CollectedMl-firstCollected;
        Check("thermal precision cache respects externally changed receiver",Math.Abs(externalReceiver.Temp-(20*60+added*externalRig.LastThermalResult.ProductOutletC)/(20+added))<1e-5);
        externalRig.Detach();
        Lab.Reset();var live=Lab.AddVessel(VesselKind.RoundBottom);var liveTarget=Lab.AddVessel(VesselKind.Flask);
        live.Water=50;live.Temp=25;var running=new DistillationAssembly(live){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};running.Configure(false,liveTarget,out _);running.Start(out _);
        double before=H(live);Lab.Tick(.1f);
        Check("live tick does not add legacy degrees-per-second heating",Math.Abs(H(live)-before-running.HeaterEnergyJ)<.005&&liveTarget.Water==0);
        running.Detach();Lab.Reset();
        foreach(bool reflux in new[]{false,true})foreach(double coolant in new[]{.005,10d}){
            double minTransfer=double.PositiveInfinity,maxTransfer=0;
            foreach(float step in new[]{1f,.1f,.01f}){
                Lab.Reset();ApparatusSet.Clear();
                var boiler=Lab.AddVessel(VesselKind.RoundBottom);var catchFlask=Lab.AddVessel(VesselKind.Flask);
                boiler.Water=50;boiler.Temp=25;catchFlask.Water=10;catchFlask.Temp=25;
                var continuous=new DistillationAssembly(boiler){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true,WaterFlowGramsPerSecond=coolant};
                continuous.Configure(reflux,catchFlask,out _);continuous.Start(out _);
                var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.Lit=true;ApparatusSet.Attach(boiler,burner);
                double initial=H(boiler)+H(catchFlask);int count=(int)Math.Round(120d/step);
                for(int i=0;i<count;i++){ApparatusSet.Tick(step);Lab.Tick(step);}
                double residual=H(boiler)+H(catchFlask)+continuous.CoolantEnergyJ+continuous.EscapedEnergyJ-initial-continuous.HeaterEnergyJ;
                double moved=continuous.CollectedMl+continuous.ReturnedMl;
                minTransfer=Math.Min(minTransfer,moved);maxTransfer=Math.Max(maxTransfer,moved);
                lines.Add($"METRIC 120s reflux={reflux} coolant={coolant} dt={step} energyResidualJ={residual:G9} transferMl={moved:G9}");
                Check($"120s energy balance with attached burner {reflux}/{coolant}/{step}",Math.Abs(residual)<.5);
                Check($"120s mass balance {reflux}/{coolant}/{step}",Math.Abs(boiler.Water+catchFlask.Water+continuous.EscapedWaterGrams-60)<1e-7);
                Check($"burner does not duplicate configured power {reflux}/{coolant}/{step}",Math.Abs(continuous.HeaterEnergyJ-250d*count*step)<.001);
                continuous.Detach();
            }
            Check($"transfer converges across 1s to 10ms steps {reflux}/{coolant}",maxTransfer-minTransfer<.03);
        }
        Lab.Reset();ApparatusSet.Clear();
        AuditPause(Check);
        foreach(double flow in new[]{0d,.1,.25,.5,1})foreach(bool lit in new[]{false,true}){
            Lab.Reset();ApparatusSet.Clear();
            var source=Lab.AddVessel(VesselKind.RoundBottom);source.Water=50;source.Temp=25;
            var receiver=Lab.AddVessel(VesselKind.Flask);
            var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.GasFlow=flow;burner.Lit=lit;ApparatusSet.Attach(source,burner);
            var rig=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};rig.Configure(false,receiver,out _);rig.Start(out _);
            double expected=lit&&flow>=.12?250*flow/.5:0,initial=H(source);
            ApparatusSet.Tick(.1f);Lab.Tick(.1f);
            Check($"burner control changes net thermal supply {flow}/{lit}",Math.Abs(rig.HeaterEnergyJ-expected*.1)<1e-5&&Math.Abs(H(source)-initial-rig.HeaterEnergyJ)<.005);
            rig.Detach();
        }
        Lab.Reset();ApparatusSet.Clear();
        foreach(double flow in new[]{0d,.1,.25,.5,1})foreach(bool lit in new[]{false,true}){
            Lab.Reset();ApparatusSet.Clear();
            var source=Lab.AddVessel(VesselKind.RoundBottom);source.Water=50;source.Temp=25;
            var receiver=Lab.AddVessel(VesselKind.Flask);
            var tripod=ApparatusSet.Add(ApparatusKind.Tripod,Vector3.zero);source.TripodRef=tripod;tripod.Holds=source;
            source.Pos=Vector3.zero;source.LiftY=ApparatusDimensions.TripodHeight;
            var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.GasFlow=flow;burner.Lit=lit;
            var rig=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};rig.Configure(false,receiver,out _);rig.Start(out _);
            bool reaches=lit&&flow>=.12&&source.LiftY<=ApparatusDimensions.BurnerHeight+burner.FlameHeight+.1f;
            double expected=reaches?250*flow/.5:0,initial=H(source);
            ApparatusSet.Tick(.1f);Lab.Tick(.1f);
            Check($"tripod burner controls thermal supply {flow}/{lit}",Math.Abs(rig.HeaterEnergyJ-expected*.1)<1e-5&&Math.Abs(H(source)-initial-rig.HeaterEnergyJ)<.005);
            burner.Pos=Vector3.right;double heatBeforeMove=rig.HeaterEnergyJ;rig.Tick(.1);
            Check($"moving burner out from tripod stops supply {flow}/{lit}",rig.HeaterEnergyJ==heatBeforeMove);
            rig.Detach();
        }
        Lab.Reset();ApparatusSet.Clear();
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/water-condenser-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
