using System;
namespace ChemLab.Chem {
// Finite wall -> finite liquid exchange with saturated vapor leaving the liquid.
// No gas superheating, mixture data or above-boiling liquid extrapolation.
public static class VesselPhaseExchange {
    public struct Result {
        public double LiquidC,WallC,HeatFromWallJ,SensibleJ,LatentJ,VaporKg,RemainingLiquidKg;
    }
    static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    public static bool TryStep(LiquidEnthalpyCurve curve,double liquidKg,double liquidC,
        double wallCapacityJPerK,double wallC,double conductanceWPerK,double seconds,double latentJPerKg,
        out Result result,out string error,PureLiquidThermalModel exactModel=null){
        result=default;error="";
        if(curve==null||(exactModel!=null&&curve!=exactModel.Curve)||!Finite(liquidKg)||liquidKg<=0||
           !Finite(wallCapacityJPerK)||wallCapacityJPerK<=0||!Finite(liquidC)||liquidC<curve.MinC||liquidC>curve.MaxC||
           !Finite(wallC)||wallC<curve.MinC||!Finite(conductanceWPerK)||conductanceWPerK<0||
           !Finite(seconds)||seconds<0||!Finite(latentJPerKg)||latentJPerKg<=0){
            error="器皿相变换热输入无效";return false;
        }
        if(wallC<=curve.MaxC){
            if(!VesselSensibleExchange.TryStep(curve,liquidKg,liquidC,wallCapacityJPerK,wallC,conductanceWPerK,seconds,out var sensible,out error,exactModel))return false;
            result=new Result{LiquidC=sensible.LiquidC,WallC=sensible.WallC,HeatFromWallJ=sensible.LiquidGainJ,
                SensibleJ=sensible.LiquidGainJ,RemainingLiquidKg=liquidKg};return true;
        }
        result=new Result{LiquidC=liquidC,WallC=wallC,RemainingLiquidKg=liquidKg};
        if(seconds==0||conductanceWPerK==0)return true;
        double H(double t)=>exactModel==null?curve.Enthalpy(t):exactModel.H(t)*1000;
        double T(double h)=>exactModel==null?curve.Temperature(h):exactModel.T(h/1000);
        double initialH=H(liquidC),saturationH=H(curve.MaxC);
        double warming=liquidKg*(saturationH-initialH),latentBudget=liquidKg*latentJPerKg;
        double inventoryLimit=warming+latentBudget;
        if(!Finite(inventoryLimit)){result=default;error="有限液体热预算溢出";return false;}
        double hi=Math.Min(inventoryLimit,wallCapacityJPerK*(wallC-liquidC)),lo=0;
        double resistanceTime=conductanceWPerK>=1?(1/conductanceWPerK)/seconds:(1/seconds)/conductanceWPerK;
        if(double.IsInfinity(resistanceTime))return true;
        double LiquidTemperature(double q)=>q>=warming?curve.MaxC:T(initialH+q/liquidKg);
        double Residual(double q)=>q*resistanceTime-(wallC-q/wallCapacityJPerK-LiquidTemperature(q));
        double heat=hi;
        // At dry-out, further exchange must stop: excess heat remains in glass.
        // Otherwise solve the common implicit heat flux through the phase plateau.
        if(Residual(hi)>0){
            for(int i=0;i<80;i++){
                heat=lo*.5+hi*.5;
                if(Residual(heat)>0)hi=heat;else lo=heat;
            }
        }
        double latent=Math.Max(0,heat-warming);
        double vapor=Math.Min(liquidKg,latent/latentJPerKg);
        double wall=wallC-heat/wallCapacityJPerK,t=LiquidTemperature(heat);
        if(!Finite(heat)||!Finite(wall)||!Finite(t)||!Finite(vapor)){result=default;error="器皿相变换热解溢出";return false;}
        result=new Result{LiquidC=t,WallC=wall,HeatFromWallJ=heat,SensibleJ=heat-latent,LatentJ=latent,
            VaporKg=vapor,RemainingLiquidKg=Math.Max(0,liquidKg-vapor)};return true;
    }
}
}
