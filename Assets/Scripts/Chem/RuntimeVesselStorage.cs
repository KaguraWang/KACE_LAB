using System;
namespace ChemLab.Chem {
public sealed partial class DistillationAssembly {
    // Legacy analytic controls may explicitly opt out of vessel inertia.
    public bool UseVesselThermalStorage=true;
    public double VesselContactWPerK=10; // explicit lumped teaching coefficient
    public double VesselStoredEnergyJ,VesselAmbientEnergyJ;
    double pendingWallVaporGrams;
    string pendingWallVaporId;
    static readonly LiquidEnthalpyCurve WaterSensibleCurve=new LiquidEnthalpyCurve(new[]{0d,100d},new[]{0d,418000d});
    static bool GlassVessel(Vessel v)=>v!=null&&v.Kind!=VesselKind.Crucible&&v.Kind!=VesselKind.Dish;
    struct VesselExchangePlan {
        public Vessel Vessel;public double LiquidC,WallC,WallGain,AmbientExport,VaporGrams;public bool HasLiquid;public string Species;
    }
    bool PlanVesselExchange(Vessel v,bool source,double seconds,out VesselExchangePlan plan,out string error){
        plan=default;error="";
        if(!GlassVessel(v)){error="器皿蓄热暂仅支持玻璃主体，不将玻璃物性套用于陶瓷";return false;}
        double mass,t;LiquidEnthalpyCurve curve;PureLiquidThermalModel model=null;
        if(PureWater(v)){
            mass=v.Water;t=source?sourceTemperatureState.Read(v):receiverTemperatureState.Read(v);curve=WaterSensibleCurve;
        }else{
            foreach(var pair in v.Solutes)if(pair.Value>1e-12){model=PureLiquidThermalModel.For(pair.Key);break;}
            if(model==null||!PureLiquid(v,model.Id)){error="器皿蓄热缺少当前混合物液相焓数据";return false;}
            mass=LiquidMass(v,model.Id);t=source?ethanolSourceTemperature.Read(v,model.Id):ethanolReceiverTemperature.Read(v,model.Id);curve=model.Curve;
        }
        // Existing glass surrogate is only declared over 0–100 C.
        if(!NonnegativeFinite(t)||t>100||!NonnegativeFinite(v.GlassTemperatureC)||v.GlassTemperatureC>100){error="玻璃蓄热当前仅支持0–100℃";return false;}
        double capacity=Game.VesselMesh.ShellVolumeMl(v.Kind)*GlassDensity*GlassCp;
        // Same declared 5 W/(m² K) classroom external coefficient as condenser.
        // Operator splitting is first order; refinement is checked separately.
        double a=Game.VesselMesh.OuterSurfaceAreaM2(v.Kind)*5*seconds/capacity;
        double fraction=double.IsInfinity(a)?1:a/(1+a);
        double wallDelta=(25-v.GlassTemperatureC)*fraction;
        double wall=v.GlassTemperatureC+wallDelta;
        plan=new VesselExchangePlan{Vessel=v,LiquidC=t,WallC=wall,HasLiquid=mass>1e-10,Species=model==null?"h2o":model.Id,
            WallGain=capacity*wallDelta,AmbientExport=-capacity*wallDelta};
        if(!plan.HasLiquid)return true;
        double latent=model==null?WaterLatentJPerGram:model.LatentJPerGram;
        if(!VesselPhaseExchange.TryStep(curve,mass/1000,t,capacity,wall,VesselContactWPerK,seconds,latent*1000,out var r,out error,model))return false;
        plan.LiquidC=r.LiquidC;plan.WallC=r.WallC;plan.WallGain-=r.HeatFromWallJ;plan.VaporGrams=r.VaporKg*1000;return true;
    }
    void CommitVesselExchange(VesselExchangePlan plan,bool source){
        var v=plan.Vessel;v.GlassTemperatureC=plan.WallC;VesselStoredEnergyJ+=plan.WallGain;VesselAmbientEnergyJ+=plan.AmbientExport;
        if(!plan.HasLiquid)return;
        if(plan.VaporGrams>0){
            Withdraw(v,plan.Species,plan.VaporGrams/ChemData.Sub[plan.Species].Mass);
            if(source&&Running){pendingWallVaporGrams=plan.VaporGrams;pendingWallVaporId=plan.Species;}
            else RecordWallVaporEscape(plan.Species,plan.VaporGrams);
        }
        if(plan.Species=="h2o"){if(source)sourceTemperatureState.Write(v,plan.LiquidC);else receiverTemperatureState.Write(v,plan.LiquidC);}
        else{
            string id=plan.Species;
            if(source)ethanolSourceTemperature.Write(v,id,plan.LiquidC);else ethanolReceiverTemperature.Write(v,id,plan.LiquidC);
        }
    }
    bool TickVesselStorage(double seconds){
        if(!UseVesselThermalStorage||!UsesEnergyThermal)return true;
        // Matching contents are required for active transfer, not for passive
        // cooling after a source has boiled dry or the contents were replaced.
        if(Running&&!ValidateEnergyContents(out string contentsError)){Notice=contentsError;Stop();return false;}
        if(!NonnegativeFinite(VesselContactWPerK)){Notice="器皿接触热导必须为非负有限值";Stop();return false;}
        if(!PlanVesselExchange(Source,true,seconds,out var source,out string error)){Notice=error;Stop();return false;}
        VesselExchangePlan receiver=default;
        if(!Reflux&&!PlanVesselExchange(Receiver,false,seconds,out receiver,out error)){Notice=error;Stop();return false;}
        if(Running&&!Reflux&&source.VaporGrams>0){
            double density=PhaseProperties.Density(source.Species,CoolantCelsius);
            if(!(density>0)||!NonnegativeFinite(density)){Notice="缺少有效液体密度";Stop();return false;}
            double room=Math.Max(0,Math.Min(Receiver.Capacity,Receiver.InteriorCapacityMl)-Receiver.Volume()-Receiver.SolidVolume())*density+receiver.VaporGrams;
            if(source.VaporGrams>room+1e-10){
                // Stop transfer before committing. Residual boiling then vents
                // from the open vessels; no condensate can overfill a receiver.
                Stop();Notice="接收瓶余量不足，已停止输送；器皿余热产生的蒸气计入逸散";
            }
        }
        CommitVesselExchange(source,true);if(!Reflux)CommitVesselExchange(receiver,false);return true;
    }
    void RecordWallVaporEscape(string id,double grams){
        if(id=="h2o"){EscapedWaterGrams+=grams;EscapedEnergyJ+=grams*(Cp*100+WaterLatentJPerGram);return;}
        var model=PureLiquidThermalModel.For(id);
        if(id=="ch3oh")EscapedMethanolGrams+=grams;else if(id=="c2h5oh")EscapedEthanolGrams+=grams;
        EscapedEnergyJ+=grams*(model.H(model.BoilingC)+model.LatentJPerGram);
    }
    void EscapePendingWallVapor(){
        if(pendingWallVaporGrams>0)RecordWallVaporEscape(pendingWallVaporId,pendingWallVaporGrams);
        pendingWallVaporGrams=0;pendingWallVaporId=null;
    }
    bool ValidateEnergyContents(out string error){
        error="";
        if(UsesWaterThermal){
            if(!PureWater(Receiver)||!NonnegativeFinite(Source.Temp)||Source.Temp>100||!NonnegativeFinite(Receiver.Temp)||Receiver.Temp>100){
                error="当前能量冷凝需要0–100℃纯水源瓶/接收瓶";return false;
            }
        }else{
            var model=LiquidModel;
            if(model!=null&&(!PureLiquid(Receiver,model.Id)||!model.Valid(Source.Temp)||
                (LiquidMass(Receiver,model.Id)>1e-10&&!model.Valid(Receiver.Temp)))){
                error="源瓶与接收瓶需为同种已登记纯液体，或使用空接收瓶";return false;
            }
        }
        return true;
    }
}
}
