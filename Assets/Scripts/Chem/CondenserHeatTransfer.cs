using System;

namespace ChemLab.Chem {
// Counter-current, steady-state pure-vapor condenser. The inlet vapor may be
// superheated; the outlet may be vapor, two-phase, or subcooled liquid.
// Three thermal regions follow NPTEL Process Design of Heat Exchanger, lecture 6:
// https://archive.nptel.ac.in/content/storage2/courses/103103027/module1/lec6/2.html
// UA = integral(dQ / local temperature difference). Vapor/coolant Cp are
// constant; liquid Cp may instead be represented by a bounded h(T) table.
// No guessed substance properties or calibrated apparatus defaults live here.
// Wall/coolant storage, pressure drop and mixture VLE are separate model layers.
public static class CondenserHeatTransfer {
    public struct Input {
        public double VaporKgPerSecond, VaporInletC, SaturationC;
        public double LatentJPerKg, VaporCpJPerKgK, LiquidCpJPerKgK;
        public double CoolantKgPerSecond, CoolantCpJPerKgK, CoolantInletC;
        public double ConductanceWPerK;
        public LiquidEnthalpyCurve LiquidCurve; // optional: bounded variable-Cp liquid
    }
    public struct Result {
        public double HeatWatts, CoolantOutletC, ProductOutletC, VaporQuality;
        public double CondensedKgPerSecond, UncondensedKgPerSecond;
        public double DesuperheatWatts, LatentWatts, SubcoolWatts;
        public double UsedConductanceWPerK;
    }
    static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    static double Temperature(Input p,double h){
        if(h<0)return p.LiquidCurve!=null?p.LiquidCurve.Temperature(h):p.SaturationC+h/p.LiquidCpJPerKgK;
        if(h<=p.LatentJPerKg)return p.SaturationC;
        return p.SaturationC+(h-p.LatentJPerKg)/p.VaporCpJPerKgK;
    }
    static double Enthalpy(Input p,double t)=>t<p.SaturationC?
        (p.LiquidCurve!=null?p.LiquidCurve.Enthalpy(t):p.LiquidCpJPerKgK*(t-p.SaturationC)):p.LatentJPerKg+p.VaporCpJPerKgK*(t-p.SaturationC);

