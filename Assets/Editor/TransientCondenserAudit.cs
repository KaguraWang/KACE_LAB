using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class TransientCondenserAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        Check("300mm condenser geometry",CondenserDimensions.TryMeasure(.3,out var dimensions)&&dimensions.CoolantMl>80&&dimensions.CoolantMl<90&&dimensions.GlassMl>0);
        Check("annular water excludes vapor bore",Math.Abs(dimensions.CoolantMl-Math.PI*(.01148*.01148-.0055*.0055)*.264*1e6)<1e-10);
        Check("supplied glass capacity conversion",Math.Abs(dimensions.GlassCapacity(2.23,.83)-dimensions.GlassMl*2.23*.83)<1e-10);
        Check("invalid geometry rejected",!CondenserDimensions.TryMeasure(.03,out _)&&!CondenserDimensions.TryMeasure(double.NaN,out _));
        lines.Add($"METRIC 300mm glassMl={dimensions.GlassMl:G9} coolantMl={dimensions.CoolantMl:G9}");
        var stream=new CondenserHeatTransfer.Input{VaporKgPerSecond=.0001,VaporInletC=100,SaturationC=100,
            LatentJPerKg=2256400,VaporCpJPerKgK=1,LiquidCpJPerKgK=4180,CoolantCpJPerKgK=4180,
            CoolantInletC=15,ConductanceWPerK=10};
        var jacket=new CondenserThermalStorage.Input{WallCapacityJPerK=120,CoolantCapacityJPerK=400,
            WallCoolantWPerK=20,WallAmbientWPerK=1,CoolantFlowCapacityWPerK=40,
            HotC=100,CoolantInletC=15,AmbientC=25,Seconds=1};
        var state=new CondenserThermalStorage.State{WallC=15,CoolantC=15};
        foreach(string id in new[]{"h2o","ch3oh","c2h5oh"})foreach(double extra in new[]{0,15})foreach(double ua in new[]{.001,1,10,100,10000})foreach(double wall in new[]{0,15,50}){
            var s=stream;var model=PureLiquidThermalModel.For(id);
            if(model!=null){s.SaturationC=model.BoilingC;s.LatentJPerKg=model.LatentJPerGram*1000;s.LiquidCurve=model.Curve;}
            s.VaporInletC=s.SaturationC+extra;s.VaporCpJPerKgK=1400; // synthetic superheat comparison
            s.ConductanceWPerK=ua;s.CoolantInletC=wall;
            bool a=CondenserHeatTransfer.TrySolveFixedWall(s,out var fast,out _),b=CondenserHeatTransfer.TrySolveFixedWallReference(s,out var reference,out _);
            string label=$"fixed wall analytic {id} superheat={extra} ua={ua} wall={wall}";
            Check(label+" energy equivalence",a&&b&&Math.Abs(fast.HeatWatts-reference.HeatWatts)<1e-8*Math.Max(1,reference.HeatWatts));
            Check(label+" phase and temperature equivalence",Math.Abs(fast.ProductOutletC-reference.ProductOutletC)<1e-6&&Math.Abs(fast.VaporQuality-reference.VaporQuality)<1e-9);
        }
        foreach(string id in new[]{"h2o","ch3oh","c2h5oh"})foreach(double dt in new[]{.01,1,10})foreach(double flow in new[]{0,4,40}){
            var s=stream;var j=jacket;j.Seconds=dt;j.CoolantFlowCapacityWPerK=flow;
            var model=PureLiquidThermalModel.For(id);
            if(model!=null){s.VaporInletC=s.SaturationC=model.BoilingC;s.LatentJPerKg=model.LatentJPerGram*1000;s.LiquidCurve=model.Curve;}
            bool ok=TransientCondenser.TryStep(s,j,state,out var r,out string error);
            string label=$"{id} dt={dt} flow={flow}";if(!ok)lines.Add("ERROR "+error);
            Check(label+" coupled solve",ok);
            var t=r.Storage;var f=r.Stream;
            double heat=f.HeatWatts*dt;
            Check(label+" shared heat",Math.Abs(heat-t.HotRemovedJ)<1e-7);
            Check(label+" all boundary ledger",Math.Abs(heat-t.WallStoredJ-t.CoolantStoredJ-t.CoolantExportedJ-t.AmbientExportedJ)<1e-7);
            Check(label+" no invented vapor",Math.Abs(f.CondensedKgPerSecond+f.UncondensedKgPerSecond-s.VaporKgPerSecond)<1e-12&&f.VaporQuality>=0&&f.VaporQuality<=1);
            Check(label+" no temperature cross",t.Next.WallC>=15&&t.Next.WallC<=s.SaturationC&&f.ProductOutletC>=t.Next.WallC-1e-8);
        }
        // Synthetic constant-Cp hot liquid result has a closed-form fixed-wall
        // solution once latent duty has consumed its share of conductance.
        var fixedWall=stream;fixedWall.ConductanceWPerK=100;
        bool fixedOk=CondenserHeatTransfer.TrySolveFixedWall(fixedWall,out var fixedResult,out _);
        double latentUA=stream.VaporKgPerSecond*stream.LatentJPerKg/85;
        double expectedT=15+85*Math.Exp(-(100-latentUA)/(.0001*4180));
        Check("fixed wall latent plus sensible analytic",fixedOk&&Math.Abs(fixedResult.ProductOutletC-expectedT)<1e-7&&fixedResult.VaporQuality==0);
        var stagnant=jacket;stagnant.CoolantFlowCapacityWPerK=stagnant.WallAmbientWPerK=0;stagnant.Seconds=10;
        double hotSum=0,first=0,last=0;var current=state;
        for(int n=0;n<100;n++){
            bool ok=TransientCondenser.TryStep(stream,stagnant,current,out var r,out _);
            if(!ok){Check("stagnant continuing solve",false);break;}
            current=r.Storage.Next;hotSum+=r.Storage.HotRemovedJ;last=r.Stream.CondensedKgPerSecond;
            if(n==0)first=last;
        }
        Check("stagnant jacket warms and loses condensation capacity",current.CoolantC>90&&last<first*.05);
        Check("stagnant total stored energy",Math.Abs(hotSum-120*(current.WallC-15)-400*(current.CoolantC-15))<1e-6);
        var noVapor=stream;noVapor.VaporKgPerSecond=0;
        Check("no vapor cooling still works",TransientCondenser.TryStep(noVapor,jacket,current,out var cool,out _)&&cool.Storage.HotRemovedJ==0&&cool.Storage.Next.WallC<current.WallC&&cool.Stream.CondensedKgPerSecond==0);
        var invalid=jacket;invalid.HotWallWPerK=1;
        Check("duplicate heat source rejected",!TransientCondenser.TryStep(stream,invalid,state,out _,out _));
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/transient-condenser-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
