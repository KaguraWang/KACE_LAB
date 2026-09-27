using System;
namespace ChemLab.Chem {
// Conservative implicit exchange between a finite vessel wall and a finite
// liquid inventory. No phase change or external heating is hidden in this step.
public static class VesselSensibleExchange {
    public struct Result {
        public double LiquidC,WallC,LiquidGainJ,WallGainJ;
    }
    static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    public static bool TryStep(LiquidEnthalpyCurve curve,double liquidKg,double liquidC,
        double wallCapacityJPerK,double wallC,double conductanceWPerK,double seconds,
        out Result result,out string error,PureLiquidThermalModel exactModel=null){
        result=default;error="";
        if(curve==null||(exactModel!=null&&curve!=exactModel.Curve)||!Finite(liquidKg)||liquidKg<=0||!Finite(wallCapacityJPerK)||wallCapacityJPerK<=0||
           !Finite(liquidC)||!Finite(wallC)||liquidC<curve.MinC||liquidC>curve.MaxC||wallC<curve.MinC||wallC>curve.MaxC||
           !Finite(conductanceWPerK)||conductanceWPerK<0||!Finite(seconds)||seconds<0){
            error="器皿换热输入无效或超出液相焓曲线范围";return false;
        }
        result=new Result{LiquidC=liquidC,WallC=wallC};
        if(seconds==0||conductanceWPerK==0||liquidC==wallC)return true;
        double H(double t)=>exactModel==null?curve.Enthalpy(t):exactModel.H(t)*1000;
        double T(double h)=>exactModel==null?curve.Temperature(h):exactModel.T(h/1000);
        double h0=H(liquidC);
        double lo=liquidKg*(H(Math.Min(liquidC,wallC))-h0);
        double hi=liquidKg*(H(Math.Max(liquidC,wallC))-h0);
        if(!Finite(lo)||!Finite(hi)){result=default;error="器皿换热能量超出有限范围";return false;}
        // Restrict both nodes to the initial temperature interval. This also
        // bounds energy for a nearly empty liquid or very small glass capacity.
        lo=Math.Max(lo,wallCapacityJPerK*(wallC-Math.Max(liquidC,wallC)));
        hi=Math.Min(hi,wallCapacityJPerK*(wallC-Math.Min(liquidC,wallC)));
        double resistanceTime=conductanceWPerK>=1?(1/conductanceWPerK)/seconds:(1/seconds)/conductanceWPerK;
        if(double.IsInfinity(resistanceTime))return true; // sub-representable exchange
        double q=0;
        for(int n=0;n<80;n++){
            q=lo*.5+hi*.5;
            double t=T(h0+q/liquidKg);
            double residual=q*resistanceTime-(wallC-q/wallCapacityJPerK-t);
            if(residual>0)hi=q;else lo=q;
        }
        double finalLiquid=T(h0+q/liquidKg),finalWall=wallC-q/wallCapacityJPerK;
        if(!Finite(finalLiquid)||!Finite(finalWall)||!Finite(q)){result=default;error="器皿换热解超出有限范围";return false;}
        result=new Result{LiquidC=finalLiquid,WallC=finalWall,LiquidGainJ=q,WallGainJ=-q};return true;
    }
}
}
