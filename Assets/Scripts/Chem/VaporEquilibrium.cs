using System;
using System.Linq;
namespace ChemLab.Chem {
// Pure-solvent, imposed-temperature equilibrium with ideal headspace gas.
// Not an adiabatic or high-pressure equation of state; no mixed-solvent activity model.
public static class VaporEquilibrium {
    public static bool TryPressure(string id,double celsius,out double bar){
        bar=double.NaN;double t=celsius+273.15,a,b,c;
        if(id=="h2o"){
            // NIST WebBook C7732185, Bridgeman/Aldrich; Stull for range gaps.
            if(t<273.15||t>373)return false;
            if(t<=303){a=5.40221;b=1838.675;c=-31.737;}
            else if(t>=304&&t<=333){a=5.20389;b=1733.926;c=-39.485;}
            else if(t>=334&&t<=363){a=5.0768;b=1659.793;c=-45.854;}
            else if(t>=344){a=5.08354;b=1663.125;c=-45.622;}
            else {a=4.6543;b=1435.264;c=-64.848;}
        }else if(id=="c2h5oh"){
            // https://webbook.nist.gov/cgi/cbook.cgi?ID=C64175&Mask=4
            if(t<273.15||t>366.63)return false;
            if(t<=351.7){a=5.37229;b=1670.409;c=-40.191;}
            else {a=5.24677;b=1598.673;c=-46.424;}
        }else return false;
        bar=Math.Pow(10,a-b/(t+c));return true;
    }
    public static void Reconcile(Vessel v){
        if(!v.Sealed)return;
        string id=v.Water>0||v.Gases.ContainsKey("h2o")?"h2o":"c2h5oh";
        // Dilute dissolved permanent gases don't count as a second solvent.
        if(v.Solutes.Keys.Any(k=>k!=id&&k!="o2"&&k!="h2"&&k!="co2")||v.Undissolved.Count>0||v.Precipitates.Count>0)return;
        if(id=="c2h5oh"&&v.Water>0)return;
        if(!TryPressure(id,v.Temp,out double saturation))return;
        double mass=Formula.MolarMass(id=="h2o"?"H2O":"C2H5OH");
        double density=id=="h2o"?1:ChemData.Sub[id].Density;
        double liquid=id=="h2o"?v.Water/mass:(v.Solutes.TryGetValue(id,out double n)?n:0);
        v.Gases.TryGetValue(id,out double gas);double total=liquid+gas;if(total<=0)return;
        double molarLitres=mass/density/1000,rt=v.GasRT;
        double emptyHead=v.InteriorCapacityMl/1000-total*molarLitres;
        if(emptyHead<=0||rt<=saturation*molarLitres)return;
        double vapor=Math.Min(total,saturation*emptyHead/(rt-saturation*molarLitres));
        double remaining=total-vapor;
        if(id=="h2o")v.Water=remaining*mass;
        else if(remaining>1e-12)v.Solutes[id]=remaining;else v.Solutes.Remove(id);
        if(vapor>1e-12)v.Gases[id]=vapor;else v.Gases.Remove(id);
    }
}
}
