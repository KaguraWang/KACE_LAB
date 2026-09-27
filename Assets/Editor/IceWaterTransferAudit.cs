using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class IceWaterTransferAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        Vessel Make(double water,double ice,float temp,VesselKind kind=VesselKind.Beaker){
            var v=new Vessel(kind,Vector3.zero){Water=water,Temp=temp};
            if(ice>0)v.Undissolved[WaterIce.Id]=ice/ChemData.Sub[WaterIce.Id].Mass;return v;
        }
        foreach(bool reverse in new[]{false,true})foreach(double warmWater in new[]{5d,50}){
            var cold=Make(0,10,-5);var warm=Make(warmWater,0,20);
            var from=reverse?warm:cold;var to=reverse?cold:warm;
            double mass=from.Mass()+to.Mass(),energy=WaterIce.Enthalpy(from)+WaterIce.Enthalpy(to);
            using(var tx=new PourTransaction(from,to)){
                Check($"preview ice/water reverse={reverse} water={warmWater}",tx.Preview(1,1,out _));
                Check("preview conserves fusion and sensible enthalpy",Math.Abs(WaterIce.Enthalpy(from)+WaterIce.Enthalpy(to)-energy)<.001&&Math.Abs(from.Mass()+to.Mass()-mass)<1e-8);
                Check("phase split follows available heat",warmWater==5?to.Temp==0&&to.Water>0&&WaterIce.IceMass(to)>0:to.Temp>0&&WaterIce.IceMass(to)==0);
                tx.Undo();Check("undo restores phase inventories and temperatures",cold.Temp==-5&&warm.Temp==20&&cold.Water==0&&Math.Abs(WaterIce.IceMass(cold)-10)<1e-8&&warm.Water==warmWater);
                tx.Preview(1,1,out _);tx.Commit();
            }
            Check("commit does not melt ice twice",Math.Abs(WaterIce.Enthalpy(from)+WaterIce.Enthalpy(to)-energy)<.001&&Math.Abs(from.Mass()+to.Mass()-mass)<1e-8);
        }
        var a=Make(0,10,-5);var b=Make(5,0,20);double initial=WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b);
        using(var flight=new PourTransaction(a,b)){
            Check("solid-only flight accepted",flight.PreviewFlight(0,.5,0,.25,out _));
            double airborne=2.5*WaterIce.IceCpJPerGramK*(-5);
            Check("airborne ice retains mass and sensible energy",Math.Abs(flight.InTransitMassGrams-2.5)<1e-8&&Math.Abs(a.Mass()+b.Mass()+flight.InTransitMassGrams-15)<1e-8&&Math.Abs(WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b)+airborne-initial)<.001);
            bool blocked=false;try{flight.Commit();}catch(InvalidOperationException){blocked=true;}Check("commit waits for airborne ice",blocked);
            flight.Preview(0,.5,out _);flight.Commit();
        }
        a=Make(50,0,80);b=Make(0,10,-5);initial=WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b);
        using(var hotPour=new PourTransaction(a,b)){
            Check("hot water above old ice model limit accepted",hotPour.Preview(1,0,out _)&&b.Temp>40&&WaterIce.IceMass(b)==0);
            hotPour.Commit();
        }
        Check("hot water pays fusion before temperature rise",Math.Abs(WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b)-initial)<.003&&Math.Abs(b.Mass()-60)<1e-8);
        a=Make(20,0,20);b=Make(0,10,-5);initial=WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b);
        Check("legacy fraction transfers ice-water energy",Lab.PourFraction(a,b,.5,true)>0&&Math.Abs(WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b)-initial)<.001&&b.Temp==0&&WaterIce.IceMass(b)>0);
        a=Make(10,0,20,VesselKind.Pipette);b=Make(0,10,-5);a.AttachedTo=b;initial=WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b);
        Check("pipette release melts only affordable ice",Math.Abs(MeteredTransfer.Move(a,5)-5)<1e-8&&b.Temp==0&&WaterIce.IceMass(b)>0&&Math.Abs(WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b)-initial)<.001);
        a=Make(0,0,25,VesselKind.Pipette);b=Make(10,10,0);a.AttachedTo=b;initial=WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b);
        Check("pipette draws liquid but not ice",Math.Abs(MeteredTransfer.Move(a,-5)-5)<1e-8&&a.Water==5&&a.Temp==0&&Math.Abs(WaterIce.IceMass(b)-10)<1e-8&&Math.Abs(WaterIce.Enthalpy(a)+WaterIce.Enthalpy(b)-initial)<.001);
        var full=Make(0,0,-10,VesselKind.Burette);double cap=Math.Min(full.Capacity,full.InteriorCapacityMl);
        full.Undissolved[WaterIce.Id]=(cap-.999)*PhaseProperties.Density(WaterIce.Id)/ChemData.Sub[WaterIce.Id].Mass;
        a=Make(1,0,0);double oldMass=full.Mass();
        using(var tx=new PourTransaction(a,full))Check("freezing expansion cannot bypass hard capacity",!tx.Preview(1,0,out _)&&a.Water==1&&full.Mass()==oldMass);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/ice-water-transfer-audit.txt",lines);EditorApplication.Exit(fail==0?0:1);
    }
}
}
