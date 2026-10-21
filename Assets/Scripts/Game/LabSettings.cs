using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {
public static class LabAppearance {
    public const string PreferenceKey="KaceLab.Appearance.Theme";
    public static bool Nessie => PlayerPrefs.GetInt(PreferenceKey,0)==1;
    public static void Select(bool nessie){
        PlayerPrefs.SetInt(PreferenceKey,nessie?1:0);PlayerPrefs.Save();
        foreach(var panel in Resources.FindObjectsOfTypeAll<ThemePanel>())panel.Refresh();
    }
}

// Decorative images never participate in input or change existing controls.
public sealed class ThemePanel : MonoBehaviour {
    RawImage art;
    public string Illustration="Settings";
    public float Opacity=.25f;
    void OnEnable(){Refresh();}
    public void Refresh(){
        if(art==null){
            var go=new GameObject("NessieBackground",typeof(RectTransform),typeof(RawImage));
            go.layer=gameObject.layer;go.transform.SetParent(transform,false);go.transform.SetAsFirstSibling();
            art=go.GetComponent<RawImage>();art.raycastTarget=false;
            var rect=art.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(5,5);rect.offsetMax=new Vector2(-5,-5);
        }
        art.enabled=LabAppearance.Nessie;
        if(art.enabled)art.texture=Resources.Load<Texture2D>("Themes/Nessie/"+Illustration);
        art.color=new Color(1,1,1,Opacity);Crop();
    }
    void OnRectTransformDimensionsChange(){Crop();}
    void Crop(){
        if(art==null||art.texture==null)return;
        var r=((RectTransform)transform).rect;
        float aspect=r.width/Mathf.Max(1,r.height),imageAspect=(float)art.texture.width/art.texture.height;
        float w=Mathf.Min(1,aspect/imageAspect),h=Mathf.Min(1,imageAspect/aspect);
        art.uvRect=new Rect((1-w)*.5f,(1-h)*.5f,w,h);
    }
}

public partial class LabUI {
    RectTransform _settings;
    Button _classicTheme,_nessieTheme;
    public bool SettingsOpen=>_settings!=null&&_settings.gameObject.activeSelf;
    void BuildSettings(){
        Localization.RegisterTranslation("外观 / 主题","Appearance / Theme");
        Localization.RegisterTranslation("主题即时生效，并自动保存。","Changes apply immediately and are saved automatically.");
        Localization.RegisterTranslation("小水怪","Little Nessie");
        Localization.RegisterTranslation("经典白色","Classic White");
        var entry=Node("SettingsEntry",_canvas.transform,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,BG2);
        entry.pivot=new Vector2(0,1);entry.anchoredPosition=new Vector2(232,-12);entry.sizeDelta=new Vector2(70,30);
        Btn(entry,"设置",()=>ShowSettings(true),BG2,14);
        foreach(var pair in new[]{("LeftPanel","Shelf"),("RightPanel","Settings"),("BottomPanel","Log"),("MainNavigation","Log")}){
            var target=_canvas.transform.Find(pair.Item1);if(target==null)continue;
            var theme=target.gameObject.AddComponent<ThemePanel>();theme.Illustration=pair.Item2;theme.Refresh();
        }
        _settings=Node("SettingsScreen",_canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.95f,.98f,.97f,.98f));
        var backdrop=_settings.gameObject.AddComponent<ThemePanel>();backdrop.Opacity=.14f;backdrop.Refresh();
        var page=Node("SettingsPage",_settings,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-330,-240),new Vector2(330,240),BG);
        Label(page,"设置",28,INK,TextAnchor.MiddleLeft,new Vector2(0,1),Vector2.one,new Vector2(28,-66),new Vector2(-100,-16));
        var close=Node("Close",page,new Vector2(1,1),Vector2.one,new Vector2(-96,-57),new Vector2(-24,-23),Color.clear);
        Btn(close,"返回",()=>ShowSettings(false),BG2,14);
        Label(page,"外观 / 主题",18,INK,TextAnchor.MiddleLeft,new Vector2(0,1),Vector2.one,new Vector2(28,-122),new Vector2(-28,-78));
        ThemeChoice(page,false,28,308);
        ThemeChoice(page,true,340,620);
        Label(page,"主题即时生效，并自动保存。",14,DIM,TextAnchor.MiddleLeft,Vector2.zero,new Vector2(1,0),new Vector2(28,22),new Vector2(-28,60));
        SetLayerRecursive(_settings.gameObject,UiLayer);
        _settings.gameObject.SetActive(false);
    }
    void ThemeChoice(Transform page,bool nessie,float left,float right){
        var card=Node(nessie?"NessieTheme":"ClassicTheme",page,new Vector2(0,1),new Vector2(0,1),new Vector2(left,-370),new Vector2(right,-140),Color.white);
        var preview=Node("Preview",card,Vector2.zero,Vector2.one,new Vector2(10,60),new Vector2(-10,-10),new Color(.91f,.97f,.96f,1));
        if(nessie){var go=new GameObject("Artwork",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(preview,false);var img=go.GetComponent<RawImage>();img.rectTransform.anchorMin=Vector2.zero;img.rectTransform.anchorMax=Vector2.one;img.rectTransform.offsetMin=img.rectTransform.offsetMax=Vector2.zero;img.texture=Resources.Load<Texture2D>("Themes/Nessie/Settings");img.raycastTarget=false;}
        else Label(preview,"KACE LAB",25,INK,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var buttonRoot=Node("Choose",card,Vector2.zero,new Vector2(1,0),new Vector2(10,10),new Vector2(-10,49),Color.clear);
        var button=Btn(buttonRoot,nessie?"小水怪":"经典白色",()=>{LabAppearance.Select(nessie);RefreshThemeChoices();},BG2,16);
        if(nessie)_nessieTheme=button;else _classicTheme=button;
    }
    void RefreshThemeChoices(){
        if(_classicTheme!=null)_classicTheme.targetGraphic.color=LabAppearance.Nessie?BG2:ACC;
        if(_nessieTheme!=null)_nessieTheme.targetGraphic.color=LabAppearance.Nessie?ACC:BG2;
    }
    public void ShowSettings(bool show){
        if(_settings==null)return;
        _settings.gameObject.SetActive(show);if(show){_settings.SetAsLastSibling();RefreshThemeChoices();}
    }
}
}
