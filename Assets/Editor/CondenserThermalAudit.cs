using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class CondenserThermalAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        bool Near(double a,double b)=>Math.Abs(a-b)<1e-7*Math.Max(1,Math.Abs(b));
        // Synthetic properties deliberately isolate solver verification from
        // database accuracy. These numbers are NOT a water/organic data table.
        var p=new CondenserHeatTransfer.Input{VaporKgPerSecond=.001,VaporInletC=100,SaturationC=100,
            LatentJPerKg=2000000,VaporCpJPerKgK=2000,LiquidCpJPerKgK=4000,
            CoolantKgPerSecond=.01,CoolantCpJPerKgK=4000,CoolantInletC=20,ConductanceWPerK=5};
        Check("saturated vapor solve succeeds",CondenserHeatTransfer.TrySolve(p,out var partial,out _));
        double phaseOnly=40*80*(1-Math.Exp(-5d/40));
        Check("partial condensation agrees with analytic phase-change NTU",Near(partial.HeatWatts,phaseOnly));
        Check("partial product remains at saturation",partial.ProductOutletC==100&&partial.VaporQuality>0&&partial.VaporQuality<1);
        Check("phase-only heat equals condensed mass times latent heat",Near(partial.CondensedKgPerSecond*p.LatentJPerKg,partial.HeatWatts));
        p.VaporInletC=120;p.ConductanceWPerK=.1;
        CondenserHeatTransfer.TrySolve(p,out var superheated,out _);
        Check("small cooler only removes superheat",superheated.ProductOutletC>100&&superheated.CondensedKgPerSecond==0&&superheated.LatentWatts==0);
        double exponent=Math.Exp(-(.1/2)*(1-2d/40));
        double sensibleNtu=2*100*(1-exponent)/(1-(2d/40)*exponent);
        Check("desuperheat agrees with counterflow sensible NTU",Near(superheated.HeatWatts,sensibleNtu));
        p.ConductanceWPerK=5;CondenserHeatTransfer.TrySolve(p,out var twoZone,out _);
        Check("desuperheating precedes partial condensation",Near(twoZone.DesuperheatWatts,40)&&twoZone.LatentWatts>0&&twoZone.SubcoolWatts==0);
        p.ConductanceWPerK=100;CondenserHeatTransfer.TrySolve(p,out var threeZone,out _);
        Check("strong cooler resolves all three zones",Near(threeZone.DesuperheatWatts,40)&&Near(threeZone.LatentWatts,2000)&&threeZone.SubcoolWatts>0&&threeZone.VaporQuality==0);
        Check("outlet is not simply assigned coolant inlet",threeZone.ProductOutletC>20&&threeZone.ProductOutletC<100&&threeZone.CoolantOutletC>20);
        foreach(double flow in new[]{0d,.00001,.0001,.001,.01,.1})foreach(double ua in new[]{0d,.001,.1,1,10,100,1000}){
            p.CoolantKgPerSecond=flow;p.ConductanceWPerK=ua;
            bool ok=CondenserHeatTransfer.TrySolve(p,out var r,out _);
            Check($"energy mass bounds flow={flow} UA={ua}",ok&&
                Near(r.CondensedKgPerSecond+r.UncondensedKgPerSecond,p.VaporKgPerSecond)&&
                Near(r.HeatWatts,r.DesuperheatWatts+r.LatentWatts+r.SubcoolWatts)&&
                Near(r.HeatWatts,flow*p.CoolantCpJPerKgK*(r.CoolantOutletC-p.CoolantInletC))&&
                r.ProductOutletC>=p.CoolantInletC-1e-8&&r.ProductOutletC<=p.VaporInletC&&
                r.CoolantOutletC<=p.VaporInletC+1e-8&&r.VaporQuality>=0&&r.VaporQuality<=1);
            Check($"finite conductance diagnostic flow={flow} UA={ua}",!double.IsNaN(r.UsedConductanceWPerK)&&!double.IsInfinity(r.UsedConductanceWPerK)&&r.UsedConductanceWPerK<=ua+1e-5);
        }
        p.CoolantKgPerSecond=.01;p.ConductanceWPerK=10;p.CoolantInletC=100;
        CondenserHeatTransfer.TrySolve(p,out var warm,out _);
        Check("coolant at saturation cannot remove latent heat",warm.CondensedKgPerSecond==0&&warm.ProductOutletC>=100);
        p.CoolantInletC=130;CondenserHeatTransfer.TrySolve(p,out var hotter,out _);
        Check("hotter coolant cannot invent condensation",hotter.HeatWatts==0&&hotter.VaporQuality==1);
        p.CoolantInletC=20;p.ConductanceWPerK=-1;
        Check("negative conductance rejected",!CondenserHeatTransfer.TrySolve(p,out _,out _));
        p.ConductanceWPerK=double.NaN;
        Check("NaN conductance rejected",!CondenserHeatTransfer.TrySolve(p,out _,out _));
        p.ConductanceWPerK=10;p.VaporKgPerSecond=0;
        Check("zero vapor has zero duty",CondenserHeatTransfer.TrySolve(p,out var empty,out _)&&empty.HeatWatts==0&&empty.CondensedKgPerSecond==0);
        // A sampled constant-Cp table must reproduce the original analytic path.
        var ts=new[]{-10d,20,40,70,100};var hs=new[]{-440000d,-320000,-240000,-120000,0};
        var constantCurve=new LiquidEnthalpyCurve(ts,hs);
        ts[0]=999;hs[0]=999;
        Check("curve owns immutable copies",constantCurve.MinC==-10&&constantCurve.KnotEnthalpy(0)==-440000);
        foreach(double inlet in new[]{100d,120})foreach(double flow in new[]{.00001,.001,.01,.1})foreach(double ua in new[]{.1,5,50,1000}){
            p.VaporKgPerSecond=.001;p.VaporInletC=inlet;p.CoolantKgPerSecond=flow;p.ConductanceWPerK=ua;p.LiquidCurve=null;
            bool oldOk=CondenserHeatTransfer.TrySolve(p,out var analytic,out _);
            p.LiquidCurve=constantCurve;
            bool newOk=CondenserHeatTransfer.TrySolve(p,out var tabulated,out _);
            Check($"constant Cp equivalence inlet={inlet} flow={flow} UA={ua}",oldOk&&newOk&&Near(analytic.HeatWatts,tabulated.HeatWatts)&&Near(analytic.ProductOutletC,tabulated.ProductOutletC)&&Near(analytic.VaporQuality,tabulated.VaporQuality));
        }
        void RejectCurve(string name,double[] t,double[] h){bool rejected=false;try{new LiquidEnthalpyCurve(t,h);}catch(ArgumentException){rejected=true;}Check(name,rejected);}
        RejectCurve("null knots rejected",null,new[]{0d,1});
        RejectCurve("mismatched knots rejected",new[]{0d,1},new[]{0d});
        RejectCurve("duplicate temperatures rejected",new[]{0d,0},new[]{0d,1});
        RejectCurve("descending enthalpy rejected",new[]{0d,1},new[]{1d,0});
        RejectCurve("NaN knot rejected",new[]{0d,double.NaN},new[]{0d,1});
        RejectCurve("infinite enthalpy rejected",new[]{0d,1},new[]{0d,double.PositiveInfinity});
        RejectCurve("enthalpy span overflow rejected",new[]{0d,1},new[]{-double.MaxValue,double.MaxValue});
        RejectCurve("normalization cannot collapse adjacent knots",new[]{0d,1,2},new[]{0d,1,1e30});
        p.CoolantInletC=-11;
        Check("curve lower extrapolation rejected",!CondenserHeatTransfer.TrySolve(p,out _,out _));
        p.CoolantInletC=20;p.SaturationC=99;
        Check("curve saturation mismatch rejected",!CondenserHeatTransfer.TrySolve(p,out _,out _));
        // Saturated ethanol avoids inventing an unsourced vapor heat capacity.
        p.SaturationC=p.VaporInletC=EthanolThermodynamics.NormalBoilingC;
        p.LatentJPerKg=1000*EthanolThermodynamics.NormalBoilingLatentJPerGram;
        p.CoolantCpJPerKgK=4180;
        var coarse=EthanolThermodynamics.BuildCondenserCurve(.5);
        var standard=EthanolThermodynamics.CondenserCurve;
        var fine=EthanolThermodynamics.BuildCondenserCurve(.125);
        EthanolThermodynamics.TryLiquidEnthalpy(p.SaturationC,out double saturatedH);
        double maxWattsDifference=0,maxTemperatureDifference=0,maxEnthalpyResidual=0;
        foreach(double cold in new[]{-10d,15,35})foreach(double flow in new[]{.00001,.001,.01})foreach(double ua in new[]{.1,5,50,1000}){
            p.CoolantInletC=cold;p.CoolantKgPerSecond=flow;p.ConductanceWPerK=ua;p.LiquidCurve=standard;
            bool ok=CondenserHeatTransfer.TrySolve(p,out var r,out _);
            Check($"ethanol bounded balance cold={cold} flow={flow} UA={ua}",ok&&Near(r.CondensedKgPerSecond+r.UncondensedKgPerSecond,p.VaporKgPerSecond)&&Near(r.HeatWatts,flow*4180*(r.CoolantOutletC-cold))&&Near(r.HeatWatts,r.LatentWatts+r.SubcoolWatts)&&r.ProductOutletC>=cold-1e-8&&r.ProductOutletC<=p.SaturationC&&r.CoolantOutletC<=p.SaturationC+1e-8&&r.UsedConductanceWPerK<=ua+1e-5&&!double.IsInfinity(r.UsedConductanceWPerK));
            if(r.VaporQuality==0){
                EthanolThermodynamics.TryLiquidEnthalpy(r.ProductOutletC,out double actualH);
                double residual=Math.Abs(r.SubcoolWatts-p.VaporKgPerSecond*1000*(saturatedH-actualH));
                maxEnthalpyResidual=Math.Max(maxEnthalpyResidual,residual);
                Check($"ethanol analytic enthalpy agrees cold={cold} flow={flow} UA={ua}",residual<.001);
            }
            p.LiquidCurve=coarse;bool coarseOk=CondenserHeatTransfer.TrySolve(p,out var c,out _);
            p.LiquidCurve=fine;bool fineOk=CondenserHeatTransfer.TrySolve(p,out var f,out _);
            double dq=Math.Abs(r.HeatWatts-f.HeatWatts),dt=Math.Abs(r.ProductOutletC-f.ProductOutletC);
            maxWattsDifference=Math.Max(maxWattsDifference,dq);maxTemperatureDifference=Math.Max(maxTemperatureDifference,dt);
            Check($"ethanol mesh convergence cold={cold} flow={flow} UA={ua}",coarseOk&&fineOk&&dq<.001&&dt<.001&&dq<=Math.Abs(c.HeatWatts-f.HeatWatts)+1e-8);
        }
        lines.Add($"ETHANOL max mesh delta W={maxWattsDifference:R} C={maxTemperatureDifference:R}; analytic enthalpy residual W={maxEnthalpyResidual:R}");
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/condenser-thermal-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
