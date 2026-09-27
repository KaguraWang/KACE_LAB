using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class DistillationAudit {
    static double Mass(Vessel v)=>v.Mass()+(v.Sealed?0:v.Gases.Sum(p=>ChemData.MassOf(p.Key,p.Value)));
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string name,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+name);}
        var source=new Vessel(VesselKind.RoundBottom,Vector3.zero){Water=50,Temp=100};
        var receiver=new Vessel(VesselKind.Flask,Vector3.right);
        var rig=new DistillationAssembly(source);
        Check("distillation rejects same receiver",!rig.Configure(false,source,out _));
        Check("distillation requires receiver",!rig.Configure(false,null,out _));
        Check("assembly connects receiver",rig.Configure(false,receiver,out _));
        Check("cooling required before start",!rig.Start(out _));
        rig.Cooling=true;Check("start sets real source heat mode",rig.Start(out _)&&source.Mode==HeatMode.Distil);
        rig.Tick(2);
        double generated=rig.HeaterPowerWatts*2/DistillationAssembly.WaterLatentJPerGram;
        Check("water reaches receiver limited by supplied vaporization energy",Math.Abs(receiver.Water-generated)<1e-8&&Math.Abs(source.Water-(50-generated))<1e-8&&Math.Abs(Mass(source)+Mass(receiver)-50)<1e-8);
        Check("empty receiver temperature comes from condenser energy balance",receiver.Temp>=15&&receiver.Temp<100&&Math.Abs(receiver.Temp-rig.LastThermalResult.ProductOutletC)<1e-4);
        double amount=source.Water;Lab.Evaporate(source,10);
        Check("attached condenser prevents duplicate water loss",source.Water==amount);
        rig.Cooling=false;rig.Tick(1);
        Check("cooling loss stops transfer without mutation",!rig.Running&&source.Water==amount&&source.Mode==HeatMode.None);
        rig.Cooling=true;receiver.Water=receiver.Capacity-.25;receiver.Temp=15;rig.Start(out _);double before=source.Water+receiver.Water;
        rig.Tick(2);rig.Tick(1);
        Check("receiver hard capacity and conservation",Math.Abs(receiver.Water-receiver.Capacity)<1e-8&&Math.Abs(source.Water+receiver.Water-before)<1e-8&&!rig.Running);
        rig.Detach();Check("detach clears source ownership",source.Distillation==null&&source.Mode==HeatMode.None);
        var ownerA=new Vessel(VesselKind.RoundBottom,Vector3.zero);
        var ownerB=new Vessel(VesselKind.RoundBottom,Vector3.right);
        var shared=new Vessel(VesselKind.Flask,Vector3.forward);
        var linkA=new DistillationAssembly(ownerA){Cooling=true};
        var linkB=new DistillationAssembly(ownerB){Cooling=true};
        linkA.Configure(false,shared,out _);linkA.Start(out _);
        Check("receiver cannot be shared by independent sources",!linkB.Configure(false,shared,out _));
        var receiverAsSource=new DistillationAssembly(shared);
        Check("receiver cannot simultaneously become reflux source",!receiverAsSource.Configure(true,null,out _));
        receiverAsSource.Detach();linkB.Detach();
        var invalidTarget=new Vessel(VesselKind.Flask,Vector3.zero){Sealed=true};
        Check("invalid reconfiguration preserves running original connection",!linkA.Configure(false,invalidTarget,out _)&&linkA.Running&&linkA.Receiver==shared&&ownerA.Mode==HeatMode.Distil);
        linkA.Configure(true,null,out _);
        Check("switching to reflux releases old receiver",linkB.Configure(false,shared,out _));
        linkB.Detach();linkA.Detach();
        Check("detaching releases receiver for another source",linkA.Configure(false,shared,out _));
        linkA.Detach();
        var reflux=new DistillationAssembly(source){Cooling=true};reflux.Configure(true,null,out _);reflux.Start(out _);
        before=Mass(source);double waterBefore=source.Water;reflux.Tick(5);Lab.Evaporate(source,5);
        Check("reflux circulates while retaining all material",reflux.ReturnedMl>0&&Math.Abs(Mass(source)-before)<1e-8&&Math.Abs(source.Water-waterBefore)<1e-8);
        source.Sealed=true;reflux.Tick(1);Check("sealed apparatus stops",!reflux.Running&&Mass(source)==before);source.Sealed=false;reflux.Detach();
        foreach(var organic in new[]{("c2h5oh","ch3cho","ch3cooh",80f),("butan_1_ol","butanal","butanoic_acid",101f)})foreach(bool isReflux in new[]{false,true}){
            var flask=new Vessel(VesselKind.RoundBottom,Vector3.zero){Water=30,Temp=organic.Item4};
            flask.Introduce("k2cr2o7",isReflux?.02:.01,true);
            flask.Introduce("h2so4",isReflux?.08:.04,true);
            // Prepare the aqueous oxidizer first. Adding dry dichromate to an
            // already-organic mixture deliberately hits the missing-solubility guard.
            flask.Solutes[organic.Item1]=.03;
            var catchFlask=new Vessel(VesselKind.Flask,Vector3.right);
            var apparatus=new DistillationAssembly(flask){Cooling=true};apparatus.Configure(isReflux,catchFlask,out _);apparatus.Start(out _);
            double initialMass=Mass(flask)+Mass(catchFlask);
            Lab.Settle(flask);apparatus.Tick(100);
            lines.Add("METRIC oxidation "+isReflux+" source="+string.Join(",",flask.Solutes.Select(p=>p.Key+":"+p.Value))+" gas="+string.Join(",",flask.Gases.Select(p=>p.Key+":"+p.Value))+" receiver="+string.Join(",",catchFlask.Solutes.Select(p=>p.Key+":"+p.Value)));
            Check(organic.Item1+" oxidation product under "+(isReflux?"reflux":"distillation"),isReflux?
                flask.Get(organic.Item3)>.02&&catchFlask.Get(organic.Item2)==0:
                catchFlask.Get(organic.Item2)>.02&&flask.Get(organic.Item2)<1e-9&&flask.Get(organic.Item3)<1e-9);
            Check(organic.Item1+" oxidation assembly mass conservation "+isReflux,Math.Abs(Mass(flask)+Mass(catchFlask)-initialMass)<1e-7);
            apparatus.Detach();
        }
        Lab.Reset();var live=Lab.AddVessel(VesselKind.RoundBottom);var target=Lab.AddVessel(VesselKind.Flask);
        var volatileSource=new Vessel(VesselKind.RoundBottom,Vector3.zero){Temp=80};volatileSource.Solutes["ch3cho"]=.01;
        var warmCatch=new Vessel(VesselKind.Flask,Vector3.zero){Water=20,Temp=25};
        var warmRig=new DistillationAssembly(volatileSource){Cooling=true};warmRig.Configure(false,warmCatch,out _);warmRig.Start(out _);warmRig.Tick(1);
        Check("warm receiver cannot pretend to retain liquid ethanal",!warmRig.Running&&volatileSource.Get("ch3cho")==.01&&warmCatch.Get("ch3cho")==0);
        warmCatch.Water=0;warmRig.CoolantCelsius=30;warmRig.Start(out _);warmRig.Tick(1);
        Check("insufficient cooling preserves uncondensed inventory and warns",volatileSource.Get("ch3cho")==.01&&warmCatch.Get("ch3cho")==0&&warmRig.Notice.Contains("不能冷凝"));
        warmRig.Detach();
        var cleanup=new DistillationAssembly(live){Cooling=true};cleanup.Configure(false,target,out _);cleanup.Start(out _);
        Lab.RemoveVessel(target);Check("removing receiver detaches running apparatus",live.Distillation==null&&!cleanup.Running&&cleanup.Receiver==null);
        Lab.Reset();
        lines.Add("TOTAL "+pass+" passed, "+fail+" failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/distillation-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
