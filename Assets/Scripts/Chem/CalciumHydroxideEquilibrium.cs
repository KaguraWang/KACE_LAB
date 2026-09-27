using System;
using System.Linq;

namespace ChemLab.Chem {
// Concentration-product teaching model for Ca(OH)2(s) <=> Ca2+ + 2OH-.
// The saturation MOLARITIES are compiler values from original measurements by
// Moody & Leyson, J. Chem. Soc. Trans. 1908, 93, 1767-72, reproduced in
// IUPAC-NIST Solubility Data Series 52, pp. 137 onward:
// https://srdata.nist.gov/solubility/IUPAC/SDS-52/SDS-52.pdf
// Derived Kc=4*s^3 assumes concentration rather than activity. It preserves
// common-ion stoichiometry, but is not an activity-corrected Ksp for brines.
public static class CalciumHydroxideEquilibrium {
    static readonly double[] Celsius={2,10,15,20,25,30,40,50,60,70,80};
    static readonly double[] Molar={.02320,.02266,.02216,.02158,.02052,.01963,.01805,.01647,.01512,.01399,.01303};
    const double Eps=1e-10;
    const string UnsupportedNotice="Ca(OH)₂ 当前温度、压力或溶剂缺少适用的定量溶解度；保留原物态与物质量";

    public static bool TryCapacity(Vessel v,out double capacity){
        capacity=double.NaN;
        if(v==null||v.Water<=1e-9||!SolventProperties.IsAqueousModelValid(v)||
           v.Temp<Celsius[0]||v.Temp>Celsius[Celsius.Length-1]||
           v.PressureBar<.9||v.PressureBar>1.1)return false;
        capacity=SaturationMolarity(v.Temp)*v.Water/1000;
        return true;
    }

    static double SaturationMolarity(double temperature){
        for(int i=1;i<Celsius.Length;i++)if(temperature<=Celsius[i]){
            double f=(temperature-Celsius[i-1])/(Celsius[i]-Celsius[i-1]);
            return Molar[i-1]*(1-f)+Molar[i]*f;
        }
        return Molar[Molar.Length-1];
    }

    static void Warn(Vessel v,string message){
        if(string.IsNullOrEmpty(v.PhaseNotice))v.PhaseNotice=message;
        if(v.PhaseNotices.Add("caoh2:unsupported:"+message))Lab.Note(v,message,"obs");
    }
    static void Add(System.Collections.Generic.Dictionary<string,double> map,string id,double amount){
        map.TryGetValue(id,out double current);map[id]=current+amount;
    }
    static void RemoveSolid(Vessel v,double amount){
        foreach(var map in new[]{v.Undissolved,v.Precipitates}){
            if(amount<=Eps)break;
            if(!map.TryGetValue("caoh2",out double current))continue;
            double take=Math.Min(current,amount);amount-=take;current-=take;
            if(current<=Eps)map.Remove("caoh2");else map["caoh2"]=current;
        }
    }
    static double Product(double ca,double oh,double delta,double litres){
        double calcium=Math.Max(0,ca+delta)/litres;
        double hydroxide=Math.Max(0,oh+2*delta)/litres;
        return calcium*hydroxide*hydroxide;
    }

    public static bool Reconcile(Vessel v){
        v.Solutes.TryGetValue("Ca2+",out double ca);
        v.Solutes.TryGetValue("OH-",out double oh);
        v.Undissolved.TryGetValue("caoh2",out double dry);
        v.Precipitates.TryGetValue("caoh2",out double sediment);
        double solid=dry+sediment;
        if(solid<=Eps&&(ca<=Eps||oh<=Eps))return false;
        if(!TryCapacity(v,out double capacity)){
            Warn(v,UnsupportedNotice);
            return false;
        }
        if(v.PhaseNotice==UnsupportedNotice)
            v.PhaseNotice="";
        v.PhaseNotices.Remove("caoh2:unsupported:"+UnsupportedNotice);
        double litres=v.Water/1000;
        double saturation=capacity/litres;
        double kc=4*saturation*saturation*saturation;
        double initial=Product(ca,oh,0,litres);
        if(solid>Eps&&initial<kc*(1-1e-9)){
            double dissolve=solid;
            if(Product(ca,oh,solid,litres)>kc){
                double lo=0,hi=solid;
                for(int i=0;i<64;i++){double mid=(lo+hi)*.5;if(Product(ca,oh,mid,litres)>kc)hi=mid;else lo=mid;}
                dissolve=(lo+hi)*.5;
            }
            if(dissolve<=Eps)return false;
            RemoveSolid(v,dissolve);
            Add(v.Solutes,"Ca2+",dissolve);Add(v.Solutes,"OH-",2*dissolve);
            Add(v.Ledger,"caoh2",dissolve);
            return true;
        }
        if(ca>Eps&&oh>Eps&&initial>kc*(1+1e-9)){
            double max=Math.Min(ca,oh/2);
            double lo=0,hi=max;
            for(int i=0;i<64;i++){double mid=(lo+hi)*.5;if(Product(ca,oh,-mid,litres)>kc)lo=mid;else hi=mid;}
            double precipitate=(lo+hi)*.5;
            if(precipitate<=Eps)return false;
            v.TakeAny("Ca2+",precipitate);v.TakeAny("OH-",2*precipitate);
            Add(v.Precipitates,"caoh2",precipitate);
            v.Turbidity=Math.Min(1f,v.Turbidity+.25f);
            PhaseProperties.Notice(v,"caoh2",false);
            if(v.Solutes.Keys.Any(id=>id!="Ca2+"&&id!="OH-"))
                Warn(v,"Ca(OH)₂ 混合离子溶液仅按浓度积近似处理；未计活度和络合物影响");
            return true;
        }
        return false;
    }
}
}