    // q measures heat removed since the hot-stream inlet. Counter-current water
    // is hottest at q=0 and at its inlet temperature when q=totalHeat.
    static double RequiredUA(Input p,double hin,double coolantCapacity,double totalHeat){
        if(totalHeat<=0)return 0;
        double saturatedVapor=p.VaporKgPerSecond*(hin-p.LatentJPerKg);
        double saturatedLiquid=p.VaporKgPerSecond*hin;
        double Difference(double q)=>Temperature(p,hin-q/p.VaporKgPerSecond)-
            (p.CoolantInletC+(totalHeat-q)/coolantCapacity);
        double Segment(double from,double to){
            if(to<=from)return 0;
            double a=Difference(from),b=Difference(to);
            if(a<=0||b<=0)return double.PositiveInfinity; // local pinch/cross
            double span=to-from,mean=(a+b)*.5;
            // Exact log-mean integral for each linear temperature segment.
            return Math.Abs(a-b)<mean*1e-8?span/mean:span*Math.Log(a/b)/(a-b);
        }
        double first=Math.Max(0,Math.Min(totalHeat,saturatedVapor)),second=Math.Max(first,Math.Min(totalHeat,saturatedLiquid));
        double ua=Segment(0,first)+Segment(first,second);
        if(p.LiquidCurve==null)return ua+Segment(second,totalHeat);
        // Split the subcooling section at every h(T) knot. On each interval
        // both stream temperatures are linear in removed heat, so the log-mean
        // integral is exact for the tabulated curve, including internal pinches.
        double start=second;
        for(int i=p.LiquidCurve.Count-2;i>=0&&start<totalHeat;i--){
            double stop=Math.Min(totalHeat,p.VaporKgPerSecond*(hin-p.LiquidCurve.KnotEnthalpy(i)));
            if(stop>start){ua+=Segment(start,stop);start=stop;}
        }
        return ua;
    }
    public static bool TrySolve(Input p,out Result result,out string error){
        return TrySolveCore(p,false,out result,out error);
    }
    // A finite-capacity wall is held at a trial temperature by the outer
    // transient solve. Infinite cold heat-capacity rate is an exact boundary
    // condition here, not an artificially large fabricated coolant flow.
    public static bool TrySolveFixedWall(Input p,out Result result,out string error){
        return TrySolveCore(p,true,out result,out error,true);
    }
#if UNITY_EDITOR
    public static bool TrySolveFixedWallReference(Input p,out Result result,out string error)=>TrySolveCore(p,true,out result,out error);
#endif
    // Exact inversion of each constant-Cp segment against a fixed-temperature
    // wall. A tabulated h(T) is piecewise constant Cp, so no curve is discarded.
    static double FixedWallHeat(Input p){
        double remaining=p.ConductanceWPerK,heat=0,t=p.VaporInletC,wall=p.CoolantInletC,m=p.VaporKgPerSecond;
        bool Sensible(double target,double cp){
            target=Math.Max(target,wall);
            double need=target>wall?m*cp*Math.Log((t-wall)/(target-wall)):double.PositiveInfinity;
            if(remaining<need){double next=wall+(t-wall)*Math.Exp(-remaining/(m*cp));heat+=m*cp*(t-next);t=next;remaining=0;return false;}
            heat+=m*cp*(t-target);t=target;remaining-=need;return true;
        }
        if(t>p.SaturationC&&!Sensible(p.SaturationC,p.VaporCpJPerKgK))return heat;
        if(t<=wall||remaining<=0)return heat;
        double latent=m*p.LatentJPerKg,latentUA=latent/(t-wall);
        if(remaining<latentUA)return heat+remaining*(t-wall);
        heat+=latent;remaining-=latentUA;
        if(p.LiquidCurve==null){Sensible(wall,p.LiquidCpJPerKgK);return heat;}
        for(int i=p.LiquidCurve.Count-2;i>=0&&remaining>0;i--){
            double low=p.LiquidCurve.KnotTemperature(i),high=p.LiquidCurve.KnotTemperature(i+1);
            double cp=(p.LiquidCurve.KnotEnthalpy(i+1)-p.LiquidCurve.KnotEnthalpy(i))/(high-low);
            if(!Sensible(low,cp))break;
        }
        return heat;
    }
    static bool TrySolveCore(Input p,bool fixedWall,out Result result,out string error,bool analytic=false){
        result=default;error="";
        foreach(double x in new[]{p.VaporKgPerSecond,p.VaporInletC,p.SaturationC,p.LatentJPerKg,
            p.VaporCpJPerKgK,p.LiquidCpJPerKgK,p.CoolantKgPerSecond,p.CoolantCpJPerKgK,
            p.CoolantInletC,p.ConductanceWPerK})
            if(!Finite(x)){error="冷凝传热输入必须是有限数值";return false;}
        if(p.VaporKgPerSecond<0||p.CoolantKgPerSecond<0||p.ConductanceWPerK<0||
           p.LatentJPerKg<=0||p.VaporCpJPerKgK<=0||p.LiquidCpJPerKgK<=0||p.CoolantCpJPerKgK<=0||
           p.VaporInletC<p.SaturationC||p.CoolantInletC<=-273.15||p.SaturationC<=-273.15){
            error="冷凝传热需要非负流量/热导、正热容/潜热，以及不低于饱和温度的入口蒸气";return false;
        }
        double hin=Enthalpy(p,p.VaporInletC),cw=fixedWall?double.PositiveInfinity:p.CoolantKgPerSecond*p.CoolantCpJPerKgK;
        if(p.LiquidCurve!=null&&(Math.Abs(p.LiquidCurve.MaxC-p.SaturationC)>1e-8||p.CoolantInletC<p.LiquidCurve.MinC)){
            error="液相焓曲线必须覆盖冷却剂入口至饱和温度，禁止超出数据范围外推";return false;
        }
        double q=0;
        if(p.VaporKgPerSecond>0&&cw>0&&p.ConductanceWPerK>0&&p.VaporInletC>p.CoolantInletC){
            double upper=Math.Min(p.VaporKgPerSecond*(hin-Enthalpy(p,p.CoolantInletC)),cw*(p.VaporInletC-p.CoolantInletC));
            // At an exactly saturated cold inlet, the coldest possible hot
            // outlet is saturated vapor, not liquid at the same temperature.
            double lower=0;
            if(analytic){
                double candidate=Math.Min(upper,Math.Max(0,FixedWallHeat(p)));
                double needed=RequiredUA(p,hin,cw,candidate);
                // At an unrepresentably small approach temperature, retain the
                // original feasible-bracket solve rather than report a pinch.
                if(Finite(candidate)&&Finite(needed)&&needed<=p.ConductanceWPerK*(1+1e-10))lower=upper=candidate;
            }
            for(int i=0;i<90&&upper>lower;i++){
                double trial=(lower+upper)*.5;
                if(RequiredUA(p,hin,cw,trial)>p.ConductanceWPerK)upper=trial;else lower=trial;
            }
            // Retain the feasible lower bracket. At a pinch, rounding the
            // midpoint to the upper bracket can imply an infinite required UA.
            q=lower;
        }
        double hout=p.VaporKgPerSecond>0?hin-q/p.VaporKgPerSecond:hin;
        double quality=Math.Max(0,Math.Min(1,hout/p.LatentJPerKg));
        double superheat=p.VaporKgPerSecond*Math.Max(0,hin-p.LatentJPerKg);
        double sensible=Math.Min(q,superheat),latent=Math.Min(Math.Max(0,q-sensible),p.VaporKgPerSecond*p.LatentJPerKg);
        result=new Result{
            HeatWatts=q,CoolantOutletC=cw>0?p.CoolantInletC+q/cw:p.CoolantInletC,
            ProductOutletC=Temperature(p,hout),VaporQuality=quality,
            CondensedKgPerSecond=p.VaporKgPerSecond*(1-quality),UncondensedKgPerSecond=p.VaporKgPerSecond*quality,
            DesuperheatWatts=sensible,LatentWatts=latent,SubcoolWatts=Math.Max(0,q-sensible-latent),
            UsedConductanceWPerK=q>0?RequiredUA(p,hin,cw,q):0
        };
        return true;
    }
}
}
