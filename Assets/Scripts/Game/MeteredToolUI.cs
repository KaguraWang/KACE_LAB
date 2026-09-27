using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;
namespace ChemLab.Game {
public sealed class FlowDial : MonoBehaviour,IDragHandler,IScrollHandler {
    public Action<float> Adjust;
    public void OnDrag(PointerEventData e)=>Adjust?.Invoke(e.delta.y*.01f);
    public void OnScroll(PointerEventData e)=>Adjust?.Invoke(e.scrollDelta.y*.05f);
}
public partial class LabUI {
    RectTransform _meteredPanel,_meteredDialPointer,_meteredLogScroll;Text _meteredStatus,_meteredFrameLabel;
    Vessel _meteredTool;
    bool _meteredLogLayoutPending;
    void UpdateMeteredToolUI(){
        if(_canvas==null)return;
        bool visible=MeteredTransfer.IsTool(_selected)&&!IsPouring&&!IsDosing;
        if(_meteredTool!=null&&_meteredTool!=_selected&&LabBootstrap.Instance!=null&&
           LabBootstrap.Instance.MeteredFrameActiveFor(_meteredTool))
            LabBootstrap.Instance.ToggleMeteredFrame(_meteredTool);
        if(_meteredPanel!=null&&_meteredTool!=_selected){Destroy(_meteredPanel.gameObject);_meteredPanel=null;}
        _meteredTool=_selected;
        if(_meteredLogScroll==null){var log=GameObject.Find("LogScroll");if(log!=null)_meteredLogScroll=log.GetComponent<RectTransform>();}
        if(_meteredLogScroll!=null){
            var anchors=_meteredLogScroll.anchorMax;
            float logRight=(visible && !CompactLayout) ? .5f : 1f;
            if(Mathf.Abs(anchors.x-logRight)>1e-5f){
                anchors.x=logRight;_meteredLogScroll.anchorMax=anchors;
                if(!_meteredLogLayoutPending)StartCoroutine(RefreshLogLayoutAfterResize());
            }
        }
        if(!visible){if(_meteredPanel!=null)_meteredPanel.gameObject.SetActive(false);return;}
        if(_meteredPanel==null){
            _meteredPanel=(RectTransform)Node("MeteredToolPanel",_canvas.transform,new Vector2(1,0),new Vector2(1,0),Vector2.zero,Vector2.zero,BG);
            _meteredPanel.pivot=new Vector2(1,0);
            _meteredPanel.sizeDelta=new Vector2(420,130);
            _meteredPanel.anchoredPosition=new Vector2(-20,20);
            _meteredStatus=Label(_meteredPanel,"",16,INK,TextAnchor.UpperLeft,Vector2.zero,Vector2.one,new Vector2(15,65),new Vector2(-112,-12));
            var frame=Node("FrameMeteredTool",_meteredPanel,Vector2.zero,Vector2.zero,new Vector2(310,78),new Vector2(405,116),BG2);
            Btn(frame,"查看全貌",()=>LabBootstrap.Instance?.ToggleMeteredFrame(_meteredTool),BG2,14,true);
            _meteredFrameLabel=frame.GetComponentInChildren<Text>();
            if(_selected.Kind==VesselKind.Burette){
                var dial=Node("BuretteFlowDial",_meteredPanel,Vector2.zero,Vector2.zero,new Vector2(15,10),new Vector2(85,65),BG2);
                Label(dial,"○",42,DIM,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).raycastTarget=false;
                _meteredDialPointer=(RectTransform)Node("FlowDialPointer",dial,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-2,0),new Vector2(2,18),ACC);
                _meteredDialPointer.pivot=new Vector2(.5f,0);
                _meteredDialPointer.anchoredPosition=Vector2.zero;
                _meteredDialPointer.GetComponent<Image>().raycastTarget=false;
                dial.gameObject.AddComponent<FlowDial>().Adjust=delta=>{
                    if(_meteredTool==null||_meteredTool.AttachedTo==null)return;
                    _meteredTool.MeteredFlowMlPerSecond=Math.Max(0,Math.Min(2,_meteredTool.MeteredFlowMlPerSecond+delta));
                };
                Label(_meteredPanel,"上下拖动旋钮 / 滚轮调速",14,DIM,TextAnchor.MiddleLeft,Vector2.zero,Vector2.zero,new Vector2(95,15),new Vector2(305,60));
            }else{
                void Action(string title,int x,double rate){var node=Node(title,_meteredPanel,Vector2.zero,Vector2.zero,new Vector2(x,12),new Vector2(x+125,57),BG2);Btn(node,title,()=>{if(_meteredTool?.AttachedTo!=null)_meteredTool.MeteredFlowMlPerSecond=rate;},BG2,16,true);}
                Action("抽取",15,-5);Action("释放",150,5);
            }
            var stop=Node("StopFlow",_meteredPanel,Vector2.zero,Vector2.zero,new Vector2(310,12),new Vector2(405,57),BG2);
            Btn(stop,"停止",()=>{if(_meteredTool!=null)_meteredTool.MeteredFlowMlPerSecond=0;},BG2,16,true);
            SetLayerRecursive(_meteredPanel.gameObject,UiLayer);PrewarmFonts();
        }
        _meteredPanel.anchoredPosition=new Vector2(-20,CompactLayout?130:20);
        _meteredPanel.localScale=Vector3.one*Mathf.Min(1f,Mathf.Max(.1f,(Screen.width*.5f-34f)/420f));
        _meteredPanel.gameObject.SetActive(true);
        var v=_meteredTool;string target=v.AttachedTo==null?"请拖到目标器皿上方":VesselNames.Name(v.AttachedTo.Kind)+" #"+v.AttachedTo.Id;
        _meteredStatus.text=$"{VesselNames.Name(v.Kind)}：{v.Volume():F2} / {v.Capacity:F0} mL\n{target}  |  {v.MeteredFlowMlPerSecond:F2} mL/s";
        if(_meteredFrameLabel!=null)_meteredFrameLabel.text=LabBootstrap.Instance!=null&&LabBootstrap.Instance.MeteredFrameActiveFor(v)?"返回视角":"查看全貌";
        if(_meteredDialPointer!=null)_meteredDialPointer.localRotation=Quaternion.Euler(0,0,135-(float)(v.MeteredFlowMlPerSecond*135));
    }

    System.Collections.IEnumerator RefreshLogLayoutAfterResize(){
        _meteredLogLayoutPending=true;
        yield return null;
        Canvas.ForceUpdateCanvases();
        RefreshLogLayout();
        yield return new WaitForEndOfFrame();
        RefreshLogLayout();
        _meteredLogLayoutPending=false;
    }
}
}
