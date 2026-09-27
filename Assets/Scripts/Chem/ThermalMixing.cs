using System;
using System.Collections.Generic;

namespace ChemLab.Chem {
// Liquid sensible-heat transfer for the teaching model. This deliberately does
// not invent heat capacities for the other organic solvents or for precipitates.
public static class ThermalMixing {
    const double Eps=1e-10;
    // NIST-JANAF liquid water Cp(298.15 K) = 75.351 J/(mol K), or about
    // 4.18 J/(g K): https://janaf.nist.gov/tables/H-063.html
    public const double WaterCpJPerGramK=4.18;
    // NIST Chemistry WebBook liquid ethanol Cp(298.15 K) = 112.4 J/(mol K).
    // Petrov et al. report measurements from 258.15 to 318.15 K; here the
    // room-temperature value is a constant-Cp teaching approximation only in
    // that interval. https://webbook.nist.gov/cgi/cbook.cgi?ID=C64175&Mask=16
    public const double EthanolCpJPerMolK=112.4;

    static IEnumerable<Dictionary<string,double>> Maps(Vessel v){
        yield return v.Solutes;yield return v.Undissolved;yield return v.Precipitates;
    }
    static double LiquidMoles(Vessel v,string id){
        if(PhaseProperties.Phase(id,v.Temp)!="l")return 0;
        double n=0;
        foreach(var map in Maps(v))if(map.TryGetValue(id,out double amount))n+=Math.Max(0,amount);
        return n;
    }
    static string UnknownLiquid(Vessel v){
        foreach(var map in Maps(v))foreach(var pair in map){
            if(pair.Value<=Eps||pair.Key=="c2h5oh"||!ChemData.Sub.TryGetValue(pair.Key,out var s)||s.IsIon)continue;
            if(PhaseProperties.Phase(pair.Key,v.Temp)=="l")return pair.Key;
        }
        return null;
    }
    static bool TryPureLiquid(Vessel v,out string id,out double mass){
        id=null;mass=0;
        if(v.Water>Eps)return false;
        foreach(var map in Maps(v))foreach(var pair in map){
            if(pair.Value<=Eps)continue;
            if(!ChemData.Sub.TryGetValue(pair.Key,out var s)||s.IsIon||PhaseProperties.Phase(pair.Key,v.Temp)!="l")return false;
            if(id!=null&&id!=pair.Key)return false;
            id=pair.Key;mass+=pair.Value*s.Mass;
        }
        return id!=null&&mass>Eps;
    }
    static double KnownCapacity(Vessel v)=>v.Water*WaterCpJPerGramK+LiquidMoles(v,"c2h5oh")*EthanolCpJPerMolK;

