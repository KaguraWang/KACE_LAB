using System;
namespace ChemLab.Chem {
public sealed partial class DistillationAssembly {
    // Legacy steady-state mode remains available for analytic regression cases.
    public bool UseTransientCondenser=true;
    public double CondenserLengthMeters=.3;
    public double CondenserWallC=>storageInitialized?storageState.WallC:CoolantCelsius;
    public double CondenserWaterC=>storageInitialized?storageState.CoolantC:CoolantCelsius;
    public double CondenserStoredEnergyJ,CondenserAmbientEnergyJ;
    public double SimulatedCondenserSeconds {get;private set;}
    CondenserThermalStorage.State storageState;
    bool storageInitialized;
    double storageLength;
    // Explicit classroom surrogate: BOROFLOAT 33's mean 20–100 C Cp,
    // held constant over the UI's 0–100 C range; NOT measured DURAN data.
    const double GlassDensity=2.23,GlassCp=.83;
    bool ValidateCondenserGeometry(out string error){
        error="";
        if(!UseTransientCondenser)return true;
        if(!CondenserDimensions.TryMeasure(CondenserLengthMeters,out _)){error="冷凝器尺寸无效";return false;}
        if(storageInitialized&&Math.Abs(storageLength-CondenserLengthMeters)>1e-6){error="热态冷凝器尺寸改变，请重新连接装置再运行";return false;}
        return true;
    }
    bool TickCondenserIdle(double seconds){
        if(!UseTransientCondenser||seconds<=0||(!Running&&!storageInitialized))return true;
        var input=new CondenserHeatTransfer.Input{VaporKgPerSecond=0,VaporInletC=100,SaturationC=100,
            LatentJPerKg=WaterLatentJPerGram*1000,VaporCpJPerKgK=1,LiquidCpJPerKgK=Cp*1000,
            CoolantCpJPerKgK=Cp*1000,CoolantInletC=CoolantCelsius};
        if(!SolveRuntimeCondenser(input,seconds,out _,out string error)){Notice=error;Stop();return false;}
        return true;
    }
    void ResetCondenserStorage(){storageInitialized=false;storageState=default;CondenserStoredEnergyJ=CondenserAmbientEnergyJ=SimulatedCondenserSeconds=0;}
    bool SolveRuntimeCondenser(CondenserHeatTransfer.Input input,double seconds,out CondenserHeatTransfer.Result result,out string error){
        result=default;error="";
        if(!UseTransientCondenser){
            if(!CondenserHeatTransfer.TrySolve(input,out result,out error))return false;
            CoolantEnergyJ+=result.HeatWatts*seconds;SimulatedCondenserSeconds+=seconds;return true;
        }
        if(!ValidateCondenserGeometry(out error))return false;
        CondenserDimensions.TryMeasure(CondenserLengthMeters,out var dimensions);
        var previous=storageInitialized?storageState:new CondenserThermalStorage.State{WallC=CoolantCelsius,CoolantC=CoolantCelsius};
        // Two equal contact resistances reproduce the declared total UA in
        // the steady limit; environmental h is an explicit teaching estimate.
        var jacket=new CondenserThermalStorage.Input{
            WallCapacityJPerK=dimensions.GlassCapacity(GlassDensity,GlassCp),CoolantCapacityJPerK=dimensions.CoolantMl*Cp,
            WallCoolantWPerK=ConductanceWPerK*2,WallAmbientWPerK=dimensions.OuterAreaM2*5,
            CoolantFlowCapacityWPerK=Cooling?WaterFlowGramsPerSecond*Cp:0,
            HotC=input.VaporInletC,CoolantInletC=CoolantCelsius,AmbientC=25,Seconds=seconds
        };
        input.ConductanceWPerK=ConductanceWPerK*2;
        if(!TransientCondenser.TryStep(input,jacket,previous,out var coupled,out error))return false;
        storageState=coupled.Storage.Next;storageInitialized=true;storageLength=CondenserLengthMeters;
        CondenserStoredEnergyJ+=coupled.Storage.WallStoredJ+coupled.Storage.CoolantStoredJ;
        CondenserAmbientEnergyJ+=coupled.Storage.AmbientExportedJ;
        CoolantEnergyJ+=coupled.Storage.CoolantExportedJ;
        SimulatedCondenserSeconds+=seconds;
        result=coupled.Stream;result.CoolantOutletC=storageState.CoolantC;
        return true;
    }
}
}
