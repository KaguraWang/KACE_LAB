using System;
using System.Linq;
namespace ChemLab.Chem {
// Sensible + fusion enthalpy transfer for the existing bounded pure-water model.
// Prediction is mutation-free, including during staged flight/capacity checks.
public static class IceWaterTransfer {
    public const string Notice="纯水冰水转移按显热与熔化潜热守恒；冰水共存保持0℃，未计容器热容、过冷成核及混合物凝固点";
    public struct State {
        public double LiquidGrams,IceGrams,TemperatureC;
        public double VolumeMl=>LiquidGrams+IceGrams/PhaseProperties.Density(WaterIce.Id);
        public void Apply(Vessel v){
            v.Water=LiquidGrams;v.Temp=(float)TemperatureC;
            v.Precipitates.Remove(WaterIce.Id);v.Undissolved.Remove(WaterIce.Id);
            if(IceGrams>1e-12)v.Undissolved[WaterIce.Id]=IceGrams/ChemData.Sub[WaterIce.Id].Mass;
        }
    }
    static bool Supported(Vessel v)=>WaterIce.CanPartition(v)||
        (Math.Abs(v.PressureBar-1.01325)<=.005&&v.Temp>40&&v.Temp<=100&&v.Water>=0&&
         v.Solutes.Values.All(n=>n<=1e-12)&&v.Undissolved.Values.All(n=>n<=1e-12)&&
         v.Precipitates.Values.All(n=>n<=1e-12)&&v.Gases.Values.All(n=>n<=1e-12));
    public static bool TryPredict(Vessel from,Vessel to,double liquidFraction,double solidFraction,out State state){
        state=default;
        if(from==null||to==null||!Supported(from)||!Supported(to)||
           liquidFraction<0||liquidFraction>1||solidFraction<0||solidFraction>1||
           double.IsNaN(liquidFraction)||double.IsNaN(solidFraction))return false;
        double fromIce=WaterIce.IceMass(from),toIce=WaterIce.IceMass(to);
        if(fromIce+toIce<=1e-12)return false;
        double liquid=from.Water*liquidFraction,ice=fromIce*solidFraction;
        if(liquid+ice<=1e-12)return false;
        double mass=to.Water+toIce+liquid+ice;
        double energy=WaterIce.Enthalpy(to)+liquid*(WaterIce.FusionJPerGram+ThermalMixing.WaterCpJPerGramK*from.Temp)+ice*WaterIce.IceCpJPerGramK*from.Temp;
        double water,t;
        if(energy<0){water=0;t=energy/(mass*WaterIce.IceCpJPerGramK);}
        else if(energy<=mass*WaterIce.FusionJPerGram){water=energy/WaterIce.FusionJPerGram;t=0;}
        else {water=mass;t=(energy-mass*WaterIce.FusionJPerGram)/(mass*ThermalMixing.WaterCpJPerGramK);}
        if(double.IsNaN(t)||double.IsInfinity(t)||t< -10||t>100)return false;
        state=new State{LiquidGrams=water,IceGrams=Math.Max(0,mass-water),TemperatureC=t};return true;
    }
}
}