    static PureLiquidThermalModel PureModel(Vessel v,out double mass){
        mass=0;
        return v!=null&&v.Gases.Count==0&&TryPureLiquid(v,out string id,out mass)?PureLiquidThermalModel.For(id):null;
    }
    static bool ModelRange(Vessel v,PureLiquidThermalModel model,double t)=>
        Math.Abs(v.PressureBar-1.01325)<=.005&&model.Valid(t);
    static string ModelNotice(PureLiquidThermalModel model)=>$"纯{model.Name}按焓守恒计算；{model.Assumptions}；未计玻璃蓄热";
    public static bool TryTemperatureAfterHeat(Vessel v,double joules,out float temperature,out string notice){
        temperature=v==null?0:v.Temp;notice="";
        if(v==null||double.IsNaN(joules)||double.IsInfinity(joules)){notice="热量必须为有限数值且需要容器";return false;}
        var model=PureModel(v,out double mass);
        if(model!=null){
            if(!ModelRange(v,model,v.Temp)){notice="纯液体热量模型超出温度或近常压范围，保留原温度";return false;}
            double h=model.H(v.Temp)+joules/mass;
            if(double.IsNaN(h)||double.IsInfinity(h)||h<model.H(model.MinC)||h>model.H(model.BoilingC)){
                notice="热量超出纯液体焓模型范围，保留原温度；此反应热入口尚未处理相变潜热";return false;
            }
            double t=model.T(h);temperature=(float)t;
            if(temperature>=model.BoilingC)temperature=(float)(t-1e-5);
            notice=ModelNotice(model);return true;
        }
        if(!TryHeatCapacity(v,out double capacity,out notice))return false;
        double trial=v.Temp+joules/capacity;
        if(!HeatCapacityTemperatureValid(v,trial)){
            notice="计算后温度超出所用液相热容近似范围，未应用该反应热；相变潜热未建模";return false;
        }
        temperature=(float)trial;return true;
    }
    // Vessel.Temp is the INITIAL temperature. Positive dT means heating.
    // No temperature or inventory mutation occurs while measuring energy.
    public static bool TryEnergyForTemperatureChange(Vessel v,double dT,out double joules,out string notice){
        joules=double.NaN;notice="";
        if(v==null||double.IsNaN(dT)||double.IsInfinity(dT)){notice="温差必须为有限数值且需要容器";return false;}
        var model=PureModel(v,out double mass);
        if(model!=null){
            if(!ModelRange(v,model,v.Temp)||!ModelRange(v,model,v.Temp+dT)){
                notice="量热区间超出纯液体温度或近常压范围，未外推焓";return false;
            }
            joules=mass*(model.H(v.Temp+dT)-model.H(v.Temp));notice=ModelNotice(model);return true;
        }
        if(!TryHeatCapacity(v,out double capacity,out notice))return false;
        if(!HeatCapacityTemperatureValid(v,v.Temp+dT)){notice="量热终温超出所用液相热容近似范围，未外推能量";return false;}
        joules=capacity*dT;return !double.IsNaN(joules)&&!double.IsInfinity(joules);
    }

    public static bool HeatCapacityTemperatureValid(Vessel v,double temp){
        if(v==null||double.IsNaN(temp)||double.IsInfinity(temp))return false;
        var model=PureModel(v,out _);if(model!=null)return ModelRange(v,model,temp);
        bool water=v.Water>Eps,ethanol=LiquidMoles(v,"c2h5oh")>Eps;
        return (!water||(temp>=0&&temp<=100))&&(!ethanol||(temp>= -15&&temp<=45));
    }
    // Total J/K of the currently represented liquid inventory. This is not a
    // property of the glass, precipitate, gas headspace, or unknown solvent.
    public static bool TryHeatCapacity(Vessel v,out double joulesPerKelvin,out string notice){
        joulesPerKelvin=0;notice="";
        if(v==null){notice="缺少容器，未计算定量热效应";return false;}
        var pure=PureModel(v,out double mass);
        if(pure!=null){
            if(!ModelRange(v,pure,v.Temp)){notice="当前纯液体超出温度或近常压范围，未计算热容";return false;}
            joulesPerKelvin=mass*pure.HeatCapacity(v.Temp);notice=ModelNotice(pure);return true;
        }
        if(UnknownLiquid(v)!=null){notice="混合液缺少专属热容/混合焓数据，未计算定量热效应；保留原温度";return false;}
        if(!HeatCapacityTemperatureValid(v,v.Temp)){
            notice="当前温度超出所用液相热容近似范围，未计算定量热效应；保留原温度";return false;
        }
        joulesPerKelvin=KnownCapacity(v);
        if(joulesPerKelvin<=Eps){notice="缺少当前液体的热容数据，未计算定量热效应；保留原温度";return false;}
        bool ethanol=LiquidMoles(v,"c2h5oh")>Eps;
        notice=ethanol?"水/乙醇使用近室温定热容近似，未计混合焓与热容温变":
                       "水溶液按水的近室温热容近似，未计溶质热容";
        if(v.SolidVolume()>Eps)notice+="；固体热容未计入";
        return true;
    }

