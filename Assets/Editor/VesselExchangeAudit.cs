using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class VesselExchangeAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        // Synthetic Cp and capacities test algebra, not material calibration.
        var linear=new LiquidEnthalpyCurve(new[]{0d,100d},new[]{0d,400000d});
        foreach(double dt in new[]{0d,1e-9,.01,1,100,1e8})foreach(double k in new[]{0d,.01,10,1e6})foreach(bool reverse in new[]{false,true}){
            double t=reverse?80:20,w=reverse?20:80;
            bool ok=VesselSensibleExchange.TryStep(linear,.05,t,80,w,k,dt,out var r,out _);
            double q=k*dt*(w-t)/(1+k*dt*(1d/200+1d/80));
            string label=$"dt={dt} k={k} reverse={reverse}";
            Check(label+" analytic",ok&&Math.Abs(r.LiquidGainJ-q)<1e-7&&Math.Abs(r.LiquidC-t-q/200)<1e-8&&Math.Abs(r.WallC-w+q/80)<1e-8);
            Check(label+" conserved bounded",r.LiquidGainJ+r.WallGainJ==0&&r.LiquidC>=20-1e-10&&r.LiquidC<=80+1e-10&&r.WallC>=20-1e-10&&r.WallC<=80+1e-10);
        }
        var nonlinear=new LiquidEnthalpyCurve(new[]{0d,30d,60d,100d},new[]{0d,60000d,150000d,350000d});
        double H(double t)=>t<=30?2000*t:t<=60?60000+3000*(t-30):150000+5000*(t-60);
        foreach(bool reverse in new[]{false,true}){
            double t=reverse?90:10,w=reverse?10:90;
            bool ok=VesselSensibleExchange.TryStep(nonlinear,.05,t,80,w,100,10,out var r,out _);
            double energy=.05*(H(r.LiquidC)-H(t));
            Check("variable Cp energy "+reverse,ok&&Math.Abs(energy-r.LiquidGainJ)<1e-8&&Math.Abs(80*(r.WallC-w)+energy)<1e-8);
            Check("variable Cp implicit flux "+reverse,Math.Abs(r.LiquidGainJ-1000*(r.WallC-r.LiquidC))<1e-7);
        }
        foreach(double invalid in new[]{-1d,double.NaN,double.PositiveInfinity}){
            Check("invalid timestep "+invalid,!VesselSensibleExchange.TryStep(linear,.05,20,80,80,10,invalid,out _,out _));
            Check("invalid conductance "+invalid,!VesselSensibleExchange.TryStep(linear,.05,20,80,80,invalid,1,out _,out _));
        }
        Check("reject wall outside property range",!VesselSensibleExchange.TryStep(linear,.05,20,80,150,10,1,out _,out _));
        Check("reject zero inventory",!VesselSensibleExchange.TryStep(linear,0,20,80,80,10,1,out _,out _));
        foreach(bool tinyConductance in new[]{false,true}){
            double k=tinyConductance?1e-310:1e308,dt=tinyConductance?1e308:1e-310;
            bool ok=VesselSensibleExchange.TryStep(linear,.05,20,80,80,k,dt,out var r,out _);
            double q=k*dt*60/(1+k*dt*(1d/200+1d/80));
            Check("extreme reciprocal scale "+tinyConductance,ok&&Math.Abs(r.LiquidGainJ-q)<1e-10);
        }
        double priorError=double.PositiveInfinity;
        foreach(double step in new[]{1d,.5,.25}){
            double t=20,w=80;
            for(int i=0;i<(int)(10/step);i++){
                VesselSensibleExchange.TryStep(linear,.05,t,80,w,10,step,out var r,out _);t=r.LiquidC;w=r.WallC;
            }
            double analytic=20+(80*60d/280)*(1-Math.Exp(-10*(1d/200+1d/80)*10));
            double error=Math.Abs(t-analytic);
            Check("time refinement "+step,error<priorError*.7);priorError=error;
            Check("repeated energy "+step,Math.Abs(200*(t-20)+80*(w-80))<1e-8);
        }
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/vessel-exchange-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
