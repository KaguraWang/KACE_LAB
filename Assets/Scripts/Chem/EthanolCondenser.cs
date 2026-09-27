using System;
using System.Linq;
namespace ChemLab.Chem {
public sealed partial class DistillationAssembly {
    public double EscapedEthanolGrams;
    public double EscapedMethanolGrams;
    public double EscapedGrams=>EscapedWaterGrams+EscapedEthanolGrams+EscapedMethanolGrams;
    static double LiquidMass(Vessel v,string id)=>v.Solutes.TryGetValue(id,out double n)?n*ChemData.Sub[id].Mass:0;
    static bool PureLiquid(Vessel v,string id)=>v!=null&&v.Water<=1e-12&&
        !v.Solutes.Any(p=>p.Key!=id&&p.Value>1e-12)&&
        !v.Undissolved.Any(p=>p.Value>1e-12)&&!v.Precipitates.Any(p=>p.Value>1e-12)&&!v.Gases.Any(p=>p.Value>1e-12);
    PureLiquidThermalModel LiquidModel {
        get {
            foreach(var pair in Source.Solutes)if(pair.Value>1e-12){
                var model=PureLiquidThermalModel.For(pair.Key);
                return model!=null&&PureLiquid(Source,pair.Key)&&LiquidMass(Source,pair.Key)>1e-10?model:null;
            }
            return null;
        }
    }
    public bool UsesEthanolThermal=>LiquidModel==PureLiquidThermalModel.Ethanol;
    public bool UsesEnergyThermal=>UsesWaterThermal||LiquidModel!=null;
    struct EthanolTemperature {
        Vessel vessel;string species;double mass,value;float displayed;
        public double Read(Vessel v,string id){
            double current=LiquidMass(v,id);
            if(vessel!=v||species!=id||current!=mass||v.Temp!=displayed){vessel=v;species=id;mass=current;displayed=v.Temp;value=v.Temp;}
            return value;
        }
        public void Write(Vessel v,string id,double t){
            value=t;v.Temp=(float)t;
            // A rounded-up float must not trigger the separate phase engine
            // before this energy owner has paid the vaporization latent heat.
            if(v.Temp>=PureLiquidThermalModel.For(id).BoilingC)v.Temp=(float)(t-1e-5);
            vessel=v;species=id;mass=LiquidMass(v,id);displayed=v.Temp;
        }
    }
    EthanolTemperature ethanolSourceTemperature,ethanolReceiverTemperature;
    void TickLiquidThermal(double dt,PureLiquidThermalModel model){
        string id=model.Id;
        HasThermalResult=false;LastThermalResult=default;
        if(!ValidateCondenserGeometry(out string geometryError)){Notice=geometryError;Stop();return;}
        if(!PureLiquid(Receiver,id)||!model.Valid(Source.Temp)||
           (LiquidMass(Receiver,id)>1e-10&&!model.Valid(Receiver.Temp))){
            Notice=$"纯{model.Name}能量冷凝需要{model.MinC}至{model.BoilingC}℃纯液体与同种纯液体/空接收瓶；混合物须另用物性模型";Stop();return;
        }
        if(!NonnegativeFinite(HeaterPowerWatts)||!NonnegativeFinite(WaterFlowGramsPerSecond)||!NonnegativeFinite(ConductanceWPerK)){
            Notice="传热参数必须是非负有限值";Stop();return;
        }
        double mass=LiquidMass(Source,id),receiverMass=LiquidMass(Receiver,id);
        // Use the same declared density as vessel volume/capacity bookkeeping.
        // Thermal expansion is not silently invented here.
        double density=PhaseProperties.Density(id,CoolantCelsius);
        if(!(density>0)||!NonnegativeFinite(density)){Notice="缺少有效液体密度";Stop();return;}
        double room=Reflux?double.PositiveInfinity:Math.Max(0,Math.Min(Receiver.Capacity,Receiver.InteriorCapacityMl)-Receiver.Volume()-Receiver.SolidVolume())*density;
        if(room<=1e-10){Notice="接收容器已满，加热停止";Stop();return;}
        double power=EffectiveHeaterPowerWatts,supplied=power*dt;
        if(!NonnegativeFinite(supplied)){Notice="时间步与功率乘积超出有限范围";Stop();return;}
        double ts=ethanolSourceTemperature.Read(Source,id),hs=model.H(ts);
        double hr=receiverMass>1e-10?model.H(Reflux?ts:ethanolReceiverTemperature.Read(Receiver,id)):0;
        double saturated=model.H(model.BoilingC);
        double warming=Math.Min(supplied,Math.Max(0,mass*(saturated-hs)));
        hs=mass>1e-10?Math.Min(saturated,hs+warming/mass):saturated;
        double wallGrams=pendingWallVaporGrams;
        double heaterGrams=Math.Min(mass,Math.Min(Math.Max(0,room-wallGrams),Math.Max(0,supplied-warming)/model.LatentJPerGram));
        double grams=wallGrams+heaterGrams;
        Notice=UseTransientCondenser?$"纯{model.Name}暂态冷凝：{model.Assumptions}；壁面/水套蓄热、玻璃定热容及换热系数教学近似":
            $"纯{model.Name}能量冷凝：{model.Assumptions}，常压饱和蒸气；功率/UA教学设定，外壁绝热，未计玻璃蓄热";
        if(grams<=1e-12){
            ethanolSourceTemperature.Write(Source,id,model.T(hs));HeaterEnergyJ+=warming;
            UnusedHeaterEnergyJ+=Math.Max(0,supplied-warming);TickCondenserIdle(dt);return;
        }
        double latent=heaterGrams*model.LatentJPerGram;
        // Wall and heater vapor share one condenser time interval. The wall
        // exchange was integrated over dt, including with the heater off.
        double activeSeconds=wallGrams>0?dt:latent/power;
        if(!TickCondenserIdle(Math.Max(0,dt-activeSeconds)))return;
        var input=new CondenserHeatTransfer.Input{
            VaporKgPerSecond=grams/1000/activeSeconds,VaporInletC=model.BoilingC,SaturationC=model.BoilingC,
            LatentJPerKg=1000*model.LatentJPerGram,LiquidCurve=model.Curve,
            LiquidCpJPerKgK=1,VaporCpJPerKgK=1, // inactive placeholders, not measured properties
            CoolantKgPerSecond=WaterFlowGramsPerSecond/1000,CoolantCpJPerKgK=Cp*1000,
            CoolantInletC=CoolantCelsius,ConductanceWPerK=ConductanceWPerK
        };
        if(!SolveRuntimeCondenser(input,activeSeconds,out var r,out string error)){Notice=error;Stop();return;}
        pendingWallVaporGrams=0;pendingWallVaporId=null;
        double condensed=Math.Min(grams,r.CondensedKgPerSecond*activeSeconds*1000),escaped=Math.Max(0,grams-condensed);
        LastThermalResult=r;HasThermalResult=true;
        if(UseTransientCondenser)Notice=$"纯{model.Name}暂态冷凝：{model.Assumptions}；壁面/水套蓄热、玻璃定热容及换热系数教学近似";
        HeaterEnergyJ+=warming+latent;UnusedHeaterEnergyJ+=Math.Max(0,supplied-warming-latent);
        if(id=="c2h5oh")EscapedEthanolGrams+=escaped;else if(id=="ch3oh")EscapedMethanolGrams+=escaped;
        EscapedEnergyJ+=escaped*(saturated+model.LatentJPerGram);
        // Wall-generated vapor was withdrawn during the wall-energy commit.
        Withdraw(Source,id,heaterGrams/ChemData.Sub[id].Mass);
        if(condensed>1e-12){
            var destination=Reflux?Source:Receiver;
            double existing=LiquidMass(destination,id);
            // Transfer actual energy, not the interpolated display temperature.
            double productH=saturated-r.SubcoolWatts*activeSeconds/condensed;
            double mixedH=(existing*(Reflux?hs:hr)+condensed*productH)/(existing+condensed);
            Add(destination.Solutes,id,condensed/ChemData.Sub[id].Mass);
            if(Reflux){hs=mixedH;ReturnedMl+=condensed/density;}
            else {ethanolReceiverTemperature.Write(Receiver,id,model.T(mixedH));CollectedMl+=condensed/density;}
        }
        ethanolSourceTemperature.Write(Source,id,model.T(hs));
        if(escaped>1e-10)Notice+="；未冷凝蒸气已逸散并计入质量/能量账本";
        if(LiquidMass(Source,id)<=1e-10||(!Reflux&&room-condensed<=1e-10))Stop();
    }
}
}
