using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ChemLab.Chem;
using UnityEngine;

namespace ChemLab.Game {
// Explicit private test player only; does not inject OS input or affect the user's session.
public sealed class ReactionTagAudit : MonoBehaviour {
    readonly List<string> lines=new List<string>();int failures;
    void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;Debug.Log(lines[lines.Count-1]);}
    IEnumerator Start(){
        Application.runInBackground=true;
        var host=LabBootstrap.Instance;host.enabled=false;
        KPrinterMode.TrySet("0",out _);
        EqTagSettings.Show=true;EqTagSettings.Duration=10;
        foreach(var kind in new[]{VesselKind.Pipette,VesselKind.Burette}){
            Lab.Reset();ApparatusSet.Clear();TestSet.Clear();host.SyncViews();yield return null;
            var cup=Lab.AddVessel(VesselKind.Beaker);cup.Water=100;cup.Solutes["H+"]=.03;cup.Solutes["Cl-"]=.03;
            var tool=Lab.AddVessel(kind);tool.Water=20;tool.Solutes["OH-"]=.02;tool.Solutes["Na+"]=.02;tool.AttachedTo=cup;
            host.SyncViews();int created=EquationTag.CreatedCount,maxTags=0;EquationTag first=null;bool stable=true;
            var watch=new System.Diagnostics.Stopwatch();double maxMs=0,totalMs=0;
            for(int i=0;i<240;i++){
                Lab.Clock+=1f/60f;
                MeteredTransfer.Move(tool,.02);
                watch.Restart();host.SyncViews();watch.Stop();maxMs=Math.Max(maxMs,watch.Elapsed.TotalMilliseconds);totalMs+=watch.Elapsed.TotalMilliseconds;
                maxTags=Math.Max(maxTags,EquationTag.Active.Count);
                var tag=EquationTag.Active.Find(t=>t!=null&&t.Ev?.Vessel==cup);
                if(first==null)first=tag;else stable&=tag==first;
                yield return null;
            }
            Check(kind+" repeated drops reuse the same equation card",first!=null&&stable);
            Check(kind+" bounded tag creation",maxTags<=2&&EquationTag.CreatedCount-created<=2);
            Check(kind+" unchanged equation layout built once",first!=null&&first.LayoutBuildCount==1);
            Check(kind+" continuous chemistry conserves neutralization",Math.Abs(cup.Solutes["H+"]-.0252)<1e-7&&Math.Abs(tool.Solutes["OH-"]-.0152)<1e-7);
            Check(kind+" physical volume delivered",Math.Abs(tool.Water-15.2)<1e-6);
            lines.Add($"METRIC {kind} 240 transfers: tags={maxTags}, created={EquationTag.CreatedCount-created}, SyncViews mean={totalMs/240:F3}ms max={maxMs:F3}ms");
            Localization.English=!Localization.English;
            host.SyncViews();Check(kind+" language switch rebuilds cached layout",first!=null&&first.LayoutBuildCount==2);
            Lab.Clock+=11;host.SyncViews();Check(kind+" tags expire after last drop",EquationTag.Active.Count==0);
        }
        Lab.Reset();host.SyncViews();
        var v=Lab.AddVessel(VesselKind.Beaker);var rule=new Rules.Rule{Eq="H+ + OH- -> H2O",Parsed=true};
        for(int i=0;i<40;i++){Lab.PushReaction(v,new Rules.Rule{Eq=rule.Eq,Cond="condition"+i,Parsed=true},.001);host.SyncViews();}
        Check("distinct reactions bounded to six cards per vessel",EquationTag.Active.Count==6);
        EqTagSettings.Show=false;host.SyncViews();Lab.PushReaction(v,rule,.001);host.SyncViews();Check("disabled tags allocate no cards",EquationTag.Active.Count==0);
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/reaction-tag-audit.txt",lines);
        Debug.Log($"[ReactionTagAudit] checks={lines.FindAll(x=>x.StartsWith("PASS")||x.StartsWith("FAIL")).Count} failures={failures}");
        Application.Quit(failures==0?0:1);
    }
}
}