    // Read the pre-transfer inventories. liquidFraction is the fraction that
    // actually arrived, not the fraction still travelling in a staged pour.
    public static bool TryTargetTemperature(Vessel source,Vessel target,double liquidFraction,
                                            out float mixed,out string notice){
        mixed=target.Temp;notice="";
        if(liquidFraction<=Eps||source.Volume()<=Eps||Math.Abs(source.Temp-target.Temp)<1e-6)return true;
        // Empty clean receiver: no incoming-liquid heat capacity data is needed.
        // Glassware heat capacity is outside this model.
        if(target.Volume()<=Eps&&target.SolidVolume()<=Eps){mixed=source.Temp;return true;}

        // Pure ethanol now shares a bounded enthalpy curve for hot and cold
        // liquid, rather than a constant-Cp arithmetic temperature average.
        if(TryPureLiquid(source,out string pureFrom,out double sourceMass)&&PureLiquidThermalModel.For(pureFrom)!=null&&
           TryPureLiquid(target,out string pureTo,out double targetMass)&&pureTo==pureFrom&&
           source.Gases.Count==0&&target.Gases.Count==0){
            var model=PureLiquidThermalModel.For(pureFrom);
            if(!(Math.Abs(source.PressureBar-1.01325)<=.005)||!(Math.Abs(target.PressureBar-1.01325)<=.005)){
                notice="纯液体焓曲线只用于近常压，当前压力缺少修正数据；保留接收器皿温度";return false;
            }
            if(!model.Valid(source.Temp)||!model.Valid(target.Temp)){
                notice="超出纯液体液相焓模型温区，未外推混温；保留接收器皿温度";return false;
            }
            mixed=(float)model.T((sourceMass*liquidFraction*model.H(source.Temp)+targetMass*model.H(target.Temp))/(sourceMass*liquidFraction+targetMass));
            notice=$"纯{model.Name}按焓守恒混温；{model.Assumptions}；未计玻璃蓄热";
            return true;
        }

        string unknownFrom=UnknownLiquid(source),unknownTo=UnknownLiquid(target);
        if(unknownFrom!=null||unknownTo!=null){
            if(TryPureLiquid(source,out string fromId,out double fromMass)&&
               TryPureLiquid(target,out string toId,out double toMass)&&fromId==toId){
                // Same pure liquid: the unknown constant Cp cancels. This is
                // still an approximation if its Cp changes with temperature.
                mixed=(float)((toMass*target.Temp+fromMass*liquidFraction*source.Temp)/
                              (toMass+fromMass*liquidFraction));
                notice="同种纯液体按等比热容近似混温；缺少该物质随温度变化的热容数据";
                return true;
            }
            notice="混合液缺少专属热容/混合焓数据，未计算混温；保留接收器皿温度";
            return false;
        }

        bool hasWater=source.Water>Eps||target.Water>Eps;
        bool hasEthanol=LiquidMoles(source,"c2h5oh")>Eps||LiquidMoles(target,"c2h5oh")>Eps;
        if((hasWater&&(source.Temp<0||source.Temp>100||target.Temp<0||target.Temp>100))||
           (hasEthanol&&(source.Temp< -15||source.Temp>45||target.Temp< -15||target.Temp>45))){
            notice="当前温度超出所用液相热容近似范围，未计算混温；保留接收器皿温度";
            return false;
        }
        double incoming=KnownCapacity(source)*liquidFraction,existing=KnownCapacity(target);
        if(incoming<=Eps||existing<=Eps){
            notice="缺少当前液体的热容数据，未计算混温；保留接收器皿温度";
            return false;
        }
        mixed=(float)((existing*target.Temp+incoming*source.Temp)/(existing+incoming));
        if(hasEthanol)notice="水/乙醇混温使用近室温热容近似，未计混合焓与热容温变";
        if(source.SolidVolume()>Eps||target.SolidVolume()>Eps)
            notice+="；固体热容未计入";
        return true;
    }
    public static void RecordNotice(Vessel target,string notice){
        if(target==null||string.IsNullOrEmpty(notice))return;
        // A thermal approximation must not hide a more urgent solubility or
        // phase warning already shown to the student for this vessel.
        if(string.IsNullOrEmpty(target.PhaseNotice)||target.PhaseNotices.Contains("thermal:"+target.PhaseNotice))
            target.PhaseNotice=notice;
        if(target.PhaseNotices.Add("thermal:"+notice))Lab.Note(target,notice,"obs");
    }
}
}
