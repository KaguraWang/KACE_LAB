using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class VesselPhaseExchangeAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        // Synthetic curve/capacities isolate the phase plateau analytically.
        var curve=new LiquidEnthalpyCurve(new[]{0d,80d},new[]{0d,320000d});
        foreach(double mass in new[]{.0001,.01,.1})foreach(double seconds in new[]{0d,.001,1,10000})foreach(double initial in new[]{20d,80d}){
            bool ok=VesselPhaseExchange.TryStep(curve,mass,initial,100,100,10,seconds,1e6,out var r,out _);
            string label=$"mass={mass} dt={seconds} initial={initial}";
            double before=mass*4000*initial;
            double after=r.RemainingLiquidKg*4000*r.LiquidC+r.VaporKg*(4000*80+1e6);
            Check(label+" finite stock",ok&&r.VaporKg>=0&&r.RemainingLiquidKg>=0&&Math.Abs(r.VaporKg+r.RemainingLiquidKg-mass)<1e-14);
            Check(label+" energy",Math.Abs(after-before-r.HeatFromWallJ)<1e-7&&Math.Abs(100*(100-r.WallC)-r.HeatFromWallJ)<1e-7);
            Check(label+" bounded phase temperature",r.LiquidC>=initial-1e-10&&r.LiquidC<=80&&r.WallC>=r.LiquidC-1e-10);
            if(initial==80){
                double expected=Math.Min(mass*1e6,10*seconds*20/(1+10*seconds/100));
                Check(label+" saturated analytic heat",Math.Abs(r.HeatFromWallJ-expected)<1e-8);
            }
        }
        foreach(var model in new[]{PureLiquidThermalModel.Methanol,PureLiquidThermalModel.Ethanol})foreach(double initial in new[]{25d,model.BoilingC})foreach(double mass in new[]{.0001,.05})foreach(double dt in new[]{.01,1,1000}){
            bool ok=VesselPhaseExchange.TryStep(model.Curve,mass,initial,80,100,10,dt,model.LatentJPerGram*1000,out var r,out _,model);
            double before=mass*1000*model.H(initial);
            double after=r.RemainingLiquidKg*1000*model.H(r.LiquidC)+r.VaporKg*1000*(model.H(model.BoilingC)+model.LatentJPerGram);
            string label=$"{model.Id} initial={initial} mass={mass} dt={dt}";
            Check(label+" exact registered enthalpy",ok&&Math.Abs(after-before-r.HeatFromWallJ)<1e-7);
            Check(label+" latent and wall balance",Math.Abs(r.VaporKg*model.LatentJPerGram*1000-r.LatentJ)<1e-8&&Math.Abs(80*(100-r.WallC)-r.HeatFromWallJ)<1e-8);
        }
        VesselPhaseExchange.TryStep(curve,.0001,80,100,100,100,1000,1e6,out var dry,out _);
        Check("dry out leaves excess wall heat",dry.RemainingLiquidKg==0&&dry.VaporKg==.0001&&Math.Abs(dry.WallC-99)<1e-10);
        VesselPhaseExchange.TryStep(curve,.1,20,100,100,1,.01,1e6,out var warm,out _);
        Check("warming pays sensible energy before vapor",warm.LiquidC>20&&warm.LiquidC<80&&warm.VaporKg==0&&warm.LatentJ==0);
        foreach(double bad in new[]{0d,-1,double.NaN,double.PositiveInfinity})
            Check("invalid latent "+bad,!VesselPhaseExchange.TryStep(curve,.1,20,100,100,10,1,bad,out _,out _));
        Check("no superheated liquid invented",!VesselPhaseExchange.TryStep(curve,.1,90,100,100,10,1,1e6,out _,out _));
        foreach(var model in new[]{PureLiquidThermalModel.Methanol,PureLiquidThermalModel.Ethanol}){
            var endpoints=new List<double[]>();
            foreach(double step in new[]{1d,.2,.05}){
                double mass=.01,t=25,wall=100,vapor=0;
                for(int i=0;i<(int)Math.Round(30/step);i++){
                    bool ok=VesselPhaseExchange.TryStep(model.Curve,mass,t,80,wall,10,step,model.LatentJPerGram*1000,out var r,out _,model);
                    if(!ok){Check(model.Id+" repeated step accepted",false);break;}
                    mass=r.RemainingLiquidKg;t=r.LiquidC;wall=r.WallC;vapor+=r.VaporKg;
                }
                double initial=.01*1000*model.H(25)+80*100;
                double final=mass*1000*model.H(t)+vapor*1000*(model.H(model.BoilingC)+model.LatentJPerGram)+80*wall;
                Check(model.Id+" repeated enthalpy "+step,Math.Abs(final-initial)<1e-6&&Math.Abs(mass+vapor-.01)<1e-12);
                endpoints.Add(new[]{wall,t,vapor});
            }
            for(int c=0;c<3;c++){
                double coarse=Math.Abs(endpoints[0][c]-endpoints[1][c]),fine=Math.Abs(endpoints[1][c]-endpoints[2][c]);
                lines.Add($"CONVERGENCE {model.Id} component={c} coarse={coarse:G12} fine={fine:G12}");
                Check(model.Id+" phase timestep convergence "+c,fine<coarse*.4+1e-9);
            }
        }
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/vessel-phase-exchange-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
