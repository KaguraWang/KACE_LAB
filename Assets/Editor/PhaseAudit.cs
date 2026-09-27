using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class PhaseAudit {
    static void AuditAniline(Action<string,bool> check){
        foreach(int setup in new[]{0,1,2,3}){
            var v=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
            v.Solutes["aniline"]=.001;
            if(setup==1){v.Solutes["H+"]=.003;v.Solutes["Cl-"]=.003;}
            if(setup==2){v.Solutes.Remove("aniline");v.Solutes["C6H5NH3+"]=.001;v.Solutes["Cl-"]=.001;v.Solutes["Na+"]=.003;v.Solutes["OH-"]=.003;}
            if(setup==3){v.Solutes["ch3cooh"]=.001;v.Solutes["nh3"]=.0005;}
            double mass=v.Mass();PhaseProperties.Reconcile(v,true);
            check("aniline family conservation "+setup,Math.Abs(v.Get("aniline")+v.Get("C6H5NH3+")-.001)<1e-12);
            check("aniline mass and charge "+setup,Math.Abs(v.Mass()-mass)<1e-8&&Math.Abs(v.Solutes.Sum(p=>ChemData.Sub[p.Key].Charge*p.Value))<1e-12);
            check("anilinium Ka "+setup,Math.Abs(v.Get("H+")*v.Get("aniline")/(v.Water/1000*v.Get("C6H5NH3+"))/AmmoniaEquilibrium.AniliniumKa-1)<1e-6);
            if(setup==1)check("acid protonates aniline",v.Get("C6H5NH3+")>.00099);
            if(setup==2)check("alkali liberates aniline",v.Get("aniline")>.00099);
            double prior=v.Get("aniline");PhaseProperties.Reconcile(v,true);
            check("aniline equilibrium idempotent "+setup,Math.Abs(v.Get("aniline")-prior)<1e-10);
        }
        foreach(bool nonwater in new[]{false,true}){
            var v=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=nonwater?25:80};
            v.Solutes["aniline"]=.001;v.Solutes["H+"]=.003;
            if(nonwater)v.Solutes["c2h5oh"]=.01;
            AmmoniaEquilibrium.Reconcile(v,true);
            check("unsupported aniline conditions preserve and warn "+nonwater,v.Get("aniline")==.001&&v.Get("C6H5NH3+")==0&&v.PhaseNotice.Contains("未覆盖"));
        }
    }
    static void AuditAcetic(Action<string,bool> check){
        double Get(Vessel v,string id)=>v.Solutes.TryGetValue(id,out double n)?n:0;
        foreach(double baseMol in new[]{0,.0005,.001,.002})foreach(bool ammoniaBase in new[]{false,true}){
            var v=new Vessel(VesselKind.Beaker,Vector3.zero);v.Water=50;v.Solutes["ch3cooh"]=.001;
            if(ammoniaBase)v.Solutes["nh3"]=baseMol;
            else if(baseMol>0){v.Solutes["Na+"]=baseMol;v.Solutes["OH-"]=baseMol;}
            double mass=v.Mass();string label=" base="+baseMol+" ammonia="+ammoniaBase;
            PhaseProperties.Reconcile(v,true);
            check("acetic family conserved"+label,Math.Abs(Get(v,"ch3cooh")+Get(v,"CH3COO-")-.001)<1e-12);
            check("acetic equilibrium Ka"+label,Math.Abs(Get(v,"H+")*Get(v,"CH3COO-")/(v.Water/1000*Get(v,"ch3cooh"))/AmmoniaEquilibrium.AceticKa-1)<1e-7);
            check("coupled acetic mass and charge"+label,Math.Abs(v.Mass()-mass)<1e-8&&Math.Abs(v.Solutes.Sum(p=>ChemData.Sub[p.Key].Charge*p.Value))<1e-12);
            if(ammoniaBase&&baseMol>0)check("coupled ammonia Kb"+label,Math.Abs(Get(v,"NH4+")*Get(v,"OH-")/(v.Water/1000*Get(v,"nh3"))/AmmoniaEquilibrium.Kb-1)<1e-7);
            double prior=v.Ph;PhaseProperties.Reconcile(v,true);check("acetic equilibrium idempotent"+label,Math.Abs(v.Ph-prior)<1e-8);
            if(baseMol==.0005&&!ammoniaBase)check("half neutralized acetic buffer pH",Math.Abs(v.Ph+Math.Log10(AmmoniaEquilibrium.AceticKa))<.01);
            if(baseMol==.001&&!ammoniaBase)check("acetate equivalence alkaline",v.Ph>8&&v.Ph<9);
        }
    }
    static void AuditAmmonia(Action<string,bool> check) {
        double Amount(Vessel v,string id)=>v.Solutes.TryGetValue(id,out double n)?n:0;
        foreach(bool closed in new[]{false,true})foreach(double acid in new[]{0.0,0.0002,0.002}) {
            var v=new Vessel(VesselKind.Bottle,Vector3.zero);v.Water=50;
            if(acid>0){v.Solutes["H+"]=acid;v.Solutes["Cl-"]=acid;}
            if(closed)v.SetSealed(true);
            v.Gases["nh3"]=.001;
            double massBefore=v.Mass()+(closed?0:.001*ChemData.Sub["nh3"].Mass);
            double chargeBefore=v.Solutes.Sum(i=>ChemData.Sub[i.Key].Charge*i.Value);
            PhaseProperties.Reconcile(v,true);
            string tag=" closed="+closed+" acid="+acid;
            double gas=v.Gases.TryGetValue("nh3",out double ng)?ng:0;
            check("NH3 nitrogen conservation"+tag,Math.Abs(Amount(v,"nh3")+Amount(v,"NH4+")+gas+v.EscapedMoles-.001)<1e-12);
            check("NH3 mass conservation including water"+tag,Math.Abs(v.Mass()+v.EscapedMoles*ChemData.Sub["nh3"].Mass-massBefore)<1e-8);
            check("NH3 charge conservation"+tag,Math.Abs(v.Solutes.Sum(i=>ChemData.Sub[i.Key].Charge*i.Value)-chargeBefore)<1e-12);
            double volume=v.Water/1000;
            check("NH3 Kb at equilibrium"+tag,Math.Abs(Amount(v,"NH4+")*Amount(v,"OH-")/(Amount(v,"nh3")*volume)/AmmoniaEquilibrium.Kb-1)<1e-8);
            check("NH3 never sediment"+tag,v.Undissolved.Count==0&&v.Precipitates.Count==0);
            if(closed)check("NH3 Henry partial pressure"+tag,Math.Abs(Amount(v,"nh3")-AmmoniaEquilibrium.Henry*volume*gas*v.GasRT/v.HeadspaceLitres)<1e-10);
            double molecular=Amount(v,"nh3");PhaseProperties.Reconcile(v,true);
            check("NH3 equilibrium idempotent"+tag,Math.Abs(Amount(v,"nh3")-molecular)<1e-10);
        }
        var saturated=new Vessel(VesselKind.Beaker,Vector3.zero);saturated.Water=10;saturated.Gases["nh3"]=1;
        PhaseProperties.Reconcile(saturated,true);
        check("NH3 saturation vents gas without precipitating",saturated.EscapedMoles>.7&&saturated.Precipitates.Count==0&&saturated.Undissolved.Count==0&&saturated.PhaseNotice.Contains("已逸散"));
        check("NH3 saturation solvent-basis cap",Math.Abs(Amount(saturated,"nh3")-475.75*saturated.Water/1000/ChemData.Sub["nh3"].Mass)<1e-10);
        check("NH3 saturation mass conservation",Math.Abs(saturated.Mass()+saturated.EscapedMoles*ChemData.Sub["nh3"].Mass-10-ChemData.Sub["nh3"].Mass)<1e-8);
        var unsupported=new Vessel(VesselKind.Beaker,Vector3.zero);unsupported.Water=50;unsupported.Temp=60;unsupported.Gases["nh3"]=.001;
        AmmoniaEquilibrium.Reconcile(unsupported,true);
        check("NH3 unsupported temperature preserves inventory and warns",unsupported.Gases["nh3"]==.001&&unsupported.EscapedMoles==0&&unsupported.PhaseNotice.Contains("未覆盖"));
        unsupported.Temp=25;unsupported.SetSealed(true);unsupported.Gases["nh3"]=.1;
        AmmoniaEquilibrium.Reconcile(unsupported,true);
        check("NH3 concentrated sealed guard preserves inventory",unsupported.Gases["nh3"]==.1&&unsupported.PhaseNotice.Contains("稀溶液"));
    }
    public static void Run() {
        Lab.EnsureReady();var lines=new List<string>();int checks=0,failures=0;
        void Check(string name,bool good){checks++;if(!good)failures++;lines.Add((good?"PASS ":"FAIL ")+name);}
        foreach(var s in ChemData.Sub.Values) {
            Check("data "+s.Id,new[]{"s","l","g","aq"}.Contains(s.State)&&s.Density>0&&s.Mass>0);
            foreach(bool wet in new[]{false,true}) {
                var v=new Vessel(VesselKind.Beaker,Vector3.zero);if(wet)v.AddWater(50);
                v.Introduce(s.Id,0.001,wet);
                Check("introduced phase "+s.Id+" wet="+wet,
                    v.Undissolved.All(k=>ChemData.Sub[k.Key].State=="s")&&v.Precipitates.Count==0);
                var p=new Vessel(VesselKind.Beaker,Vector3.zero);if(wet)p.AddWater(50);p.Produce(s.Id,0.001);
                Check("product phase "+s.Id+" wet="+wet,p.Precipitates.All(k=>ChemData.Sub[k.Key].State=="s")&&p.Undissolved.All(k=>ChemData.Sub[k.Key].State=="s"));
            }
        }
        var ammonia=new Vessel(VesselKind.Beaker,Vector3.zero);Lab.AddLiquid(ammonia,"nh3",10);
        AuditAmmonia(Check);
        AuditAniline(Check);
        var neatAcetic=new Vessel(VesselKind.Beaker,Vector3.zero);
        Check("pure acetic acid shelf dose accepted",ChemLab.Game.DoseService.TryAdd(neatAcetic,"ch3cooh",.01,true,out _));
        Check("pure acetic acid is molecular without invented aqueous ions",neatAcetic.Water==0&&neatAcetic.Solutes.Count==1&&Math.Abs(neatAcetic.Get("ch3cooh")-.01)<1e-12&&neatAcetic.Ledger.Count==0);
        Check("pure acetic acid dose preserves mass",Math.Abs(neatAcetic.Mass()-.01*ChemData.Sub["ch3cooh"].Mass)<1e-9);
        neatAcetic.AddWater(100);Lab.Settle(neatAcetic);
        Check("water activates acetic equilibrium",neatAcetic.Ph>2.8&&neatAcetic.Ph<3.0&&neatAcetic.Solutes.ContainsKey("CH3COO-"));
        Check("acetic dilution conserves mass",Math.Abs(neatAcetic.Mass()-100-.01*ChemData.Sub["ch3cooh"].Mass)<1e-8);
        AuditAcetic(Check);
        foreach(int setup in new[]{0,1,2,3,4}){
            var nitroReduction=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50,Temp=setup==3?25:80,Mode=setup==4?HeatMode.Distil:HeatMode.Reflux};
            nitroReduction.Solutes["c6h5no2"]=.002;
            if(setup!=1)nitroReduction.Undissolved["fe"]=.003;
            if(setup!=2)nitroReduction.Solutes["H+"]=.006;
            double mass=nitroReduction.Mass();Lab.Settle(nitroReduction);double expected=setup==0?.001:0;
            Check("nitro reduction needs iron acid and heated reflux "+setup,Math.Abs(nitroReduction.Get("aniline")-expected)<1e-9);
            Check("nitro reduction substrate balance "+setup,Math.Abs(nitroReduction.Get("c6h5no2")-(.002-expected))<1e-9);
            double retained=nitroReduction.Mass()+nitroReduction.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            Check("nitro reduction total mass including competing hydrogen "+setup,Math.Abs(retained-mass)<1e-8);
            if(setup==0)Check("nitro reduction consumes iron and produces ferrous ions",nitroReduction.Get("fe")<1e-10&&Math.Abs(nitroReduction.Get("Fe2+")-.003)<1e-9);
        }
        foreach(bool warm in new[]{false,true})foreach(bool sulfuric in new[]{false,true})foreach(bool concentrated in new[]{false,true}){
            var nitrationVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50,Temp=warm?55:25,ConcAcid=concentrated};
            nitrationVessel.Solutes["c6h6"]=.002;nitrationVessel.Solutes["NO3-"]=.001;nitrationVessel.Solutes["H+"]=.01;
            if(sulfuric)nitrationVessel.Solutes["SO4^2-"]=.001;
            double mass=nitrationVessel.Mass(),expected=warm&&sulfuric&&concentrated?.001:0;Lab.Settle(nitrationVessel);
            string tag=$" warm={warm} sulfuric={sulfuric} concentrated={concentrated}";
            Check("nitration requires mixed-acid demonstration conditions"+tag,Math.Abs(nitrationVessel.Get("c6h5no2")-expected)<1e-9);
            Check("nitration consumes actual nitrate"+tag,Math.Abs(nitrationVessel.Get("NO3-")-(.001-expected))<1e-9);
            Check("nitration retains sulfuric catalyst"+tag,Math.Abs(nitrationVessel.Get("SO4^2-")-(sulfuric?.001:0))<1e-9);
            Check("nitration conserves mass"+tag,Math.Abs(nitrationVessel.Mass()-mass)<1e-8);
        }
        foreach(string nitricId in new[]{"hno3","hno3_conc"}){
            var molecularNitration=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=55,ConcAcid=true};
            molecularNitration.Solutes["c6h6"]=.002;molecularNitration.Solutes[nitricId]=.001;molecularNitration.Solutes["h2so4_conc"]=.005;
            double mass=molecularNitration.Mass();Lab.Settle(molecularNitration);
            Check("nitration consumes molecular acid variant "+nitricId,molecularNitration.Get(nitricId)<1e-10&&Math.Abs(molecularNitration.Get("c6h5no2")-.001)<1e-9);
            Check("molecular nitration conserves mass "+nitricId,Math.Abs(molecularNitration.Mass()-mass)<1e-8);
        }
        foreach(string oxidant in new[]{"Cr2O7^2-","MnO4-","none"})foreach(bool acidPresent in new[]{false,true})foreach(bool hot in new[]{false,true}){
            var oxidationVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50,Temp=hot?80:25,Mode=HeatMode.Reflux};
            oxidationVessel.Solutes["c2h5cho"]=.006;
            if(oxidant!="none")oxidationVessel.Solutes[oxidant]=.001;
            if(acidPresent)oxidationVessel.Solutes["H+"]=.02;
            double oxidationMass=oxidationVessel.Mass();Lab.Settle(oxidationVessel);
            double expected=acidPresent&&hot?(oxidant=="Cr2O7^2-"?.003:oxidant=="MnO4-"?.0025:0):0;
            string tag=oxidant+" acid="+acidPresent+" hot="+hot;
            Check("mapped aldehyde oxidation actual reagent limited "+tag,Math.Abs(oxidationVessel.Get("ch3ch2cooh")-expected)<1e-9);
            Check("mapped aldehyde oxidation substrate balance "+tag,Math.Abs(oxidationVessel.Get("c2h5cho")-(.006-expected))<1e-9);
            Check("mapped aldehyde oxidation total mass "+tag,Math.Abs(oxidationVessel.Mass()-oxidationMass)<1e-8);
        }
        foreach(var organicCrRule in Rules.All.Where(r=>r.Cond=="organic_cr_distil"||r.Cond=="organic_cr_reflux")){
            foreach(int organicCrSetup in new[]{0,1,2,3}){
                var crVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50,Temp=organicCrSetup==1?25:80,Mode=organicCrRule.Cond=="organic_cr_distil"?HeatMode.Distil:HeatMode.Reflux};
                if(organicCrSetup==2)crVessel.Mode=HeatMode.Heat;
                string crFrom=organicCrRule.Reactants[0].Id,crTo=organicCrRule.Products[0].Id;
                crVessel.Solutes[crFrom]=.006;crVessel.Solutes["Cr2O7^2-"]=.001;
                if(organicCrSetup!=0)crVessel.Solutes["H+"]=.02;
                double crMass=crVessel.Mass();Lab.Settle(crVessel);
                double crExtent=organicCrSetup==3?.001/organicCrRule.Reactants[1].Coef:0;
                Check("organic Cr conditions and product "+crFrom+crTo+organicCrSetup,Math.Abs(crVessel.Get(crTo)-3*crExtent)<1e-10);
                Check("organic Cr oxidant consumed "+crFrom+crTo+organicCrSetup,Math.Abs(crVessel.Get("Cr2O7^2-")-(organicCrSetup==3?0:.001))<1e-10);
                Check("organic Cr reduced chromium "+crFrom+crTo+organicCrSetup,Math.Abs(crVessel.Get("Cr3+")-(organicCrSetup==3?.002:0))<1e-10);
                Check("organic Cr mass conserved "+crFrom+crTo+organicCrSetup,Math.Abs(crVessel.Mass()-crMass)<1e-8);
            }
        }
        var cyanideDose=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50};
        var transactionA=new Vessel(VesselKind.Beaker,Vector3.zero){Water=40};var transactionB=new Vessel(VesselKind.Beaker,Vector3.zero){Water=10};
        transactionA.Solutes["H+"]=.001;transactionA.Solutes["Cl-"]=.001;transactionA.Undissolved["cu"]=.002;
        transactionB.Solutes["OH-"]=.001;transactionB.Solutes["Na+"]=.001;
        double transactionMass=transactionA.Mass()+transactionB.Mass();float transactionClock=Lab.Clock;
        using(var stagedPour=new PourTransaction(transactionA,transactionB)){
            Check("staged pour liquid solid masses available",stagedPour.LiquidMassGrams>40&&Math.Abs(stagedPour.SolidMassGrams-.002*ChemData.Sub["cu"].Mass)<1e-12);
            Check("staged pour preview accepts independent fractions",stagedPour.Preview(.5,.25,out _));
            Lab.Tick(2);Lab.Settle(transactionB);
            Check("staged pour pauses clock and neutralization",Lab.Clock==transactionClock&&transactionB.Get("H+")==.0005&&transactionB.Get("OH-")==.001);
            Check("staged pour selected solid amount",Math.Abs(transactionB.Get("cu")-.0005)<1e-12&&Math.Abs(transactionA.Get("cu")-.0015)<1e-12);
            Check("staged pour preview mass conserved",Math.Abs(transactionA.Mass()+transactionB.Mass()-transactionMass)<1e-8);
            stagedPour.PreviewFlight(.5,.25,.2,.1,out _);
            Check("staged pour airborne mass counted once",stagedPour.InTransitMassGrams>0&&Math.Abs(transactionA.Mass()+transactionB.Mass()+stagedPour.InTransitMassGrams-transactionMass)<1e-8);
            bool blockedCommit=false;try{stagedPour.Commit();}catch(InvalidOperationException){blockedCommit=true;}
            Check("staged pour cannot commit airborne contents",blockedCommit&&PourTransaction.IsActive);
            stagedPour.Undo();Check("staged pour undo restores original inventories",transactionA.Water==40&&transactionB.Water==10&&transactionB.Get("H+")==0&&transactionA.Get("cu")==.002);
            stagedPour.Preview(.25,.5,out _);stagedPour.Preview(.5,0,out _);
            Check("staged pour reselect is absolute not cumulative",transactionA.Water==20&&transactionB.Water==30&&transactionB.Get("cu")==0);
        }
        Check("staged pour disposal cancels and resumes",!PourTransaction.IsActive&&transactionA.Water==40&&transactionB.Water==10&&transactionB.Get("H+")==0);
        using(var commitPour=new PourTransaction(transactionA,transactionB)){
            commitPour.Preview(.5,0,out _);commitPour.Commit();
            Check("staged pour commit resumes reaction only at end",!PourTransaction.IsActive&&transactionB.Get("H+")<1e-8&&transactionB.Get("OH-")<.000501);
            Check("staged pour commit conserves reaction mass",Math.Abs(transactionA.Mass()+transactionB.Mass()-transactionMass)<1e-8);
        }
        var fullReceiver=new Vessel(VesselKind.Beaker,Vector3.zero){Water=230};
        using(var cappedPreview=new PourTransaction(transactionA,fullReceiver)){
            cappedPreview.Preview(.1,0,out _);double validWater=fullReceiver.Water;
            Check("staged pour rejects overcapacity without erasing valid preview",!cappedPreview.Preview(1,1,out _)&&fullReceiver.Water==validWater);
        }
        var pourSource=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50};
        var pourTarget=new Vessel(VesselKind.Beaker,Vector3.zero){Water=230};
        pourSource.Introduce("nacl",.001,true);pourSource.Gases["o2"]=.002;
        double pourMass=pourSource.Mass()+pourTarget.Mass(),pourRoom=.95*Math.Min(pourTarget.Capacity,pourTarget.InteriorCapacityMl)-pourTarget.Volume()-pourTarget.SolidVolume();
        double pourExpectedFraction=pourRoom/(pourSource.Volume()+pourSource.SolidVolume());
        Lab.PourFraction(pourSource,pourTarget,1,true);
        Check("pour respects receiving safe capacity",Math.Abs(pourTarget.Volume()+pourTarget.SolidVolume()-.95*Math.Min(pourTarget.Capacity,pourTarget.InteriorCapacityMl))<1e-8);
        Check("pour conserves dissolved salt ledger",Math.Abs(pourSource.Ledger["nacl"]+pourTarget.Ledger["nacl"]-.001)<1e-12&&Math.Abs(pourTarget.Ledger["nacl"]-.001*pourExpectedFraction)<1e-12);
        Check("pour leaves headspace gas in source",pourSource.Get("o2")==.002&&pourTarget.Get("o2")==0);
        Check("pour conserves combined condensed mass",Math.Abs(pourSource.Mass()+pourTarget.Mass()-pourMass)<1e-8);
        Check("pour invalid references and fraction rejected",Lab.PourFraction(null,pourTarget,.5,true)==0&&Lab.PourFraction(pourSource,null,.5,true)==0&&Lab.PourFraction(pourSource,pourTarget,double.NaN,true)==0&&Lab.PourFraction(pourSource,pourTarget,double.PositiveInfinity,true)==0);
        var hotWater=new Vessel(VesselKind.Beaker,Vector3.zero){Water=40,Temp=80};
        var coldWater=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=20};
        Lab.PourFraction(hotWater,coldWater,.5,true);
        Check("pour mixes aqueous temperature before reaction",Math.Abs(coldWater.Temp-50)<.01&&Math.Abs(hotWater.Temp-80)<.01);
        var hotStaged=new Vessel(VesselKind.Beaker,Vector3.zero){Water=40,Temp=80};
        var coldStaged=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=20};
        using(var stagedThermal=new PourTransaction(hotStaged,coldStaged)){
            Check("staged preview mixes arrived liquid only",stagedThermal.PreviewFlight(.5,0,.25,0,out _)&&Math.Abs(coldStaged.Temp-40)<.01);
            stagedThermal.Undo();
            Check("staged undo restores both temperatures",Math.Abs(coldStaged.Temp-20)<.01&&Math.Abs(hotStaged.Temp-80)<.01);
            Check("staged reselection recomputes heat from snapshot",stagedThermal.Preview(.5,0,out _)&&Math.Abs(coldStaged.Temp-50)<.01);
            stagedThermal.Commit();
            Check("staged commit keeps mixed temperature",Math.Abs(coldStaged.Temp-50)<.01);
        }
        var warmEthanol=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=40};
        var coolEthanol=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=20};
        warmEthanol.Solutes["c2h5oh"]=.2;coolEthanol.Solutes["c2h5oh"]=.1;
        Lab.PourFraction(warmEthanol,coolEthanol,.5,true);
        EthanolThermodynamics.TryLiquidEnthalpy(40,out double warmEthanolH);
        EthanolThermodynamics.TryLiquidEnthalpy(20,out double coolEthanolH);
        EthanolThermodynamics.TryLiquidEnthalpy(coolEthanol.Temp,out double pouredEthanolH);
        Check("pure ethanol pour conserves variable-Cp enthalpy",coolEthanol.Temp>30&&Math.Abs(2*pouredEthanolH-warmEthanolH-coolEthanolH)<.0001);
        var warmMixed=new Vessel(VesselKind.Beaker,Vector3.zero){Water=10,Temp=40};
        var coolMixed=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=20};
        warmMixed.Solutes["c2h5oh"]=.1;
        Lab.PourFraction(warmMixed,coolMixed,.5,true);
        double expectedMixed=(20*4.18*20+(5*4.18+.05*112.4)*40)/(20*4.18+5*4.18+.05*112.4);
        Check("water ethanol pour weights sourced heat capacities",Math.Abs(coolMixed.Temp-expectedMixed)<.01);
        var unknownHot=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=40};
        var unknownCold=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=20};
        unknownHot.Solutes["n_hexane"]=.1;
        Lab.PourFraction(unknownHot,unknownCold,.5,true);
        Check("missing mixed-solvent heat capacity warns without fabricated temperature",Math.Abs(unknownCold.Temp-20)<.01&&unknownCold.PhaseNotice.Contains("缺少"));
        var coolingAudit=Lab.AddVessel(VesselKind.Beaker);
        coolingAudit.Water=20;coolingAudit.Temp=5;
        Lab.Tick(1);
        Check("unheated cold liquid approaches ambient gradually",Math.Abs(coolingAudit.Temp-(25-20*Math.Exp(-.06)))<.01);
        coolingAudit.Temp=45;
        Lab.Tick(1);
        Check("unheated hot liquid approaches ambient gradually",Math.Abs(coolingAudit.Temp-(25+20*Math.Exp(-.06)))<.01);
        Lab.RemoveVessel(coolingAudit);
        var heatWater=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        Lab.ApplyReactionHeat(heatWater,.001,-10);
        Check("aqueous reaction heat uses actual water inventory",Math.Abs(heatWater.Temp-(25+10/(100*4.18)))<.0001);
        var heatEthanol=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};heatEthanol.Solutes["c2h5oh"]=.1;
        Lab.ApplyReactionHeat(heatEthanol,.001,-10);
        var ethanolModel=PureLiquidThermalModel.Ethanol;
        Check("neat ethanol reaction heat conserves integrated enthalpy",Math.Abs(heatEthanol.Mass()*ethanolModel.H(heatEthanol.Temp)-10)<.001);
        double ethanolIntervalHeat=heatEthanol.Mass()*(ethanolModel.H(heatEthanol.Temp+2)-ethanolModel.H(heatEthanol.Temp));
        Check("ethanol calorimetry integrates temperature dependent capacity",Math.Abs(Lab.EnergyFromTemp(heatEthanol,2)-ethanolIntervalHeat)<1e-8);
        Check("ethanol calorimetry enthalpy inverts energy",Math.Abs(Lab.EnthalpyFromTemp(heatEthanol,2,.001)+ethanolIntervalHeat)<1e-8);
        var heatMixed=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=25};heatMixed.Solutes["c2h5oh"]=.1;
        Lab.ApplyReactionHeat(heatMixed,.001,-10);
        Check("water ethanol reaction heat weights both liquid capacities",Math.Abs(heatMixed.Temp-(25+10/(20*4.18+.1*112.4)))<.0001&&heatMixed.PhaseNotice.Contains("近室温"));
        var heatUnknown=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};heatUnknown.Solutes["n_hexane"]=.1;
        Lab.ApplyReactionHeat(heatUnknown,.001,-10);
        Check("unknown nonwater reaction heat preserves temperature and warns",heatUnknown.Temp==25&&heatUnknown.PhaseNotice.Contains("缺少"));
        Check("unknown nonwater calorimetry does not invent energy",double.IsNaN(Lab.EnergyFromTemp(heatUnknown,2))&&double.IsNaN(Lab.EnthalpyFromTemp(heatUnknown,2,.001)));
        var heatEmpty=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};
        Lab.ApplyReactionHeat(heatEmpty,.001,-10);
        Check("empty vessel reaction heat never substitutes phantom water",heatEmpty.Temp==25&&heatEmpty.PhaseNotice.Contains("缺少"));
        var heatBeyond=new Vessel(VesselKind.Beaker,Vector3.zero){Water=1,Temp=99};
        Lab.ApplyReactionHeat(heatBeyond,.01,-100);
        Check("reaction heat beyond liquid model range leaves temperature and warns",heatBeyond.Temp==99&&heatBeyond.PhaseNotice.Contains("范围"));
        var heatWarningPriority=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25,PhaseNotice="当前试剂过饱和"};
        Lab.ApplyReactionHeat(heatWarningPriority,.001,-10);
        Check("thermal approximation does not hide saturation warning",heatWarningPriority.PhaseNotice=="当前试剂过饱和"&&heatWarningPriority.PhaseNotices.Any(n=>n.StartsWith("thermal:")));
        foreach(double zincMoles in new[]{.001,.01}){
            var unmeasuredReaction=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
            unmeasuredReaction.Undissolved["zn"]=zincMoles;
            unmeasuredReaction.Solutes["H+"]=2*zincMoles;
            unmeasuredReaction.Solutes["Cl-"]=2*zincMoles;
            Lab.Settle(unmeasuredReaction);
            Check("legacy qualitative heat cannot add fixed Celsius zinc="+zincMoles,
                  Math.Abs(unmeasuredReaction.Temp-25)<1e-6&&unmeasuredReaction.PhaseNotice.Contains("定量焓变")&&unmeasuredReaction.Get("Zn2+")>0);
        }
        var measuredNeutralization=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        measuredNeutralization.Solutes["H+"]=.001;measuredNeutralization.Solutes["Cl-"]=.001;
        measuredNeutralization.Solutes["OH-"]=.001;measuredNeutralization.Solutes["Na+"]=.001;
        Lab.Settle(measuredNeutralization);
        Check("measured neutralization applies enthalpy once instead of legacy offset",
              Math.Abs(measuredNeutralization.Temp-(25+57.3/(measuredNeutralization.Water*4.18)))<.001);
        var tinyQuicklime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        int quicklimeLogStart=Lab.Log.Count;
        Lab.AddReagent(tinyQuicklime,"cao",.001);
        Check("tiny quicklime reacts without claiming boiling through legacy addition",
              tinyQuicklime.Get("Ca2+")>0&&tinyQuicklime.Temp>25&&tinyQuicklime.Temp<25.01&&tinyQuicklime.Fx.Bubble==0&&tinyQuicklime.Fx.Smoke==0&&
              !Lab.Log.Skip(quicklimeLogStart).Any(x=>x.Text.Contains("沸腾")));
        var tinyShelfLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        int shelfLimeLogStart=Lab.Log.Count;
        bool shelfLimeAdded=ChemLab.Game.DoseService.TryAdd(tinyShelfLime,"cao",.001,false,out _);
        Check("tiny quicklime shelf dose does not boil or create gas bubbles",
              shelfLimeAdded&&tinyShelfLime.Get("Ca2+")>0&&tinyShelfLime.Temp>25&&tinyShelfLime.Temp<25.01&&tinyShelfLime.Fx.Bubble==0&&tinyShelfLime.Fx.Smoke==0&&
              !Lab.Log.Skip(shelfLimeLogStart).Any(x=>x.Text.Contains("沸腾")));
        var limeSaturation=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        double limeInput=.5/ChemData.Sub["caoh2"].Mass;
        limeSaturation.Introduce("caoh2",limeInput,true);double limeMass=limeSaturation.Mass();
        Lab.Settle(limeSaturation);
        double limeDissolved=Math.Min(limeSaturation.Get("Ca2+"),limeSaturation.Get("OH-")/2);
        Check("calcium hydroxide 25C saturation follows measured 0.02052 M",
              Math.Abs(limeDissolved-.02052*.1)<2e-7&&
              Math.Abs(limeSaturation.Get("caoh2")-(limeInput-limeDissolved))<2e-7&&
              Math.Abs(limeSaturation.Mass()-limeMass)<1e-7);
        limeSaturation.Temp=60;Lab.Settle(limeSaturation);
        double hotLimeDissolved=Math.Min(limeSaturation.Get("Ca2+"),limeSaturation.Get("OH-")/2);
        Check("calcium hydroxide retrograde solubility precipitates at 60C",
              Math.Abs(hotLimeDissolved-.01512*.1)<2e-7&&Math.Abs(limeSaturation.Mass()-limeMass)<1e-7);
        limeSaturation.Temp=25;Lab.Settle(limeSaturation);
        Check("calcium hydroxide cooling redissolves to 25C saturation",
              Math.Abs(Math.Min(limeSaturation.Get("Ca2+"),limeSaturation.Get("OH-")/2)-.02052*.1)<2e-7&&
              Math.Abs(limeSaturation.Mass()-limeMass)<1e-7);
        var strongLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        double caoInput=2/ChemData.Sub["cao"].Mass;double strongLimeMass=strongLime.Mass()+2;
        Lab.AddReagent(strongLime,"cao",2);
        bool strongLimeCapacity=CalciumHydroxideEquilibrium.TryCapacity(strongLime,out double actualLimeCapacity);
        Check("quicklime hydration makes solid hydroxide plus saturated limewater",
              strongLimeCapacity&&Math.Abs(strongLime.Get("Ca2+")-actualLimeCapacity)<3e-7&&
              Math.Abs(strongLime.Get("caoh2")-(caoInput-strongLime.Get("Ca2+")))<3e-7&&
              strongLime.Temp>25&&strongLime.Temp<40&&Math.Abs(strongLime.Mass()-strongLimeMass)<1e-6);
        var commonIonLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        commonIonLime.Solutes["Ca2+"]=.01;commonIonLime.Solutes["OH-"]=.01;
        commonIonLime.Solutes["Na+"]=.01;commonIonLime.Solutes["Cl-"]=.02;
        double commonIonMass=commonIonLime.Mass();Lab.Settle(commonIonLime);
        double commonIonMolarCa=commonIonLime.Get("Ca2+")/.1;
        double commonIonMolarOH=commonIonLime.Get("OH-")/.1;
        Check("calcium hydroxide common ion precipitation obeys concentration product and mass",
              commonIonLime.Get("caoh2")>0&&
              Math.Abs(commonIonMolarCa*commonIonMolarOH*commonIonMolarOH-4*Math.Pow(.02052,3))<1e-8&&
              Math.Abs(commonIonLime.Mass()-commonIonMass)<1e-7);
        var mixedSolventLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        mixedSolventLime.Solutes["c2h5oh"]=.01;mixedSolventLime.Undissolved["caoh2"]=.001;
        Lab.Settle(mixedSolventLime);
        Check("mixed solvent does not borrow pure-water hydroxide solubility",
              mixedSolventLime.Get("Ca2+")==0&&mixedSolventLime.Undissolved["caoh2"]==.001&&
              mixedSolventLime.PhaseNotice.Contains("缺少"));
        var pressureLime=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=2};
        pressureLime.TrappedAirMoles=2*pressureLime.HeadspaceLitres/pressureLime.GasRT;
        pressureLime.Undissolved["caoh2"]=.001;Lab.Settle(pressureLime);
        Check("pressurized hydroxide preserves inventory when solubility data absent",
              pressureLime.Get("Ca2+")==0&&pressureLime.Undissolved["caoh2"]==.001&&
              pressureLime.PhaseNotice.Contains("缺少"));
        pressureLime.PressureBar=1.01325;
        pressureLime.TrappedAirMoles=pressureLime.PressureBar*pressureLime.HeadspaceLitres/pressureLime.GasRT;
        Lab.Settle(pressureLime);
        Check("returning hydroxide to supported pressure dissolves and clears stale warning",
              pressureLime.Get("Ca2+")>.00099&&pressureLime.Get("caoh2")<1e-9&&
              !pressureLime.PhaseNotice.Contains("缺少"));
        foreach(double carbonDioxide in new[]{.005,.01,.02}){
            var slurry=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
            slurry.TrappedAirMoles=slurry.PressureBar*slurry.HeadspaceLitres/slurry.GasRT;
            slurry.Undissolved["caoh2"]=.01;slurry.Gases["co2"]=carbonDioxide;
            double beforeSlurry=slurry.Mass();Lab.Settle(slurry);
            string label=" wet portlandite co2="+carbonDioxide;
            double expectedCarbonate=Math.Min(.01,carbonDioxide);
            if(carbonDioxide<=.01)Check("solid hydroxide carbonates until limiting reactant"+label,
                Math.Abs(slurry.Get("caco3")-expectedCarbonate)<1e-8&&
                Math.Abs(slurry.Get("caoh2")+slurry.Get("Ca2+")-(.01-expectedCarbonate))<1e-8&&
                slurry.Get("co2")<1e-8&&slurry.Turbidity>0);
            else Check("fresh gas first forms visible carbonate before excess dissolves"+label,
                Math.Abs(slurry.Get("caco3")-.01)<1e-8&&slurry.Get("caoh2")<1e-8&&
                slurry.Get("co2")>.009&&slurry.Turbidity>0);
            Check("sealed lime slurry conserves mass and calcium"+label,
                Math.Abs(slurry.Mass()-beforeSlurry)<1e-7&&
                Math.Abs(slurry.Get("caoh2")+slurry.Get("Ca2+")+slurry.Get("caco3")-.01)<1e-8);
        }
        var bicarbonateStage=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
        bicarbonateStage.TrappedAirMoles=bicarbonateStage.PressureBar*bicarbonateStage.HeadspaceLitres/bicarbonateStage.GasRT;
        bicarbonateStage.Precipitates["caco3"]=.001;bicarbonateStage.Solutes["co2"]=.002;
        double beforeBicarbonate=bicarbonateStage.Mass();Lab.Settle(bicarbonateStage);
        Check("dissolved excess carbon dioxide clears carbonate to bicarbonate",
              bicarbonateStage.Get("caco3")<1e-8&&Math.Abs(bicarbonateStage.Get("Ca2+")-.001)<1e-8&&
              Math.Abs(bicarbonateStage.Get("HCO3-")-.002)<1e-8&&bicarbonateStage.Turbidity<.01f&&
              Math.Abs(bicarbonateStage.Mass()-beforeBicarbonate)<1e-7);
        var stagedLimeTest=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
        stagedLimeTest.TrappedAirMoles=stagedLimeTest.PressureBar*stagedLimeTest.HeadspaceLitres/stagedLimeTest.GasRT;
        stagedLimeTest.Undissolved["caoh2"]=.001;stagedLimeTest.Gases["co2"]=.001;
        Lab.Settle(stagedLimeTest);Lab.Settle(stagedLimeTest);
        Check("repeated settling without new carbon dioxide keeps limewater cloudy",
              Math.Abs(stagedLimeTest.Get("caco3")-.001)<1e-8&&stagedLimeTest.Turbidity>0);
        double beforeExtraCarbon=stagedLimeTest.Mass();
        stagedLimeTest.Solutes["co2"]=.001;Lab.Settle(stagedLimeTest);
        Check("later dissolved carbon dioxide clears limewater after initial cloudy stage",
              stagedLimeTest.Get("caco3")<1e-8&&
              Math.Abs(stagedLimeTest.Get("HCO3-")-.002)<1e-8&&stagedLimeTest.Turbidity<.01f&&
              Math.Abs(stagedLimeTest.Mass()-beforeExtraCarbon-ChemData.Sub["co2"].Mass*.001)<1e-7);
        var stagedGasTest=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
        stagedGasTest.TrappedAirMoles=stagedGasTest.PressureBar*stagedGasTest.HeadspaceLitres/stagedGasTest.GasRT;
        stagedGasTest.Undissolved["caoh2"]=.001;stagedGasTest.Gases["co2"]=.001;
        Lab.Settle(stagedGasTest);Lab.Settle(stagedGasTest);
        Check("repeat settling without fresh gas leaves the first carbonate stage",
              Math.Abs(stagedGasTest.Get("caco3")-.001)<1e-8&&stagedGasTest.Turbidity>0);
        double beforeExtraGas=stagedGasTest.Mass();
        stagedGasTest.Gases["co2"]=.001;Lab.Settle(stagedGasTest);
        Check("later headspace carbon dioxide clears existing carbonate",
              stagedGasTest.Get("caco3")<1e-8&&
              Math.Abs(stagedGasTest.Get("HCO3-")-.002)<1e-8&&stagedGasTest.Turbidity<.01f&&
              Math.Abs(stagedGasTest.Mass()-beforeExtraGas-ChemData.Sub["co2"].Mass*.001)<1e-7);
        var dryLimeGas=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};
        dryLimeGas.Undissolved["caoh2"]=.01;dryLimeGas.Gases["co2"]=.01;
        Lab.Settle(dryLimeGas);
        Check("dry solid hydroxide does not borrow wet slurry carbonation route",
              dryLimeGas.Get("caoh2")==.01&&dryLimeGas.Get("co2")==.01&&dryLimeGas.Get("caco3")==0);
        var alcoholLimeGas=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true};
        alcoholLimeGas.Solutes["c2h5oh"]=.01;alcoholLimeGas.Undissolved["caoh2"]=.01;
        alcoholLimeGas.Gases["co2"]=.01;Lab.Settle(alcoholLimeGas);
        Check("mixed solvent does not borrow aqueous slurry carbonation route",
              alcoholLimeGas.Get("caoh2")==.01&&alcoholLimeGas.Get("caco3")==0&&
              alcoholLimeGas.PhaseNotice.Contains("缺少"));
        foreach(double oxygenDose in new[]{0.0,.00475,.0095}){
            var combustion=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true,Temp=25};
            combustion.Solutes["n_hexane"]=.001;
            if(oxygenDose>0)combustion.Gases["o2"]=oxygenDose;
            double before=combustion.Mass();
            Lab.Ignite(combustion);
            double expectedExtent=Math.Min(.001,oxygenDose/9.5);
            Check("closed hexane combustion stoichiometry oxygen="+oxygenDose,
                  Math.Abs(combustion.Get("n_hexane")-(.001-expectedExtent))<1e-10&&
                  Math.Abs(combustion.Get("o2")-(oxygenDose-expectedExtent*9.5))<1e-10&&
                  Math.Abs(combustion.Get("co2")-expectedExtent*6)<1e-10&&
                  Math.Abs(combustion.Water-expectedExtent*7*Formula.MolarMass("H2O"))<1e-9);
            Check("closed hexane conserves mass and never invents temperature oxygen="+oxygenDose,
                  Math.Abs(combustion.Mass()-before)<1e-7&&combustion.Temp==25&&combustion.Fx.Flame==(expectedExtent>0));
        }
        var oxygenOnly=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};oxygenOnly.Gases["o2"]=.01;
        int oxygenOnlyLog=Lab.Log.Count;Lab.Ignite(oxygenOnly);
        Check("oxygen alone cannot trigger fuel ignition or reaction report",
              oxygenOnly.Get("o2")==.01&&!oxygenOnly.Fx.Flame&&
              !Lab.Log.Skip(oxygenOnlyLog).Any(x=>x.Kind=="rxn"));
        var closedMethane=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};closedMethane.Gases["ch4"]=.001;
        Lab.Ignite(closedMethane);
        Check("sealed methane without oxygen cannot burn",closedMethane.Get("ch4")==.001&&closedMethane.Get("co2")==0&&!closedMethane.Fx.Flame);
        var openMethane=new Vessel(VesselKind.Beaker,Vector3.zero);openMethane.Gases["ch4"]=.001;
        Lab.Ignite(openMethane);
        Check("open methane uses ambient oxygen through balanced rule",
              openMethane.Get("ch4")==0&&Math.Abs(openMethane.Get("co2")-.001)<1e-10&&
              Math.Abs(openMethane.Water-.002*Formula.MolarMass("H2O"))<1e-9&&openMethane.Fx.Flame);
        var halogenFuel=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};
        halogenFuel.Solutes["c2h5br"]=.001;halogenFuel.Gases["o2"]=.01;
        Lab.Ignite(halogenFuel);
        Check("generic combustion refuses unsupported heteroatom products",
              halogenFuel.Get("c2h5br")==.001&&halogenFuel.Get("o2")==.01&&!halogenFuel.Fx.Flame);
        var knownCombustionHeat=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true,Water=100,Temp=25};
        knownCombustionHeat.Solutes["c2h5oh"]=.001;knownCombustionHeat.Gases["o2"]=.003;
        Lab.Ignite(knownCombustionHeat);
        Check("known ethanol combustion heat scales with extent and actual water inventory",
              Math.Abs(knownCombustionHeat.Temp-(25+1367/((100+.003*Formula.MolarMass("H2O"))*4.18)))<.001&&
              knownCombustionHeat.Get("c2h5oh")==0);
        foreach(string pouredOrganic in new[]{"c2h5oh","n_hexane","butan_1_ol"}){
            var organicSource=new Vessel(VesselKind.Beaker,Vector3.zero);var organicTarget=new Vessel(VesselKind.Beaker,Vector3.zero);
            organicSource.Solutes[pouredOrganic]=.1;double organicVolume=organicSource.Volume();
            double reportedPour=Lab.PourFraction(organicSource,organicTarget,.4,true);
            Check("nonwater pour reports transferred volume "+pouredOrganic,Math.Abs(reportedPour-organicVolume*.4)<1e-10&&reportedPour>0);
            Check("nonwater pour inventory and receiver volume "+pouredOrganic,Math.Abs(organicSource.Get(pouredOrganic)-.06)<1e-12&&Math.Abs(organicTarget.Get(pouredOrganic)-.04)<1e-12&&Math.Abs(organicTarget.Volume()-reportedPour)<1e-10);
        }
        foreach(string nitrile in new[]{"ch3cn","c2h5cn"})foreach(int hydrolysisSetup in new[]{0,1,2,3,4}){
            var hydrolysisVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=hydrolysisSetup==3?0:50,Temp=hydrolysisSetup==1?25:80,Mode=hydrolysisSetup==4?HeatMode.Distil:HeatMode.Reflux};
            hydrolysisVessel.Solutes[nitrile]=.002;
            if(hydrolysisSetup!=2){hydrolysisVessel.Solutes["H+"]=.001;hydrolysisVessel.Solutes["Cl-"]=.001;}
            double hydrolysisMass=hydrolysisVessel.Mass(),hydrolysisWater=hydrolysisVessel.Water;
            Lab.Settle(hydrolysisVessel);double expectedHydrolysis=hydrolysisSetup==0?.001:0;
            string hydrolysisLabel=nitrile+" setup="+hydrolysisSetup;
            Check("nitrile hydrolysis substrate limited "+hydrolysisLabel,Math.Abs(hydrolysisVessel.Get(nitrile)-(.002-expectedHydrolysis))<1e-10);
            Check("nitrile hydrolysis retains nitrogen "+hydrolysisLabel,Math.Abs(hydrolysisVessel.Get("NH4+")-expectedHydrolysis)<1e-8);
            Check("nitrile hydrolysis correct acid "+hydrolysisLabel,Math.Abs(hydrolysisVessel.Get(nitrile=="ch3cn"?"ch3cooh":"ch3ch2cooh")-expectedHydrolysis)<1e-8);
            Check("nitrile hydrolysis consumes two waters "+hydrolysisLabel,Math.Abs(hydrolysisVessel.Water-(hydrolysisWater-2*expectedHydrolysis*ChemData.Sub["h2o"].Mass))<1e-8);
            Check("nitrile hydrolysis mass conserved "+hydrolysisLabel,Math.Abs(hydrolysisVessel.Mass()-hydrolysisMass)<1e-8);
        }
        foreach(string acyl in new[]{"ch3cocl","c6h5cocl"})foreach(double waterMol in new[]{0,.001,.01}){
            var acylVessel=new Vessel(VesselKind.Bottle,Vector3.zero){Water=waterMol*ChemData.Sub["h2o"].Mass,Temp=40};
            // Open vessel without a timed evaporation step: isolate reaction
            // stoichiometry from the separate closed-headspace vapor equilibrium.
            acylVessel.Solutes[acyl]=.002;
            double mass=acylVessel.Mass(),water=acylVessel.Water,extent=Math.Min(.002,waterMol);
            Lab.Settle(acylVessel);string label=acyl+" waterMol="+waterMol;
            Check("acyl hydrolysis limited by actual water "+label,Math.Abs(acylVessel.Get(acyl)-(.002-extent))<1e-9);
            double acid=acylVessel.Get(acyl=="ch3cocl"?"ch3cooh":"c6h5cooh")+(acyl=="ch3cocl"?acylVessel.Get("CH3COO-"):0);
            Check("acyl hydrolysis forms correct acid "+label,Math.Abs(acid-extent)<1e-8);
            Check("acyl hydrolysis consumes stoichiometric water "+label,Math.Abs(acylVessel.Water-(water-extent*ChemData.Sub["h2o"].Mass))<1e-8);
            double retainedMass=acylVessel.Mass()+acylVessel.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            Check("acyl hydrolysis mass including HCl conserved "+label,Math.Abs(retainedMass-mass)<1e-8);
        }
        foreach(var pair in new[]{(VesselKind.Burette,VesselKind.Beaker),(VesselKind.Pipette,VesselKind.Beaker),(VesselKind.Beaker,VesselKind.Pipette)}){
            bool rejected=false;try{using(var forbidden=new PourTransaction(new Vessel(pair.Item1,Vector3.zero),new Vessel(pair.Item2,Vector3.zero))){} }catch(ArgumentException){rejected=true;}
            Check("dedicated volumetric tool rejects staged pour "+pair,rejected&&!PourTransaction.IsActive);
        }
        var buretteSource=new Vessel(VesselKind.Beaker,Vector3.zero){Water=60};var buretteTarget=new Vessel(VesselKind.Burette,Vector3.zero);
        var reservoir=new Vessel(VesselKind.Beaker,Vector3.zero){Water=30};var pipetteTool=new Vessel(VesselKind.Pipette,Vector3.zero){AttachedTo=reservoir};
        var attachmentTool=new Vessel(VesselKind.Pipette,Vector3.zero);var attachmentTarget=new Vessel(VesselKind.Beaker,new Vector3(2,0,3));
        Check("metered attachment positions tool above receiver",ChemLab.Game.SnapController.AttachMetered(attachmentTool,attachmentTarget)&&attachmentTool.AttachedTo==attachmentTarget&&attachmentTool.Pos==attachmentTarget.Pos&&attachmentTool.LiftY>ChemLab.Game.VesselMesh.Get(attachmentTarget.Kind).H);
        Check("metered attachment rejects itself",!ChemLab.Game.SnapController.AttachMetered(attachmentTool,attachmentTool));
        Check("metered attachment rejects another metered tool",!ChemLab.Game.SnapController.AttachMetered(attachmentTool,new Vessel(VesselKind.Burette,Vector3.zero)));
        attachmentTarget.AttachedTo=attachmentTool;Check("metered cyclic relationship cannot transfer",MeteredTransfer.Move(attachmentTool,1)==0&&!MeteredTransfer.CanAttach(attachmentTool,attachmentTarget));attachmentTarget.AttachedTo=null;
        foreach(var kind in new[]{VesselKind.Burette,VesselKind.Pipette}){
            var shelfTool=new Vessel(kind,Vector3.zero);double cap=shelfTool.Capacity;
            Check("shelf fills metered tool to nominal capacity "+kind,ChemLab.Game.DoseService.TryAdd(shelfTool,"h2o",cap,false,out _)&&Math.Abs(shelfTool.Volume()-cap)<1e-9);
            Check("shelf rejects metered tool overflow "+kind,!ChemLab.Game.DoseService.TryAdd(shelfTool,"h2o",.01,false,out _)&&Math.Abs(shelfTool.Volume()-cap)<1e-9);
        }
        reservoir.Solutes["Na+"]=.001;reservoir.Solutes["Cl-"]=.001;reservoir.Undissolved["zn"]=.002;
        double meteredMass=reservoir.Mass()+pipetteTool.Mass();
        foreach(bool deposit in new[]{false,true}){
            var pendingLiquidSource=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20};
            var probe=new Vessel(VesselKind.Pipette,Vector3.zero){AttachedTo=pendingLiquidSource};
            double ethanolMol=10*PhaseProperties.Density("c2h5oh",25)/ChemData.Sub["c2h5oh"].Mass;
            (deposit?pendingLiquidSource.Precipitates:pendingLiquidSource.Undissolved)["c2h5oh"]=ethanolMol;
            pendingLiquidSource.Undissolved["zn"]=.002;
            double before=pendingLiquidSource.Mass()+probe.Mass();
            Check("pending liquid counts in volume "+deposit,Math.Abs(pendingLiquidSource.Volume()-30)<1e-9);
            double moved=MeteredTransfer.Move(probe,-40);
            Check("pending liquid aspiration respects capacity "+deposit,Math.Abs(moved-25)<1e-9&&Math.Abs(probe.Volume()-25)<1e-8&&Math.Abs(pendingLiquidSource.Volume()-5)<1e-8);
            Check("pending liquid transfers proportionally and leaves solid "+deposit,Math.Abs(probe.Get("c2h5oh")-ethanolMol*25/30)<1e-10&&probe.Get("zn")==0&&Math.Abs(pendingLiquidSource.Get("zn")-.002)<1e-10);
            Check("pending liquid transfer mass conserved "+deposit,Math.Abs(pendingLiquidSource.Mass()+probe.Mass()-before)<1e-8);
        }
        Check("pipette aspiration stops at nominal capacity",Math.Abs(MeteredTransfer.Move(pipetteTool,-30)-25)<1e-9&&Math.Abs(pipetteTool.Water-25)<1e-9&&Math.Abs(reservoir.Water-5)<1e-9);
        Check("pipette carries dissolved solute but not solid",Math.Abs(pipetteTool.Get("Na+")-.001*25/30)<1e-10&&pipetteTool.Get("zn")==0&&Math.Abs(reservoir.Get("zn")-.002)<1e-10);
        Check("pipette full aspiration transfers nothing",MeteredTransfer.Move(pipetteTool,-1)==0);
        Check("pipette release selected volume",Math.Abs(MeteredTransfer.Move(pipetteTool,10)-10)<1e-9&&Math.Abs(pipetteTool.Water-15)<1e-9);
        var hotReservoir=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50,Temp=80};
        var coolReceiver=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=20};
        var thermalPipette=new Vessel(VesselKind.Pipette,Vector3.zero){AttachedTo=hotReservoir};
        Check("empty pipette adopts aspirated liquid temperature",Math.Abs(MeteredTransfer.Move(thermalPipette,-10)-10)<1e-9&&Math.Abs(thermalPipette.Temp-80)<.01);
        thermalPipette.AttachedTo=coolReceiver;
        Check("pipette release mixes target temperature",Math.Abs(MeteredTransfer.Move(thermalPipette,10)-10)<1e-9&&Math.Abs(coolReceiver.Temp-40)<.01);
        Check("metered transfer conserves total mass",Math.Abs(reservoir.Mass()+pipetteTool.Mass()-meteredMass)<1e-8);
        var titrationAcid=new Vessel(VesselKind.Flask,Vector3.zero){Water=25};var titrationBase=new Vessel(VesselKind.Burette,Vector3.zero){Water=50,AttachedTo=titrationAcid};
        titrationAcid.Solutes["H+"]=.0025;titrationAcid.Solutes["Cl-"]=.0025;
        titrationBase.Solutes["Na+"]=.005;titrationBase.Solutes["OH-"]=.005;
        double titrationMass=titrationAcid.Mass()+titrationBase.Mass();
        MeteredTransfer.Move(titrationBase,24);
        Check("burette titration remains acidic before equivalence",Math.Abs(titrationAcid.Get("H+")-.0001)<1e-9&&titrationAcid.Ph<4);
        MeteredTransfer.Move(titrationBase,1);
        Check("burette titration reaches strong acid equivalence at 25 mL",titrationAcid.Get("H+")<1e-8&&titrationAcid.Get("OH-")<1e-8&&Math.Abs(titrationAcid.Ph-7)<.1);
        MeteredTransfer.Move(titrationBase,1);
        Check("burette titration becomes alkaline after equivalence",Math.Abs(titrationAcid.Get("OH-")-.0001)<1e-9&&titrationAcid.Ph>10);
        Check("burette titration conserves mass and spectator ions",Math.Abs(titrationAcid.Mass()+titrationBase.Mass()-titrationMass)<1e-8&&Math.Abs(titrationAcid.Get("Na+")-.0026)<1e-9&&Math.Abs(titrationAcid.Get("Cl-")-.0025)<1e-9);
        var dosingBurette=new Vessel(VesselKind.Burette,Vector3.zero){Water=10,AttachedTo=reservoir};
        Check("burette cannot aspirate",MeteredTransfer.Move(dosingBurette,-1)==0&&dosingBurette.Water==10);
        Check("burette dispenses actual requested liquid",Math.Abs(MeteredTransfer.Move(dosingBurette,.25)-.25)<1e-9&&Math.Abs(dosingBurette.Water-9.75)<1e-9);
        using(var pauseTransfer=new PourTransaction(new Vessel(VesselKind.Beaker,Vector3.zero),new Vessel(VesselKind.Beaker,Vector3.zero)))Check("staged pause blocks metered liquid transfer",MeteredTransfer.Move(dosingBurette,1)==0);
        dosingBurette.AttachedTo=null;Check("detached burette cannot dispense",MeteredTransfer.Move(dosingBurette,1)==0);
        using(var fillBurette=new PourTransaction(buretteSource,buretteTarget)){
            Check("burette accepts exactly nominal capacity",fillBurette.Preview(50.0/60,0,out _)&&Math.Abs(buretteTarget.Water-50)<1e-9);
            Check("burette rejects above nominal without modifying preview",!fillBurette.Preview(51.0/60,0,out _)&&Math.Abs(buretteTarget.Water-50)<1e-9);
        }
        double cyanideDoseMass=cyanideDose.Mass();
        cyanideDose.Introduce("kcn",.001,true);
        Check("KCN supplies actual cyanide",Math.Abs(cyanideDose.Get("CN-")-.001)<1e-10&&Math.Abs(cyanideDose.Get("K+")-.001)<1e-10);
        Check("KCN dissolution mass",Math.Abs(cyanideDose.Mass()-cyanideDoseMass-.001*ChemData.Sub["kcn"].Mass)<1e-8);
        foreach(string alkylHalide in new[]{"c2h5br","ch3i"})foreach(bool makeAmine in new[]{false,true})foreach(int substitutionSetup in new[]{0,1,2,3}){
            var substitutionVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=1,Temp=substitutionSetup==1?25:80};
            if(substitutionSetup!=2)substitutionVessel.Solutes["c2h5oh"]=.1;
            substitutionVessel.Solutes[alkylHalide]=.002;
            string nucleophile=makeAmine?"nh3":"CN-";
            double initialNucleophile=substitutionSetup==3?0:makeAmine?.002:.001;
            substitutionVessel.Solutes[nucleophile]=initialNucleophile;
            if(!makeAmine)substitutionVessel.Solutes["K+"]=initialNucleophile;
            double SubstitutionMass()=>substitutionVessel.Mass()+substitutionVessel.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            double initialSubstitutionMass=SubstitutionMass();
            Lab.Settle(substitutionVessel);
            double substitutionExtent=substitutionSetup==0?.001:0;
            string substitutionProduct=alkylHalide=="ch3i"?(makeAmine?"ch3nh2":"ch3cn"):(makeAmine?"c2h5nh2":"c2h5cn");
            string substitutionLabel=alkylHalide+" amine="+makeAmine+" setup="+substitutionSetup;
            Check("substitution product "+substitutionLabel,Math.Abs(substitutionVessel.Get(substitutionProduct)-substitutionExtent)<1e-10);
            Check("substitution reagent limit "+substitutionLabel,Math.Abs(substitutionVessel.Get(alkylHalide)-(.002-substitutionExtent))<1e-10&&Math.Abs(substitutionVessel.Get(nucleophile)-(initialNucleophile-substitutionExtent*(makeAmine?2:1)))<1e-10);
            Check("substitution halide "+substitutionLabel,Math.Abs(substitutionVessel.Get(alkylHalide=="ch3i"?"I-":"Br-")-substitutionExtent)<1e-10);
            Check("substitution ammonium "+substitutionLabel,Math.Abs(substitutionVessel.Get("NH4+")-(makeAmine?substitutionExtent:0))<1e-10);
            Check("substitution mass "+substitutionLabel,Math.Abs(SubstitutionMass()-initialSubstitutionMass)<1e-8);
        }
        foreach(bool ethanolHalide in new[]{false,true})foreach(bool hotHalide in new[]{false,true}){
            var halideVessel=new Vessel(VesselKind.Beaker,Vector3.zero){Water=ethanolHalide?1:50,Temp=hotHalide?80:25};
            if(ethanolHalide)halideVessel.Solutes["c2h5oh"]=.1;
            halideVessel.Solutes["c2h5br"]=.002;halideVessel.Solutes["OH-"]=.001;halideVessel.Solutes["Na+"]=.001;
            double HalideMass()=>halideVessel.Mass()+halideVessel.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            double initialHalideMass=HalideMass(),initialAlcohol=halideVessel.Get("c2h5oh");
            Lab.Settle(halideVessel);double halideExtent=hotHalide?.001:0;
            Check("halide hydroxide limits conversion "+ethanolHalide+hotHalide,Math.Abs(halideVessel.Get("c2h5br")-(.002-halideExtent))<1e-10&&Math.Abs(halideVessel.Get("OH-")-(.001-halideExtent))<1e-10);
            Check("halide ion produced "+ethanolHalide+hotHalide,Math.Abs(halideVessel.Get("Br-")-halideExtent)<1e-10);
            Check("halide correct solvent product "+ethanolHalide+hotHalide,Math.Abs((ethanolHalide?halideVessel.Get("c2h4"):halideVessel.Get("c2h5oh")-initialAlcohol)-halideExtent)<1e-10);
            Check("hydroxide is not fictitious ammonia "+ethanolHalide+hotHalide,halideVessel.Get("c2h5nh2")==0);
            Check("halide conversion mass "+ethanolHalide+hotHalide,Math.Abs(HalideMass()-initialHalideMass)<1e-8);
        }
        foreach(var hydrogenRule in Rules.All.Where(r=>r.Cond=="nickel_hydrogenation")){
            string alkeneReactant=hydrogenRule.Reactants[0].Id,alkaneProduct=hydrogenRule.Products[0].Id;
            foreach(int setup in new[]{0,1,2,3}){
                var hydrogenVessel=new Vessel(VesselKind.Beaker,Vector3.zero);
                hydrogenVessel.Temp=setup==1?25:150;
                hydrogenVessel.Introduce(alkeneReactant,.002,false,false);
                if(setup!=2)hydrogenVessel.Gases["h2"]=.001;
                if(setup!=0)hydrogenVessel.Undissolved["ni"]=.0001;
                double HydrogenMass(Vessel x)=>x.Mass()+x.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
                double beforeHydrogenMass=HydrogenMass(hydrogenVessel);
                Lab.Settle(hydrogenVessel);
                double expectedHydrogenExtent=setup==3?.001:0;
                Check("alkene hydrogenation condition "+alkeneReactant+" "+setup,Math.Abs(hydrogenVessel.Get(alkaneProduct)-expectedHydrogenExtent)<1e-10);
                Check("alkene hydrogenation reactant conserved "+alkeneReactant+" "+setup,Math.Abs(hydrogenVessel.Get(alkeneReactant)-(.002-expectedHydrogenExtent))<1e-10);
                Check("alkene hydrogenation mass conserved "+alkeneReactant+" "+setup,Math.Abs(HydrogenMass(hydrogenVessel)-beforeHydrogenMass)<1e-8);
                Check("alkene catalyst retained "+alkeneReactant+" "+setup,Math.Abs(hydrogenVessel.Get("ni")-(setup==0?0:.0001))<1e-12);
            }
        }
        foreach(string acidOrganicId in new[]{"butanoic_acid","pentanoic_acid","hexanoic_acid","heptanoic_acid","octanoic_acid"}){
            var acidOrganic=ChemData.Sub[acidOrganicId];var acidOrganicPhase=PhaseProperties.Data[acidOrganicId];
            Check("new organic acid ambient liquid "+acidOrganicId,PhaseProperties.Phase(acidOrganicId)=="l");
            Check("new organic acid id and name "+acidOrganicId,ChemData.Resolve(acidOrganicId)==acidOrganicId&&ChemData.Resolve(acidOrganic.Name)==acidOrganicId);
            Check("new organic acid phase thresholds "+acidOrganicId,PhaseProperties.Phase(acidOrganicId,acidOrganicPhase.Melt-1)=="s"&&PhaseProperties.Phase(acidOrganicId,acidOrganicPhase.Boil+1)=="g");
            var acidOrganicVessel=new Vessel(VesselKind.Beaker,Vector3.zero);acidOrganicVessel.AddSubstance(acidOrganicId,.001);
            Check("new organic acid dry dose mass "+acidOrganicId,Math.Abs(acidOrganicVessel.Mass()-.001*acidOrganic.Mass)<1e-10&&acidOrganicVessel.Undissolved.Count==0);
        }
        foreach(string aldehydeId in new[]{"butanal","pentanal","hexanal","heptanal","octanal"}){
            var aldehyde=ChemData.Sub[aldehydeId];var aldehydePhase=PhaseProperties.Data[aldehydeId];
            Check("new aldehyde ambient liquid "+aldehydeId,PhaseProperties.Phase(aldehydeId)=="l");
            Check("new aldehyde id and name "+aldehydeId,ChemData.Resolve(aldehydeId)==aldehydeId&&ChemData.Resolve(aldehyde.Name)==aldehydeId);
            Check("new aldehyde phase thresholds "+aldehydeId,PhaseProperties.Phase(aldehydeId,aldehydePhase.Melt-1)=="s"&&PhaseProperties.Phase(aldehydeId,aldehydePhase.Boil+1)=="g");
            var aldehydeVessel=new Vessel(VesselKind.Beaker,Vector3.zero);aldehydeVessel.AddSubstance(aldehydeId,.001);
            Check("new aldehyde dry dose mass "+aldehydeId,Math.Abs(aldehydeVessel.Mass()-.001*aldehyde.Mass)<1e-10&&aldehydeVessel.Undissolved.Count==0);
        }
        foreach(string alkeneId in new[]{"but_1_ene","pent_1_ene","hex_1_ene","hept_1_ene","oct_1_ene"}){
            var alkene=ChemData.Sub[alkeneId];var alkenePhase=PhaseProperties.Data[alkeneId];
            Check("new alkene ambient phase "+alkeneId,PhaseProperties.Phase(alkeneId)==(alkeneId=="but_1_ene"?"g":"l"));
            Check("new alkene unambiguous id "+alkeneId,ChemData.Resolve(alkeneId)==alkeneId&&ChemData.Resolve(alkene.Name)==alkeneId);
            Check("new alkene phase thresholds "+alkeneId,PhaseProperties.Phase(alkeneId,alkenePhase.Melt-1)=="s"&&PhaseProperties.Phase(alkeneId,alkenePhase.Boil+1)=="g");
            var alkeneVessel=new Vessel(VesselKind.Beaker,Vector3.zero);alkeneVessel.AddSubstance(alkeneId,.001);
            double alkeneTotalMass=alkeneVessel.Mass()+alkeneVessel.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            Check("new alkene dose mass including gas "+alkeneId,Math.Abs(alkeneTotalMass-.001*alkene.Mass)<1e-10&&alkeneVessel.Undissolved.Count==0);
        }
        foreach(string organicId in new[]{"n_pentane","n_hexane","n_heptane","n_octane","butan_1_ol","pentan_1_ol","hexan_1_ol","heptan_1_ol","octan_1_ol"}){
            var organic=ChemData.Sub[organicId];var physical=PhaseProperties.Data[organicId];
            Check("new organic room phase "+organicId,organic.State=="l"&&PhaseProperties.Phase(organicId)=="l");
            Check("new organic formula index "+organicId,ChemData.SpeciesIndex.TryGetValue(organic.Formula.ToLowerInvariant(),out var indexedOrganic)&&indexedOrganic==organicId);
            Check("new organic normal-pressure phase thresholds "+organicId,PhaseProperties.Phase(organicId,physical.Melt-1)=="s"&&PhaseProperties.Phase(organicId,physical.Boil+1)=="g");
            var organicVessel=new Vessel(VesselKind.Beaker,Vector3.zero);organicVessel.AddSubstance(organicId,.001);
            Check("new organic dry dose mass "+organicId,Math.Abs(organicVessel.Mass()-.001*organic.Mass)<1e-10&&organicVessel.Undissolved.Count==0);
        }
        foreach(string drySaltId in new[]{"nacl","bacl2"}){
            var drySalt=new Vessel(VesselKind.Beaker,Vector3.zero);drySalt.Water=.1;
            drySalt.AddSubstance(drySaltId,1e-5);double dryMass=drySalt.Mass()-.1;
            drySalt.Temp=110;Lab.Evaporate(drySalt,1);
            Check("evaporated salt leaves no aqueous ions "+drySaltId,drySalt.Water==0&&drySalt.Solutes.Count==0&&Math.Abs(drySalt.Get(drySaltId)-1e-5)<1e-12);
            Check("dry salt recovery conserves mass "+drySaltId,Math.Abs(drySalt.Mass()-dryMass)<1e-10);
            drySalt.RecoverDrySalts();Check("dry salt recovery is idempotent "+drySaltId,Math.Abs(drySalt.Get(drySaltId)-1e-5)<1e-12);
        }
        foreach(double waterMl in new[]{.01,.5,50}){
            var saltMicro=new Vessel(VesselKind.Beaker,Vector3.zero);double saltMol=waterMl/1000*.1;
            saltMicro.AddSubstance("nacl",saltMol);saltMicro.AddWater(waterMl);
            Check("solid first dissolves in sub-mL water "+waterMl,saltMicro.Undissolved.Count==0&&Math.Abs(saltMicro.Get("Na+")-saltMol)<1e-12);
            var sodiumWater=new Vessel(VesselKind.Beaker,Vector3.zero);sodiumWater.Water=waterMl;
            double incoming=.001,availableWater=waterMl/Formula.MolarMass("H2O"),reacted=Math.Min(incoming,availableWater);
            Lab.AddReagent(sodiumWater,"na",incoming*ChemData.Sub["na"].Mass);
            Check("water reaction registers incoming reagent "+waterMl,Math.Abs(sodiumWater.Get("Na+")-reacted)<1e-10);
            Check("water-limited reaction retains unused sodium "+waterMl,Math.Abs(sodiumWater.Get("na")-(incoming-reacted))<1e-10);
            double gasMass=sodiumWater.Gases.Sum(p=>p.Value*ChemData.Sub[p.Key].Mass);
            Check("water reaction total mass including gas "+waterMl,Math.Abs(sodiumWater.Mass()+gasMass-waterMl-incoming*ChemData.Sub["na"].Mass)<1e-8);
        }
        var conditionMethod=typeof(Lab).GetMethod("CheckCond",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        bool Concentrated(Vessel sample)=>(bool)conditionMethod.Invoke(null,new object[]{sample,new Rules.Rule{Cond="conc_nh3"}});
        var ammoniaCondition=new Vessel(VesselKind.Beaker,Vector3.zero);ammoniaCondition.Gases["nh3"]=.1;
        Check("dry ammonia gas is not concentrated ammonia solution",!Concentrated(ammoniaCondition));
        ammoniaCondition.Water=.01;
        Check("headspace ammonia cannot satisfy dissolved concentration gate",!Concentrated(ammoniaCondition));
        ammoniaCondition.Solutes["nh3"]=.00008;
        Check("microvolume concentrated ammonia uses actual dissolved molarity",Concentrated(ammoniaCondition));
        ammoniaCondition.Solutes["c2h5oh"]=.001;
        Check("aqueous ammonia concentration gate rejects unsupported mixed solvent",!Concentrated(ammoniaCondition));
        foreach(double microlabMl in new[]{.01,.1,.5,1,10}){
            var micro=new Vessel(VesselKind.Beaker,Vector3.zero);micro.Water=microlabMl;
            micro.Solutes["H+"]=microlabMl/1000*.1;micro.Solutes["Cl-"]=microlabMl/1000*.1;
            Check("microvolume strong acid pH scale invariance "+microlabMl,Math.Abs(micro.Ph-1)<1e-10);
            var acidLine=ChemLab.Chem.VesselView.IonView(micro).First(p=>p.Id=="H+");
            Check("microvolume label uses actual solvent volume "+microlabMl,Math.Abs(acidLine.Conc-.1)<1e-10);
            micro.Solutes.Clear();micro.Solutes["ch3cooh"]=microlabMl/1000*.1;
            PhaseProperties.Reconcile(micro);
            Check("microvolume weak acid pH scale invariance "+microlabMl,micro.Ph>2.8&&micro.Ph<3.0);
        }
        var driedLabel=new Vessel(VesselKind.Beaker,Vector3.zero);
        driedLabel.Solutes["Na+"]=.001;
        var dryIonLine=ChemLab.Chem.VesselView.IonView(driedLabel).First(p=>p.Id=="Na+");
        Check("dry residue has undefined rather than invented molar concentration",double.IsNaN(dryIonLine.Conc));
        var waterStoich=new Vessel(VesselKind.Beaker,Vector3.zero);double waterMolarMass=Formula.MolarMass("H2O");
        waterStoich.Produce("h2o",.5);
        Check("reaction-produced water uses elemental molar mass",Math.Abs(waterStoich.Mass()-.5*waterMolarMass)<1e-12&&Math.Abs(waterStoich.Get("h2o")-.5)<1e-12);
        double waterTaken=waterStoich.TakeAny("h2o",.2);
        Check("reaction water consumption conserves mass",Math.Abs(waterTaken-.2)<1e-12&&Math.Abs(waterStoich.Mass()-.3*waterMolarMass)<1e-12);
        var cylinderGeometry=ChemLab.Game.VesselMesh.Get(VesselKind.Cylinder);
        var pipetteGeometry=ChemLab.Game.VesselMesh.Get(VesselKind.Pipette);var buretteGeometry=ChemLab.Game.VesselMesh.Get(VesselKind.Burette);
        Check("25 mL pipette manufacturer length",Math.Abs(pipetteGeometry.H*100-530)<.001);
        Check("50 mL burette manufacturer height",Math.Abs(buretteGeometry.H*100-820)<.001);
        Check("pipette bulb integrates to single mark in upper stem",Math.Abs(ChemLab.Game.VesselGraduations.VolumeBelow(VesselKind.Pipette,4.5f)-25)<.002&&Math.Abs(ChemLab.Game.VesselGraduations.Marks(VesselKind.Pipette)[0].Height-4.5f)<.002);
        Check("100 mL cylinder manufacturer body dimensions",Math.Abs(cylinderGeometry.R*200-29)<1e-5&&Math.Abs(cylinderGeometry.H*100-256)<1e-5);
        Check("cylinder solid foot is excluded from liquid capacity",ChemLab.Game.VesselGraduations.VolumeBelow(VesselKind.Cylinder,.04f)<.001);
        var cylinderFoot=ChemLab.Game.VesselMesh.BuildCylinderFoot();
        Check("cylinder hexagonal foot sits on bench",Math.Abs(cylinderFoot.bounds.min.y)<1e-7&&Math.Abs(cylinderFoot.bounds.max.y-.04)<1e-7&&Math.Abs(cylinderFoot.bounds.size.x-.6)<1e-7);
        UnityEngine.Object.DestroyImmediate(cylinderFoot);
        foreach(string id in new[]{"h2so4","hno3"}){
            var dryAcid=new Vessel(VesselKind.Beaker,Vector3.zero);
            double expected=0.001*ChemData.Sub[id].Mass;
            Check("pure shelf ambient liquid acid "+id,PhaseProperties.Phase(id,25)=="l");
            Check("pure shelf dry liquid acid dose "+id,
                ChemLab.Game.DoseService.TryAdd(dryAcid,id,0.001,true,out _)&&dryAcid.Water==0&&
                dryAcid.Solutes.ContainsKey(id)&&dryAcid.Undissolved.Count==0&&
                Math.Abs(dryAcid.Mass()-expected)<1e-9&&
                Math.Abs(dryAcid.LiquidVolume()-expected/(id=="h2so4"?1.8:1.5129))<1e-9);
        }
        foreach(string id in new[]{"h2s","hf"}){
            var dryGas=new Vessel(VesselKind.Beaker,Vector3.zero);
            Check("pure shelf ambient acid gas "+id,PhaseProperties.Phase(id,25)=="g");
            Check("pure shelf dry acid gas dose "+id,
                ChemLab.Game.DoseService.TryAdd(dryGas,id,0.001,true,out _)&&
                dryGas.Gases.TryGetValue(id,out double gas)&&Math.Abs(gas-0.001)<1e-12&&
                dryGas.Solutes.Count==0&&dryGas.Undissolved.Count==0);
        }
        foreach(string id in new[]{"h2co3","hclo","hno2"}){
            var drySolution=new Vessel(VesselKind.Beaker,Vector3.zero);
            Check("solution-only shelf still needs solvent "+id,
                PhaseProperties.Phase(id,25)=="aq"&&
                !ChemLab.Game.DoseService.TryAdd(drySolution,id,0.001,true,out _)&&
                drySolution.Get(id)==0&&drySolution.Water==0);
        }
        foreach(string id in new[]{"hcl","hbr","hi","ch3cho","c2h5nh2"}){
            var dry=new Vessel(VesselKind.Beaker,Vector3.zero);
            Check("pure shelf ambient gas "+id,PhaseProperties.Phase(id,25)=="g");
            Check("pure shelf dry gas dose "+id,ChemLab.Game.DoseService.TryAdd(dry,id,0.001,true,out _)&&dry.Gases.TryGetValue(id,out double gas)&&Math.Abs(gas-0.001)<1e-12&&dry.Solutes.Count==0&&dry.Undissolved.Count==0);
        }
        Check("acetaldehyde is liquid below normal boiling point",PhaseProperties.Phase("ch3cho",10)=="l");
        ammonia.Solutes.TryGetValue("OH-",out double hydroxide);
        Check("ammonia carrier accounts for water consumed in ionization",Math.Abs(ammonia.Water+hydroxide*Formula.MolarMass("H2O")-10)<0.001);
        Check("ammonia is alkaline solution, never powder",ammonia.Ph>7&&ammonia.Undissolved.Count==0&&ammonia.Precipitates.Count==0);
        var zinc=new Vessel(VesselKind.Beaker,Vector3.zero);zinc.AddSubstance("zn",0.01);
        Check("unreacted zinc label is solid not precipitate",ChemLab.Chem.VesselView.AllLines(zinc,true).Any(x=>x.Id=="zn"&&x.IsUndissolved&&!x.IsPrecipitate));
        var salt=new Vessel(VesselKind.Beaker,Vector3.zero);salt.AddWater(20);salt.Introduce("nacl",0.001,true);
        Check("compound ledger records amount only once",salt.Ledger.TryGetValue("nacl",out var amount)&&Math.Abs(amount-0.001)<1e-10);
        foreach(string id in new[]{"nacl","h3po4","h2s","nh3","cuso45h2o","c17h33coona"}){
            var massV=new Vessel(VesselKind.Beaker,Vector3.zero);massV.Water=50;
            massV.Introduce(id,0.001,true);
            Check("dissolution conserves total mass "+id,Math.Abs(massV.Mass()-50-ChemData.Sub[id].Mass*0.001)<1e-9);
            double charge=massV.Solutes.Sum(i=>ChemData.Sub[i.Key].Charge*i.Value);
            Check("dissolution conserves net charge "+id,Math.Abs(charge)<1e-12);
        }
        Check("complex ion formula does not multiply atoms by charge",Math.Abs(Formula.MolarMass("[Cu(NH3)4]2+")-Formula.MolarMass("Cu(NH3)4"))<1e-12);
        var pureAcid=new Vessel(VesselKind.Beaker,Vector3.zero);
        Check("dry shelf phosphoric acid accepts solid dose",ChemLab.Game.DoseService.TryAdd(pureAcid,"h3po4",0.001,true,out _));
        Check("dry phosphoric acid remains a solid after settling",pureAcid.Undissolved.ContainsKey("h3po4")&&pureAcid.Solutes.Count==0&&pureAcid.Water==0);
        Check("pure phosphoric solid uses crystal density",Math.Abs(ChemLab.Game.SolidGeometry.VolumeMl(pureAcid)-0.001*ChemData.Sub["h3po4"].Mass/1.9)<1e-10);
        pureAcid.Temp=50;PhaseProperties.Reconcile(pureAcid);
        Check("neat phosphoric melts without ionizing",pureAcid.Undissolved.Count==0&&pureAcid.Solutes.ContainsKey("h3po4")&&pureAcid.LiquidVolume()>0);
        pureAcid.Temp=25;PhaseProperties.Reconcile(pureAcid);
        Check("neat phosphoric freezing conserves mass",pureAcid.Solutes.Count==0&&Math.Abs(pureAcid.Undissolved["h3po4"]-0.001)<1e-12);
        var sealedV=new Vessel(VesselKind.Bottle,Vector3.zero);sealedV.AddWater(50);sealedV.SetSealed(true);
        sealedV.Gases["o2"]=0.002;sealedV.Gases["h2"]=0.001;
        PhaseProperties.Reconcile(sealedV,true);
        Check("sealed oxygen conserved",Math.Abs(sealedV.Gases["o2"]+sealedV.Solutes["o2"]-0.002)<1e-12);
        Check("sealed hydrogen conserved",Math.Abs(sealedV.Gases["h2"]+sealedV.Solutes["h2"]-0.001)<1e-12);
        Check("sealed gas cannot vent",sealedV.EscapedMoles==0&&sealedV.PressureBar>1.01325);
        double partial=sealedV.Gases["o2"]*0.08314462618*298.15/sealedV.HeadspaceLitres;
        Check("Henry uses partial not total pressure",Math.Abs(sealedV.Solutes["o2"]-0.0013*partial*sealedV.Water/1000)<1e-12);
        double oxygen=sealedV.Solutes["o2"];
        PhaseProperties.Reconcile(sealedV,true);
        Check("sealed equilibrium is stable",Math.Abs(sealedV.Solutes["o2"]-oxygen)<1e-12);
        double waterBeforeHeat=sealedV.Water;sealedV.Temp=110;Lab.Evaporate(sealedV,10);
        Check("sealed heating does not delete solvent",sealedV.Water==waterBeforeHeat);
        var receiver=new Vessel(VesselKind.Beaker,Vector3.zero);
        Check("sealed boundary blocks pouring",Lab.PourFraction(sealedV,receiver,0.5,true)==0&&sealedV.Water==waterBeforeHeat&&receiver.Water==0);
        Lab.Empty(sealedV);
        Check("washing resets pressure and seal",!sealedV.Sealed&&sealedV.TrappedAirMoles==0&&sealedV.PressureBar==1.01325);
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind))){
            var cavity=new Vessel(kind,Vector3.zero);
            double inner=ChemLab.Game.VesselMesh.InnerCapacity(kind)*1000.0;
            Check("nominal capacity physically fits cavity "+kind,inner>=cavity.Capacity);
            var marks=ChemLab.Game.VesselGraduations.Marks(kind);
            Check("graduation volume matches cavity "+kind,marks.All(m=>m.Height>0&&m.Height<ChemLab.Game.VesselMesh.Get(kind).H&&Math.Abs(ChemLab.Game.VesselGraduations.VolumeBelow(kind,m.Height)-m.Volume)<0.002));
            if(kind==VesselKind.Burette)Check("burette scale descends and excludes tip hold-up",marks.Count==501&&marks[0].Height>marks[500].Height&&marks[500].Volume>0&&Math.Abs(marks[0].Volume-marks[500].Volume-50)<1e-10);
            if(kind==VesselKind.Pipette)Check("volumetric pipette has only one calibration mark",marks.Count==1&&marks[0].Reading==25);
            Check("headspace uses physical cavity "+kind,Math.Abs(cavity.HeadspaceLitres-inner/1000)<1e-12);
            cavity.Water=inner*0.1;cavity.Undissolved["zn"]=inner*0.1*PhaseProperties.Density("zn")/ChemData.Sub["zn"].Mass;
            cavity.Precipitates["caco3"]=inner*0.1*PhaseProperties.Density("caco3")/ChemData.Sub["caco3"].Mass;
            Check("headspace subtracts liquid solid and precipitate "+kind,Math.Abs(cavity.HeadspaceLitres-inner*0.7/1000)<1e-10);
        }
        foreach(var species in ChemData.Sub.Values.Where(s=>PhaseProperties.Phase(s.Id)=="g")){
            var ideal=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};ideal.Gases[species.Id]=0.1;
            PhaseProperties.Reconcile(ideal);
            double expectedPressure=0.1*Vessel.GasConstant*298.15/ideal.HeadspaceLitres;
            Check("all dry gases use ideal pressure even above 2 bar "+species.Id,expectedPressure>2&&Math.Abs(ideal.PressureBar-expectedPressure)<1e-10&&ideal.PhaseNotice=="");
            ideal.Temp=50;PhaseProperties.Reconcile(ideal);
            Check("all dry gas pressure scales with Kelvin temperature "+species.Id,Math.Abs(ideal.PressureBar/expectedPressure-323.15/298.15)<1e-10&&Math.Abs(ideal.Gases[species.Id]-0.1)<1e-12);
        }
        var displaced=new Vessel(VesselKind.Bottle,Vector3.zero);displaced.SetSealed(true);
        double air=displaced.TrappedAirMoles;
        displaced.Undissolved["zn"]=displaced.InteriorCapacityMl*0.25*PhaseProperties.Density("zn")/ChemData.Sub["zn"].Mass;
        PhaseProperties.Reconcile(displaced);
        Check("solid displacement raises closed ideal gas pressure",Math.Abs(displaced.PressureBar-1.01325/0.75)<1e-10&&displaced.TrappedAirMoles==air);
        displaced.Water=displaced.InteriorCapacityMl;
        PhaseProperties.Reconcile(displaced);
        Check("overfull cavity reports unsupported pressure without losing inventory",displaced.PhaseNotice.Contains("暂停压力平衡")&&displaced.TrappedAirMoles==air&&displaced.Water==displaced.InteriorCapacityMl);
        var gasBottle=new Vessel(VesselKind.Bottle,Vector3.zero);gasBottle.Gases["o2"]=0.001;
        gasBottle.SetSealed(true);
        Check("sealing does not double count resident gas as air",Math.Abs(gasBottle.PressureBar-1.01325)<1e-10);
        Check("closed liquid addition rejected without mutation",!ChemLab.Game.DoseService.TryAdd(gasBottle,"h2o",1,false,out _)&&gasBottle.Water==0);
        double initialGas=gasBottle.Gases.Values.Sum();gasBottle.SetSealed(false);
        Check("opening transfers headspace gas to escaped ledger",gasBottle.Gases.Count==0&&Math.Abs(gasBottle.EscapedMoles-initialGas)<1e-12&&gasBottle.PressureBar==1.01325);
        foreach(string solvent in new[]{"h2o","c2h5oh"}){
            var closed=new Vessel(VesselKind.Bottle,Vector3.zero);
            double molarMass=ChemData.Sub[solvent].Mass;
            if(solvent=="h2o")closed.Water=20;else closed.Solutes[solvent]=20/molarMass;
            closed.SetSealed(true);double coldPressure=closed.PressureBar;
            closed.Temp=80;PhaseProperties.Reconcile(closed);
            double liquid=solvent=="h2o"?closed.Water/molarMass:closed.Solutes[solvent];
            closed.Gases.TryGetValue(solvent,out double steam);
            Check("closed solvent vapor conserves moles "+solvent,Math.Abs(liquid+steam-20/molarMass)<1e-12);
            Check("closed mass includes retained vapor "+solvent,Math.Abs(closed.Mass()-20)<1e-10);
            Check("heated vapor increases pressure "+solvent,closed.PressureBar>coldPressure&&steam>0);
            VaporEquilibrium.TryPressure(solvent,80,out double psat);
            Check("vapor partial pressure matches Antoine "+solvent,Math.Abs(steam*0.08314462618*353.15/closed.HeadspaceLitres-psat)<1e-10);
            double warmVapor=steam;closed.Temp=25;PhaseProperties.Reconcile(closed);
            Check("cooling condenses retained vapor "+solvent,closed.Gases[solvent]<warmVapor&&closed.EscapedMoles==0);
        }
        Check("water vapor pressure at 25 C",VaporEquilibrium.TryPressure("h2o",25,out double p25)&&Math.Abs(p25-0.0317)<0.0002);
        var ethanol=new Vessel(VesselKind.Beaker,Vector3.zero);ethanol.Solutes["c2h5oh"]=0.1;
        foreach(string unknownMixtureSolvent in new[]{"n_hexane","butan_1_ol","acetone"}){
            var unknownMixture=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50};
            unknownMixture.Solutes[unknownMixtureSolvent]=.01;double beforeMixtureMass=unknownMixture.Mass();
            bool accepted=ChemLab.Game.DoseService.TryAdd(unknownMixture,"nacl",.001,true,out string mixtureNotice);
            Check("mixed missing data retains added solid "+unknownMixtureSolvent,accepted&&unknownMixture.Undissolved.TryGetValue("nacl",out double kept)&&Math.Abs(kept-.001)<1e-12);
            Check("mixed missing data does not invent ions "+unknownMixtureSolvent,unknownMixture.Get("Na+")==0&&unknownMixture.Get("Cl-")==0&&mixtureNotice.Contains("缺少"));
            Check("mixed missing data conserves dose mass "+unknownMixtureSolvent,Math.Abs(unknownMixture.Mass()-beforeMixtureMass-.001*ChemData.Sub["nacl"].Mass)<1e-8);
            Lab.Settle(unknownMixture);
            Check("mixed missing data remains solid on subsequent settle "+unknownMixtureSolvent,unknownMixture.Get("Na+")==0&&Math.Abs(unknownMixture.Get("nacl")-.001)<1e-12);
        }
        foreach(string expandedSolvent in SolventProperties.Organic){
            if(PhaseProperties.Data.TryGetValue(expandedSolvent,out var solventPhase)&&!double.IsNaN(solventPhase.Melt)){
                var frozenSolvent=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=(float)solventPhase.Melt-5};
                frozenSolvent.Solutes[expandedSolvent]=.1;double frozenMass=frozenSolvent.Mass();
                PhaseProperties.Reconcile(frozenSolvent);
                Check("pure organic freezes and stops being solvent "+expandedSolvent,frozenSolvent.Undissolved.ContainsKey(expandedSolvent)&&!SolventProperties.HasSolvent(frozenSolvent));
                frozenSolvent.Temp=(float)(solventPhase.Melt+solventPhase.Boil)/2;
                PhaseProperties.Reconcile(frozenSolvent);
                Check("pure organic melts with mass conserved "+expandedSolvent,SolventProperties.HasSolvent(frozenSolvent)&&Math.Abs(frozenSolvent.Get(expandedSolvent)-.1)<1e-12&&Math.Abs(frozenSolvent.Mass()-frozenMass)<1e-8);
            }
            var solventGuard=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50};
            solventGuard.Solutes[expandedSolvent]=.1;
            Check("organic inventory blocks pure water coefficients "+expandedSolvent,!SolventProperties.IsAqueousModelValid(solventGuard)&&!PhaseProperties.TryCapacity(solventGuard,"o2",out _));
            solventGuard.Water=0;
            Check("organic inventory recognized dry "+expandedSolvent,SolventProperties.HasSolvent(solventGuard));
            if(expandedSolvent=="c2h5oh"||expandedSolvent=="ch3oh")continue;
            Check("no invented nonwater capacity "+expandedSolvent,!PhaseProperties.TryCapacity(solventGuard,"kno3",out _));
            solventGuard.Solutes["c2h5oh"]=.1;
            Check("third solvent blocks pure ethanol curve "+expandedSolvent,!PureSolubility.TryCapacity(solventGuard,"kcl",out _));
            solventGuard.Water=84.95;solventGuard.Solutes["c2h5oh"]=15.05/ChemData.Sub["c2h5oh"].Mass;
            Check("third solvent blocks binary water ethanol curve "+expandedSolvent,!MixedSolubility.TryNaCl(solventGuard,out _));
        }
        Check("nonwater solvent is recognized",SolventProperties.HasSolvent(ethanol));
        Check("water coefficients never used in ethanol",!PhaseProperties.TryCapacity(ethanol,"o2",out _));
        ethanol.Water=50;
        Check("unsupported mixed solute cannot use water coefficients",!PhaseProperties.TryCapacity(ethanol,"kno3",out _));
        var mixed=new Vessel(VesselKind.Beaker,Vector3.zero);mixed.Water=84.95;mixed.Solutes["c2h5oh"]=15.05/ChemData.Sub["c2h5oh"].Mass;mixed.Temp=30;
        Check("mixed NaCl capacity reproduces NIST measured point",PhaseProperties.TryCapacity(mixed,"nacl",out double mixedCap)&&Math.Abs(mixedCap-0.4453)<1e-10);
        mixed.Introduce("nacl",0.5,true);
        Check("mixed solvent dissolves only saturation amount",Math.Abs(PhaseProperties.Dissolved(mixed,"nacl")-0.4453)<1e-10);
        Check("mixed solvent retains excess solid",mixed.Precipitates.TryGetValue("nacl",out double excess)&&Math.Abs(excess-0.0547)<1e-10);
        mixed.Temp=40;PhaseProperties.Reconcile(mixed);
        Check("warming mixed solvent redissolves solid",Math.Abs(PhaseProperties.Dissolved(mixed,"nacl")-0.4543)<1e-10);
        Check("mixed heating conserves salt",Math.Abs(PhaseProperties.Dissolved(mixed,"nacl")+mixed.Precipitates["nacl"]-0.5)<1e-10);
        mixed.PressureBar=3;
        Check("ambient mixed data never extrapolated to high pressure",!PhaseProperties.TryCapacity(mixed,"nacl",out _));
        foreach(var example in new[]{("nabr","c2h5oh",25f,.02496),("kcl","c2h5oh",25f,.00034),("nacl","ch3oh",30f,.0136),("nabr","ch3oh",25f,.14938),("kcl","ch3oh",30f,.00554)}){
            var alcohol=new Vessel(VesselKind.Beaker,Vector3.zero);alcohol.Temp=example.Item3;
            alcohol.Solutes[example.Item2]=20/ChemData.Sub[example.Item2].Mass;
            double expected=20*example.Item4/(1-example.Item4)/ChemData.Sub[example.Item1].Mass;
            Check("pure alcohol measured capacity "+example.Item1+"/"+example.Item2,PhaseProperties.TryCapacity(alcohol,example.Item1,out double cap)&&Math.Abs(cap-expected)<1e-12);
            Check("pure alcohol dosing accepted "+example.Item1+"/"+example.Item2,ChemLab.Game.DoseService.TryAdd(alcohol,example.Item1,expected*2,true,out _));
            Check("pure alcohol partial dissolution "+example.Item1+"/"+example.Item2,Math.Abs(PhaseProperties.Dissolved(alcohol,example.Item1)-expected)<1e-10);
            Check("pure alcohol excess and mass conserved "+example.Item1+"/"+example.Item2,alcohol.Precipitates.TryGetValue(example.Item1,out double rest)&&Math.Abs(rest-expected)<1e-10&&Math.Abs(alcohol.Mass()-20-expected*2*ChemData.Sub[example.Item1].Mass)<1e-9);
        }
        var unknown=new Vessel(VesselKind.Beaker,Vector3.zero);unknown.Solutes["acetone"]=0.1;
        var freezingWater=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=-5};
        freezingWater.AddWater(20);PhaseProperties.Reconcile(freezingWater);
        Check("pure water freezes without losing mass",freezingWater.Water==0&&freezingWater.Undissolved.ContainsKey(WaterIce.Id)&&Math.Abs(freezingWater.Mass()-20)<1e-10);
        Check("ice occupies expanded solid volume not fluid or solvent",Math.Abs(freezingWater.SolidVolume()-20/.9167)<1e-9&&freezingWater.Volume()==0&&!SolventProperties.HasSolvent(freezingWater));
        var emptyReceiver=new Vessel(VesselKind.Pipette,Vector3.zero){AttachedTo=freezingWater};
        Check("metered transfer does not withdraw ice",MeteredTransfer.Move(emptyReceiver,-5)==0&&Math.Abs(freezingWater.Mass()-20)<1e-10);
        freezingWater.Temp=0;PhaseProperties.Reconcile(freezingWater);
        Check("zero Celsius preserves latent phase split",freezingWater.Water==0&&Math.Abs(freezingWater.Mass()-20)<1e-10);
        freezingWater.Temp=5;PhaseProperties.Reconcile(freezingWater);
        Check("pure ice melts into water without duplicate inventory",Math.Abs(freezingWater.Water-20)<1e-10&&freezingWater.Undissolved.Count==0&&freezingWater.SolidVolume()==0&&Math.Abs(freezingWater.Mass()-20)<1e-10);
        Check("water phase approximation is explicitly disclosed",freezingWater.PhaseNotice.Contains("潜热"));
        foreach(bool pressureCase in new[]{false,true}){
            var unsupportedWater=new Vessel(VesselKind.Beaker,Vector3.zero){Water=20,Temp=-5};
            if(pressureCase)unsupportedWater.PressureBar=3;else unsupportedWater.Solutes["nacl"]=.01;
            double before=unsupportedWater.Mass();PhaseProperties.Reconcile(unsupportedWater);
            Check("unsupported freezing preserves inventory and warns "+pressureCase,Math.Abs(unsupportedWater.Mass()-before)<1e-9&&unsupportedWater.Water==20&&!unsupportedWater.Has(WaterIce.Id)&&unsupportedWater.PhaseNotice.Contains("缺少"));
        }
        var frozenDose=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=-5};
        Check("cold water dosing reserves expansion before capacity check",!ChemLab.Game.DoseService.TryAdd(frozenDose,"h2o",230,false,out _)&&frozenDose.Mass()==0);
        var latentWater=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=-5};latentWater.AddWater(20);
        double initialEnthalpy=WaterIce.Enthalpy(latentWater);
        double warmToZero=20*2.1*5,halfFusion=10*333.5;
        Check("ice heat warms to zero then melts only funded mass",WaterIce.ApplyHeat(latentWater,warmToZero+halfFusion)&&
            latentWater.Temp==0&&Math.Abs(latentWater.Water-10)<1e-8&&Math.Abs(WaterIce.IceMass(latentWater)-10)<1e-8);
        Check("ice heat conserves enthalpy and total mass",Math.Abs(WaterIce.Enthalpy(latentWater)-initialEnthalpy-warmToZero-halfFusion)<1e-7&&Math.Abs(latentWater.Mass()-20)<1e-8);
        PhaseProperties.Reconcile(latentWater);
        Check("chemistry reconciliation preserves partially melted ice",latentWater.Temp==0&&Math.Abs(latentWater.Water-10)<1e-8&&latentWater.PhaseNotice.Contains("潜热"));
        Check("removing latent heat refreezes partial water at zero",WaterIce.ApplyHeat(latentWater,-halfFusion)&&latentWater.Temp==0&&latentWater.Water<1e-8&&Math.Abs(WaterIce.IceMass(latentWater)-20)<1e-8);
        Check("ice warms above zero only after full fusion",WaterIce.ApplyHeat(latentWater,20*333.5+20*4.18*5)&&Math.Abs(latentWater.Temp-5)<1e-6&&Math.Abs(latentWater.Water-20)<1e-8&&WaterIce.IceMass(latentWater)==0);
        latentWater.Temp=-5;WaterIce.Reconcile(latentWater);
        WaterIce.AdvanceTemperature(latentWater,5);
        Check("apparatus temperature step cannot skip latent heat",latentWater.Temp==0&&latentWater.Water>0&&latentWater.Water<1&&Math.Abs(latentWater.Mass()-20)<1e-8);
        double beforeOutOfRange=WaterIce.Enthalpy(latentWater),beforeOutMass=latentWater.Mass();
        Check("invalid heat input leaves ice inventory unchanged",!WaterIce.ApplyHeat(latentWater,double.NaN)&&!WaterIce.ApplyHeat(latentWater,1e9)&&WaterIce.Enthalpy(latentWater)==beforeOutOfRange&&latentWater.Mass()==beforeOutMass);
        foreach(var example in new[]{("nabr","c2h5oh",25f,.02496),("kcl","c2h5oh",25f,.00034),("nacl","ch3oh",30f,.0136),("nabr","ch3oh",25f,.14938),("kcl","ch3oh",30f,.00554)}){
            var pendingMelt=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=example.Item3};
            pendingMelt.Precipitates[example.Item2]=20/ChemData.Sub[example.Item2].Mass;
            double expected=20*example.Item4/(1-example.Item4)/ChemData.Sub[example.Item1].Mass;
            pendingMelt.Undissolved[example.Item1]=expected*2;
            double mass=pendingMelt.Mass();
            Check("melted deposit recognized as solvent "+example.Item1+"/"+example.Item2,SolventProperties.HasSolvent(pendingMelt)&&!SolventProperties.IsAqueousModelValid(pendingMelt));
            Check("melted deposit uses solvent-specific capacity "+example.Item1+"/"+example.Item2,PhaseProperties.TryCapacity(pendingMelt,example.Item1,out double cap)&&Math.Abs(cap-expected)<1e-12);
            PhaseProperties.Reconcile(pendingMelt);
            Check("melted deposit dissolves saturation amount without mass loss "+example.Item1+"/"+example.Item2,
                Math.Abs(PhaseProperties.Dissolved(pendingMelt,example.Item1)-expected)<1e-10&&
                pendingMelt.Precipitates.TryGetValue(example.Item1,out double rest)&&Math.Abs(rest-expected)<1e-10&&Math.Abs(pendingMelt.Mass()-mass)<1e-9);
        }
        var pendingMixed=new Vessel(VesselKind.Beaker,Vector3.zero){Water=50};
        pendingMixed.Precipitates["c2h5oh"]=.1;
        Check("liquid deposit prevents applying pure-water gas coefficients",!SolventProperties.IsAqueousModelValid(pendingMixed)&&!PhaseProperties.TryCapacity(pendingMixed,"o2",out _));
        var frozenDeposit=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=10};
        frozenDeposit.Precipitates["octanoic_acid"]=.1;
        Check("actually frozen deposit is not counted as solvent",!SolventProperties.HasSolvent(frozenDeposit));
        Check("unknown nonwater solubility preserves solid and warns",ChemLab.Game.DoseService.TryAdd(unknown,"nacl",0.001,true,out var warning)&&unknown.Undissolved.ContainsKey("nacl")&&warning.Contains("缺少"));
        var oxygenVessel=new Vessel(VesselKind.Beaker,Vector3.zero);oxygenVessel.Water=50;oxygenVessel.Solutes["o2"]=0.01;
        PhaseProperties.TryCapacity(oxygenVessel,"o2",out double oxygenCapacity);
        PhaseProperties.Reconcile(oxygenVessel,false);
        Check("gas separation does not falsely report escape",oxygenVessel.PhaseNotice.Contains("尚未逸散")&&oxygenVessel.EscapedMoles==0&&Math.Abs(oxygenVessel.Gases["o2"]+oxygenVessel.Solutes["o2"]-0.01)<1e-12);
        PhaseProperties.Reconcile(oxygenVessel,true);
        Check("gas escape notice follows actual ledger transfer",oxygenVessel.PhaseNotice.Contains("已逸散")&&!oxygenVessel.Gases.ContainsKey("o2")&&Math.Abs(oxygenVessel.EscapedMoles-(0.01-oxygenCapacity))<1e-12);
        var tutorial=(List<ChemLab.Game.TutorialStep>)typeof(ChemLab.Game.PetTutorial)
            .GetMethod("BuildLab",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        string teaching=string.Join("\n",tutorial.Select(s=>s.Say));
        Check("tutorial explains drag highlight quantity units and Enter",new[]{"拖到目标器皿","轮廓高亮","g 或 mol","Enter","取消不会加入"}.All(teaching.Contains));
        Check("tutorial no longer advertises removed dose presets",!teaching.Contains("份量预设")&&!teaching.Contains("少许 / 适量 / 过量"));
        Lab.Reset();var demo=Lab.AddVessel(VesselKind.Beaker);demo.AddWater(80);
        tutorial.First(s=>s.Title.EndsWith("加第一种试剂")).Do();
        Check("tutorial silver dose is 0.001 mol and no premature AgCl",Math.Abs(demo.Get("Ag+")-0.001)<1e-9&&!demo.Precipitates.ContainsKey("agcl"));
        tutorial.First(s=>s.Title.EndsWith("加第二种试剂")).Do();
        Check("tutorial chloride dose produces expected AgCl",demo.Precipitates.TryGetValue("agcl",out double agcl)&&Math.Abs(agcl-0.001)<1e-8);
        tutorial.First(s=>s.Title.EndsWith("准备加热")).Do();
        var demoTube=Lab.Vessels.Last();
        Check("tutorial heating demo uses validated mol dose",demoTube.Kind==VesselKind.Tube&&Math.Abs(demoTube.Get("Cu2+")-0.001)<1e-9);
        double FullMass(Vessel x)=>x.Mass()+(x.Sealed?0:x.Gases.Sum(p=>ChemData.MassOf(p.Key,p.Value)));
        var borohydride=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100};
        borohydride.Solutes["ch3cho"]=.004;
        borohydride.Undissolved["nabh4"]=.001;
        double borohydrideMass=FullMass(borohydride);
        Lab.Settle(borohydride);
        Check("organic borohydride reduces with four-carbonyl stoichiometry",Math.Abs(borohydride.Get("c2h5oh")-.004)<1e-9&&Math.Abs(borohydride.Get("nabo2")-.001)<1e-9);
        Check("organic borohydride consumes reagent and conserves mass",borohydride.Get("nabh4")<1e-10&&Math.Abs(FullMass(borohydride)-borohydrideMass)<1e-7);
        var lithiumHydride=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100};
        lithiumHydride.Solutes["ch3cho"]=.002;lithiumHydride.Undissolved["lialh4"]=.001;
        Lab.Settle(lithiumHydride);
        Check("unsupported aqueous lithium hydride does not create alcohol and warns",lithiumHydride.Get("c2h5oh")<1e-10&&Math.Abs(lithiumHydride.Get("ch3cho")-.002)<1e-9&&lithiumHydride.PhaseNotice.Contains("无水还原"));
        var diazo=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=4};
        diazo.Solutes["aniline"]=.001;diazo.Solutes["hno2"]=.001;
        diazo.Solutes["H+"]=.001;diazo.Solutes["Cl-"]=.001;
        double diazoMass=FullMass(diazo);Lab.Settle(diazo);
        Check("cold aniline diazotization consumes acid and conserves mass",diazo.Get("c6h5n2cl")>.00099&&diazo.Get("hno2")<1e-9&&Math.Abs(FullMass(diazo)-diazoMass)<1e-7);
        var azo=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=4};
        azo.Solutes["c6h5n2cl"]=.001;azo.Undissolved["phoh"]=.001;
        double azoMass=FullMass(azo);Lab.Settle(azo);
        Check("phenol coupling keeps oxygen in hydroxyazo dye",azo.Get("azo_dye")>.00099&&azo.Get("azo_benzene")<1e-10&&Math.Abs(FullMass(azo)-azoMass)<1e-7);
        var coldBromination=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100};
        coldBromination.Solutes["c6h6"]=.001;coldBromination.Solutes["br2"]=.001;coldBromination.Undissolved["mno2"]=.001;
        Lab.Settle(coldBromination);
        Check("benzene bromination rejects unrelated MnO2 catalyst",coldBromination.Get("c6h5br")<1e-10);
        var ironBromination=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100};
        ironBromination.Solutes["c6h6"]=.001;ironBromination.Solutes["br2"]=.001;ironBromination.Undissolved["fe"]=.001;
        double brominationMass=FullMass(ironBromination);Lab.Settle(ironBromination);
        Check("iron-catalyzed benzene bromination uses bromine and conserves mass",ironBromination.Get("c6h5br")>.00099&&ironBromination.Get("br2")<1e-9&&Math.Abs(FullMass(ironBromination)-brominationMass)<1e-7);
        var glacialEster=new Vessel(VesselKind.Flask,Vector3.zero){Water=20,Temp=80,Mode=HeatMode.Reflux,ConcAcid=true};
        glacialEster.Solutes["ch3cooh_glacial"]=.001;glacialEster.Solutes["c2h5oh"]=.001;
        double esterMass=FullMass(glacialEster);Lab.Settle(glacialEster);
        Check("glacial acetic acid esterification consumes ethanol and conserves mass",glacialEster.Get("ch3cooc2h5")>.00099&&glacialEster.Get("c2h5oh")<1e-9&&Math.Abs(FullMass(glacialEster)-esterMass)<1e-7);
        Lab.Reset();
        lines.Add("TOTAL "+(checks-failures)+" passed, "+failures+" failed");
        int bad=0;
        foreach(var rule in Rules.All.Where(r=>r.Parsed)) {
            string Side(List<Rules.Species> terms)=>string.Join(" + ",terms.Select(p=>p.Coef.ToString()+ChemData.Sub[p.Id].Formula));
            if(!SpecRunner.CheckEquation(Side(rule.Reactants)+" -> "+Side(rule.Products),out var detail)){lines.Add("RULE_AUDIT "+rule.Eq+" | "+detail);bad++;}
        }
        lines.Add("REACTION_EQUATION_AUDIT flagged="+bad);
        int orphanGas=0;
        foreach(var rule in Rules.All.Where(r=>r.Parsed&&!string.IsNullOrEmpty(r.Gas))){
            if(rule.Reactants.Any(s=>s.Id==rule.Gas)||rule.Products.Any(s=>s.Id==rule.Gas))continue;
            lines.Add("GAS_METADATA_ORPHAN "+rule.Eq+" | gas="+rule.Gas);orphanGas++;
        }
        lines.Add("GAS_METADATA_AUDIT orphan="+orphanGas);
        int dissBad=0;
        foreach(var item in ChemData.Diss){
            var d=item.Value;if(d.None||d.Ions.Count==0||!ChemData.Sub.ContainsKey(item.Key))continue;
            string equation=ChemData.Sub[item.Key].Formula+(d.WaterProduced<0?" + "+(-d.WaterProduced)+"H2O":"")+" -> "+string.Join(" + ",d.Ions.Select(i=>i.Value.ToString()+i.Key))+(d.WaterProduced>0?" + "+d.WaterProduced+"H2O":"");
            if(!SpecRunner.CheckEquation(equation,out var detail)){lines.Add("DISSOCIATION_AUDIT "+item.Key+" | "+equation+" | "+detail);dissBad++;}
        }
        lines.Add("DISSOCIATION_AUDIT flagged="+dissBad);
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/phase-audit.txt",lines);Debug.Log(lines[checks]);
        File.WriteAllLines("Logs/vessel-capacities.tsv",new[]{"Kind\tNominal_mL\tInner_mL\tOuterDiameter_mm\tHeight_mm"}.Concat(
            Enum.GetValues(typeof(VesselKind)).Cast<VesselKind>().Select(k=>{
                var geo=ChemLab.Game.VesselMesh.Get(k);
                return k+"\t"+Vessel.NominalCapacity(k)+"\t"+(ChemLab.Game.VesselMesh.InnerCapacity(k)*1000).ToString("F3")+"\t"+(geo.R*200).ToString("F1")+"\t"+(geo.H*100).ToString("F1");
            })));
        EditorApplication.Exit(failures==0&&bad==0&&dissBad==0&&orphanGas==0?0:1);
    }
}
}
