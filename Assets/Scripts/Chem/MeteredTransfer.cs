using System;
using System.Collections.Generic;
using System.Linq;
namespace ChemLab.Chem {
public static class MeteredTransfer {
    public static bool IsTool(Vessel v)=>v!=null&&(v.Kind==VesselKind.Burette||v.Kind==VesselKind.Pipette);
    public static bool CanAttach(Vessel tool,Vessel target)=>IsTool(tool)&&target!=null&&target!=tool&&!IsTool(target)&&target.AttachedTo==null&&!tool.Sealed&&!target.Sealed;
    // Positive flow dispenses; negative flow aspirates (pipette only). Units mL/s.
    public static double Move(Vessel tool,double requestedMl){
        var other=tool?.AttachedTo;
        if(!CanAttach(tool,other)||PourTransaction.IsActive||double.IsNaN(requestedMl)||double.IsInfinity(requestedMl))return 0;
        if(requestedMl<0&&tool.Kind!=VesselKind.Pipette)return 0;
        var from=requestedMl<0?other:tool;var to=requestedMl<0?tool:other;
        double volume=from.Volume();if(volume<=1e-10)return 0;
        double room=Math.Max(0,Math.Min(to.Capacity,to.InteriorCapacityMl)-to.Volume()-to.SolidVolume());
        double moved=Math.Min(Math.Abs(requestedMl),Math.Min(volume,room));if(moved<=1e-10)return 0;
        ThermalMixing.TryTargetTemperature(from,to,moved/volume,out float mixedTemp,out string thermalNotice);
        bool iceTransfer=IceWaterTransfer.TryPredict(from,to,moved/volume,0,out var iceState);
        if(iceTransfer&&iceState.VolumeMl>Math.Min(to.Capacity,to.InteriorCapacityMl)+1e-9)return 0;
        double fraction=moved/volume,water=from.Water*fraction;from.Water-=water;to.Water+=water;
        void MoveMap(Dictionary<string,double> source,Dictionary<string,double> target,bool dissolved){
            foreach(var id in source.Keys.ToArray()){
                if(!dissolved&&PhaseProperties.Phase(id,from.Temp)!="l")continue;
                double n=source[id]*fraction;source[id]-=n;if(source[id]<=1e-14)source.Remove(id);
                target.TryGetValue(id,out double old);target[id]=old+n;
            }
        }
        MoveMap(from.Solutes,to.Solutes,true);MoveMap(from.Undissolved,to.Undissolved,false);MoveMap(from.Precipitates,to.Precipitates,false);
        MoveMap(from.Ledger,to.Ledger,true);
        to.Temp=mixedTemp;
        if(iceTransfer){iceState.Apply(to);thermalNotice=IceWaterTransfer.Notice;}
        ThermalMixing.RecordNotice(to,thermalNotice);
        // Actual delivery reacts immediately; this is not a paused pour preview.
        Lab.Settle(from);Lab.Settle(to);return moved;
    }
    public static void Tick(float dt){
        if(PourTransaction.IsActive||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        foreach(var tool in Lab.Vessels){
            if(!IsTool(tool)||tool.MeteredFlowMlPerSecond==0)continue;
            if(tool.AttachedTo==null||!Lab.Vessels.Contains(tool.AttachedTo)||Move(tool,tool.MeteredFlowMlPerSecond*dt)<=1e-10)tool.MeteredFlowMlPerSecond=0;
        }
    }
}
}
