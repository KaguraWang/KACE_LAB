using System;
namespace ChemLab.Chem {
// Farelo, Lopes & Ferra (2004), DOI 10.1021/je049782s, NIST ThermoML.
// https://trc.nist.gov/ThermoML/10.1021/je049782s.json
// NaCl molality = mol / kg of WATER+ETHANOL SOLVENT, not kg solution.
// Original temperature and solvent mass-fraction coordinates are retained.
public static class MixedSolubility {
    static readonly double[] Fractions={0.0483,0.0994,0.1505,0.201};
    static readonly double[][,] Curves={
        new double[,]{{298.15,5.521},{300.85,5.536},{303.15,5.551},{303.35,5.552},{305.75,5.578},{308.15,5.587},{310.55,5.607},{313.15,5.621},{315.55,5.642},{320.45,5.678},{323.05,5.694},{323.15,5.696}},
        new double[,]{{298.05,4.939},{300.75,4.955},{303.15,4.975},{305.75,5.002},{308.25,5.016},{310.6,5.04},{313.15,5.055},{315.75,5.079},{318.05,5.095},{320.75,5.12},{322.85,5.137},{323.25,5.145}},
        new double[,]{{298.15,4.411},{298.35,4.410},{300.35,4.436},{303.15,4.453},{305.75,4.473},{308.25,4.506},{313.15,4.543},{318.05,4.590},{320.45,4.597},{322.85,4.626}},
        new double[,]{{298.1,3.950},{298.45,3.953},{300.65,3.971},{303.15,3.987},{305.75,4.015},{308.25,4.037},{310.65,4.062},{313.05,4.077},{313.35,4.085},{315.6,4.101},{317.8,4.128},{320.85,4.157},{323.25,4.183}}
    };
    static bool AtTemperature(int index,double kelvin,out double molality){
        molality=0;var curve=Curves[index];int n=curve.GetLength(0);
        if(kelvin<curve[0,0]-1e-7||kelvin>curve[n-1,0]+1e-7)return false;
        for(int i=1;i<n;i++)if(kelvin<=curve[i,0]+1e-7){
            double t=Math.Max(0,Math.Min(1,(kelvin-curve[i-1,0])/(curve[i,0]-curve[i-1,0])));
            molality=curve[i-1,1]*(1-t)+curve[i,1]*t;return true;
        }
        molality=curve[n-1,1];return true;
    }
    public static bool TryNaCl(Vessel v,out double capacity){
        capacity=double.NaN;if(Math.Abs(v.PressureBar-1.013)>0.002)return false;
        foreach(var id in SolventProperties.Organic)if(id!="c2h5oh"&&(v.Solutes.ContainsKey(id)||v.Undissolved.ContainsKey(id)))return false;
        v.Solutes.TryGetValue("c2h5oh",out double ethanol);v.Undissolved.TryGetValue("c2h5oh",out double extra);
        double grams=(ethanol+extra)*ChemData.Sub["c2h5oh"].Mass,total=grams+v.Water;
        if(total<=0)return false;double fraction=grams/total,temperature=v.Temp+273.15;
        for(int i=0;i<Fractions.Length;i++)if(Math.Abs(fraction-Fractions[i])<1e-8){
            if(!AtTemperature(i,temperature,out double exact))return false;capacity=exact*total/1000;return true;
        }
        for(int i=1;i<Fractions.Length;i++)if(fraction>Fractions[i-1]&&fraction<Fractions[i]){
            if(!AtTemperature(i-1,temperature,out double left)||!AtTemperature(i,temperature,out double right))return false;
            double t=(fraction-Fractions[i-1])/(Fractions[i]-Fractions[i-1]);capacity=(left*(1-t)+right*t)*total/1000;return true;
        }
        return false; // No extrapolation to pure ethanol, other temperatures, or pressure.
    }
}
}
