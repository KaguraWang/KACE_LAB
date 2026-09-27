using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;
namespace ChemLab.Game {
public partial class LabUI {
    string _dragSubstance,_doseSubstance;Vessel _doseTarget;
    VesselView _dropView;RectTransform _dragGhost,_doseModal;
    InputField _doseInput;Text _doseError,_unitText;bool _doseMol;
    public bool IsDosing=>_dragSubstance!=null||_doseModal!=null;
    public bool DoseDialogOpen=>_doseModal!=null;
    public string DoseErrorForTest=>_doseModal!=null?_doseError.text:"";
    public bool DoseMolForTest=>_doseMol;
    public string DoseSubstanceForTest=>_doseModal!=null?_doseSubstance:"";
    public string DoseTargetForTest=>_doseModal!=null?_doseTarget?.Id:"";
    public Vessel DropTarget=>_dropView!=null?_dropView.Model:null;
    public void BeginReagentDrag(string id,Vector2 pointer){
        if(_doseModal!=null)return;CancelReagentDrag();PetMenu.Close();_dragSubstance=id;
        _dragGhost=(RectTransform)Node("ReagentGhost",_canvas.transform,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(160,32),BG2);
        _dragGhost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
        Label(_dragGhost,PhaseProperties.ShelfName(id),13,ACC,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        SetLayerRecursive(_dragGhost.gameObject,UiLayer);MoveReagentDrag(pointer);
    }
    public void MoveReagentDrag(Vector2 pointer){
        if(_dragSubstance==null)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT,pointer,_canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:UICam,out var local);
        _dragGhost.anchoredPosition=local+_canvasRT.rect.size*_canvasRT.pivot+new Vector2(18,18);
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pointer},hits);
        bool blocked=pointer.x<0||pointer.y<0||pointer.x>Screen.width||pointer.y>Screen.height||hits.Exists(h=>h.gameObject.transform.IsChildOf(_canvas.transform));
        VesselView target=null;
        if(!blocked && Lab3D!=null){Physics.SyncTransforms();var ray=Lab3D.Cam.ScreenPointToRay(pointer);var world=Physics.RaycastAll(ray,100);Array.Sort(world,(a,b)=>a.distance.CompareTo(b.distance));foreach(var h in world){target=h.collider.GetComponentInParent<VesselView>();if(target!=null)break;}}
        if(_dropView!=target){if(_dropView!=null)_dropView.SetHighlight(false);_dropView=target;}
        if(_dropView!=null)_dropView.SetHighlight(true);
    }
    public void EndReagentDrag(Vector2 pointer){
        if(_dragSubstance==null)return;MoveReagentDrag(pointer);var target=DropTarget;var id=_dragSubstance;CancelReagentDrag();
        CloseCompactDrawers();
        if(target!=null)ShowDoseDialog(target,id);else Hint("请将试剂拖到器皿上后松开");
    }
    public void CancelReagentDrag(){if(_dropView!=null)_dropView.SetHighlight(false);_dropView=null;_dragSubstance=null;if(_dragGhost!=null){Destroy(_dragGhost.gameObject);_dragGhost=null;}}
    public void ShowDoseDialog(Vessel target,string id){
        if(_doseModal!=null||!Lab.Vessels.Contains(target))return;_doseTarget=target;_doseSubstance=id;_doseMol=false;
        _doseModal=(RectTransform)Node("DoseModal",_canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,0.55f));
        var panel=Node("DosePanel",_doseModal,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(-240,-160),new Vector2(240,160),BG);
        Label(panel,"加入 "+PhaseProperties.ShelfName(id)+" → "+VesselNames.Name(target.Kind),18,ACC,TextAnchor.UpperLeft,new Vector2(0,1),Vector2.one,new Vector2(22,-56),new Vector2(-20,-20));
        Label(panel,"输入物质的量或质量（g / mol）",13,DIM,TextAnchor.UpperLeft,new Vector2(0,1),Vector2.one,new Vector2(22,-89),new Vector2(-20,-63));
        var input=Node("DoseAmount",panel,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-145),new Vector2(330,-103),BG2);
        _doseInput=input.gameObject.AddComponent<InputField>();_doseInput.textComponent=Label(input,"",18,INK,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(10,0),new Vector2(-10,0));_doseInput.textComponent.raycastTarget=false;_doseInput.targetGraphic=input.GetComponent<Image>();_doseInput.text="0.1";
        var unit=Node("DoseUnit",panel,new Vector2(0,1),new Vector2(0,1),new Vector2(344,-145),new Vector2(458,-103),BG2);
        _unitText=Btn(unit,"g ▾",()=>{_doseMol=!_doseMol;_unitText.text=_doseMol?"mol ▾":"g ▾";},BG2,17,true).GetComponentInChildren<Text>();
        _doseError=Label(panel,"",12,WARN,TextAnchor.UpperLeft,new Vector2(0,1),Vector2.one,new Vector2(22,-230),new Vector2(-22,-160));
        var cancel=Node("DoseCancel",panel,Vector2.zero,Vector2.zero,new Vector2(22,20),new Vector2(220,62),BG2);Btn(cancel,"取消",CloseDose,BG2,16,true);
        var confirm=Node("DoseConfirm",panel,Vector2.zero,Vector2.zero,new Vector2(240,20),new Vector2(458,62),ACC);Btn(confirm,"确定 / Enter",ConfirmDose,ACC,16,true);
        SetLayerRecursive(_doseModal.gameObject,UiLayer);PrewarmFonts();_doseInput.Select();_doseInput.ActivateInputField();
    }
    public void CloseDose(){if(_doseModal!=null){_doseModal.gameObject.SetActive(false);Destroy(_doseModal.gameObject);}_doseModal=null;_doseTarget=null;_doseInput=null;}
    public void ConfirmDose(){
        if(_doseModal==null)return;
        if(!double.TryParse(_doseInput.text,NumberStyles.Float,CultureInfo.InvariantCulture,out double amount)||double.IsNaN(amount)||double.IsInfinity(amount)||amount<=0){_doseError.text="请输入大于 0 的有限数值";return;}
        if(!Lab.Vessels.Contains(_doseTarget)){CloseDose();return;}
        if(!DoseService.TryAdd(_doseTarget,_doseSubstance,amount,_doseMol,out string message)){_doseError.text=message;return;}
        SelectExternal(_doseTarget);CloseDose();Hint(message);RefreshLog();
    }
    void UpdateDoseInput(){
        if(_dropView!=null)_dropView.SetHighlight(true);
        if(LabInput.GetKeyDown(KeyCode.Escape)){CancelReagentDrag();CloseDose();return;}
        if(_doseModal!=null&&(LabInput.GetKeyDown(KeyCode.Return)||LabInput.GetKeyDown(KeyCode.KeypadEnter)))ConfirmDose();
    }
    public void SetDoseForTest(string amount,bool mol){if(_doseInput!=null)_doseInput.text=amount;_doseMol=mol;}
}
public static class DoseService {
    public static bool TryAdd(Vessel v,string id,double amount,bool molUnit,out string message){
        message="";if(!ChemData.Sub.TryGetValue(id,out var s)||amount<=0||double.IsNaN(amount)||double.IsInfinity(amount)){message="无效的物质或数量";return false;}
        double mol=molUnit?amount:amount/s.Mass;
        string phase=PhaseProperties.Phase(id,v.Temp);
        if(v.Sealed&&phase!="g"){message="容器已密闭，请先打开再加入液体或固体；气体可经模拟进气口加入";return false;}
        if(phase=="aq"&&!SolventProperties.HasSolvent(v)){message="该条目只能以溶液形式加入，请先放入溶剂";return false;}
        if(phase=="aq"&&!SolventProperties.IsAqueousModelValid(v)){message="当前为"+SolventProperties.Description(v)+"，尚缺该条目的溶剂专属平衡数据，不能按水溶液直接加入";return false;}
        double addedVolume=phase=="g"?0:mol*s.Mass/Math.Max(0.01,PhaseProperties.Density(id,v.Temp));
        if(id=="h2o"&&v.Temp<0&&WaterIce.CanPartition(v))addedVolume=mol*s.Mass/ChemData.Sub[WaterIce.Id].Density;
        double occupied=v.Volume()+SolidGeometry.VolumeMl(v);
        double capacity=Math.Min(v.Capacity,VesselMesh.InnerCapacity(v.Kind)*1000)*(MeteredTransfer.IsTool(v)?1:.95);
        if(occupied+addedVolume>capacity){message="超过容器安全容量，请减量或更换容器";return false;}
        v.PhaseNotice="";
        if(id=="h2o")v.AddWater(mol*s.Mass/s.Density);
        else if(phase=="g"){
            v.Gases.TryGetValue(id,out double old);v.Gases[id]=old+mol;PhaseProperties.Reconcile(v,true);
            if(SolventProperties.HasSolvent(v)&&!PhaseProperties.TryCapacity(v,id,out _))v.PhaseNotice="已通入气体；当前溶剂/温压缺少该气体的定量溶解模型，未假定完全溶解";
        }
        else if(phase=="s"&&v.Water<=0){
            v.Undissolved.TryGetValue(id,out double old);v.Undissolved[id]=old+mol;
            if(SolventProperties.HasSolvent(v)&&!PhaseProperties.TryCapacity(v,id,out _))v.PhaseNotice="当前溶剂/温压缺少该物质的定量溶解度，暂保留固体，未假定完全溶解";
        }
        else if(phase=="l"&&(v.Water<=1e-9||!SolventProperties.IsAqueousModelValid(v))){
            // Shelf liquids are pure reagents, not implicit aqueous stock bottles.
            v.Introduce(id,mol,false,false);
        }
        else v.AddSubstance(id,mol);
        Lab.Note(v,"加入 "+PhaseProperties.ShelfName(id)+" "+amount.ToString("G6")+(molUnit?" mol":" g"),"add");Lab.Settle(v);message=string.IsNullOrEmpty(v.PhaseNotice)?"已加入 "+PhaseProperties.ShelfName(id):v.PhaseNotice;return true;
    }
}
}
