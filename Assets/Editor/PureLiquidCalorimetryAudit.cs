using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class PureLiquidCalorimetryAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        foreach(var model in new[]{PureLiquidThermalModel.Ethanol,PureLiquidThermalModel.Methanol}){
            foreach(float initial in new[]{10f,25f,40f,55f})foreach(double delta in new[]{-5d,2,8}){
                var v=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=initial};v.Solutes[model.Id]=20/ChemData.Sub[model.Id].Mass;
                double expected=20*(model.H(initial+delta)-model.H(initial));
                double q=Lab.EnergyFromTemp(v,delta);
                string key=$"{model.Id} T={initial} dT={delta}";
                Check(key+" integrated energy",Math.Abs(q-expected)<1e-8&&v.Temp==initial);
                double dh=Lab.EnthalpyFromTemp(v,delta,.01);
                Lab.ApplyReactionHeat(v,.01,dh);
                Check(key+" forward inverse agree",Math.Abs(v.Temp-initial-delta)<1e-4&&Math.Abs(v.Mass()-20)<1e-8);
                Check(key+" local capacity matches derivative",ThermalMixing.TryHeatCapacity(v,out double capacity,out _)&&Math.Abs(capacity-20*model.HeatCapacity(v.Temp))<1e-8);
            }
            var bounded=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};bounded.Solutes[model.Id]=20/ChemData.Sub[model.Id].Mass;
            Lab.ApplyReactionHeat(bounded,1,-1e6);
            Check(model.Id+" phase crossing rejected without inventory mutation",bounded.Temp==25&&Math.Abs(bounded.Mass()-20)<1e-8&&bounded.PhaseNotice.Contains("相变"));
            Check(model.Id+" calorimetry cannot cross boiling or freeze range",double.IsNaN(Lab.EnergyFromTemp(bounded,100))&&double.IsNaN(Lab.EnergyFromTemp(bounded,-100)));
            bounded.PressureBar=2;
            Lab.ApplyReactionHeat(bounded,.01,-1);
            Check(model.Id+" pressure guard",bounded.Temp==25&&double.IsNaN(Lab.EnergyFromTemp(bounded,1)));
            bounded.PressureBar=1.01325;
            Lab.ApplyReactionHeat(bounded,double.MaxValue,-double.MaxValue);
            Check(model.Id+" overflow guarded",bounded.Temp==25);
        }
        var water=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        Check("aqueous energy path unchanged",Math.Abs(Lab.EnergyFromTemp(water,10)-4180)<1e-8);
        Check("nonfinite intervals rejected",double.IsNaN(Lab.EnergyFromTemp(water,double.NaN))&&double.IsNaN(Lab.EnergyFromTemp(water,double.PositiveInfinity)));
        Check("nonfinite extent rejected",double.IsNaN(Lab.EnthalpyFromTemp(water,1,double.NaN))&&double.IsNaN(Lab.EnthalpyFromTemp(water,1,double.PositiveInfinity)));
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/pure-liquid-calorimetry-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
