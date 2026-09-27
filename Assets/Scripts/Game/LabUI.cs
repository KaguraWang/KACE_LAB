/* =====================================================================
   实验台界面：器材架 / 容器面板 / 试剂架 / 实验记录
   全部用代码搭 uGUI，不依赖预制体。
   注意用 ScreenSpaceCamera 模式 —— 这样 Cam.Render() 到 RenderTexture 时
   界面会一起被渲染进去，无头截图才能验证到 UI。
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {

public partial class LabUI : MonoBehaviour {
    public LabBootstrap Lab3D;
    public Camera UICam;

    /// <summary>界面用的字体，标签也复用同一个（保证中文字形一致）</summary>
    public static Font UiFont;
    Canvas _canvas;
    RectTransform _pourBar, _burnerBar, _testBar;
    Text _testLabel;
    Button _testBtn1, _testBtn2;
    /* 容器标签的控制 */
    RectTransform _tagBar;
    Button _tagToggle, _tagMode, _tagName, _tagAmt, _tagConc;
    /* 方程式弹出标签的控制 */
    RectTransform _eqBar;
    Button _eqShow, _eqIonic;
    InputField _eqDur;
    Slider _pourSlider;
    Text _pourLabel, _burnerLabel;
    Button _pourBtn, _burnerBtn;
    RectTransform _canvasRT;
    public const float DesignW = 1920f;
    public const float DesignH = 1080f;
    public int UiLayer = 31;             // UI 独立层，由专用相机渲染
    Font _font;
    Vessel _selected;
    public Vessel SelectedVessel => _selected;
    string _cat = "metal";
    string _query = "";
    InputField _searchField;   /* 给审计用：看输入框里到底有什么 */

    RectTransform _contentList;      // 试剂列表容器
    ReagentArtCatalog _reagentArt;
    RectTransform _vesselInfo;       // 容器内容物
    RectTransform _logContent;       // 记录
    Text _vesselTitle, _vesselVol, _gTemp, _gPh, _gFill;
    Image _phMarker;
    Text _hint;
    readonly List<(string id, string name, string formula, string state, char sol, string cls, string color)> _reagents
        = new List<(string, string, string, string, char, string, string)>();

    /* 配色 */
    static readonly Color BG      = new Color(0.055f, 0.075f, 0.10f, 0.92f);
    static readonly Color BG2     = new Color(0.085f, 0.11f, 0.145f, 0.95f);
    static readonly Color ACC     = new Color(0.37f, 0.78f, 1.00f);
    static readonly Color INK     = new Color(0.86f, 0.91f, 0.96f);
    static readonly Color DIM     = new Color(0.55f, 0.63f, 0.72f);
    static readonly Color WARN    = new Color(1.00f, 0.70f, 0.28f);
    static readonly Color LOGADD  = new Color(0.75f, 0.89f, 1.00f);
    static readonly Color LOGRXN  = new Color(1.00f, 0.90f, 0.62f);
    static readonly Color LOGOBS  = new Color(0.78f, 0.95f, 0.84f);

    static readonly string[] Cats = { "metal", "nonmetal", "oxide", "acid", "base", "salt", "organic", "indicator", "other" };
    static readonly string[] CatNames = { "金属", "非金属", "氧化物", "酸", "碱", "盐", "有机物", "指示剂", "其它" };

    static readonly Dictionary<string,string> ClsName = new() {
        {"metal","金属"},{"nonmetal","非金属"},{"oxide","氧化物"},{"acid","酸"},{"base","碱"},
        {"salt","盐"},{"organic","有机物"},{"indicator","指示剂"},{"other","其它"},
    };

    static void SetLayerRecursive(GameObject go, int layer) {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
    }

    public void Build() {
        Lab.EnsureReady();
        _font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 16);
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        UiFont = _font;   /* 标签复用同一个字体，中文字形才一致 */

        /* Canvas */
        var cgo = new GameObject("LabCanvas");
        cgo.transform.SetParent(transform, false);
        _canvas = cgo.AddComponent<Canvas>();
        _canvasRT=(RectTransform)_canvas.transform;
        /* 屏幕空间覆盖层：真正的 HUD，永远贴在屏幕上、永远在最前面，
           与相机怎么转、窗口多大都无关。
           ScreenCapture.CaptureScreenshot 会把它一起截进去。 */
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(DesignW, DesignH);
        sc.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null) {
            var ego = new GameObject("EventSystem");
            ego.transform.SetParent(transform, false);
            ego.AddComponent<EventSystem>();
            ego.AddComponent<StandaloneInputModule>();
        }

        BuildLeft();
        BuildRight();
        BuildBottom();
        BuildResponsiveControls();

        SetLayerRecursive(_canvas.gameObject, UiLayer);
        CollectReagents();
        RefreshReagents();
        BuildLangButton(_canvas.transform);
        BuildCalcButton(_canvas.transform);
        PrewarmFonts();
        RefreshVesselPanel();
        RefreshLog();
        ApplyResponsiveLayout();
    }

    /* ------------------------------------------------------------ 工具 */
    RectTransform Node(string name, Transform parent, Vector2 amin, Vector2 amax,
                       Vector2 offMin, Vector2 offMax, Color col, bool rounded = true) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        if (col.a > 0.001f) {
            var img = go.AddComponent<Image>();
            img.color = col;
        }
        return rt;
    }

    Text Label(Transform parent, string txt, int size, Color col, TextAnchor anchor,
               Vector2 amin, Vector2 amax, Vector2 offMin, Vector2 offMax) {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        var t = go.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.color = col;
        t.alignment = anchor; t.text = txt;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        /* 中英文字宽差很多，长单词必须能截断，否则会溢出盖住相邻控件 */
        t.verticalOverflow = VerticalWrapMode.Truncate;
        /* 必须是 Truncate：折行会让两行文字溢出控件高度，压到相邻控件上 */
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        RegisterLang(t);
        t.raycastTarget = false;
        return t;
    }

    /// <summary>按底色亮度选字色：浅底用深字，深底用浅字。
    /// 感知亮度用 0.299R+0.587G+0.114B —— 比直接平均更接近人眼。</summary>
    static Color InkOn(Color bg) {
        float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        return lum > 0.62f ? new Color(0.06f, 0.08f, 0.11f) : INK;
    }

    Button Btn(Transform parent, string txt, Action onClick, Color? bg = null, int size = 16, bool doseModal = false) {
        var go = new GameObject("Btn_" + txt);
        go.transform.SetParent(parent, false);
        /* 关键：必须显式给 RectTransform 设锚点。
           不设的话 Unity 用默认锚点（父节点中心）和 100x100 尺寸，
           所有按钮就全堆在一起互相压着 —— 之前的重叠就是这么来的。
           这里让它撑满调用方预先摆好的父矩形。 */
        var rtb = go.AddComponent<RectTransform>();
        rtb.anchorMin = Vector2.zero;
        rtb.anchorMax = Vector2.one;
        rtb.offsetMin = Vector2.zero;
        rtb.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bg ?? BG2;
        Button b;
        if(doseModal){var dose=go.AddComponent<DoseModalButton>();dose.Owner=this;b=dose;}
        else b=go.AddComponent<Button>();
        var cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1f,1f,1f,0.9f);
        cb.pressedColor = new Color(0.8f,0.85f,0.9f,1f); cb.selectedColor = Color.white;
        b.colors = cb;
        if (onClick != null) b.onClick.AddListener(() => onClick());
        var t = Label(go.transform, txt, size, InkOn(img.color), TextAnchor.MiddleCenter,
                      Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));
        t.raycastTarget = false;
        return b;
    }

    /* ------------------------------------------------------------ 左：器材 + 容器 */
    void BuildLeft() {
        var root = Node("LeftPanel", _canvas.transform, new Vector2(0,0), new Vector2(0,1),
                        new Vector2(18, 200), new Vector2(400, -30), BG);

        /* 器材按钮：两列 */
        var kinds = new (VesselKind k, string n)[] {
            (VesselKind.Beaker,"烧杯"), (VesselKind.Flask,"锥形瓶"),
            (VesselKind.Tube,"试管"),  (VesselKind.Cylinder,"量筒"),
            (VesselKind.RoundBottom,"圆底烧瓶"), (VesselKind.Burette,"滴定管"), (VesselKind.Pipette,"移液管"), (VesselKind.Dish,"蒸发皿"),
            (VesselKind.Crucible,"坩埚"), (VesselKind.Bottle,"试剂瓶"),
        };
        for (int i = 0; i < kinds.Length; i++) {
            int col = i % 2, row = i / 2;
            var go = Node("Ap_" + kinds[i].n, root, new Vector2(0,1), new Vector2(0,1),
                          Vector2.zero, Vector2.zero, BG2);
            var rt = (RectTransform)go;
            rt.pivot = new Vector2(0,1);
            /* 五行到 -218 为止；容器卡片从 -224 起，不能遮住最后一行。 */
            rt.anchoredPosition = new Vector2(16 + col * 186, -52 - row * 34);
            rt.sizeDelta = new Vector2(178, 30);
            var k = kinds[i].k;
            var vesselButton=Btn(rt, kinds[i].n+"  "+Vessel.NominalCapacity(k).ToString("0")+" mL", () => { var v = Lab.AddVessel(k); Select(v); CloseCompactDrawers(); });
            vesselButton.gameObject.name="Btn_"+kinds[i].n;
            var vesselButtonText = vesselButton.GetComponentInChildren<Text>();
            if (vesselButtonText != null) {
                vesselButtonText.resizeTextForBestFit = true;
                vesselButtonText.resizeTextMinSize = 8;
                vesselButtonText.resizeTextMaxSize = vesselButtonText.fontSize;
            }
        }

        /* 容器信息 */
        for(int i=0;i<2;i++){
            bool reflux=i==1;
            var slot=Node("Ap_"+(reflux?"RefluxKit":"DistillationKit"),root,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero,BG2);
            slot.pivot=new Vector2(0,1);slot.anchoredPosition=new Vector2(16+i*186,-222);slot.sizeDelta=new Vector2(178,30);
            Btn(slot,(reflux?"回流套件":"蒸馏套件")+" 250 mL",()=>{DistillationView.CreateKit(reflux);CloseCompactDrawers();},BG2,14);
        }
        float y = -258;
        var info = Node("VesselCard", root, new Vector2(0,1), new Vector2(1,1),
                        Vector2.zero, Vector2.zero, BG2);
        var irt = (RectTransform)info;
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0, y);
        irt.sizeDelta = new Vector2(-28, 620);   /* 620 才装得下所有区段，见下方分层 */

        _vesselTitle = Label(info, "未选择容器", 11, ACC, TextAnchor.UpperLeft,
                             new Vector2(0,1), new Vector2(1,1), new Vector2(12,-34), new Vector2(-12,-10));
        _vesselVol = Label(info, "", 11, DIM, TextAnchor.UpperRight,
                           new Vector2(0,1), new Vector2(1,1), new Vector2(12,-34), new Vector2(-12,-10));

        /* 温度 / pH / 液面 */
        string[] names = { "温度", "pH", "液面" };
        var vals = new Text[3];
        for (int i = 0; i < 3; i++) {
            float x0 = 12 + i * 88;
            var cell = Node("Cell" + i, info, new Vector2(0,1), new Vector2(0,1), Vector2.zero, Vector2.zero,
                            new Color(0.10f,0.14f,0.18f,0.9f));
            var crt = (RectTransform)cell; crt.pivot = new Vector2(0,1);
            crt.anchoredPosition = new Vector2(x0, -52);
            crt.sizeDelta = new Vector2(80, 46);
            Label(cell, names[i], 11, DIM, TextAnchor.UpperLeft,
                  new Vector2(0,1), new Vector2(1,1), new Vector2(7,-16), new Vector2(-5,-3));
            vals[i] = Label(cell, "—", 11, INK, TextAnchor.LowerLeft,
                            new Vector2(0,0), new Vector2(1,1), new Vector2(7,5), new Vector2(-5,-18));
        }
        _gTemp = vals[0]; _gPh = vals[1]; _gFill = vals[2];

        /* pH 色条 */
        var bar = Node("PhBar", info, new Vector2(0,1), new Vector2(1,1), Vector2.zero, Vector2.zero, Color.white);
        var brt = (RectTransform)bar; brt.pivot = new Vector2(0.5f,1f);
        brt.anchoredPosition = new Vector2(0, -106); brt.sizeDelta = new Vector2(-24, 8);
        bar.GetComponent<Image>().color = new Color(0.9f,0.9f,0.9f,0.15f);
        /* 用 7 段色块近似 pH 渐变（Node 传透明色就不会自带 Image 挡住） */
        var grad = Node("Grad", bar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        Color[] phc = { new Color(.88f,.23f,.23f), new Color(.91f,.63f,.10f), new Color(.91f,.88f,.10f),
                        new Color(.37f,1f,.62f), new Color(.23f,.85f,.78f), new Color(.23f,.54f,.91f),
                        new Color(.54f,.23f,.91f) };
        for (int i = 0; i < 7; i++) {
            var seg = Node("S"+i, grad, new Vector2(i/7f,0), new Vector2((i+1)/7f,1), Vector2.zero, Vector2.zero, phc[i]);
        }

        _phMarker = Node("PhMarker", bar, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                         Vector2.zero, Vector2.zero, Color.white).GetComponent<Image>();
        var mrt = _phMarker.rectTransform;
        mrt.sizeDelta = new Vector2(6, 16); mrt.anchoredPosition = Vector2.zero;

        /* 内容物 */
        Label(info, "内容物", 11, DIM, TextAnchor.UpperLeft,
              new Vector2(0,1), new Vector2(1,1), new Vector2(12,-140), new Vector2(-12,-122));
        _vesselInfo = Node("Contents", info, new Vector2(0,1), new Vector2(1,1),
                           new Vector2(12, 0), new Vector2(-12, 0), new Color(0,0,0,0));
        _vesselInfo.pivot = new Vector2(0.5f, 1f);
        _vesselInfo.anchoredPosition = new Vector2(0, -144);
        _vesselInfo.sizeDelta = new Vector2(0, 106);   /* 到顶部 250 为止，不再压下面的栏 */

        /* 操作按钮 */
        var acts = new (string n, Action f)[] {
            ("加热", () => { if (_selected != null) Lab.SetHeat(_selected, !_selected.Heating); }),
            ("搅拌", () => { if (_selected != null) Lab.SetStir(_selected, !_selected.Stirring); }),
            ("点燃", () => { if (_selected != null) { Lab.Ignite(_selected); Lab.RxnCount++; } }),
            ("倾倒→", Pour), ("倒空", () => { if (_selected != null) Lab.Empty(_selected); }),
            ("移走", RemoveSel),
        };
        for (int i = 0; i < acts.Length; i++) {
            int col = i % 3, row = i / 3;
            var go = Node("Act"+i, info, new Vector2(0,0), new Vector2(0,0), Vector2.zero, Vector2.zero, BG);
            var rt = (RectTransform)go; rt.pivot = new Vector2(0,0);
            rt.anchoredPosition = new Vector2(12 + col * 88, 14 + (1 - row) * 40);
            rt.sizeDelta = new Vector2(80, 34);
            var f = acts[i].f;
            Btn(rt, acts[i].n, f);
        }

        /* ---- 器材交互区：倾倒量 / 本生灯火力 ----
           这两条只在容器吸到别的容器或本生灯上时才出现 */
        _pourBar = Node("PourBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 100), new Vector2(-12, 126), new Color(0,0,0,0));
        _pourLabel = BarLabel(_pourBar, "倾倒量", 74, 0, 2, 12);
        _pourSlider = MakeSlider(_pourBar, "PourSlider", new Vector2(78, 4), new Vector2(150, 18), 0.50,
                                 v => { if (_selected != null) _selected.PourAmount = (float)v; });
        _pourBtn = Btn(_pourBar, "倒", Pour, ACC, 13);
        FixBtnRect(_pourBtn, new Vector2(236, 0), new Vector2(52, 24));

        _burnerBar = Node("BurnerBar", info, new Vector2(0,0), new Vector2(1,0),
                          new Vector2(12, 130), new Vector2(-12, 156), new Color(0,0,0,0));
        _burnerLabel = BarLabel(_burnerBar, "本生灯火力", 86, 0, 2, 12);
        var fireSettings=Btn(_burnerBar,"调节火力…",()=>ShowBurnerDialog(ApparatusSet.HeatingBurner(_selected)),BG2,13);
        FixBtnRect(fireSettings,new Vector2(100,0),new Vector2(120,24));
        _burnerBtn = Btn(_burnerBar, "点燃", ToggleBurner, WARN, 13);
        FixBtnRect(_burnerBtn, new Vector2(228, 0), new Vector2(60, 24));
        _pourBar.gameObject.SetActive(false);
        _burnerBar.gameObject.SetActive(false);

        /* ---- 离子检验器材区：石蕊试纸与木条 ---- */
        _testBar = Node("TestBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 160), new Vector2(-12, 186), new Color(0,0,0,0));
        /* 「检验器材」做成按钮：点一下切到下一种道具。
           原来 CurrentTest() 的注释写着「按顺序循环」，但 _testIdx 从来没被改过，
           永远停在 0 = 红色石蕊试纸 —— 木条根本切不到。 */
        {
            var tslot = Node("TestPick", _testBar, new Vector2(0,0), new Vector2(0,0),
                             Vector2.zero, Vector2.zero, new Color(0,0,0,0));
            var tr2 = (RectTransform)tslot.transform;
            tr2.pivot = new Vector2(0,0);
            tr2.anchoredPosition = new Vector2(4, 2);
            tr2.sizeDelta = new Vector2(70, 22);
            _testPick = Btn(tslot, "检验器材", CycleTest, BG2, 11);
            _testLabel = _testPick.GetComponentInChildren<Text>();
        }
        /* Btn(parent,…) 会创建一个**撑满父节点**的按钮，
           所以必须先建好摆正位置的小容器，再把按钮放进去；
           直接以 _testBar 作父节点会让两个按钮各撑满整条，渲染成大色块。 */
        {
            var slot1 = Node("TBtn1", _testBar, new Vector2(0,0), new Vector2(0,0),
                             Vector2.zero, Vector2.zero, new Color(0,0,0,0));
            var r1 = (RectTransform)slot1.transform;
            r1.pivot = new Vector2(0,0);
            r1.anchoredPosition = new Vector2(78, 2);
            r1.sizeDelta = new Vector2(68, 22);
            _testBtn1 = Btn(slot1, "湿润", TestAction1, ACC, 12);

            var slot2 = Node("TBtn2", _testBar, new Vector2(0,0), new Vector2(0,0),
                             Vector2.zero, Vector2.zero, new Color(0,0,0,0));
            var r2 = (RectTransform)slot2.transform;
            r2.pivot = new Vector2(0,0);
            r2.anchoredPosition = new Vector2(152, 2);
            r2.sizeDelta = new Vector2(68, 22);
            _testBtn2 = Btn(slot2, "伸入", TestAction2, WARN, 12);
        }
        _testBar.gameObject.SetActive(false);

        /* ---- 容器标签控制区 ---- */
        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 192), new Vector2(-12, 256), new Color(0,0,0,0));   /* 64 高，两行 */
        BarLabel(_tagBar, "容器标签", 74, 0, 36);
        MakeTagBtn("标签", () => { TagSettings.ShowTags = !TagSettings.ShowTags; }, 78, 36, 72);
        MakeTagBtn("具体/离子", () => { TagSettings.Molecular = !TagSettings.Molecular; }, 156, 36, 76);
        MakeTagBtn("详情", () => { TagSettings.ShowAllDetails = !TagSettings.ShowAllDetails; }, 234, 36, 72);
        MakeTagBtn("物质", () => { TagSettings.ShowName = !TagSettings.ShowName; }, 78, 8, 72);
        MakeTagBtn("mol", () => { TagSettings.ShowAmount = !TagSettings.ShowAmount; }, 156, 8, 72);
        MakeTagBtn("浓度", () => { TagSettings.ShowConc = !TagSettings.ShowConc; }, 234, 8, 72);
        _tagBar.gameObject.SetActive(false);

        /* ---- 反应方程式标签控制区 ---- */
        _eqBar = Node("EqBar", info, new Vector2(0,0), new Vector2(1,0),
                      new Vector2(12, 264), new Vector2(-12, 364), new Color(0,0,0,0));   /* 100 高，三行 */
        BarLabel(_eqBar, "方程式标签", 74, 0, 70);
        _eqShow  = MakeEqBtn("显示 开", () => { EqTagSettings.Show = !EqTagSettings.Show; }, 78, 70, 74);
        _eqIonic = MakeEqBtn("离子式 关", () => { EqTagSettings.Ionic = !EqTagSettings.Ionic; }, 78, 40, 74);
        /* 时长：数字输入框（按要求放在界面上，不放进标签里） */
        BarLabel(_eqBar, "时长", 24, 156, 70);
        _eqDur = MakeNumField(_eqBar, new Vector2(182, 68), new Vector2(66, 26), EqTagSettings.Duration,
                              v => { EqTagSettings.Duration = Mathf.Clamp(v, 1f, 120f); });
        BarLabel(_eqBar, "秒", 20, 252, 70);
        _eqBar.gameObject.SetActive(false);
    }

    /* ------------------------------------------------------------ 栏内小标签
       之前这里踩了两个坑，导致文字跑到卡片外面：
       ① 用纵向拉伸锚点（anchorMin.y=0, anchorMax.y=1）再设 sizeDelta.y=26，
          那是在拉伸出来的高度上**又加** 26，高度就失控了；
       ② pivot 是中心、锚在栏的左边缘，矩形会向左凸出一半。
       统一用这个函数：锚点与 pivot 都是 (0, 0.5)，尺寸就是真实尺寸。 */
    /// <summary>Btn 建出来的按钮是「撑满父节点」的（锚点 0..1），
    /// 直接改 sizeDelta 不会得到想要的尺寸。要自由摆放必须先断开拉伸。</summary>
    static void FixBtnRect(Button b, Vector2 pos, Vector2 size) {
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    Text BarLabel(Transform parent, string text, float width, float x, float y, int size = 11) {
        var t = Label(parent, text, size, DIM, TextAnchor.MiddleLeft,
                      new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
        var rt = (RectTransform)t.transform;
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 22);
        return t;
    }

    /* ---------------- 滑动条 ---------------- */
    Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size, double val, Action<double> onChange) {
        var go = Node(name, parent, new Vector2(0,0), new Vector2(0,0), Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var rt = (RectTransform)go.transform;
        rt.pivot = new Vector2(0,0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var sl = go.AddComponent<Slider>();

        var bg = Node("BG", go.transform, new Vector2(0,0), new Vector2(1,1),
                      new Vector2(0, size.y*0.34f), new Vector2(0, -size.y*0.34f), BG2);
        var fillArea = Node("FillArea", go.transform, new Vector2(0,0), new Vector2(1,1),
                            new Vector2(2, size.y*0.36f), new Vector2(-2, -size.y*0.36f), new Color(0,0,0,0));
        var fill = Node("Fill", fillArea, new Vector2(0,0), new Vector2(0,1),
                        Vector2.zero, new Vector2(0,0), ACC);
        var handleArea = Node("HandleArea", go.transform, new Vector2(0,0), new Vector2(1,1),
                              new Vector2(6, 0), new Vector2(-6, 0), new Color(0,0,0,0));
        var handle = Node("Handle", handleArea, new Vector2(0,0.5f), new Vector2(0,0.5f),
                          Vector2.zero, Vector2.zero, INK);
        ((RectTransform)handle.transform).sizeDelta = new Vector2(10, size.y);

        sl.fillRect = (RectTransform)fill.transform;
        sl.handleRect = (RectTransform)handle.transform;
        sl.targetGraphic = handle.GetComponent<Image>();
        sl.direction = Slider.Direction.LeftToRight;
        sl.minValue = 0; sl.maxValue = 1;
        sl.value = (float)val;
        sl.onValueChanged.AddListener(v => onChange(v));
        return sl;
    }

    /// <summary>当前选中的检验器材（按顺序循环）</summary>
    TestItem CurrentTest() {
        if (TestSet.All.Count == 0) return null;
        _testIdx = ((_testIdx % TestSet.All.Count) + TestSet.All.Count) % TestSet.All.Count;
        return TestSet.All[_testIdx];
    }
    int _testIdx;

    Button MakeEqBtn(string label, System.Action act, float x, float y, float w) {
        var slot = Node("EQ_" + label, _eqBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(x, y);        /* x 是栏内绝对坐标 */
        r.sizeDelta = new Vector2(w, 24);
        return Btn(slot, label, act, BG2, 11);
    }

    /// <summary>数字输入框 —— 显示时长用。InputField 要有背景图和一个文本子节点。</summary>
    InputField MakeNumField(Transform parent, Vector2 pos, Vector2 size, float initial, System.Action<float> onChange) {
        var go = new GameObject("NumField");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,0); rt.anchorMax = new Vector2(0,0);
        rt.pivot = new Vector2(0,0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.16f, 0.24f, 1f);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = _font; txt.fontSize = 12;
        txt.color = INK;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.supportRichText = false;
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(3,0); trt.offsetMax = new Vector2(-3,0);

        var f = go.AddComponent<InputField>();
        f.targetGraphic = img;
        f.textComponent = txt;
        f.contentType = InputField.ContentType.DecimalNumber;
        f.characterLimit = 5;
        f.text = initial.ToString("0.#");
        f.onEndEdit.AddListener(s => {
            float v;
            if (float.TryParse(s, out v)) { onChange(v); f.text = EqTagSettings.Duration.ToString("0.#"); }
            else f.text = EqTagSettings.Duration.ToString("0.#");
        });
        return f;
    }

    /// <summary>标签区用的小按钮：Btn 会撑满父节点，所以先摆好小容器</summary>
    Button MakeTagBtn(string label, System.Action act, float x, float y, float w = 72) {
        var slot = Node("TB_" + label, _tagBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(x, y);        /* x 是栏内绝对坐标 */
        r.sizeDelta = new Vector2(w, 22);
        return Btn(slot, label, act, BG2, 11);
    }

    /// <summary>按钮文字随开关状态变，让用户看得出当前是什么</summary>
    void RefreshTagButtons() {
        if (_tagBar == null) return;
        void Set(Button b, string txt) {
            if (b == null) return;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = txt;
        }
        int k = 0;
        var kids = _tagBar.GetComponentsInChildren<Button>();
        foreach (var b in kids) {
            switch (k++) {
                case 0: Set(b, TagSettings.ShowTags ? "标签 开" : "标签 关"); break;
                case 1: Set(b, TagSettings.Molecular ? "具体物质" : "离子状态"); break;
                case 2: Set(b, TagSettings.ShowAllDetails ? "全部详情" : "选中详情"); break;
                case 3: Set(b, TagSettings.ShowName ? "物质 开" : "物质 关"); break;
                case 4: Set(b, TagSettings.ShowAmount ? "mol 开" : "mol 关"); break;
                case 5: Set(b, TagSettings.ShowConc ? "浓度 开" : "浓度 关"); break;
            }
        }
    }

    Button _testPick;
    void CycleTest() {
        if (TestSet.All.Count == 0) return;
        _testIdx = (_testIdx + 1) % TestSet.All.Count;
        RefreshVesselPanel();
    }

    /// <summary>第一个按钮随器材类型变化：石蕊是「湿润」，木条是「点燃/熄灭」</summary>
    void TestAction1() {
        var t = CurrentTest();
        if (t == null || _selected == null) return;
        if (t.Kind == TestItemKind.Splint) {
            if (t.Splint == SplintState.Burning) GasTest.Extinguish(t);
            else GasTest.Light(t);
        } else {
            GasTest.Moisten(t);
        }
        RefreshVesselPanel(); RefreshLog();
    }

    /// <summary>第二个按钮：把器材伸到当前容器口</summary>
    void TestAction2() {
        var t = CurrentTest();
        if (t == null || _selected == null) return;
        GasTest.Insert(_selected, t);
        Lab.RxnCount++;
        RefreshVesselPanel(); RefreshLog();
    }

    void ToggleBurner() {
        var a = ApparatusSet.HeatingBurner(_selected);
        if (a == null) return;
        if (a.Lit) { a.Lit = false; Lab.Note(_selected, "熄灭本生灯", "setup"); return; }
        if (!a.CanLight) { Lab.Note(_selected, "出气量太小，点不着", "setup"); return; }
        a.Lit = true;
        Lab.Note(_selected, $"点燃本生灯（火力 {a.GasFlow * 100:F0}%）", "setup");
    }

    /// <summary>顶端「焓变计算器」入口</summary>
    void BuildCalcButton(Transform parent) {
        var go = Node("CalcBtn", parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero, new Color(0.16f, 0.42f, 0.32f));
        var rt = (RectTransform)go.transform;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(310, -12);
        rt.sizeDelta = new Vector2(130, 30);
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        b.onClick.AddListener(() => OpenCalculator());
        Label(go.transform, "焓变计算器", 14, Color.white, TextAnchor.MiddleCenter,
              Vector2.zero, Vector2.one, new Vector2(2, 0), new Vector2(-2, 0));
    }

    void OpenCalculator() {
        var host = LabBootstrap.Instance != null ? LabBootstrap.Instance.gameObject : gameObject;
        EnthalpyScene.Show(host, _font);
    }
    /* ================================================================
       测试接口（只给自动化自检用，游戏逻辑不碰）
         靠读代码猜「按钮为什么点不动」不可靠。有了这些接口，
         测试才能真的点按钮、真的做射线检测、真的比矩形。
       ================================================================ */
    /// <summary>建一个只用来测试的界面实例。
    /// 必须配 CanvasScaler 与参考分辨率 —— 否则画布尺寸是默认值，
    /// 控件世界坐标全错，射线测试毫无意义。</summary>
    static UnityEngine.EventSystems.EventSystem _testES;
    static UnityEngine.EventSystems.EventSystem EnsureTestES() {
        if (_testES != null) return _testES;
        _testES = UnityEngine.EventSystems.EventSystem.current;
        if (_testES == null) {
            var ego = new GameObject("EventSystem_Test");
            _testES = ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
            ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return _testES;
    }

    public static LabUI CreateForTest() {
        if (LabUI.UiFont == null) LabUI.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureTestES();
        var go = new GameObject("LabUI_Test");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(DesignW, DesignH);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var ui = go.AddComponent<LabUI>();
        ui.UiLayer = 31;
        ui.Build();
        Canvas.ForceUpdateCanvases();
        return ui;
    }

    public int TestReagentTotal => _reagents != null ? _reagents.Count : -1;
    public List<string> TestListButtons() => TestAllButtons();
    /// <summary>把左栏所有控件的位置打出来（世界坐标 = 屏幕坐标）。
    /// 靠肉眼看截图找排版问题太慢，让它自己报。 </summary>
    public string TestDumpLayout() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        var panel = _canvas.transform.Find("LeftPanel") as RectTransform;
        if (panel == null) return "[Layout] 找不到 LeftPanel";
        var p0 = panel.TransformPoint(panel.rect.min);
        var p1 = panel.TransformPoint(panel.rect.max);
        float px0 = Mathf.Min(p0.x, p1.x), px1 = Mathf.Max(p0.x, p1.x);
        float py0 = Mathf.Min(p0.y, p1.y), py1 = Mathf.Max(p0.y, p1.y);
        sb.AppendLine("[Layout] 面板 x " + px0.ToString("F0") + ".." + px1.ToString("F0")
                    + "  y " + py0.ToString("F0") + ".." + py1.ToString("F0"));
        foreach (var g in panel.GetComponentsInChildren<Graphic>(true)) {
            if (g == null) continue;
            var t = g as Text;
            string label = t != null ? t.text : "";
            if (string.IsNullOrEmpty(label)) continue;
            if (label.Length > 12) label = label.Substring(0, 12) + "…";
            var rt = g.rectTransform;
            if (rt == null) continue;
            var a2 = rt.TransformPoint(rt.rect.min);
            var b2 = rt.TransformPoint(rt.rect.max);
            float x0 = Mathf.Min(a2.x, b2.x), x1 = Mathf.Max(a2.x, b2.x);
            float y0 = Mathf.Min(a2.y, b2.y), y1 = Mathf.Max(a2.y, b2.y);
            /* 越界的标出来 */
            bool outside = x0 < px0 - 1 || x1 > px1 + 1 || y0 < py0 - 1 || y1 > py1 + 1;
            sb.AppendLine("[Layout] " + (outside ? "✗越界 " : "      ")
                + Path(g.transform).Replace("LeftPanel/", "") + "  「" + label + "」"
                + " x " + x0.ToString("F0") + ".." + x1.ToString("F0")
                + " y " + y0.ToString("F0") + ".." + y1.ToString("F0"));
        }
        return sb.ToString();
    }

    /// <summary>把两个工具栏的直接子节点连同 anchoredPosition / pivot / rect 打出来。
    /// 光看屏幕坐标对不上号，这样能看到真实的摆放参数。</summary>
    public string TestDumpBars() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        foreach (var barName in new[] { "VesselCard/TagBar", "VesselCard/EqBar" }) {
            var bar = _canvas.transform.Find("LeftPanel/" + barName) as RectTransform;
            if (bar == null) { sb.AppendLine("[Bars] 找不到 " + barName); continue; }
            sb.AppendLine("[Bars] " + barName + "  anchorMin=" + bar.anchorMin + " anchorMax=" + bar.anchorMax
                        + " pivot=" + bar.pivot + " rect=" + bar.rect + " anchoredPos=" + bar.anchoredPosition);
            foreach (var ch in bar.GetComponentsInChildren<RectTransform>(true)) {
                if (ch.parent != bar) continue;
                sb.AppendLine("[Bars]    " + ch.name + "  pivot=" + ch.pivot
                            + "  anchoredPos=" + ch.anchoredPosition
                            + "  sizeDelta=" + ch.sizeDelta
                            + "  rect=" + ch.rect);
            }
        }
        return sb.ToString();
    }

    /// <summary>左栏里所有「可点图形」的世界矩形。
    /// 用来看器材架按钮有没有被容器卡片盖住之类的问题。 </summary>
    public List<(string name, Rect2 r)> TestPanelClickables() {
        var res = new List<(string, Rect2)>();
        var panel = _canvas.transform.Find("LeftPanel") as RectTransform;
        if (panel == null) return res;
        Canvas.ForceUpdateCanvases();
        foreach (var g in panel.GetComponentsInChildren<Graphic>(true)) {
            if (g == null || !g.raycastTarget) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            /* pH 色条是「渐变分段 + 游标」，本来就该叠在一起，不算冲突 */
            if (Path(g.transform).IndexOf("/PhBar/") >= 0) continue;
            var rt = g.rectTransform;
            if (rt == null) continue;
            var a = rt.TransformPoint(rt.rect.min);
            var b = rt.TransformPoint(rt.rect.max);
            res.Add((Path(g.transform), new Rect2 {
                xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x),
                yMin = Mathf.Min(a.y, b.y), yMax = Mathf.Max(a.y, b.y),
            }));
        }
        return res;
    }

    /// <summary>把关键控件的中心点按「游戏内屏幕坐标」打出来，供外部自动化点击。
    /// 这样测试脚本就不用写死比例坐标 —— 分辨率一变比例布局就不一样了。
    /// 输出格式：CLICK|名字|x|y</summary>
    public string TestClickTargets() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SCREEN|" + Screen.width + "|" + Screen.height);
        void Emit(string name, RectTransform rt) {
            if (rt == null) return;
            var c = rt.TransformPoint(rt.rect.center);
            sb.AppendLine("CLICK|" + name + "|" + Mathf.RoundToInt(c.x) + "|" + Mathf.RoundToInt(c.y));
        }
        var panel = _canvas.transform.Find("LeftPanel");
        if (panel != null) {
            foreach (var rt in panel.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name.StartsWith("Ap_")) Emit("vessel_" + rt.name.Substring(3), rt);
            }
        }
        var right = _canvas.transform.Find("RightPanel");
        if (right != null) {
            foreach (var rt in right.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name.StartsWith("Cat")) {
                    var txt = rt.GetComponentInChildren<Text>();
                    if (txt != null) Emit("cat_" + txt.text, rt);
                }
                if (rt.name == "Search") Emit("search", rt);
                if (rt.name.StartsWith("R_"))
                    Emit("reagent_" + rt.name.Substring(2), rt);
                if (rt.name.StartsWith("Pr") && rt.name.Length == 3)
                    Emit("pr_" + rt.name.Substring(2) + "_" + (rt.GetComponentInChildren<Text>() != null
                         ? rt.GetComponentInChildren<Text>().text : "?"), rt);
            }
        }
        foreach (var rt in _canvas.GetComponentsInChildren<RectTransform>(true)) {
            if (rt.name == "CalcBtn") Emit("opencalc", rt);
        }
        return sb.ToString();
    }
    public string ReagentAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("REAGENT|cat|" + _cat);
        sb.AppendLine("REAGENT|query|" + (_query != null ? _query : ""));
        sb.AppendLine("REAGENT|queryField|" + (_searchField != null ? _searchField.text : "(没存引用)"));
        sb.AppendLine("REAGENT|total|" + _reagents.Count);
        int shown = 0;
        if (_contentList != null) {
            for (int i = 0; i < _contentList.childCount; i++) {
                var ch = _contentList.GetChild(i);
                if (ch != null && ch.gameObject.activeSelf) shown++;
            }
        }
        sb.AppendLine("REAGENT|visible|" + shown);
        return sb.ToString();
    }

    /// <summary>诊断：搜索框那里到底有什么在挡着。</summary>
    public string ProbeSearch() {
        var sb = new System.Text.StringBuilder();
        if (_searchField == null) { sb.AppendLine("PROBE|搜索框引用为空"); return sb.ToString(); }
        var srt = (RectTransform)_searchField.transform;
        var center = RectTransformUtility.WorldToScreenPoint(null, srt.TransformPoint(srt.rect.center));
        sb.AppendLine("PROBE|搜索框中心屏幕坐标|" + Mathf.RoundToInt(center.x) + "," + Mathf.RoundToInt(center.y));
        sb.AppendLine("PROBE|搜索框世界矩形|" + srt.rect);
        sb.AppendLine("PROBE|raycastTarget 情况:");
        foreach (var g in srt.GetComponentsInChildren<Graphic>(true)) {
            sb.AppendLine("PROBE|   " + g.gameObject.name + " (" + g.GetType().Name + ") raycast=" + g.raycastTarget
                        + " active=" + g.gameObject.activeInHierarchy);
        }
        /* 用 EventSystem 的射线看看这个点上命中了谁 */
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null) {
            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = center;
            var res = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
                var gr = cv.GetComponent<GraphicRaycaster>();
                if (gr == null || !gr.enabled || !cv.gameObject.activeInHierarchy) continue;
                gr.Raycast(ped, res);
            }
            /* 焦点有没有切过去 —— InputField 要靠这个才收键盘 */
            var sel = es.currentSelectedGameObject;
            sb.AppendLine("PROBE|当前选中对象|" + (sel == null ? "(无)" : sel.name));
            sb.AppendLine("PROBE|输入模块|" + (es.currentInputModule == null ? "(无！)" : es.currentInputModule.GetType().Name));
            sb.AppendLine("PROBE|isFocused(输入框)|" + _searchField.isFocused);
            sb.AppendLine("PROBE|Input.inputString 可用|" + (Input.inputString != null ? "yes" : "no"));
            if (es.currentInputModule is UnityEngine.EventSystems.StandaloneInputModule sim) {
                sb.AppendLine("PROBE|Standalone 轴向|" + sim.horizontalAxis + "/" + sim.verticalAxis
                            + " 提交=" + sim.submitButton + " 取消=" + sim.cancelButton);
            }
            sb.AppendLine("PROBE|该点射线命中 " + res.Count + " 个:");
            for (int i = 0; i < Mathf.Min(6, res.Count); i++)
                sb.AppendLine("PROBE|   " + i + ": " + res[i].gameObject.name
                            + " 画布=" + (res[i].module is GraphicRaycaster ? "有" : "?")
                            + " 深度=" + res[i].depth + " 排序=" + res[i].sortingOrder);
        }
        return sb.ToString();
    }

    public string TestCat => _cat;
    public string TestQuery => _query;
    public string TestAmount => "drag-and-drop";
    public int TestVisibleCount => _contentList != null ? _contentList.childCount : -1;

    /// <summary>按过滤条件算出「会列出多少条」，不依赖 childCount
    /// （Destroy 是帧末执行的，刚刷完就数 childCount 会数到旧行）</summary>
    public int TestFilterCount(string cat, string query) {
        IEnumerable<(string id, string name, string formula, string state, char sol, string cls, string color)> list;
        if (!string.IsNullOrEmpty(query)) {
            var q = query.ToLowerInvariant();
            list = _reagents.Where(r => r.name.ToLowerInvariant().Contains(q)
                                     || r.formula.ToLowerInvariant().Contains(q)
                                     || r.id.Contains(q));
        } else {
            list = _reagents.Where(r => r.cls == cat);
        }
        return list.Count();
    }

    /// <summary>真的触发某个按钮的 onClick</summary>
    public bool TestClickButton(string namePart) {
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) {
            if (b.gameObject.name.Contains(namePart)) { b.onClick.Invoke(); return true; }
        }
        return false;
    }

    public List<string> TestAllButtons() {
        var res = new List<string>();
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) res.Add(b.gameObject.name);
        return res;
    }

    /// <summary>几何遮挡检测：按钮中心有没有被别的可点图形盖住。
    /// 不用 GraphicRaycaster —— 无头编辑器里它依赖画布/屏幕尺寸，结果不可靠。
    /// 判据：所有 raycastTarget 为真的图形里，矩形包含按钮中心、
    ///       且在兄弟顺序上排在按钮之后的（后画的在上面）就算遮挡。 </summary>
    public string TestOcclusionAt(string buttonNamePart) {
        Button target = null;
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == buttonNamePart) { target = b; break; }
        if (target == null) return "✗ 找不到按钮 " + buttonNamePart;

        Canvas.ForceUpdateCanvases();
        var trt = (RectTransform)target.transform;
        Vector3 c = trt.TransformPoint(trt.rect.center);

        var hits = new List<string>();
        foreach (var g in _canvas.GetComponentsInChildren<Graphic>(true)) {
            if (g == null || !g.raycastTarget) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            if (g.transform == target.transform || g.transform.IsChildOf(target.transform)) continue;
            if (target.transform.IsChildOf(g.transform)) continue;      // 自己的父节点不算
            var rt = g.rectTransform;
            if (rt == null) continue;
            Vector3 a = rt.TransformPoint(rt.rect.min), b2 = rt.TransformPoint(rt.rect.max);
            bool inside = c.x >= Mathf.Min(a.x, b2.x) && c.x <= Mathf.Max(a.x, b2.x)
                       && c.y >= Mathf.Min(a.y, b2.y) && c.y <= Mathf.Max(a.y, b2.y);
            if (!inside) continue;
            /* 后画的在上面：比较在画布下的渲染顺序 */
            if (RenderOrder(g.transform) > RenderOrder(target.transform))
                hits.Add(g.gameObject.name + "(" + Path(g.transform) + ")");
        }
        return hits.Count == 0
            ? "✓ 按钮「" + target.name + "」没有被遮挡"
            : "✗ 按钮「" + target.name + "」被 " + string.Join(" / ", hits) + " 盖住";
    }
    static int RenderOrder(Transform t) {
        int ord = 0, mul = 1;
        while (t != null) { ord += t.GetSiblingIndex() * mul; mul *= 1000; t = t.parent; }
        return ord;
    }

    static string Path(Transform t) {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    /// <summary>卡片内各区段的矩形（**世界坐标**）。
    /// 不能直接比 RectTransform.rect —— 那是各自局部坐标，只有换算到同一空间才有可比性。</summary>
    public List<(string name, Rect2 r)> TestCardRects() {
        var res = new List<(string, Rect2)>();
        var card = _canvas.transform.Find("LeftPanel/VesselCard") as RectTransform;
        if (card == null) return res;
        var list = new List<RectTransform>();
        string[] want = { "Contents", "PourBar", "BurnerBar", "TestBar", "TagBar", "EqBar" };
        foreach (var w in want) {
            var t = card.Find(w) as RectTransform;
            if (t != null && t.gameObject.activeSelf) list.Add(t);
        }
        for (int i = 0; i < 6; i++) {
            var t = card.Find("Act" + i) as RectTransform;
            if (t != null) list.Add(t);
        }
        foreach (var t in list) {
            var a = t.TransformPoint(t.rect.min);
            var b = t.TransformPoint(t.rect.max);
            res.Add((t.name, new Rect2 {
                xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x),
                yMin = Mathf.Min(a.y, b.y), yMax = Mathf.Max(a.y, b.y),
            }));
        }
        return res;
    }
    public struct Rect2 { public float xMin, xMax, yMin, yMax; }

    /// <summary>直接设过滤器并刷新（绕开输入框，单测过滤逻辑）</summary>
    public void TestSetFilter(string cat, string query) {
        _cat = cat; _query = query ?? "";
        RefreshReagents();
    }

    /* ------------------------------------------------------------ 右：试剂架 */
    /// <summary>右上角的语言切换按钮</summary>
    void BuildLangButton(Transform parent) {
        var go = Node("LangBtn", parent, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero, ACC);
        var rt = (RectTransform)go.transform;
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-14, -12);
        rt.sizeDelta = new Vector2(72, 30);
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        var lbl = Label(go.transform, "EN", 15, new Color(0.05f, 0.07f, 0.10f), TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(2, 0), new Vector2(-2, 0));
        var lblRT = (RectTransform)lbl.transform;
        lblRT.pivot = new Vector2(0.5f, 0.5f);
        b.onClick.AddListener(() => {
            Lang.Toggle();
            ApplyLang();
            RefreshReagents();
            if (Lab3D != null) Lab3D.SyncViews();
            lbl.text = Localization.English ? "中" : "EN";
        });
        lbl.text = Localization.English ? "中" : "EN";
    }

    void BuildRight() {
        var root = Node("RightPanel", _canvas.transform, new Vector2(1,0), new Vector2(1,1),
                        new Vector2(-336, 200), new Vector2(-18, -30), BG);

        Label(root, "试剂架", 17, ACC, TextAnchor.UpperLeft,
              new Vector2(0,1), new Vector2(1,1), new Vector2(14,-38), new Vector2(-14,-12));
        Label(root, "拖入容器添加", 12, DIM, TextAnchor.UpperRight,
              new Vector2(0,1), new Vector2(1,1), new Vector2(14,-36), new Vector2(-14,-14));

        /* 搜索框 */
        var sf = Node("Search", root, new Vector2(0,1), new Vector2(1,1), Vector2.zero, Vector2.zero, BG2);
        var srt = (RectTransform)sf; srt.pivot = new Vector2(0.5f,1f);
        srt.anchoredPosition = new Vector2(0, -46); srt.sizeDelta = new Vector2(-24, 34);
        var inp = sf.gameObject.AddComponent<InputField>();
        var ph = Label(sf, "搜索名称 / 化学式…", 11, DIM, TextAnchor.MiddleLeft,
                       Vector2.zero, Vector2.one, new Vector2(10,0), new Vector2(-10,0));
        var txt = Label(sf, "", 11, INK, TextAnchor.MiddleLeft,
                        Vector2.zero, Vector2.one, new Vector2(10,0), new Vector2(-10,0));
        /* 这两个 Text 必须关掉 raycastTarget —— Label() 默认是 true，
           它们铺满输入框、盖在 targetGraphic 上面，
           点击全被 Text 吃掉，输入框拿不到焦点、onValueChanged 不触发。 */
        txt.raycastTarget = false;
        ph.raycastTarget = false;
        inp.textComponent = txt; inp.placeholder = ph;
        inp.targetGraphic = sf.GetComponent<Image>();
        _searchField = inp;
        inp.onValueChanged.AddListener(s => {
            Debug.Log("[SearchDbg] onValueChanged 触发，文本=" + s);
            _query = s.Trim(); RefreshReagents();
        });

        /* 分类标签 */
        for (int i = 0; i < Cats.Length; i++) {
            int col = i % 5, row = i / 5;
            var go = Node("Cat"+i, root, new Vector2(0,1), new Vector2(0,1), Vector2.zero, Vector2.zero, BG2);
            var rt = (RectTransform)go; rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(12 + col * 60, -96 - row * 38); rt.sizeDelta = new Vector2(56, 30);
            var c = Cats[i];
            var catButton = Btn(rt, CatNames[i], () => { _cat = c; _query = ""; if (inp != null) inp.text = ""; RefreshReagents(); }, null, 12);
            var catText = catButton.GetComponentInChildren<Text>();
            if (catText != null) { catText.resizeTextForBestFit = true; catText.resizeTextMinSize = 8; catText.resizeTextMaxSize = 12; }
        }

        /* 试剂滚动列表 */
        var sv = Node("Scroll", root, new Vector2(0,0), new Vector2(1,1),
                      new Vector2(12, 12), new Vector2(-12, -180), new Color(0,0,0,0.001f));
        var scroll = sv.gameObject.AddComponent<ScrollRect>();
        sv.gameObject.AddComponent<RectMask2D>();
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 26f;
        var content = new GameObject("Content");
        content.transform.SetParent(sv, false);
        _contentList = content.AddComponent<RectTransform>();
        _contentList.anchorMin = new Vector2(0,1); _contentList.anchorMax = new Vector2(1,1);
        _contentList.pivot = new Vector2(0.5f,1f);
        _contentList.offsetMin = new Vector2(0,0); _contentList.offsetMax = new Vector2(0,0);
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;
        grid.cellSize=new Vector2(94,164);grid.spacing=new Vector2(6,8);
        grid.childAlignment=TextAnchor.UpperLeft;
        var fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _contentList; scroll.viewport = sv;
    }

    /* ------------------------------------------------------------ 底：记录 */
    void BuildBottom() {
        var root = Node("BottomPanel", _canvas.transform, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(18, 14), new Vector2(-18, 186), BG);
        Label(root, "实验记录", 12, ACC, TextAnchor.UpperLeft,
              new Vector2(0,1), new Vector2(0,1), new Vector2(14,-30), new Vector2(140,-10));
        _hint = Label(root, "左侧选器材 → 从右侧拖试剂到容器 → 输入数量", 11, DIM, TextAnchor.UpperLeft,
                      new Vector2(0,1), new Vector2(1,1), new Vector2(150,-29), new Vector2(-150,-11));
        var clr = Node("ClearLog", root, new Vector2(1,1), new Vector2(1,1), Vector2.zero, Vector2.zero, BG2);
        var crt = (RectTransform)clr; crt.pivot = new Vector2(1,1);
        crt.anchoredPosition = new Vector2(-12, -8); crt.sizeDelta = new Vector2(76, 26);
        Btn(crt, "清空记录", () => { Lab.Log.Clear(); Lab.RxnCount = 0; RefreshLog(); }, null, 12);

        var sv = Node("LogScroll", root, new Vector2(0,0), new Vector2(1,1),
                      new Vector2(14, 8), new Vector2(-14, -38), new Color(0,0,0,0.001f));
        var scroll = sv.gameObject.AddComponent<ScrollRect>();
        sv.gameObject.AddComponent<RectMask2D>();
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 26f;
        var content = new GameObject("Content");
        content.transform.SetParent(sv, false);
        _logContent = content.AddComponent<RectTransform>();
        _logContent.anchorMin = new Vector2(0,1); _logContent.anchorMax = new Vector2(1,1);
        _logContent.pivot = new Vector2(0.5f,1f);
        _logContent.offsetMin = Vector2.zero; _logContent.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.spacing = 1;
        var fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _logContent; scroll.viewport = sv;
    }

    /* ------------------------------------------------------------ 试剂 */
    void CollectReagents() {
        _reagents.Clear();
        foreach (var kv in ChemData.Sub) {
            var s = kv.Value;
            if (s.IsIon || s.Cls=="internal_phase") continue;
            if (string.IsNullOrEmpty(s.Name) || s.Name == s.Id) continue;
            if (s.State == "aq" && s.Cls == "ion") continue;
            string color = !string.IsNullOrEmpty(s.SolidColor) ? s.SolidColor : s.SolColor;
            if(s.Formula.Contains("R")||s.Id=="nascent_o")continue;
            _reagents.Add((s.Id, PhaseProperties.ShelfName(s.Id), s.Formula, PhaseProperties.Phase(s.Id), s.Sol, s.Cls, color));
        }
    }

    void RefreshReagents() {
        if (_contentList == null) return;
        for (int i = _contentList.childCount - 1; i >= 0; i--) {
            var oldRow=_contentList.GetChild(i).gameObject;
            oldRow.SetActive(false);
            UnityEngine.Object.Destroy(oldRow);
        }

        IEnumerable<(string id, string name, string formula, string state, char sol, string cls, string color)> list;
        if (!string.IsNullOrEmpty(_query)) {
            var q = _query.ToLowerInvariant();
            list = _reagents.Where(r => r.name.ToLowerInvariant().Contains(q)
                                     || r.formula.ToLowerInvariant().Contains(q)
                                     || r.id.Contains(q));
        } else {
            list = _reagents.Where(r => r.cls == _cat);
        }
        var arr = list.OrderBy(r => r.name, StringComparer.Ordinal).ToArray();
        if(_reagentArt==null)_reagentArt=Resources.Load<ReagentArtCatalog>("ReagentArtCatalog");
        /* EN 按钮只在 Build 时建一次 —— 这里再建会不断叠加 */
        PrewarmFonts();

        foreach (var r in arr) {
            var row = new GameObject("R_" + r.id);
            row.transform.SetParent(_contentList, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(94,164);
            var rimg = row.AddComponent<Image>();
            rimg.color = new Color(0.10f, 0.135f, 0.175f, 0.85f);
            var btn = row.AddComponent<Button>();
            var cb = btn.colors; cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.55f, 0.75f, 1f, 1f);
            cb.pressedColor = new Color(0.4f, 0.6f, 0.85f, 1f);
            btn.colors = cb;
            var id = r.id; var st = r.state; var sol = r.sol;
            var drag=row.AddComponent<ReagentDrag>();drag.Owner=this;drag.SubstanceId=id;
            btn.onClick.AddListener(() => Hint("请将试剂拖入目标容器"));

            var photo=Node("ReagentImage",row.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-41,-88),new Vector2(41,-6),Color.white);
            var photoImage=photo.GetComponent<Image>();photoImage.raycastTarget=false;
            var sprite=_reagentArt!=null?_reagentArt.Find(id):null;
            if(sprite!=null){photoImage.sprite=sprite;photoImage.preserveAspect=true;}
            else{
                photoImage.color=new Color(.17f,.21f,.26f,1);
                var missing=Label(photo,"暂无图片",11,new Color(.78f,.84f,.91f),TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,new Vector2(3,3),new Vector2(-3,-3));
                missing.raycastTarget=false;
            }
            var nm = Label(row.transform, r.name, 11, INK, TextAnchor.MiddleCenter,
                           new Vector2(0,1),Vector2.one,new Vector2(5,-132),new Vector2(-5,-92));
            nm.raycastTarget = false;
            var fm = Label(row.transform, r.formula, 10, new Color(.7f,.79f,.87f), TextAnchor.MiddleCenter,
                           new Vector2(0,1),Vector2.one,new Vector2(5,-158),new Vector2(-5,-135));
            fm.resizeTextForBestFit=true;fm.resizeTextMinSize=8;fm.resizeTextMaxSize=10;
            fm.raycastTarget = false;
        }
        // A new category/search starts at its first row, not the old list's
        // scroll offset or elastic velocity (which can leave short results blank).
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentList);
        var reagentScroll=_contentList.GetComponentInParent<ScrollRect>();
        if(reagentScroll!=null){reagentScroll.StopMovement();reagentScroll.verticalNormalizedPosition=1f;}
    }

    /* ------------------------------------------------------------ 加试剂 */
    void Pour() {
        if (_selected == null || Lab.Vessels.Count < 2) { Hint("至少需要两个容器才能倾倒"); return; }
        /* 悬停在别的容器上方时，倒进那只容器，量由滑动条决定；
           否则退回默认比例倒在旁边那只里 */
        var target = _selected.AttachedTo ?? Lab.Vessels.FirstOrDefault(x => x != _selected);
        if (target == null) return;
        BeginStagedPour(_selected,target);
    }

    void RemoveSel() {
        if (_selected == null) return;
        Lab.RemoveVessel(_selected);
        _selected = Lab.Vessels.FirstOrDefault();
        RefreshVesselPanel();
    }

    void Select(Vessel v) { _selected = v; RefreshVesselPanel(); }
    public void SelectExternal(Vessel v) { _selected = v; RefreshVesselPanel(); }

    void Hint(string s) {
        if (_hint == null) return;
        _hint.text = s; _hint.color = WARN;
        CancelInvoke(nameof(ResetHint));
        Invoke(nameof(ResetHint), 2.4f);
    }
    void ResetHint() {
        if (_hint == null) return;
        _hint.text = "WASD 移动 · 拖空白转视角 · 拽物品移动 · Shift 拖动升降 · E 熄木条 · F 开关本生灯";
        _hint.color = DIM;
    }

    /* ------------------------------------------------------------ 刷新 */
    public void RefreshVesselPanel() {
        if (_vesselTitle == null) return;
        var v = _selected;
        /* 检验器材是全局工具，不依赖是否选中容器 —— 所以先于下面的空选中分支处理。
           原先放在分支之后，没选容器时整块逻辑都执行不到，工具栏永远不出现。 */
        if (_tagBar != null) {
            _tagBar.gameObject.SetActive(true);
            RefreshTagButtons();
        if (_eqBar != null) {
            _eqBar.gameObject.SetActive(true);
            var e1 = _eqShow  != null ? _eqShow.GetComponentInChildren<Text>()  : null;
            var e2 = _eqIonic != null ? _eqIonic.GetComponentInChildren<Text>() : null;
            if (e1 != null) Localization.Apply(e1, EqTagSettings.Show ? "显示 开" : "显示 关");
            if (e2 != null) Localization.Apply(e2, EqTagSettings.Ionic ? "离子式 开" : "离子式 关");
            if (_eqDur != null && !_eqDur.isFocused)
                _eqDur.text = EqTagSettings.Duration.ToString("0.#");
        }
        }
        if (_testBar != null) {
            bool showTest = TestSet.All.Count > 0;
            _testBar.gameObject.SetActive(showTest);
            if (showTest) {
                var ct = CurrentTest();
                if (ct != null) {
                    _testLabel.text = $"{ct.Name}·{ct.StateText}";
                    var cb1 = _testBtn1.GetComponentInChildren<Text>();
                    var cb2 = _testBtn2.GetComponentInChildren<Text>();
                    if (cb1 != null) cb1.text = ct.Kind == TestItemKind.Splint
                        ? (ct.Splint == SplintState.Burning ? "熄灭" : "点燃")
                        : (ct.Moist ? "晾干" : "湿润");
                    if (cb2 != null) cb2.text = "伸入";
                }
            }
        }

        if (v == null) {
            Localization.Apply(_vesselTitle, "未选择容器"); Localization.Apply(_vesselVol, "");
            _gTemp.text = "—"; _gPh.text = "—"; _gFill.text = "—";
            ClearChildren(_vesselInfo); return;
        }
        Localization.Apply(_vesselTitle, $"{VesselNames.Name(v.Kind)} #{v.Id.Replace("v","")}");
        Localization.Apply(_vesselVol, v.Sealed?$"密闭 · {v.PressureBar:F3} bar（绝压）":$"{v.Volume():F0} / {v.Capacity:F0} mL");
        _gTemp.text = $"{v.Temp:F0} ℃";
        _gTemp.color = v.Temp > 70 ? WARN : (v.Temp > 40 ? new Color(1f,0.85f,0.55f) : INK);
        _gPh.text = v.Water > 0.5 ? v.Ph.ToString("F1") : "—";
        _gFill.text = (v.FillFraction() * 100).ToString("F0") + "%";
        if (_phMarker != null) {
            var rt = _phMarker.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(v.Water > 0.5 ? Mathf.Clamp01((float)(v.Ph / 14.0)) : 0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        RefreshContents(v);

        /* 器材交互条：吸附到别的容器/本生灯时出现；检验器材区则常驻 */
        if (_pourBar != null) {
            bool on = v.AttachedTo != null&&!MeteredTransfer.IsTool(v);
            _pourBar.gameObject.SetActive(on);
            if (on) {
                _pourLabel.text = $"倾倒量 {v.PourAmount * 100:F0}%";
                _pourSlider.SetValueWithoutNotify(v.PourAmount);
            }
        }
        if (_testBar != null) {
            bool showTest = TestSet.All.Count > 0;
            _testBar.gameObject.SetActive(showTest);
            if (showTest) {
                var t = CurrentTest();
                if (t != null) {
                    _testLabel.text = $"{t.Name}·{t.StateText}";
                    var b1 = _testBtn1.GetComponentInChildren<Text>();
                    var b2 = _testBtn2.GetComponentInChildren<Text>();
                    if (b1 != null) b1.text = t.Kind == TestItemKind.Splint
                        ? (t.Splint == SplintState.Burning ? "熄灭" : "点燃")
                        : (t.Moist ? "晾干" : "湿润");
                    if (b2 != null) b2.text = "伸入";
                }
            }
        }
        if (_burnerBar != null) {
            var heatingBurner=ApparatusSet.HeatingBurner(v);
            bool on = heatingBurner != null;
            _burnerBar.gameObject.SetActive(on);
            if (on) {
                _burnerLabel.text = heatingBurner.Lit ? "本生灯 已点燃" : "本生灯 未点燃";
                var bt = _burnerBtn.GetComponentInChildren<Text>();
                if (bt != null) bt.text = heatingBurner.Lit ? "熄灭" : "点燃";
            }
        }
    }

    void RefreshContents(Vessel v) {
        ClearChildren(_vesselInfo);
        float y = 0;
        void Group(string title) {
            var t = Label(_vesselInfo, title, 11, DIM, TextAnchor.UpperLeft,
                          new Vector2(0,1), new Vector2(1,1), new Vector2(0, -y - 15), new Vector2(0, -y));
            y += 17;
        }
        void Item(string name, string amt, Color c) {
            var sw = Node("sw", _vesselInfo, new Vector2(0,1), new Vector2(0,1), Vector2.zero, Vector2.zero, c);
            var srt = (RectTransform)sw; srt.pivot = new Vector2(0,1);
            srt.anchoredPosition = new Vector2(0, -y - 3); srt.sizeDelta = new Vector2(9,9);
            Label(_vesselInfo, name, 11, INK, TextAnchor.UpperLeft,
                  new Vector2(0,1), new Vector2(1,1), new Vector2(15,-y-14), new Vector2(-64,-y));
            Label(_vesselInfo, amt, 11, DIM, TextAnchor.UpperRight,
                  new Vector2(0,1), new Vector2(1,1), new Vector2(0,-y-14), new Vector2(0,-y));
            y += 16;
        }
        Color ColOf(string id) {
            var s = ChemData.Sub[id];
            string hex = !string.IsNullOrEmpty(s.SolidColor) ? s.SolidColor : s.SolColor;
            Color c = new Color(0.6f,0.7f,0.8f,0.9f);
            if (!string.IsNullOrEmpty(hex)) ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }
        void Dump(string title, Dictionary<string,double> map) {
            var items = map.Where(kv => kv.Value > 1e-9).OrderByDescending(kv => kv.Value).Take(12).ToList();
            if (items.Count == 0) return;
            Group(title);
            foreach (var kv in items) {
                var s = ChemData.Sub[kv.Key];
                double mass = kv.Value * s.Mass;
                string amt = mass >= 1 ? $"{mass:F2} g" : $"{(mass*1000):F0} mg";
                Item(s.IsIon ? kv.Key : s.Name, amt, ColOf(kv.Key));
            }
        }
        if (v.Water > 0.05) Item("水", $"{v.Water:F1} mL", new Color(0.62f,0.85f,1f));
        Dump("溶液中", v.Solutes);
        Dump("未溶固体", v.Undissolved);
        Dump("沉淀", v.Precipitates);
        Dump("气体", v.Gases);
        var rt = _vesselInfo; rt.sizeDelta = new Vector2(rt.sizeDelta.x, y);
    }

    void ClearChildren(RectTransform rt) {
        if (rt == null) return;
        for (int i = rt.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(rt.GetChild(i).gameObject);
    }

    int _lastLogCount = -1;
    public void RefreshLog() {
        if (_logContent == null) return;
        if (Lab.Log.Count == _lastLogCount) return;
        _lastLogCount = Lab.Log.Count;
        ClearChildren(_logContent);
        foreach (var e in Lab.Log.Skip(Math.Max(0, Lab.Log.Count - 60))) {
            var row = new GameObject("L");
            row.transform.SetParent(_logContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 18);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 18; le.minHeight = 18;
            Color c = e.Kind == "rxn" ? LOGRXN : e.Kind == "add" ? LOGADD : e.Kind == "setup" ? DIM : LOGOBS;
            string prefix = e.Kind == "rxn" ? "▸ " : "";
            var vn = string.IsNullOrEmpty(e.VesselId) ? "" : e.VesselId.Replace("v","#") + "  ";
            string source = $"{e.T:00.0}s   {vn}{prefix}{e.Text}";
            Label(row.transform, source, 12, c, TextAnchor.MiddleLeft,
                  Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));
        }
        var cv = _logContent.parent.parent.GetComponent<ScrollRect>();
        if (cv != null) Canvas.ForceUpdateCanvases();
        RefreshLogLayout();
    }

    public void RefreshLogLayout() {
        if (_logContent == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_logContent);
        Canvas.ForceUpdateCanvases();
        foreach (Transform row in _logContent) {
            var label = row.GetComponentInChildren<Text>();
            var layout = row.GetComponent<LayoutElement>();
            if (label == null || layout == null) continue;
            float height = Mathf.Max(18f, label.preferredHeight + 2f);
            var rect = row as RectTransform;
            if (rect != null) { var size = rect.sizeDelta; size.y = height; rect.sizeDelta = size; }
            layout.minHeight = layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_logContent);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>把用到的字符按每个字号都塞进动态字体图集。
    /// 不预热的话，图集会在渲染中途重建，留下新旧两批字形 —— 就是文字重影。</summary>
    /* ---------------- 语言切换 ----------------
       思路：不改写每一个标签，而是把「原文」记下来，
       切语言时按原文整体查表替换。这样新增界面文字也自动生效。 */
    public static readonly List<Text> LangTexts = new List<Text>();
    static readonly Dictionary<Text, string> LangOrig = new Dictionary<Text, string>();

    public static void RegisterLang(Text t) {
        if (t == null || LangOrig.ContainsKey(t)) return;
        LangOrig[t] = t.text;
        LangTexts.Add(t);
        Localization.Register(t, t.text);
    }

    /// <summary>把登记过的文本按当前语言整体替换</summary>
    public static void ApplyLang() {
        foreach (var kv in LangOrig) {
            if (kv.Key == null) continue;
            Localization.Apply(kv.Key, kv.Value);
        }
    }

    public void PrewarmFonts() {
        if (_font == null || _canvas == null) return;
        var texts = _canvas.GetComponentsInChildren<Text>(true);
        var sizes = new System.Collections.Generic.HashSet<int>();
        var chars = new System.Collections.Generic.HashSet<char>();
        foreach (var t in texts) {
            if (t.font != _font || string.IsNullOrEmpty(t.text)) continue;
            sizes.Add(t.fontSize);
            foreach (var c in t.text) chars.Add(c);
        }
        if (chars.Count == 0) return;
        var arr = new char[chars.Count];
        chars.CopyTo(arr);
        var s = new string(arr);
        foreach (var sz in sizes) {
            _font.RequestCharactersInTexture(s, sz, FontStyle.Normal);
            _font.RequestCharactersInTexture(s, sz, FontStyle.Bold);
        }
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>把画布贴到相机前方，按 1920 宽的设计基准等比映射到视锥上</summary>
    public void LayoutCanvas() {
        if (_canvas == null) return;
        /* 用专用 UI 相机，画布 world scale 恒为 1 —— 这样动态字体按原生尺寸渲染。
           之前缩到 0.0005 倍，字体在图集里被压扁，渲染出来是重影的。
           把画布放到「视锥在这一距离上的宽度正好等于设计宽度」的位置即可。 */
        /* Overlay 模式下画布由 CanvasScaler 自己管，不需要手动摆位 */
        DesignHeight = DesignH;
    }
    public float DesignHeight = DesignH;

    void LateUpdate() {
        ApplyResponsiveLayout();
        UpdateStagedPour();
        if(IsPouring)return;
        UpdateMeteredToolUI();
        UpdateDoseInput();
        UpdateBurnerDialog();
        UpdateThermalLedger();
        if (_canvas == null) return;
        if (_canvas.worldCamera != UICam) _canvas.worldCamera = UICam;
        LayoutCanvas();
        RefreshVesselPanel();
        RefreshLog();
    }
}

}
