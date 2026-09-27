using System;
namespace ChemLab.Chem {
// Couples finite vapor throughput (including latent/subcooling enthalpy) to
// finite wall/coolant storage. Both subsystems share ONE transferred heat.
public static class TransientCondenser {
    public struct Result {
        public CondenserHeatTransfer.Result Stream;
        public CondenserThermalStorage.Result Storage;
    }
    public static bool TryStep(CondenserHeatTransfer.Input stream,CondenserThermalStorage.Input jacket,
        CondenserThermalStorage.State state,out Result result,out string error){
        result=default;error="";
        if(jacket.HotWallWPerK!=0){error="联合冷凝使用蒸气侧热导，不能再叠加第二份壁面热源";return false;}
        if(!CondenserThermalStorage.TryStepHeat(jacket,state,0,out var idle,out error))return false;
        stream.CoolantInletC=idle.Next.WallC;
        if(!CondenserHeatTransfer.TrySolveFixedWall(stream,out var coldest,out error))return false;
        double upper=coldest.HeatWatts*jacket.Seconds,lower=0;
        if(double.IsNaN(upper)||double.IsInfinity(upper)){error="联合冷凝热量超出有限范围";return false;}
        if(upper==0){result=new Result{Stream=coldest,Storage=idle};return true;}
        // Wall temperature rises monotonically with supplied heat; stream heat
        // removal decreases with wall temperature. This brackets their unique
        // passive equilibrium without applying either heat twice.
        for(int n=0;n<64;n++){
            double trial=lower+(upper-lower)*.5;
            if(!CondenserThermalStorage.TryStepHeat(jacket,state,trial,out var storage,out error))return false;
            stream.CoolantInletC=storage.Next.WallC;
            if(!CondenserHeatTransfer.TrySolveFixedWall(stream,out var flux,out error))return false;
            if(flux.HeatWatts*jacket.Seconds>trial)lower=trial;else upper=trial;
        }
        double heat=lower+(upper-lower)*.5;
        if(!CondenserThermalStorage.TryStepHeat(jacket,state,heat,out var finalStorage,out error))return false;
        stream.CoolantInletC=finalStorage.Next.WallC;
        if(!CondenserHeatTransfer.TrySolveFixedWall(stream,out var finalStream,out error))return false;
        if(Math.Abs(finalStream.HeatWatts*jacket.Seconds-heat)>1e-8*Math.Max(1,heat)){
            error="联合冷凝热流未收敛，未提交状态";return false;
        }
        result=new Result{Stream=finalStream,Storage=finalStorage};return true;
    }
}
}
