using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class EthanolCondenserAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string s,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+s);}
        double Mass(Vessel v)=>v.Solutes.TryGetValue("c2h5oh",out double n)?n*ChemData.Sub["c2h5oh"].Mass:0;
        double H(Vessel v){EthanolThermodynamics.TryLiquidEnthalpy(v.Temp,out double h);return Mass(v)*h;}
        Vessel Make(VesselKind kind,double grams,float t){var v=Lab.AddVessel(kind);v.Water=0;v.Temp=t;if(grams>0)v.Solutes["c2h5oh"]=grams/ChemData.Sub["c2h5oh"].Mass;return v;}
        foreach(bool reflux in new[]{false,true})foreach(double flow in new[]{0d,.005,10})foreach(float step in new[]{1f,.1f,.01f}){
            Lab.Reset();ApparatusSet.Clear();
            var s=Make(VesselKind.RoundBottom,50,75);var r=Make(VesselKind.Flask,10,25);
            var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.Lit=true;burner.GasFlow=.5;ApparatusSet.Attach(s,burner);
            var rig=new DistillationAssembly(s){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true,WaterFlowGramsPerSecond=flow};rig.Configure(reflux,r,out _);rig.Start(out _);
            double initial=H(s)+H(r),mass=Mass(s)+Mass(r),elapsed=0;
            for(int i=0;i<(int)Math.Round(30/step);i++){Lab.Tick(step);ApparatusSet.Tick(step);elapsed+=step;}
            double residual=H(s)+H(r)+rig.CoolantEnergyJ+rig.EscapedEnergyJ-initial-rig.HeaterEnergyJ;
            string key=$"reflux={reflux} flow={flow} step={step}";
            lines.Add($"METRIC {key} residualJ={residual:R}");
            Check(key+" mass",Math.Abs(Mass(s)+Mass(r)+rig.EscapedEthanolGrams-mass)<1e-7);
            Check(key+" energy",Math.Abs(residual)<.01);
            Check(key+" supply not doubled",Math.Abs(rig.HeaterEnergyJ-250*elapsed)<1e-6);
            Check(key+" bounded liquid",s.Temp<=78.35&&s.Gases.Count==0&&s.Undissolved.Count==0);
            Check(key+" actual vapor",rig.HasThermalResult&&(flow==0?rig.EscapedEthanolGrams>0:(reflux?rig.ReturnedMl>0:rig.CollectedMl>0)));
            if(flow==10)Check(key+" no escape with adequate cooling",rig.EscapedEthanolGrams<1e-8);
            rig.Detach();
        }
        Lab.Reset();ApparatusSet.Clear();
        var source=Make(VesselKind.RoundBottom,10,78.35f);var receiver=Make(VesselKind.Flask,0,25);
        var test=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};test.Configure(false,receiver,out _);test.Start(out _);
        double capacity=Math.Min(receiver.Capacity,receiver.InteriorCapacityMl);
        receiver.Solutes["c2h5oh"]=(capacity-.1)*PhaseProperties.Density("c2h5oh")/ChemData.Sub["c2h5oh"].Mass;
        double before=H(source)+H(receiver),totalMass=Mass(source)+Mass(receiver);test.Tick(100);
        Check("capacity hard stop",!test.Running&&receiver.Volume()<=capacity+1e-8&&test.UnusedHeaterEnergyJ>0);
        Check("capacity balance",Math.Abs(Mass(source)+Mass(receiver)+test.EscapedEthanolGrams-totalMass)<1e-8&&Math.Abs(H(source)+H(receiver)+test.CoolantEnergyJ+test.EscapedEnergyJ-before-test.HeaterEnergyJ)<.01);
        test.Detach();
        Lab.Reset();source=Make(VesselKind.RoundBottom,.01,78.35f);receiver=Make(VesselKind.Flask,0,25);
        test=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};test.Configure(false,receiver,out _);test.Start(out _);test.Tick(100);
        Check("dry boiler stops",!test.Running&&Mass(source)<1e-10&&Math.Abs(Mass(receiver)+test.EscapedEthanolGrams-.01)<1e-8);
        test.Detach();
        Lab.Reset();source=Make(VesselKind.RoundBottom,10,25);receiver=Make(VesselKind.Flask,0,25);receiver.Water=1;
        test=new DistillationAssembly(source){UseTransientCondenser=false,UseVesselThermalStorage=false,Cooling=true};test.Configure(false,receiver,out _);test.Start(out _);test.Tick(1);
        Check("unsupported mixed receiver preserves stock",!test.Running&&Mass(source)==10&&receiver.Water==1&&test.HeaterEnergyJ==0);
        test.Detach();Lab.Reset();ApparatusSet.Clear();
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/ethanol-condenser-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
