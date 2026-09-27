using System;
namespace ChemLab.Chem {
// Two finite-capacity nodes: glass wall and well-mixed coolant jacket.
// User-supplied conductances/capacities; no invented material constants.
// Backward Euler is passive for arbitrarily large finite timesteps. The same
// endpoint fluxes update the nodes and all boundary ledgers exactly once.
public static class CondenserThermalStorage {
    public struct Input {
        public double WallCapacityJPerK,CoolantCapacityJPerK;
        public double HotWallWPerK,WallCoolantWPerK,WallAmbientWPerK;
        public double CoolantFlowCapacityWPerK;
        public double HotC,CoolantInletC,AmbientC,Seconds;
    }
    public struct State {public double WallC,CoolantC;}
    public struct Result {
        public State Next;
        public double HotRemovedJ,CoolantExportedJ,AmbientExportedJ;
        public double WallStoredJ,CoolantStoredJ,WallToCoolantJ;
    }
    static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    // Limit hot-stream extraction to energy actually available in this step.
    // Reducing the active hot contact preserves the full wall/jacket/ambient
    // balance; clipping HeatRemoved alone would silently create stored energy.
    public static bool TryStepLimited(Input p,State state,double availableHotJ,out Result result,out string error){
        result=default;error="";
        if(!Finite(availableHotJ)||availableHotJ<0){error="可用蒸气热量必须为非负有限值";return false;}
        if(!TryStep(p,state,out var full,out error))return false;
        if(full.HotRemovedJ<=availableHotJ){result=full;return true;}
        double low=0,high=p.HotWallWPerK;
        var candidate=p;candidate.HotWallWPerK=0;
        if(!TryStep(candidate,state,out result,out error))return false;
        for(int n=0;n<64;n++){
            double mid=low+(high-low)*.5;candidate.HotWallWPerK=mid;
            if(!TryStep(candidate,state,out var trial,out error)){result=default;return false;}
            if(trial.HotRemovedJ>availableHotJ)high=mid;else{low=mid;result=trial;}
        }
        return true;
    }
    public static bool TryStep(Input p,State state,out Result result,out string error){
        return TryStepCore(p,state,0,out result,out error);
    }
    public static bool TryStepHeat(Input p,State state,double heatJ,out Result result,out string error){
        result=default;error="";
        if(!Finite(heatJ)||p.HotWallWPerK!=0||(p.Seconds==0&&heatJ!=0)){
            error="指定壁面供热时须禁用热侧热导，且热量有限、零步长不注入热量";return false;
        }
        return TryStepCore(p,state,heatJ,out result,out error);
    }
    static bool TryStepCore(Input p,State state,double heatJ,out Result result,out string error){
        result=default;error="";
        foreach(double x in new[]{p.WallCapacityJPerK,p.CoolantCapacityJPerK,p.HotWallWPerK,p.WallCoolantWPerK,
            p.WallAmbientWPerK,p.CoolantFlowCapacityWPerK,p.HotC,p.CoolantInletC,p.AmbientC,p.Seconds,state.WallC,state.CoolantC})
            if(!Finite(x)){error="冷凝器热惯性参数必须为有限数值";return false;}
        if(p.WallCapacityJPerK<=0||p.CoolantCapacityJPerK<=0||p.Seconds<0||p.HotWallWPerK<0||
            p.WallCoolantWPerK<0||p.WallAmbientWPerK<0||p.CoolantFlowCapacityWPerK<0||
            p.HotC<=-273.15||p.CoolantInletC<=-273.15||p.AmbientC<=-273.15||state.WallC<=-273.15||state.CoolantC<=-273.15){
            error="热容必须为正；时间、流量热容率与热导非负，温度高于绝对零度";return false;
        }
        if(p.Seconds==0){result.Next=state;return true;}
        // Solve for changes, not absolute temperatures: this avoids subtracting
        // large nearly equal absolute thermal energies on very small timesteps.
        double kh=p.HotWallWPerK*p.Seconds,k=p.WallCoolantWPerK*p.Seconds,
            ka=p.WallAmbientWPerK*p.Seconds,f=p.CoolantFlowCapacityWPerK*p.Seconds;
        double a=p.WallCapacityJPerK+kh+k+ka,d=p.CoolantCapacityJPerK+k+f;
        double b=heatJ+kh*(p.HotC-state.WallC)+k*(state.CoolantC-state.WallC)+ka*(p.AmbientC-state.WallC);
        double c=k*(state.WallC-state.CoolantC)+f*(p.CoolantInletC-state.CoolantC);
        // Positive Schur complement expanded to avoid a*d-k*k cancellation.
        double baseWall=p.WallCapacityJPerK+kh+ka,baseWater=p.CoolantCapacityJPerK+f;
        double schur=baseWall+k*(baseWater/d);
        double dw=(b+(k/d)*c)/schur,dc=(c+k*dw)/d;
        double tw=state.WallC+dw,tc=state.CoolantC+dc;
        var next=new Result{
            Next=new State{WallC=tw,CoolantC=tc},
            HotRemovedJ=heatJ+kh*(p.HotC-tw),AmbientExportedJ=ka*(tw-p.AmbientC),
            CoolantExportedJ=f*(tc-p.CoolantInletC),WallToCoolantJ=k*(tw-tc),
            WallStoredJ=p.WallCapacityJPerK*dw,CoolantStoredJ=p.CoolantCapacityJPerK*dc
        };
        foreach(double x in new[]{a,d,b,c,schur,dw,dc,tw,tc,next.HotRemovedJ,next.AmbientExportedJ,
            next.CoolantExportedJ,next.WallToCoolantJ,next.WallStoredJ,next.CoolantStoredJ})
            if(!Finite(x)){error="冷凝器热惯性计算超出数值范围；未修改状态";return false;}
        result=next;return true;
    }
}
}
