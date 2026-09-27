using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;
namespace ChemLab.Game {
public partial class LabUI {
    RectTransform _burnerModal;
    Apparatus _controlledBurner;
    Slider _fireSlider;
    Text _fireValue,_fireStatus,_fireToggle;
    public bool BurnerDialogOpen=>_burnerModal!=null;
    public void ShowBurnerDialog(Apparatus burner){
        if(burner==null||burner.Kind!=ApparatusKind.BunsenBurner||!ApparatusSet.All.Contains(burner)||IsPouring)return;
        CloseBurnerDialog();CloseDose();CancelReagentDrag();PetMenu.Close();CloseCompactDrawers();
        _controlledBurner=burner;
        _burnerModal=(RectTransform)Node("BurnerModal",_canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.6f));
        var panel=Node("BurnerPanel",_burnerModal,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-230,-170),new Vector2(230,170),BG);
        Label(panel,"本生灯 · 调节火力",20,ACC,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(24,278),new Vector2(-24,-20));
        _fireValue=Label(panel,"",24,INK,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(24,220),new Vector2(-24,-72));
        _fireSlider=MakeSlider(panel,"BurnerPowerSlider",new Vector2(24,178),new Vector2(412,30),burner.GasFlow,value=>{
            if(_controlledBurner==null)return;
            _controlledBurner.GasFlow=value;
            if(!_controlledBurner.CanLight)_controlledBurner.Lit=false;
            RefreshBurnerDialog();
        });
        _fireStatus=Label(panel,"",14,INK,TextAnchor.UpperLeft,Vector2.zero,Vector2.one,new Vector2(24,80),new Vector2(-24,-176));
        var toggle=Node("BurnerIgnition",panel,Vector2.zero,Vector2.zero,new Vector2(24,24),new Vector2(220,66),BG2);
        _fireToggle=Btn(toggle,"点燃",()=>{if(_controlledBurner!=null){_controlledBurner.Lit=!_controlledBurner.Lit&&_controlledBurner.CanLight;RefreshBurnerDialog();}},BG2,16,true).GetComponentInChildren<Text>();
        var close=Node("BurnerClose",panel,Vector2.zero,Vector2.zero,new Vector2(240,24),new Vector2(436,66),ACC);
        Btn(close,"完成 / Enter",CloseBurnerDialog,ACC,16,true);
        SetLayerRecursive(_burnerModal.gameObject,UiLayer);RefreshBurnerDialog();PrewarmFonts();
    }
    void RefreshBurnerDialog(){
        if(_burnerModal==null||_controlledBurner==null)return;
        _fireValue.text=$"火力 {_controlledBurner.GasFlow*100:F0}%";
        _fireSlider.SetValueWithoutNotify((float)_controlledBurner.GasFlow);
        _fireToggle.text=_controlledBurner.Lit?"熄灭":"点燃";
        _fireStatus.text=(_controlledBurner.Lit?"已点燃":"未点燃")+" · 拖动滑条立即生效\n"+
            (_controlledBurner.CanLight?"调高火力会增大火焰并加快加热。":"低于 12% 无法维持火焰；调高后需重新点燃。")+"\nEsc 关闭，保留当前设置。";
    }
    public void CloseBurnerDialog(){
        if(_burnerModal!=null){_burnerModal.gameObject.SetActive(false);Destroy(_burnerModal.gameObject);}
        _burnerModal=null;_controlledBurner=null;
    }
    void UpdateBurnerDialog(){
        if(_burnerModal==null)return;
        if(!ApparatusSet.All.Contains(_controlledBurner)||LabInput.GetKeyDown(KeyCode.Escape)||LabInput.GetKeyDown(KeyCode.Return)||LabInput.GetKeyDown(KeyCode.KeypadEnter)){CloseBurnerDialog();return;}
        RefreshBurnerDialog();
    }
}
}
