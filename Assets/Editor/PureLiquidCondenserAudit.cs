using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class PureLiquidCondenserAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        var model=PureLiquidThermalModel.Methanol;string id=model.Id;double molarMass=ChemData.Sub[id].Mass;
        double Mass(Vessel v)=>v.Solutes.TryGetValue(id,out double n)?n*molarMass:0;
        double Energy(Vessel v)=>Mass(v)*model.H(v.Temp);
        Check("NIST latent conversion",Math.Abs(model.LatentJPerGram*32.0419-35210)<1e-8);
        Check("NIST heat capacity conversion",Math.Abs((model.H(26)-model.H(25))*32.0419-81.11)<1e-8);
        Check("property range and no family inference",!model.Valid(-1)&&!model.Valid(70)&&PureLiquidThermalModel.For("propan_1_ol")==null);
        foreach(bool reflux in new[]{false,true})foreach(double flow in new[]{0d,.005,10})foreach(float step in new[]{1f,.1f,.01f}){
            Lab.Reset();ApparatusSet.Clear();
            var s=Lab.AddVessel(VesselKind.RoundBottom);s.Water=0;s.Temp=60;s.Solutes[id]=50/molarMass;
            var r=Lab.AddVessel(VesselKind.Flask);r.Water=0;r.Temp=25;r.Solutes[id]=10/molarMass;
            var rig=new DistillationAssembly(s){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true,WaterFlowGramsPerSecond=flow};rig.Configure(reflux,r,out _);rig.Start(out _);
            double before=Energy(s)+Energy(r),elapsed=0;
            for(int i=0;i<(int)Math.Round(30/step);i++){Lab.Tick(step);elapsed+=step;}
            double residual=Energy(s)+Energy(r)+rig.CoolantEnergyJ+rig.EscapedEnergyJ-before-rig.HeaterEnergyJ;
            string key=$"reflux={reflux} flow={flow} step={step}";
            lines.Add($"METRIC {key} energyResidualJ={residual:R}");
            Check(key+" mass",Math.Abs(Mass(s)+Mass(r)+rig.EscapedMethanolGrams-60)<1e-7);
            Check(key+" energy",Math.Abs(residual)<.01);
            Check(key+" actual energy flow",rig.HasThermalResult&&Math.Abs(rig.HeaterEnergyJ-250*elapsed)<1e-6);
            Check(key+" phase engine cannot preempt latent heat",s.Gases.Count==0&&s.Temp<model.BoilingC&&rig.Running);
            Check(key+" species ledger",rig.EscapedEthanolGrams==0&&rig.EscapedWaterGrams==0&&rig.EscapedGrams==rig.EscapedMethanolGrams);
            rig.Detach();
        }
        Lab.Reset();
        var hot=Lab.AddVessel(VesselKind.Beaker);hot.Water=0;hot.Temp=60;hot.Solutes[id]=10/molarMass;
        var cold=Lab.AddVessel(VesselKind.Beaker);cold.Water=0;cold.Temp=20;cold.Solutes[id]=10/molarMass;
        using(var pour=new PourTransaction(hot,cold)){
            Check("methanol pour uses enthalpy",pour.Preview(.5,0,out _)&&Math.Abs(cold.Temp-100d/3)<1e-4);
        }
        Check("methanol undo restores thermal inventory",hot.Temp==60&&cold.Temp==20&&Math.Abs(Mass(hot)-10)<1e-8);
        Lab.Reset();ApparatusSet.Clear();
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/pure-liquid-condenser-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
