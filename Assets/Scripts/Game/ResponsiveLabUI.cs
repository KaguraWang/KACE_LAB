using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChemLab.Game {
// Scrolls the existing direct children, keeping public scene/test paths intact.
public sealed class LeftPanelScroll : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler {
    RectTransform panel;
    RectTransform[] content;
    Vector2[] original;
    float offset;
    float contentBottom;
    public void Configure(RectTransform host,RectTransform[] items){
        panel=host;content=items;original=new Vector2[items.Length];
        contentBottom=0;
        for(int i=0;i<items.Length;i++){
            original[i]=items[i].anchoredPosition;
            contentBottom=Mathf.Max(contentBottom,-original[i].y+items[i].rect.height*items[i].pivot.y);
        }
        Apply();
    }
    float Maximum()=>panel==null?0:Mathf.Max(0,contentBottom-panel.rect.height+12);
    void Apply(){
        if(content==null)return;
        offset=Mathf.Clamp(offset,0,Maximum());
        for(int i=0;i<content.Length;i++)if(content[i]!=null){
            var want=original[i]+Vector2.up*offset;
            if((content[i].anchoredPosition-want).sqrMagnitude>1e-8f)content[i].anchoredPosition=want;
        }
    }
    void Update()=>Apply();
    public void OnScroll(PointerEventData e){offset-=e.scrollDelta.y*42f;Apply();}
    public void OnBeginDrag(PointerEventData e){ }
    public void OnDrag(PointerEventData e){offset-=e.delta.y;Apply();}
}

public partial class LabUI {
    RectTransform _responsiveLeft,_responsiveRight,_responsiveBottom;
    RectTransform _leftDrawerToggle,_rightDrawerToggle,_logDrawerToggle;
    Text _leftDrawerMark,_rightDrawerMark,_logDrawerMark;
    readonly PanelMotion _leftMotion=new PanelMotion(),_rightMotion=new PanelMotion(),_logMotion=new PanelMotion();
    CanvasScaler _responsiveScaler;
    bool _compactLeftOpen,_compactRightOpen;
    bool _desktopLeftOpen=true,_desktopRightOpen=true,_logOpen=true;
    bool? _lastCompact;
    public bool CompactLayout=>Screen.width<1180;

    // Layout is recalculated for window resizing every frame. Restore its base
    // position first so animation offsets never accumulate into permanent drift.
    sealed class PanelMotion {
        RectTransform rect;CanvasGroup group;Vector2 basePosition;
        bool initialized,applied;float progress;
        public void Prepare(RectTransform target){
            rect=target;if(group==null)group=target.gameObject.AddComponent<CanvasGroup>();
            if(applied)rect.anchoredPosition=basePosition;
            applied=false;
        }
        public void Present(bool open,Vector2 direction){
            basePosition=rect.anchoredPosition;
            if(!initialized){progress=open?1:0;initialized=true;}
            else progress=Mathf.MoveTowards(progress,open?1:0,Time.unscaledDeltaTime/.26f);
            float eased=progress*progress*(3-2*progress);
            rect.anchoredPosition=basePosition+direction*(24*(1-eased));applied=true;
            group.alpha=eased;group.blocksRaycasts=open;group.interactable=open&&progress>=.999f;
            bool active=open||progress>0;
            if(rect.gameObject.activeSelf!=active)rect.gameObject.SetActive(active);
        }
    }

    static Rect ScreenBounds(RectTransform rt){
        var corners=new Vector3[4];rt.GetWorldCorners(corners);
        var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
        var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
    }
    public Rect TagWorkspaceRect(){
        float scale=_responsiveScaler!=null?_responsiveScaler.scaleFactor:1f;
        float x0=12*scale,x1=Screen.width-12*scale;
        float y0=12*scale,y1=Screen.height-54*scale;
        if(_responsiveBottom!=null&&_responsiveBottom.gameObject.activeInHierarchy)
            y0=Mathf.Max(y0,ScreenBounds(_responsiveBottom).yMax+10*scale);
        if(_responsiveLeft!=null&&_responsiveLeft.gameObject.activeInHierarchy)
            x0=Mathf.Max(x0,ScreenBounds(_responsiveLeft).xMax+10*scale);
        if(_responsiveRight!=null&&_responsiveRight.gameObject.activeInHierarchy)
            x1=Mathf.Min(x1,ScreenBounds(_responsiveRight).xMin-10*scale);
        return Rect.MinMaxRect(x0,y0,Mathf.Max(x0+1,x1),Mathf.Max(y0+1,y1));
    }

    void BuildResponsiveControls(){
        _responsiveLeft=_canvas.transform.Find("LeftPanel") as RectTransform;
        _responsiveRight=_canvas.transform.Find("RightPanel") as RectTransform;
        _responsiveBottom=_canvas.transform.Find("BottomPanel") as RectTransform;
        _responsiveScaler=_canvas.GetComponent<CanvasScaler>();
        if(_responsiveLeft==null||_responsiveRight==null||_responsiveBottom==null)return;
        var items=new System.Collections.Generic.List<RectTransform>();
        foreach(Transform child in _responsiveLeft)
            if(child.name.StartsWith("Ap_")||child.name=="VesselCard")items.Add((RectTransform)child);
        _responsiveLeft.gameObject.AddComponent<RectMask2D>();
        _responsiveLeft.gameObject.AddComponent<LeftPanelScroll>().Configure(_responsiveLeft,items.ToArray());
        var header=Node("LeftStickyHeader",_responsiveLeft,new Vector2(0,1),new Vector2(1,1),
                        new Vector2(0,-44),Vector2.zero,BG);
        const string title="器材架 <color=#666666>· 滚动查看操作</color>";
        ChemLab.Chem.Localization.RegisterTranslation(title,"Apparatus <color=#666666>· scroll to view controls</color>");
        Label(header,title,14,ACC,TextAnchor.MiddleLeft,
              Vector2.zero,Vector2.one,new Vector2(14,0),new Vector2(-8,0));

        _leftDrawerToggle=Node("LeftDrawerToggle",_canvas.transform,
                               new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,ACC);
        _leftDrawerToggle.pivot=new Vector2(0,1);
        _leftDrawerToggle.anchoredPosition=new Vector2(18,-12);
        _leftDrawerToggle.sizeDelta=new Vector2(110,30);
        var leftDrawerButton=Btn(_leftDrawerToggle,"器材",()=>{
            if(CompactLayout){_compactLeftOpen=!_compactLeftOpen;if(_compactLeftOpen)_compactRightOpen=false;}
            else _desktopLeftOpen=!_desktopLeftOpen;
        },ACC,14);
        var leftDrawerText=leftDrawerButton.GetComponentInChildren<Text>();
        if(leftDrawerText!=null){var r=(RectTransform)leftDrawerText.transform;r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(2,0);r.offsetMax=new Vector2(-24,0);leftDrawerText.alignment=TextAnchor.MiddleCenter;}
        _leftDrawerMark=Label(_leftDrawerToggle,"−",14,InkOn(ACC),TextAnchor.MiddleCenter,
            new Vector2(1,0),Vector2.one,new Vector2(-20,0),Vector2.zero);

        _rightDrawerToggle=Node("RightDrawerToggle",_canvas.transform,
                                new Vector2(1,1),new Vector2(1,1),Vector2.zero,Vector2.zero,ACC);
        _rightDrawerToggle.pivot=new Vector2(1,1);
        _rightDrawerToggle.anchoredPosition=new Vector2(-124,-12);
        _rightDrawerToggle.sizeDelta=new Vector2(110,30);
        var rightDrawerButton=Btn(_rightDrawerToggle,"试剂",()=>{
            if(CompactLayout){_compactRightOpen=!_compactRightOpen;if(_compactRightOpen)_compactLeftOpen=false;}
            else _desktopRightOpen=!_desktopRightOpen;
        },ACC,14);
        var rightDrawerText=rightDrawerButton.GetComponentInChildren<Text>();
        if(rightDrawerText!=null){var r=(RectTransform)rightDrawerText.transform;r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(2,0);r.offsetMax=new Vector2(-24,0);rightDrawerText.alignment=TextAnchor.MiddleCenter;}
        _rightDrawerMark=Label(_rightDrawerToggle,"−",14,InkOn(ACC),TextAnchor.MiddleCenter,
            new Vector2(1,0),Vector2.one,new Vector2(-20,0),Vector2.zero);

        _logDrawerToggle=Node("LogDrawerToggle",_canvas.transform,
            new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,ACC);
        _logDrawerToggle.pivot=new Vector2(0,1);_logDrawerToggle.anchoredPosition=new Vector2(136,-12);
        _logDrawerToggle.sizeDelta=new Vector2(90,30);
        var logDrawerButton=Btn(_logDrawerToggle,"记录",()=>_logOpen=!_logOpen,ACC,14);
        var logDrawerText=logDrawerButton.GetComponentInChildren<Text>();
        if(logDrawerText!=null){var r=(RectTransform)logDrawerText.transform;r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(2,0);r.offsetMax=new Vector2(-24,0);logDrawerText.alignment=TextAnchor.MiddleCenter;}
        _logDrawerMark=Label(_logDrawerToggle,"−",14,InkOn(ACC),TextAnchor.MiddleCenter,
            new Vector2(1,0),Vector2.one,new Vector2(-20,0),Vector2.zero);
    }

    public void CloseCompactDrawers(){
        if(!CompactLayout)return;
        _compactLeftOpen=false;_compactRightOpen=false;
        ApplyResponsiveLayout();
    }

    void ApplyResponsiveLayout(){
        if(_responsiveLeft==null||_responsiveRight==null||_responsiveBottom==null)return;
        _leftMotion.Prepare(_responsiveLeft);_rightMotion.Prepare(_responsiveRight);_logMotion.Prepare(_responsiveBottom);
        // Narrow windows keep legible native text with one drawer open. Larger
        // displays grow the whole HUD proportionally instead of shrinking it.
        if(_responsiveScaler!=null){
            if(_responsiveScaler.uiScaleMode!=CanvasScaler.ScaleMode.ConstantPixelSize)
                _responsiveScaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            float scale=Screen.width<800?Screen.width/800f:Mathf.Max(1f,Screen.width/1280f);
            if(Mathf.Abs(_responsiveScaler.scaleFactor-scale)>1e-5f)_responsiveScaler.scaleFactor=scale;
        }
        bool compact=CompactLayout;
        if(_lastCompact!=compact){_lastCompact=compact;_compactLeftOpen=false;_compactRightOpen=false;}
        var leftBottom=_responsiveLeft.offsetMin;
        float leftY=compact?130:200;
        if(Mathf.Abs(leftBottom.y-leftY)>1e-5f){leftBottom.y=leftY;_responsiveLeft.offsetMin=leftBottom;}
        var leftTop=_responsiveLeft.offsetMax;leftTop.y=-52;_responsiveLeft.offsetMax=leftTop;
        var rightTop=_responsiveRight.offsetMax;rightTop.y=-52;_responsiveRight.offsetMax=rightTop;
        var rightBottom=_responsiveRight.offsetMin;
        float rightX=compact?-338:-336,rightY=compact?130:200;
        if(Mathf.Abs(rightBottom.x-rightX)>1e-5f||Mathf.Abs(rightBottom.y-rightY)>1e-5f){
            rightBottom.x=rightX;rightBottom.y=rightY;_responsiveRight.offsetMin=rightBottom;
        }
        var logTop=_responsiveBottom.offsetMax;
        float logY=compact?118:186;
        if(Mathf.Abs(logTop.y-logY)>1e-5f){logTop.y=logY;_responsiveBottom.offsetMax=logTop;}
        bool leftVisible=compact?_compactLeftOpen:_desktopLeftOpen,rightVisible=compact?_compactRightOpen:_desktopRightOpen;
        _leftMotion.Present(leftVisible,Vector2.left);_rightMotion.Present(rightVisible,Vector2.right);_logMotion.Present(_logOpen,Vector2.down);
        _leftDrawerMark.text=leftVisible?"−":"+";_rightDrawerMark.text=rightVisible?"−":"+";_logDrawerMark.text=_logOpen?"−":"+";
        DrawerColor(_leftDrawerToggle,leftVisible);
        DrawerColor(_rightDrawerToggle,rightVisible);
        DrawerColor(_logDrawerToggle,_logOpen);
    }
    static void DrawerColor(RectTransform toggle,bool selected){
        if(toggle==null)return;
        var shade=selected?LabUiTheme.Accent:LabUiTheme.Card;
        var background=toggle.GetComponent<Image>();if(background!=null)background.color=shade;
        var button=toggle.GetComponentInChildren<Button>();if(button!=null&&button.targetGraphic!=null)button.targetGraphic.color=shade;
    }
}
}
