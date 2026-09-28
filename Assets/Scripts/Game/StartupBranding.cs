using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Game {
// Own opening sequence: no engine logo, no OS input or window manipulation.
public sealed class StartupBranding : MonoBehaviour {
    public const float FadeIn=.65f, Hold=1.4f, FadeOut=.65f, Reveal=.25f;
    public static bool Active { get; private set; }
    public static StartupBranding Instance { get; private set; }
    public float Elapsed { get; private set; }
    public RawImage Logo { get; private set; }
    CanvasGroup group;
    bool started;
    float lastFrame;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState(){Active=false;Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Launch(){
        var args=Environment.GetCommandLineArgs();
        bool verify=Array.IndexOf(args,"-startupBrandTest")>=0;
        // Existing automation retains its original startup timing. The dedicated
        // startup test explicitly exercises the production fade and input gate.
        if(!verify&&Array.Exists(args,a=>a=="-noStartupSplash"||a=="-windowTargetInput"||a.EndsWith("Test",StringComparison.Ordinal)))return;
        var go=new GameObject("Chemical industry opening",typeof(RectTransform));
        DontDestroyOnLoad(go);go.AddComponent<StartupBranding>();
    }
    void Awake(){
        Active=true;Instance=this;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
        group=gameObject.AddComponent<CanvasGroup>();
        gameObject.AddComponent<GraphicRaycaster>();
        var backdrop=new GameObject("White backdrop",typeof(RectTransform),typeof(Image));backdrop.transform.SetParent(transform,false);
        var bg=backdrop.GetComponent<Image>();bg.color=Color.white;
        var br=(RectTransform)backdrop.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;
        var logo=new GameObject("Provided opening artwork",typeof(RectTransform),typeof(RawImage));logo.transform.SetParent(transform,false);
        Logo=logo.GetComponent<RawImage>();Logo.texture=Resources.Load<Texture2D>("Branding/ChemicalIndustrySplash");Logo.raycastTarget=false;
        Logo.color=new Color(1,1,1,0);
        var rect=(RectTransform)logo.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        Resize();
    }
    IEnumerator Start(){yield return new WaitForEndOfFrame();lastFrame=Time.realtimeSinceStartup;started=true;}
    void Resize(){
        if(Logo.texture==null)return;
        float ratio=(float)Logo.texture.width/Logo.texture.height;
        float height=Mathf.Min(Screen.height*.58f,Screen.width*.70f/ratio);
        Logo.rectTransform.sizeDelta=new Vector2(height*ratio,height);
    }
    public static float Opacity(float time){
        if(time<FadeIn)return Mathf.SmoothStep(0,1,Mathf.Clamp01(time/FadeIn));
        if(time<FadeIn+Hold)return 1;
        return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01((time-FadeIn-Hold)/FadeOut));
    }
    void Update(){
        if(!started)return;
        float now=Time.realtimeSinceStartup;
        // Loading stalls must not consume the whole fade before it is visible.
        Elapsed+=Mathf.Min(.05f,Mathf.Max(0,now-lastFrame));lastFrame=now;Resize();
        Logo.color=new Color(1,1,1,Opacity(Elapsed));
        group.alpha=1-Mathf.Clamp01((Elapsed-FadeIn-Hold-FadeOut)/Reveal);
        if(Elapsed>=FadeIn+Hold+FadeOut+Reveal){Active=false;Destroy(gameObject);}
    }
    void OnDestroy(){if(Instance==this){Instance=null;Active=false;}}
}
}
