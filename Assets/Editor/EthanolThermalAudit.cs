using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class EthanolThermalAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        foreach(var row in new[]{(40d,2.577),(50d,2.677),(60d,2.785),(70d,2.901)}){
            Check("reproduces original rounded heat capacity "+row.Item1,EthanolThermodynamics.TryLiquidHeatCapacity(row.Item1,out double cp)&&Math.Abs(cp/1.000165-row.Item2)<.0006);
        }
        EthanolThermodynamics.TryLiquidEnthalpy(40,out double h40);
        foreach(var row in new[]{(50d,26.26),(60d,53.57),(70d,81.99)}){
            EthanolThermodynamics.TryLiquidEnthalpy(row.Item1,out double h);
            Check("reproduces original rounded enthalpy increment "+row.Item1,Math.Abs((h-h40)/1.000165-row.Item2)<.006);
        }
        double previous=double.NegativeInfinity;
        for(int i=0;i<=100;i++){
            double t=-15+(EthanolThermodynamics.NormalBoilingC+15)*i/100;
            bool valid=EthanolThermodynamics.TryLiquidEnthalpy(t,out double h);
            Check("monotonic invertible enthalpy "+i,valid&&h>previous&&EthanolThermodynamics.TryLiquidTemperature(h,out double recovered)&&Math.Abs(recovered-t)<1e-10);previous=h;
        }
        foreach(double t in new[]{25d,40d}){
            EthanolThermodynamics.TryLiquidEnthalpy(t-.00001,out double before);EthanolThermodynamics.TryLiquidEnthalpy(t+.00001,out double after);
            EthanolThermodynamics.TryLiquidHeatCapacity(t,out double cp);
            Check("continuous enthalpy and matching derivative "+t,Math.Abs((after-before)/.00002-cp)<1e-5);
        }
        foreach(double t in new[]{-15.01,78.36,double.NaN,double.PositiveInfinity})Check("reject unsupported temperature "+t,!EthanolThermodynamics.TryLiquidEnthalpy(t,out _));
        Check("normal boiling latent uses molar to mass unit conversion",Math.Abs(EthanolThermodynamics.NormalBoilingLatentJPerGram*46.0684-38560)<1e-8);
        var source=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=75};source.Solutes["c2h5oh"]=10/ChemData.Sub["c2h5oh"].Mass;
        var target=new Vessel(VesselKind.Beaker,Vector3.right){Temp=25};target.Solutes["c2h5oh"]=10/ChemData.Sub["c2h5oh"].Mass;
        EthanolThermodynamics.TryLiquidEnthalpy(75,out double hot);EthanolThermodynamics.TryLiquidEnthalpy(25,out double cold);
        bool mixed=ThermalMixing.TryTargetTemperature(source,target,1,out float mix,out _);
        EthanolThermodynamics.TryLiquidEnthalpy(mix,out double mixH);
        Check("hot pure ethanol mixes by enthalpy not arithmetic temperature",mixed&&mix>50.1&&Math.Abs(20*mixH-10*(hot+cold))<.001);
        using(var pour=new PourTransaction(source,target)){
            bool accepted=pour.Preview(.5,0,out _);EthanolThermodynamics.TryLiquidEnthalpy(target.Temp,out double targetH);
            Check("pour preview uses same enthalpy curve",accepted&&Math.Abs(15*targetH-5*hot-10*cold)<.001);
            pour.Undo();Check("undo restores temperatures and quantities",source.Temp==75&&target.Temp==25&&Math.Abs(source.Mass()-10)<1e-9&&Math.Abs(target.Mass()-10)<1e-9);
        }
        target.Water=10;
        Check("hot water ethanol mixture does not borrow pure ethanol curve",!ThermalMixing.TryTargetTemperature(source,target,1,out _,out _));
        target.Water=0;target.PressureBar=2;
        Check("pressure outside data domain cannot borrow pure ethanol curve",!ThermalMixing.TryTargetTemperature(source,target,1,out _,out _));
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/ethanol-thermal-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
