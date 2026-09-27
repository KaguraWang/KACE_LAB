using System;
using System.Collections.Generic;
using System.Linq;
namespace ChemLab.Chem {
// Pinho & Macedo, DOI 10.1021/je049922y, NIST ThermoML.
// https://trc.nist.gov/ThermoML/10.1021/je049922y.json
// Measured saturation MASS FRACTIONS of solute in solution, at 101.3 kPa.
public static class PureSolubility {
    public static readonly string[] Solutes={"nacl","nabr","kcl"};
    static readonly Dictionary<string,double[,]> Curves=new Dictionary<string,double[,]>{
        {"nacl/ch3oh",new double[,]{{303.15,.0136},{313.15,.0131},{333.15,.0122}}},
        {"nabr/ch3oh",new double[,]{{298.15,.14938},{303.15,.14754},{313.15,.14461},{323.15,.14196},{333.15,.13941}}},
        {"kcl/ch3oh",new double[,]{{303.15,.00554},{313.15,.00593},{333.15,.00670}}},
        {"nabr/c2h5oh",new double[,]{{298.15,.02496},{323.15,.02416},{348.15,.02297}}},
        {"kcl/c2h5oh",new double[,]{{298.15,.00034},{323.15,.00039},{348.15,.00049}}}
    };
    public static bool TryCapacity(Vessel v,string solute,out double capacity){
        capacity=double.NaN;if(v.Water>1e-9||Math.Abs(v.PressureBar-1.013)>0.002)return false;
        string solvent=null;double grams=0;
        foreach(var id in SolventProperties.Organic){
            double liquid=SolventProperties.LiquidMoles(v,id);
            if(liquid<=1e-9)continue;if(solvent!=null)return false;
            solvent=id;grams=liquid*ChemData.Sub[id].Mass;
        }
        if(solvent==null||!Curves.TryGetValue(solute+"/"+solvent,out var curve))return false;
        double t=v.Temp+273.15;int n=curve.GetLength(0);
        if(t<curve[0,0]||t>curve[n-1,0])return false;
        double fraction=curve[n-1,1];
        for(int i=1;i<n;i++)if(t<=curve[i,0]){double f=(t-curve[i-1,0])/(curve[i,0]-curve[i-1,0]);fraction=curve[i-1,1]*(1-f)+curve[i,1]*f;break;}
        capacity=grams*fraction/(1-fraction)/ChemData.Sub[solute].Mass;return true;
    }
    public static void Reconcile(Vessel v){
        if(v.Water>1e-9||!SolventProperties.HasSolvent(v))return;
        foreach(string id in Solutes){
            if(!TryCapacity(v,id,out double capacity))continue;
            v.Solutes.TryGetValue(id,out double dissolved);v.Undissolved.TryGetValue(id,out double dry);v.Precipitates.TryGetValue(id,out double sediment);
            double total=dissolved+dry+sediment;if(total<=0)continue;
            double take=Math.Min(total,capacity),rest=total-take;
            v.Undissolved.Remove(id);
            if(take>0)v.Solutes[id]=take;else v.Solutes.Remove(id);
            if(rest>1e-12){v.Precipitates[id]=rest;PhaseProperties.Notice(v,id,false);}else v.Precipitates.Remove(id);
        }
        // Formula-unit inventory does not assume complete ionic dissociation in alcohol.
        // Ion pairing/speciation requires additional solvent-specific equilibrium data.
    }
}
}
