using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;
namespace ChemLab.Game {
public partial class LabUI {
    RectTransform _thermalModal;
    DistillationAssembly _thermalRig;
    Text _thermalBody;
    bool _thermalExplanation;
    public bool ThermalLedgerOpen=>_thermalModal!=null;
    public void ShowThermalLedger(DistillationAssembly rig,bool explanation=false){
        if(rig==null||IsPouring)return;
        CloseThermalLedger();CloseDose();CloseBurnerDialog();CancelReagentDrag();CloseCompactDrawers();PetMenu.Close();
        _thermalRig=rig;_thermalExplanation=explanation;
        _thermalModal=(RectTransform)Node("ThermalLedgerModal",_canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.65f));
        var panel=Node("ThermalLedgerPanel",_thermalModal,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-260,-240),new Vector2(260,240),BG);
        Label(panel,explanation?"冷凝传热 · 模型说明":"冷凝传热 · 实时账本",21,ACC,TextAnchor.UpperLeft,Vector2.zero,Vector2.one,new Vector2(24,420),new Vector2(-24,-22));
        _thermalBody=Label(panel,"",17,INK,TextAnchor.UpperLeft,Vector2.zero,Vector2.one,new Vector2(24,76),new Vector2(-24,-76));
        var close=Node("ThermalLedgerClose",panel,Vector2.zero,Vector2.zero,new Vector2(24,22),new Vector2(496,62),ACC);
        Btn(close,"关闭 / Enter / Esc",CloseThermalLedger,ACC,16);
        SetLayerRecursive(_thermalModal.gameObject,UiLayer);RefreshThermalLedger();PrewarmFonts();
    }
    void RefreshThermalLedger(){
        var r=_thermalRig;if(r==null||_thermalBody==null)return;
        string body=_thermalExplanation?
            "按实际模型计算冷凝管、器皿玻璃及水套体积。\n\n玻璃采用 BOROFLOAT 平均热容代理，并作 0–100℃定热容近似。\n\n空气换热、液体接触热导和两侧 UA 分配为教学设定，不是实测仪器标定。\n\n储热、冷却水外流和环境散热分别记账。\n器皿蓄热："+(r.UseVesselThermalStorage?"已启用":"未启用"):
            $"状态：{(r.Running?"运行中":"已停止")}　管长：{r.CondenserLengthMeters*1000:F0} mm\n\n"+
            $"加热输入　　　　　　 {r.HeaterEnergyJ:F1} J\n冷却水带走　　　　　 {r.CoolantEnergyJ:F1} J\n壁面 / 水套储热　　　 {r.CondenserStoredEnergyJ:F1} J\n环境散热　　　　　　 {r.CondenserAmbientEnergyJ:F1} J\n"+
            $"壁温 {r.CondenserWallC:F2} ℃　水套 {r.CondenserWaterC:F2} ℃\n"+
            (r.UseVesselThermalStorage?$"器皿玻璃储热　　　　 {r.VesselStoredEnergyJ:F1} J\n器皿环境散热　　　　 {r.VesselAmbientEnergyJ:F1} J\n源瓶壁 {r.Source.GlassTemperatureC:F2} ℃　接收壁 {r.Receiver.GlassTemperatureC:F2} ℃\n":"器皿蓄热：未启用\n")+
            $"逸散 {r.EscapedGrams:F4} g / {r.EscapedEnergyJ:F1} J";
        Localization.Apply(_thermalBody,body);
    }
    public void CloseThermalLedger(){
        if(_thermalModal!=null){_thermalModal.gameObject.SetActive(false);Destroy(_thermalModal.gameObject);}
        _thermalModal=null;_thermalRig=null;_thermalBody=null;
    }
    void UpdateThermalLedger(){
        if(_thermalModal==null)return;
        if(_thermalRig?.Source?.Distillation!=_thermalRig||LabInput.GetKeyDown(KeyCode.Escape)||LabInput.GetKeyDown(KeyCode.Return)||LabInput.GetKeyDown(KeyCode.KeypadEnter)){CloseThermalLedger();return;}
        RefreshThermalLedger();
    }
}
}
