using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class CondenserStorageAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        // Synthetic coefficients isolate numerical behavior, not apparatus data.
        var p=new CondenserThermalStorage.Input{WallCapacityJPerK=100,CoolantCapacityJPerK=300,
            HotWallWPerK=20,WallCoolantWPerK=15,WallAmbientWPerK=2,CoolantFlowCapacityWPerK=40,
            HotC=100,CoolantInletC=15,AmbientC=25,Seconds=1};
        var initial=new CondenserThermalStorage.State{WallC=25,CoolantC=15};
        foreach(double dt in new[]{0,1e-9,.001,.1,1,10,10000})foreach(double flow in new[]{0,1,40})foreach(double ambient in new[]{0,2}){
            var q=p;q.Seconds=dt;q.CoolantFlowCapacityWPerK=flow;q.WallAmbientWPerK=ambient;
            bool ok=CondenserThermalStorage.TryStep(q,initial,out var r,out _);
            string label=$"dt={dt} flow={flow} ambient={ambient}";
            double total=Math.Abs(r.HotRemovedJ)+Math.Abs(r.AmbientExportedJ)+Math.Abs(r.CoolantExportedJ)+1;
            Check(label+" solved",ok);
            Check(label+" conserved",Math.Abs(r.HotRemovedJ-r.AmbientExportedJ-r.CoolantExportedJ-r.WallStoredJ-r.CoolantStoredJ)<total*1e-9);
            Check(label+" passive bounded",r.Next.WallC>=15&&r.Next.WallC<=100&&r.Next.CoolantC>=15&&r.Next.CoolantC<=100);
            if(flow==0)Check(label+" closed coolant retains heat",r.CoolantExportedJ==0);
        }
        var isolated=p;isolated.HotWallWPerK=isolated.WallAmbientWPerK=isolated.CoolantFlowCapacityWPerK=0;
        var state=new CondenserThermalStorage.State{WallC=90,CoolantC=10};
        double before=state.WallC*100+state.CoolantC*300;
        for(int n=0;n<1000;n++){CondenserThermalStorage.TryStep(isolated,state,out var r,out _);state=r.Next;}
        Check("isolated equilibrium mass weighted",Math.Abs(state.WallC-30)<1e-8&&Math.Abs(state.CoolantC-30)<1e-8);
        Check("isolated long run energy",Math.Abs(state.WallC*100+state.CoolantC*300-before)<1e-7);
        var reverse=p;reverse.HotC=5;reverse.CoolantInletC=10;reverse.AmbientC=15;
        Check("reverse heat signed not deleted",CondenserThermalStorage.TryStep(reverse,new CondenserThermalStorage.State{WallC=80,CoolantC=70},out var rr,out _)&&rr.HotRemovedJ<0&&rr.WallStoredJ<0);
        double[] endpoints=new double[3];int i=0;
        foreach(double dt in new[]{.1,.01,.001}){
            state=initial;var q=p;q.Seconds=dt;double hot=0,cold=0,air=0;
            for(int n=0;n<(int)Math.Round(10/dt);n++){
                CondenserThermalStorage.TryStep(q,state,out var r,out _);state=r.Next;
                hot+=r.HotRemovedJ;cold+=r.CoolantExportedJ;air+=r.AmbientExportedJ;
            }
            endpoints[i++]=state.WallC;
            Check("10s ledger dt="+dt,Math.Abs(hot-cold-air-100*(state.WallC-initial.WallC)-300*(state.CoolantC-initial.CoolantC))<1e-7);
        }
        Check("first order timestep convergence",Math.Abs(endpoints[1]-endpoints[2])<Math.Abs(endpoints[0]-endpoints[1])*.2);
        foreach(double budget in new[]{0,.001,10,100,10000}){
            bool ok=CondenserThermalStorage.TryStepLimited(p,initial,budget,out var r,out _);
            Check("finite vapor budget "+budget,ok&&r.HotRemovedJ<=budget&&r.HotRemovedJ>=0);
            Check("limited energy conserved "+budget,Math.Abs(r.HotRemovedJ-r.AmbientExportedJ-r.CoolantExportedJ-r.WallStoredJ-r.CoolantStoredJ)<1e-8);
            if(budget==0)Check("no vapor still cools warm glass",r.HotRemovedJ==0&&r.WallStoredJ<0&&r.CoolantStoredJ>0);
        }
        Check("invalid vapor budget",!CondenserThermalStorage.TryStepLimited(p,initial,double.NaN,out _,out _));
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,-1}){
            var q=p;q.WallCapacityJPerK=bad;Check("invalid wall capacity "+bad,!CondenserThermalStorage.TryStep(q,initial,out _,out _));
            q=p;q.Seconds=bad;Check("invalid timestep "+bad,!CondenserThermalStorage.TryStep(q,initial,out _,out _));
        }
        var extreme=p;extreme.Seconds=double.MaxValue;
        Check("overflow rejected atomically",!CondenserThermalStorage.TryStep(extreme,initial,out var rejected,out _)&&rejected.HotRemovedJ==0&&initial.WallC==25);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/condenser-storage-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
