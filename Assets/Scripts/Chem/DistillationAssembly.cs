using System;
using System.Collections.Generic;
using System.Linq;
namespace ChemLab.Chem {
// Teaching simple-distillation/reflux transport, not a VLE/purity prediction.
// Layout basis: RSC Education, Reflux and distillation practical videos,
// https://edu.rsc.org/practical/reflux-and-distillation-practical-videos-16-18-students/4012294.article
// Condenser water enters the lower port and leaves the upper port.
public sealed partial class DistillationAssembly {
    public readonly Vessel Source;
    public Vessel Receiver {get;private set;}
    public bool Reflux {get;private set;}
    public bool Cooling,Running;
    public double CoolantCelsius=15;
    public double FlowMlPerSecond=1; // demonstration playback rate, NOT measured kinetics
    public double CollectedMl,ReturnedMl;
    const string Approximation="教学蒸馏近似：按已知常压沸点筛选可冷凝组分，不预测混合物气液平衡、馏分纯度或实际蒸馏速率";
    public string Notice=Approximation;
    public DistillationAssembly(Vessel source){Source=source;}
    public bool Configure(bool reflux,Vessel receiver,out string message){
        message="";
        if(Source==null||MeteredTransfer.IsTool(Source)||Source.Sealed){message="请选择未密闭的加热烧瓶";return false;}
        if(Source.ReceivingDistillation!=null){message="该容器正作为接收瓶，请先拆除原连接";return false;}
        if(!reflux&&(receiver==null||receiver==Source||MeteredTransfer.IsTool(receiver)||receiver.Sealed)){
            message="蒸馏需要另一个未密闭的接收容器";return false;
        }
        if(!reflux&&receiver.Distillation!=null){message="接收容器不能同时作为另一个装置的加热瓶";return false;}
        if(!reflux&&receiver.ReceivingDistillation!=null&&receiver.ReceivingDistillation!=this){message="该接收瓶已连接另一套蒸馏装置";return false;}
        if(Source.Distillation!=null&&Source.Distillation!=this){message="该器皿已经连接另一套装置";return false;}
        Stop();ReleaseReceiver();Reflux=reflux;Receiver=reflux?Source:receiver;Source.Distillation=this;
        if(!reflux)Receiver.ReceivingDistillation=this;
        CollectedMl=ReturnedMl=0;
        HeaterEnergyJ=CoolantEnergyJ=EscapedWaterGrams=EscapedEnergyJ=UnusedHeaterEnergyJ=0;
        HasThermalResult=false;LastThermalResult=default;
        ResetCondenserStorage();
        VesselStoredEnergyJ=VesselAmbientEnergyJ=0;
        sourceTemperatureState=default;receiverTemperatureState=default;
        ethanolSourceTemperature=default;ethanolReceiverTemperature=default;EscapedEthanolGrams=EscapedMethanolGrams=0;
        Notice=Approximation;return true;
    }
    public bool Start(out string message){
        if(!Ready(out message)){Notice=message;return false;}
        Running=true;Notice=Approximation;Source.Mode=Reflux?HeatMode.Reflux:HeatMode.Distil;return true;
    }
    bool Ready(out string message){
        message="";
        if(Source==null||Receiver==null||Source.Distillation!=this){message="请先连接装置和接收容器";return false;}
        if(Source.Sealed||Receiver.Sealed){message="蒸馏/回流装置必须保持通大气，不能密闭加热";return false;}
        if(!Cooling){message="请先打开冷凝水（下口进、上口出）";return false;}
        if(double.IsNaN(CoolantCelsius)||double.IsInfinity(CoolantCelsius)||CoolantCelsius<0||CoolantCelsius>40){message="冷凝水温度超出当前0–40℃教学模型";return false;}
        if(!(Math.Abs(Source.PressureBar-1.01325)<=.005)||!(Math.Abs(Receiver.PressureBar-1.01325)<=.005)){message="当前只有常压蒸馏数据，未套用到减压/加压条件";return false;}
        if(double.IsNaN(FlowMlPerSecond)||double.IsInfinity(FlowMlPerSecond)||FlowMlPerSecond<=0){message="蒸馏演示流量必须为正数";return false;}
        return true;
    }
    public bool RetainsVapor=>Running&&Ready(out _);
    public void Stop(){Running=false;if(Source!=null&&(Source.Mode==HeatMode.Reflux||Source.Mode==HeatMode.Distil))Source.Mode=HeatMode.None;}
    void ReleaseReceiver(){if(Receiver?.ReceivingDistillation==this)Receiver.ReceivingDistillation=null;}
    public void Detach(){Stop();ReleaseReceiver();if(Source?.Distillation==this)Source.Distillation=null;Receiver=null;}
    static void Add(Dictionary<string,double> map,string id,double n){map.TryGetValue(id,out double old);map[id]=old+n;}
    static double Available(Vessel v,string id){
        if(id=="h2o")return v.Water/ChemData.Sub[id].Mass;
        v.Solutes.TryGetValue(id,out double a);v.Gases.TryGetValue(id,out double b);return a+b;
    }
    static void Withdraw(Vessel v,string id,double n){
        if(id=="h2o"){v.Water=Math.Max(0,v.Water-n*ChemData.Sub[id].Mass);return;}
        double requested=n;
        foreach(var map in new[]{v.Gases,v.Solutes}){
            map.TryGetValue(id,out double has);double take=Math.Min(has,n);n-=take;
            if(has-take<=1e-12)map.Remove(id);else map[id]=has-take;
            if(n<=1e-12)break;
        }
        // Molecular stock bookkeeping must not retain the transferred material.
        if(v.Ledger.TryGetValue(id,out double stock)){
            double remaining=Math.Max(0,stock-(requested-n));
            if(remaining<=1e-12)v.Ledger.Remove(id);else v.Ledger[id]=remaining;
        }
    }
    public void Tick(double dt){
        if(PourTransaction.IsActive||dt<=0||double.IsNaN(dt)||double.IsInfinity(dt))return;
        if(!Running){if(Source?.Distillation==this&&Receiver!=null)TickVesselStorage(dt);TickCondenserIdle(dt);return;}
        if(!Ready(out string reason)){Notice=reason;Stop();return;}
        if(!ValidateCondenserGeometry(out string geometryError)){Notice=geometryError;Stop();return;}
        if(UsesEnergyThermal&&(!ValidateEnergyContents(out reason)||!NonnegativeFinite(HeaterPowerWatts)||
            !NonnegativeFinite(WaterFlowGramsPerSecond)||!NonnegativeFinite(ConductanceWPerK)||!NonnegativeFinite(EffectiveHeaterPowerWatts*dt))){
            Notice=string.IsNullOrEmpty(reason)?"传热参数或供热量无效":reason;Stop();return;
        }
        // Preserve species identity even when wall heat boils the source dry.
        var liquidModel=LiquidModel;
        if(!TickVesselStorage(dt))return;
        if(!Running){TickCondenserIdle(dt);return;}
        if(liquidModel!=null){try{TickLiquidThermal(dt,liquidModel);}finally{EscapePendingWallVapor();}return;}
        if(UsesWaterThermal){TickWaterThermal(dt);return;}
        Notice=Approximation+"；当前混合物/有机物尚未接入能量冷凝模型";
        double budget=FlowMlPerSecond*dt;
        bool delivered=false;
        var candidates=new List<KeyValuePair<string,double>>();
        foreach(string id in Source.Solutes.Keys.Concat(Source.Gases.Keys).Concat(new[]{"h2o"}).Distinct()){
            if(!ChemData.Sub.TryGetValue(id,out var s)||s.IsIon||Available(Source,id)<=1e-12)continue;
            double boil=id=="h2o"?100:PhaseProperties.Data.TryGetValue(id,out var data)?data.Boil:double.NaN;
            if(double.IsNaN(boil)){Notice="部分物质缺少常压沸点；未凭空蒸馏该物质";continue;}
            if(Source.Temp<boil)continue;
            if(CoolantCelsius>=boil){Notice="当前冷凝水温度不能冷凝部分低沸点物质；未把它们伪装成馏出液";continue;}
            candidates.Add(new KeyValuePair<string,double>(id,boil));
        }
        foreach(var pair in candidates.OrderBy(p=>p.Value)){
            string id=pair.Key;var s=ChemData.Sub[id];
            double density=PhaseProperties.Density(id,CoolantCelsius);
            if(density<=0||double.IsNaN(density)||double.IsInfinity(density))continue;
            double room=Reflux?double.PositiveInfinity:Math.Max(0,Math.Min(Receiver.Capacity,Receiver.InteriorCapacityMl)-Receiver.Volume()-Receiver.SolidVolume());
            if(room<=1e-10){Notice="接收容器已满，蒸馏已停止；未丢弃或覆盖物料";Stop();break;}
            // Low-boiling condensate needs a cold receiver, not a fictitious liquid
            // in a warm container. An empty receiver adopts condenser temperature.
            if(!Reflux&&Receiver.Mass()>1e-10&&Receiver.Temp>=pair.Value){Notice="接收容器温度过高，请先冷却或更换空容器";Stop();break;}
            double n=Math.Min(Available(Source,id),Math.Min(budget,room)*density/s.Mass);
            if(n<=1e-12)continue;
            double ml=n*s.Mass/density;
            if(Reflux){ReturnedMl+=ml;} // closed circulation, no net inventory loss
            else {
                bool empty=Receiver.Mass()<=1e-10;
                Withdraw(Source,id,n);
                if(empty)Receiver.Temp=(float)CoolantCelsius;
                if(id=="h2o")Receiver.Water+=n*s.Mass;else Add(Receiver.Solutes,id,n);
                CollectedMl+=ml;delivered=true;
            }
            budget-=ml;if(budget<=1e-10)break;
        }
        if(delivered)Lab.Settle(Receiver);
    }
}
}
