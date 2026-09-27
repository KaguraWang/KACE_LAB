using System;
using System.Collections.Generic;
using System.Linq;

namespace ChemLab.Chem {
// Inventory transaction for the staged pour UI. Preview never calls chemistry;
// each preview starts from the same snapshot, so undo cannot steal B's material.
public sealed class PourTransaction : IDisposable {
    public static bool IsActive { get; private set; }
    readonly Vessel source,target;
    readonly Snapshot beforeSource,beforeTarget;
    string pendingThermalNotice="";
    bool ended;
    public double LiquidMassGrams { get; }
    public double SolidMassGrams { get; }
    public double LiquidVolumeMl { get; }
    public double SolidVolumeMl { get; }
    public double InTransitMassGrams { get; private set; }
    sealed class Snapshot {
        public readonly double Water;
        public readonly float Temp;
        public readonly Dictionary<string,double>[] Maps;
        public Snapshot(Vessel v){Water=v.Water;Temp=v.Temp;Maps=Inventory(v).Select(m=>new Dictionary<string,double>(m)).ToArray();}
        public void Restore(Vessel v){v.Water=Water;v.Temp=Temp;var maps=Inventory(v);for(int i=0;i<maps.Length;i++){maps[i].Clear();foreach(var p in Maps[i])maps[i][p.Key]=p.Value;}}
    }
    static Dictionary<string,double>[] Inventory(Vessel v)=>new[]{v.Solutes,v.Undissolved,v.Precipitates,v.Ledger};
    bool Solid(string id)=>PhaseProperties.Phase(id,source.Temp)=="s";
    public PourTransaction(Vessel from,Vessel to){
        if(IsActive)throw new InvalidOperationException("已有倾倒操作进行中");
        if(from==null||to==null||from==to||from.Sealed||to.Sealed)throw new ArgumentException("请选择两个不同且已打开的容器");
        if(from.Kind==VesselKind.Burette||from.Kind==VesselKind.Pipette||to.Kind==VesselKind.Pipette)throw new ArgumentException("滴定管和移液管请使用专用滴液、抽取或释放操作；移液管不可通过倾倒加液");
        source=from;target=to;beforeSource=new Snapshot(from);beforeTarget=new Snapshot(to);
        double liquid=from.Water,solid=0;
        var maps=Inventory(from);
        for(int i=0;i<3;i++)foreach(var p in maps[i]){double mass=p.Value*ChemData.Sub[p.Key].Mass;if(i>0&&Solid(p.Key))solid+=mass;else liquid+=mass;}
        LiquidMassGrams=liquid;SolidMassGrams=solid;LiquidVolumeMl=from.Volume();SolidVolumeMl=from.SolidVolume();IsActive=true;
    }
    static void Add(Dictionary<string,double> map,string id,double n){map.TryGetValue(id,out double old);map[id]=old+n;}
    public bool Preview(double liquidFraction,double solidFraction,out string message){
        return PreviewFlight(liquidFraction,solidFraction,liquidFraction,solidFraction,out message);
    }
    public bool PreviewFlight(double liquidFraction,double solidFraction,double liquidArrived,double solidArrived,out string message){
        message="";
        if(ended){message="倾倒操作已结束";return false;}
        if(double.IsNaN(liquidFraction)||double.IsInfinity(liquidFraction)||double.IsNaN(solidFraction)||double.IsInfinity(solidFraction)||liquidFraction<0||liquidFraction>1||solidFraction<0||solidFraction>1){message="倾倒比例必须在0至100%之间";return false;}
        if(double.IsNaN(liquidArrived)||double.IsNaN(solidArrived)||liquidArrived<0||solidArrived<0||liquidArrived>liquidFraction||solidArrived>solidFraction){message="飞行中物质量无效";return false;}
        // Validate using original volumes; no mutation occurs for a rejected preview.
        var currentSource=new Snapshot(source);var currentTarget=new Snapshot(target);
        beforeSource.Restore(source);beforeTarget.Restore(target);
        double movedVolume=source.Volume()*liquidFraction+source.SolidVolume()*solidFraction;
        double capacityFactor=target.Kind==VesselKind.Burette?1:.95;
        bool iceFinal=IceWaterTransfer.TryPredict(source,target,liquidFraction,solidFraction,out var finalIce);
        bool iceArrived=IceWaterTransfer.TryPredict(source,target,liquidArrived,solidArrived,out var arrivedIce);
        double finalVolume=iceFinal?finalIce.VolumeMl:target.Volume()+target.SolidVolume()+movedVolume;
        double arrivedVolume=iceArrived?arrivedIce.VolumeMl:target.Volume()+target.SolidVolume()+source.Volume()*liquidArrived+source.SolidVolume()*solidArrived;
        if(Math.Max(finalVolume,arrivedVolume)>capacityFactor*Math.Min(target.Capacity,target.InteriorCapacityMl)+1e-9){
            currentSource.Restore(source);currentTarget.Restore(target);message="超过目标容器安全容量，请减量";return false;
        }
        ThermalMixing.TryTargetTemperature(source,target,liquidArrived,out float mixedTemp,out string thermalNotice);
        double water=source.Water;source.Water-=water*liquidFraction;target.Water+=water*liquidArrived;
        var sourceMaps=Inventory(source);var targetMaps=Inventory(target);
        for(int i=0;i<3;i++)foreach(var id in sourceMaps[i].Keys.ToArray()){
            bool isSolid=i>0&&Solid(id);double original=sourceMaps[i][id];
            double amount=original*(isSolid?solidFraction:liquidFraction),arrived=original*(isSolid?solidArrived:liquidArrived);
            if(amount<=0)continue;sourceMaps[i][id]-=amount;if(sourceMaps[i][id]<=1e-12)sourceMaps[i].Remove(id);if(arrived>0)Add(targetMaps[i],id,arrived);
        }
        foreach(var id in source.Ledger.Keys.ToArray()){
            double original=source.Ledger[id],amount=original*liquidFraction;if(amount<=0)continue;
            source.Ledger[id]-=amount;if(source.Ledger[id]<=1e-12)source.Ledger.Remove(id);if(liquidArrived>0)Add(target.Ledger,id,original*liquidArrived);
        }
        target.Temp=mixedTemp;
        if(iceArrived){arrivedIce.Apply(target);thermalNotice=IceWaterTransfer.Notice;}
        pendingThermalNotice=thermalNotice;
        if(!string.IsNullOrEmpty(thermalNotice))message=thermalNotice;
        InTransitMassGrams=(liquidFraction-liquidArrived)*LiquidMassGrams+(solidFraction-solidArrived)*SolidMassGrams;
        return true;
    }
    public void Undo(){if(ended)return;beforeSource.Restore(source);beforeTarget.Restore(target);pendingThermalNotice="";InTransitMassGrams=0;}
    public void Commit(){if(ended)return;if(InTransitMassGrams>1e-10)throw new InvalidOperationException("请等待内容物全部到达后再完成");ended=true;IsActive=false;ThermalMixing.RecordNotice(target,pendingThermalNotice);Lab.Settle(source);Lab.Settle(target);}
    public void Dispose(){if(ended)return;Undo();ended=true;IsActive=false;}
}
}
