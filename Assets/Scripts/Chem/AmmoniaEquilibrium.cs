using System;
namespace ChemLab.Chem {
// 25 C concentration-based teaching equilibrium; no activity-coefficient model.
// IPCS EHC 54 section 2.1.2: Kb=1.774e-5; water solubility at 101 kPa,
// 20 C:529 and 40 C:316 g/L water. Linear interpolation gives 475.75 at 25 C.
// https://inchem.org/documents/ehc/ehc/ehc54.htm
// NIST C7664417 Henry compilation: selected kH=58 mol/(kg bar) at 298.15 K.
// https://webbook.nist.gov/cgi/cbook.cgi?ID=C7664417&Mask=10
public static class AmmoniaEquilibrium {
    public const double Kb=1.774e-5,Kw=1e-14,Henry=58;
    // John McMurry, OpenStax Organic Chemistry 20.2: aqueous acetic acid, 25 C.
    // https://openstax.org/books/organic-chemistry/pages/20-2-structure-and-properties-of-carboxylic-acids
    public const double AceticKa=1.75e-5;
    // Aqueous anilinium pKa=4.6; same 25 C teaching scope as the joint solver.
    // https://research.cm.utexas.edu/nbauld/CHAPTER%2021.htm
    public static readonly double AniliniumKa=Math.Pow(10,-4.6);
    const double Ka=Kw/Kb,WaterMass=18.015;
    static double Get(Vessel v,string id)=>v.Solutes.TryGetValue(id,out double n)?n:0;
    public static bool TryCapacity(Vessel v,out double mol){
        mol=double.NaN;
        if(v.Water<=0||Math.Abs(v.Temp-25)>0.001||!SolventProperties.IsAqueousModelValid(v))return false;
        if(v.Sealed){
            v.Gases.TryGetValue("nh3",out double gas);
            mol=Henry*v.Water/1000*gas*v.GasRT/v.HeadspaceLitres;return true;
        }
        if(Math.Abs(v.PressureBar-1.01)>.01)return false;
        mol=475.75*v.Water/1000/ChemData.Sub["nh3"].Mass;return true;
    }
    static void Put(Vessel v,string id,double n){if(n>1e-16)v.Solutes[id]=n;else v.Solutes.Remove(id);}
    public static void Reconcile(Vessel v,bool vent){
        v.Gases.TryGetValue("nh3",out double gas);
        double molecular=Get(v,"nh3"),ammonium=Get(v,"NH4+"),total=gas+molecular+ammonium;
        double oldAcetate=Get(v,"CH3COO-"),aceticTotal=Get(v,"ch3cooh")+oldAcetate;
        double oldAnilinium=Get(v,"C6H5NH3+"),anilineTotal=Get(v,"aniline")+oldAnilinium;
        if((total<=1e-14&&aceticTotal<=1e-14&&anilineTotal<=1e-14)||v.Water<=0)return;
        if(!TryCapacity(v,out _)){
            if(string.IsNullOrEmpty(v.PhaseNotice))v.PhaseNotice="氨/铵、乙酸/乙酸根、苯胺/苯胺鎓定量平衡目前覆盖 25℃ 水溶剂；当前条件未覆盖，保留物质量";
            return;
        }
        // Conservative implementation guard, not a claimed measured validity boundary.
        // Do not extrapolate the dilute Henry relation to concentrated closed ammonia.
        if(v.Sealed&&total/(v.Water/1000)>.1){
            v.PhaseNotice="密闭氨体系超出当前稀溶液近似（0.1 mol/L）；保留气液物质量，压力仍按 PV=nRT 计算";return;
        }
        double oldWater=v.Water,oldOH=Get(v,"OH-"),oldH=Get(v,"H+");
        double conservedCharge=ammonium+oldAnilinium+oldH-oldOH-oldAcetate;
        // Solve all acid/base families against ONE hydrogen concentration.
        // Independent sequential equilibrations would give incompatible pH values.
        double Acetate(double h)=>aceticTotal*AceticKa/(AceticKa+h);
        double Anilinium(double h)=>anilineTotal*h/(AniliniumKa+h);
        double head=v.HeadspaceLitres;
        // NH3 + H2O <=> NH4+ + OH-. Water consumed in ionization is tracked,
        // while spectator-ion inventory and the proton/charge balance stay unchanged.
        void Evaluate(double h,out double water,out double free,out double ion,out double hp,out double oh){
            water=(oldWater+oldOH*WaterMass)/(1+Kw/h*WaterMass/1000);
            double litres=water/1000;hp=h*litres;oh=Kw/h*litres;
            if(v.Sealed){
                double gasPerAqueous=Math.Max(1e-9,head+(oldWater-water)/1000)/(v.GasRT*Henry*litres);
                free=total/(1+h/Ka+gasPerAqueous);
            }else free=Math.Min(total/(1+h/Ka),475.75*litres/ChemData.Sub["nh3"].Mass);
            ion=free*h/Ka;
        }
        double lo=-16,hi=2;
        for(int i=0;i<90;i++){
            double mid=(lo+hi)*.5;Evaluate(Math.Pow(10,mid),out _,out _,out double nh4,out double hp,out double oh);
            if(nh4+Anilinium(Math.Pow(10,mid))+hp-oh-Acetate(Math.Pow(10,mid))>conservedCharge)hi=mid;else lo=mid;
        }
        Evaluate(Math.Pow(10,(lo+hi)*.5),out double finalWater,out double dissolved,out double finalAmmonium,out double finalH,out double finalOH);
        v.Water=finalWater;Put(v,"nh3",dissolved);Put(v,"NH4+",finalAmmonium);Put(v,"H+",finalH);Put(v,"OH-",finalOH);
        double finalAcetate=Acetate(Math.Pow(10,(lo+hi)*.5));
        Put(v,"CH3COO-",finalAcetate);Put(v,"ch3cooh",aceticTotal-finalAcetate);
        double finalAnilinium=Anilinium(Math.Pow(10,(lo+hi)*.5));
        Put(v,"C6H5NH3+",finalAnilinium);Put(v,"aniline",anilineTotal-finalAnilinium);
        double remaining=Math.Max(0,total-dissolved-finalAmmonium);
        if(vent&&!v.Sealed){
            v.Gases.Remove("nh3");v.EscapedMoles+=remaining;
            if(remaining>1e-9)PhaseProperties.Notice(v,"nh3",true,true);
        }else if(remaining>1e-14)v.Gases["nh3"]=remaining;else v.Gases.Remove("nh3");
    }
}
}
