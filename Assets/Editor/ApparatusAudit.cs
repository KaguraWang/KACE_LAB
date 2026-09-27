using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class ApparatusAudit {
    public static void Run(){
        Lab.EnsureReady();Lab.Reset();ApparatusSet.Clear();var lines=new List<string>();int failures=0;
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
        var burnerMesh=ApparatusMesh.BuildBunsenBurner();var tripodMesh=ApparatusMesh.BuildTripodRing();
        Check("burner height 150 mm",Math.Abs(burnerMesh.bounds.max.y-1.5)<1e-6);
        Check("tripod height 209.55 mm",Math.Abs(tripodMesh.bounds.max.y-2.0955)<1e-6);
        var burner=ApparatusSet.Add(ApparatusKind.BunsenBurner,Vector3.zero);burner.Lit=true;
        var vessel=Lab.AddVessel(VesselKind.Beaker);vessel.Pos=Vector3.zero;vessel.LiftY=.5f;
        double original=vessel.Temp;ApparatusSet.Tick(.1f);
        Check("old short burner height no longer heats",vessel.Temp==original);
        var tripod=ApparatusSet.Add(ApparatusKind.Tripod,Vector3.zero);
        vessel.LiftY=SnapController.LiftOntoApparatus(tripod);ApparatusSet.Tick(.1f);
        Check("flame reaches vessel at corrected tripod support height",vessel.Temp>original);
        burner.Lit=false;original=vessel.Temp;ApparatusSet.Tick(.1f);
        Check("unlit burner cannot heat",vessel.Temp==original);
        vessel.TripodRef=tripod;tripod.Holds=vessel;vessel.Water=50;vessel.Temp=25;Lab.SetHeat(vessel,true);
        Lab.Tick(.1f);ApparatusSet.Tick(.1f);
        Check("tripod with extinguished burner does not fall back to virtual heating",vessel.Temp==25);
        burner.Lit=true;burner.GasFlow=1;vessel.Temp=25;
        Lab.Tick(.1f);ApparatusSet.Tick(.1f);
        Check("tripod actual heating is not doubled by heat mode",Math.Abs(vessel.Temp-(25+.1*burner.HeatRate))<1e-5);
        burner.Pos=Vector3.right;vessel.Temp=25;Lab.Tick(.1f);ApparatusSet.Tick(.1f);
        Check("moving lamp away does not activate an invisible heater",vessel.Temp==25);
        UnityEngine.Object.DestroyImmediate(burnerMesh);UnityEngine.Object.DestroyImmediate(tripodMesh);
        lines.Add("TOTAL "+(lines.Count-failures)+" passed, "+failures+" failed");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/apparatus-audit.txt",lines);EditorApplication.Exit(failures==0?0:1);
    }
}
}
