using System;
using System.Linq;
namespace ChemLab.Chem {
public sealed partial class DistillationAssembly {
    // Net power delivered to the boiler and UA are explicit classroom settings,
    // NOT measured/calibrated performance claims about a physical Bunsen burner.
    public double HeaterPowerWatts=250,WaterFlowGramsPerSecond=10,ConductanceWPerK=10;
    // Attached burner uses a declared linear classroom control law: the set
    // power is delivered at 50% flow. This is not a calibrated gas-flow model.
    public double EffectiveHeaterPowerWatts {
        get {
            var burner=ApparatusSet.HeatingBurner(Source);
            if(burner==null)return Source.TripodRef!=null||Source.Burner!=null?0:HeaterPowerWatts;
            return ApparatusSet.BurnerHeats(Source,burner)?HeaterPowerWatts*Math.Min(1,Math.Max(0,burner.GasFlow))/.5:0;
        }
    }
    public double HeaterEnergyJ,CoolantEnergyJ,EscapedWaterGrams,EscapedEnergyJ,UnusedHeaterEnergyJ;
    public CondenserHeatTransfer.Result LastThermalResult;
    public bool HasThermalResult;
    // NIST IAPWS-95 steam table at 100 C: h_fg = 2256.4 kJ/kg.
    // https://tsapps.nist.gov/publication/get_pdf.cfm?pub_id=906704
    // Constant liquid Cp uses the project's stated 4.18 J/(g K) approximation.
    public const double WaterLatentJPerGram=2256.4;
    const double Cp=ThermalMixing.WaterCpJPerGramK;
    // Vessel.Temp is float for the existing UI/Unity consumers. Integrating
    // every small heat increment through that float causes cumulative drift.
    // Keep exact thermal temperatures while invalidating them on external edits.
    struct WaterTemperature {
        Vessel vessel;double mass,value;float displayed;
        public double Read(Vessel v){
            if(vessel!=v||v.Water!=mass||v.Temp!=displayed){vessel=v;mass=v.Water;displayed=v.Temp;value=v.Temp;}
            return value;
        }
        public void Write(Vessel v,double temperature){
            value=temperature;v.Temp=(float)temperature;vessel=v;mass=v.Water;displayed=v.Temp;
        }
    }
    WaterTemperature sourceTemperatureState,receiverTemperatureState;
    static bool PureWater(Vessel v)=>v!=null&&v.Water>=0&&
        !v.Solutes.Any(p=>p.Value>1e-12)&&!v.Undissolved.Any(p=>p.Value>1e-12)&&
        !v.Precipitates.Any(p=>p.Value>1e-12)&&!v.Gases.Any(p=>p.Value>1e-12);
    public bool UsesWaterThermal=>PureWater(Source);
    public static bool OwnsThermal(Vessel v)=>
        OwnsThermal(v.Distillation)||OwnsThermal(v.ReceivingDistillation);
    static bool OwnsThermal(DistillationAssembly rig)=>rig!=null&&
        (rig.Running||rig.UseVesselThermalStorage)&&rig.UsesEnergyThermal;
    static bool NonnegativeFinite(double v)=>v>=0&&!double.IsNaN(v)&&!double.IsInfinity(v);
    void TickWaterThermal(double dt){
        HasThermalResult=false;LastThermalResult=default;
        // Reject incompatible geometry before warming or booking heater energy.
        if(!ValidateCondenserGeometry(out string geometryError)){Notice=geometryError;Stop();return;}
        if(!PureWater(Receiver)||Source.Temp<0||Source.Temp>100||Receiver.Temp<0||Receiver.Temp>100){
            Notice="当前能量冷凝仅支持0–100℃纯水源瓶/接收瓶；混合物及超出温区需另有热物性模型";Stop();return;
        }
        if(!NonnegativeFinite(HeaterPowerWatts)||!NonnegativeFinite(WaterFlowGramsPerSecond)||!NonnegativeFinite(ConductanceWPerK)){
            Notice="加热功率、冷却水流量和总热导必须是非负有限值";Stop();return;
        }
        Notice=UseTransientCondenser?"纯水暂态冷凝：壁面/水套蓄热；玻璃定热容、外部换热及两侧热导为明确教学近似，液膜仅作动画":
            "纯水能量冷凝：功率/UA为教学设定，水取定热容；锅炉/接收器外壁绝热，未计玻璃蓄热；液膜仅作动画";
        if(Source.Water<=1e-10){Notice="源瓶已无水，加热停止";Stop();return;}
        double room=Reflux?double.PositiveInfinity:Math.Max(0,Math.Min(Receiver.Capacity,Receiver.InteriorCapacityMl)-Receiver.Water);
        if(room<=1e-10){Notice="接收容器已满，加热停止";Stop();return;}
        double power=EffectiveHeaterPowerWatts;
        double supplied=power*dt;
        if(!NonnegativeFinite(supplied)){Notice="时间步与功率乘积超出有限范围";Stop();return;}
        double sourceTemperature=sourceTemperatureState.Read(Source);
        double receiverTemperature=Reflux?sourceTemperature:receiverTemperatureState.Read(Receiver);
        double warming=Math.Min(supplied,Source.Water*Cp*(100-sourceTemperature));
        sourceTemperature=Math.Min(100,sourceTemperature+warming/(Source.Water*Cp));
        sourceTemperatureState.Write(Source,sourceTemperature);
        HeaterEnergyJ+=warming;
        double grams=Math.Min(Source.Water,Math.Min(room,(supplied-warming)/WaterLatentJPerGram));
        if(grams<=1e-12){UnusedHeaterEnergyJ+=Math.Max(0,supplied-warming);TickCondenserIdle(dt);return;}
        // Only the interval during which steam is actually generated contributes
        // condenser duty. Capacity/dry-boil limits reject unused heater energy.
        double latent=grams*WaterLatentJPerGram,activeSeconds=latent/power;
        if(!TickCondenserIdle(Math.Max(0,dt-activeSeconds)))return;
        var input=new CondenserHeatTransfer.Input{
            VaporKgPerSecond=grams/1000/activeSeconds,VaporInletC=100,SaturationC=100,
            LatentJPerKg=WaterLatentJPerGram*1000,LiquidCpJPerKgK=Cp*1000,
            // Saturated inlet never enters the superheat branch: this positive
            // placeholder is mathematically inactive, NOT steam heat-capacity data.
            VaporCpJPerKgK=1,CoolantKgPerSecond=WaterFlowGramsPerSecond/1000,
            CoolantCpJPerKgK=Cp*1000,CoolantInletC=CoolantCelsius,ConductanceWPerK=ConductanceWPerK
        };
        if(!SolveRuntimeCondenser(input,activeSeconds,out var result,out string error)){Notice=error;Stop();return;}
        LastThermalResult=result;HasThermalResult=true;
        if(UseTransientCondenser)Notice="纯水暂态冷凝：壁面/水套蓄热；玻璃定热容、外部换热及两侧热导为明确教学近似，液膜仅作动画";
        HeaterEnergyJ+=latent;UnusedHeaterEnergyJ+=Math.Max(0,supplied-warming-latent);
        double condensed=result.CondensedKgPerSecond*activeSeconds*1000;
        double escaped=Math.Max(0,grams-condensed);
        EscapedWaterGrams+=escaped;EscapedEnergyJ+=escaped*(Cp*100+WaterLatentJPerGram);
        Source.Water=Math.Max(0,Source.Water-grams);
        if(condensed>1e-12){
            var destination=Reflux?Source:Receiver;
            double mixed=(destination.Water*(Reflux?sourceTemperature:receiverTemperature)+condensed*result.ProductOutletC)/(destination.Water+condensed);
            destination.Water+=condensed;
            if(Reflux){sourceTemperature=mixed;ReturnedMl+=condensed;}
            else {receiverTemperatureState.Write(Receiver,mixed);CollectedMl+=condensed;}
        }
        sourceTemperatureState.Write(Source,sourceTemperature);
        if(escaped>1e-10)Notice+="；冷凝能力不足，未冷凝水蒸气已逸散并计入质量/能量账本";
        if(Source.Water<=1e-10||(!Reflux&&Receiver.Water>=Math.Min(Receiver.Capacity,Receiver.InteriorCapacityMl)-1e-10))Stop();
    }
}
}
