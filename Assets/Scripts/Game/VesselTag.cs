/* =====================================================================
   容器悬浮标签（tag）
     · 浮在容器上方，用一根白线连到容器表面
     · 永远面朝镜头（billboard）
     · 内容：物质名 / 物质的量(mol) / 浓度(mol dm-3)
     · 可切换「具体物质」与「离子状态」，各条目可单独开关
   ===================================================================== */
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

// World-space informational cards must stay readable over 3D glass. This only
// changes UI depth testing; Physics.RaycastAll still obeys physical depth.
internal static class OverlayTagMaterial {
    static Material _value;
    public static Material Value {
        get {
            if (_value == null) {
                var shader = Shader.Find("UI/Default");
                if (shader == null) return null;
                _value = new Material(shader) { hideFlags = HideFlags.DontSave };
                _value.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            return _value;
        }
    }
}

/// <summary>标签显示设置（界面改它，标签读它）</summary>
public static class TagSettings {
    public static bool ShowTags = true;
    // Keep the bench readable: the selected vessel opens its composition;
    // users may explicitly expand every tag from the label controls.
    public static bool ShowAllDetails = false;
    /// <summary>true = 显示成具体物质；false = 显示成离子状态</summary>
    public static bool Molecular = true;
    public static bool ShowName = true;
    public static bool ShowAmount = true;
    public static bool ShowConc = true;
    public static bool ShowLine = true;
    /// <summary>标签面板的世界宽度（世界单位）</summary>
    public const float Width = 0.92f;
}

public class VesselTag : MonoBehaviour {
    public Vessel Model;
    /// <summary>错位槽位（0/1/2）：容器挨得近时标签会互相压住，按槽位抬到不同高度</summary>
    public int Slot;
    /// <summary>被重叠消解顶起来的额外世界高度（由 LabBootstrap 每帧写）</summary>
    public float ExtraLift;
    /// <summary>重叠排布产生的屏幕像素位移；画布仍由白线连接原容器。</summary>
    public Vector2 ScreenOffset;
    public bool RenderVisible => _canvas != null && _canvas.enabled && gameObject.activeInHierarchy;
    public int ShownSpeciesCount { get; private set; }
    /// <summary>上一次 Sync 走到哪一步、关键量是多少（诊断用）</summary>
    public string LastSyncTrace = "(还没跑过)";

    /// <summary>面板在屏幕上的矩形，供重叠消解用</summary>
    public Rect ScreenRect {
        get {
            if (_panel == null || _cam == null) return new Rect(0, 0, 0, 0);
            var c = _panel.transform.position;
            var sc = _cam.WorldToScreenPoint(c);
            if (sc.z <= 0.01f) return new Rect(0, 0, 0, 0);
            /* 面板的世界尺寸换算成屏幕像素 */
            float perWorld = 1f;
            var p0 = _cam.WorldToScreenPoint(c);
            var p1 = _cam.WorldToScreenPoint(c + _cam.transform.up * 0.10f);
            perWorld = Vector3.Distance(p0, p1) / 0.10f;
            float wpx = _panel.rect.width / CanvasUnitsPerWorld * perWorld;
            float hpx = _panel.rect.height / CanvasUnitsPerWorld * perWorld;
            return new Rect(sc.x - wpx * 0.5f, sc.y - hpx * 0.5f, wpx, hpx);
        }
    }

    /// <summary>每世界单位对应多少屏幕像素（消解时把像素位移换回世界单位）</summary>
    public float PixelsPerWorld {
        get {
            if (_cam == null) return 100f;
            var o = _panel != null ? _panel.transform.position : transform.position;
            var a = _cam.WorldToScreenPoint(o);
            var b = _cam.WorldToScreenPoint(o + Vector3.up * 0.10f);
            return Mathf.Max(1f, Vector3.Distance(a, b) / 0.10f);
        }
    }

    Canvas _canvas;
    RectTransform _panel;
    Text _title;
    Text _capacity;
    public string CapacityText => _capacity != null ? _capacity.text : "";
    readonly List<Text> _rowName = new List<Text>();
    readonly List<Text> _rowAmt  = new List<Text>();
    LineRenderer _line;
    Camera _cam;

    const float CanvasUnitsPerWorld = 340f;      // 面板 340 画布单位 = 1 世界单位
    const float RowH = 34f;                      // 行高（画布单位）
    const float PadX = 14f;
    const float PanelW = 300f;

    public static VesselTag Create(Vessel v, Transform parent, Font font, Camera cam) {
        var go = new GameObject("Tag_" + v.Id);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<VesselTag>();
        t.Model = v;
        t._cam = cam;

        /* 世界空间画布：整体缩放到合适的世界尺寸 */
        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);
        t._canvas = cgo.AddComponent<Canvas>();
        t._canvas.renderMode = RenderMode.WorldSpace;
        t._canvas.overrideSorting = true;   // 让每个标签能独立排层
        t._canvas.sortingOrder = 10;        // 低于方程式标签
        var crt = (RectTransform)cgo.transform;
        crt.sizeDelta = new Vector2(PanelW, 100f);
        crt.localScale = Vector3.one * (1f / CanvasUnitsPerWorld);
        cgo.layer = LabBootstrap.UiLayerConst;

        /* 背景板 */
        var bg = new GameObject("BG");
        bg.transform.SetParent(cgo.transform, false);
        var bimg = bg.AddComponent<Image>();
        bimg.color = new Color(0.04f, 0.09f, 0.16f, 0.86f);
        if (OverlayTagMaterial.Value != null) bimg.material = OverlayTagMaterial.Value;
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        /* 标题 */
        t._title = MakeText(cgo.transform, "标题", 20, new Color(0.55f, 0.92f, 0.72f), TextAnchor.MiddleLeft);
        var trt = (RectTransform)t._title.transform;
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0, -6);
        trt.sizeDelta = new Vector2(-PadX * 2, 28);

        t._capacity = MakeText(cgo.transform, "默认容量", 18, new Color(0.65f,0.80f,0.95f), TextAnchor.MiddleLeft);
        var caprt=(RectTransform)t._capacity.transform;
        caprt.anchorMin=new Vector2(0,1);caprt.anchorMax=new Vector2(1,1);caprt.pivot=new Vector2(0.5f,1);
        caprt.anchoredPosition=new Vector2(0,-34);caprt.sizeDelta=new Vector2(-PadX*2,28);

        /* 白线 */
        var lgo = new GameObject("Line");
        lgo.transform.SetParent(go.transform, false);
        t._line = lgo.AddComponent<LineRenderer>();
        t._line.positionCount = 2;
        t._line.startWidth = 0.020f;
        t._line.endWidth = 0.020f;
        t._line.useWorldSpace = true;
        t._line.material = new Material(Shader.Find("ChemLab/Particle") ?? Shader.Find("Sprites/Default"));
        t._line.startColor = Color.white;
        t._line.endColor = new Color(1f, 1f, 1f, 0.55f);
        t._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        t._line.receiveShadows = false;

        return t;
    }

    static Text MakeText(Transform parent, string name, int size, Color col, TextAnchor anchor) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tx = go.AddComponent<Text>();
        tx.font = LabUI.UiFont;
        tx.fontSize = size;
        tx.color = col;
        tx.alignment = anchor;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        tx.raycastTarget = false;
        tx.supportRichText = true;
        if (OverlayTagMaterial.Value != null) tx.material = OverlayTagMaterial.Value;
        return tx;
    }

    /// <summary>无头环境没有 LateUpdate，所以同步是显式调用的</summary>
    /// <summary>面板顶端的世界 Y。方程式标签用它把自己排到容器标签上方，
    /// 这样两张标签不会重叠 —— 而不是靠隐藏其中一张来回避。
    /// 容器标签是永久显示的，只有反应标签才有时限。 </summary>
    public float TopWorldY {
        get {
            if (_panel != null && RenderVisible) {
                var corners=new Vector3[4];_panel.GetWorldCorners(corners);
                return Mathf.Max(corners[1].y,corners[2].y)+0.055f;
            }
            if (Model == null) return 1.0f;
            var g = VesselMesh.Get(Model.Kind);
            float baseY = Model.Pos.y + Model.LiftY + g.H + 0.10f;
            float worldH = 0.42f;                      // 面板高度的保守估计
            if (_panel != null && _panel.rect.height > 1f)
                worldH = _panel.rect.height / CanvasUnitsPerWorld;
            /* 要加上「被后面几个槽位顶上去」的高度，否则方程式标签会压在容器标签上 */
            return baseY + worldH + 0.055f + Slot * (worldH + 0.07f);
        }
    }

    public void Sync() {
        if (Model == null) { LastSyncTrace = "Model 为空"; return; }
        /* 容器标签是**永久**的，只在用户自己关掉标签显示时才消失。
           （以前这里还加了「有方程式标签时让位」，那是错的 ——
             容器标签会一消失就再也回不来。） */
        var framed=LabBootstrap.Instance!=null?LabBootstrap.Instance.FramedMeteredTool:null;
        bool show = TagSettings.ShowTags && (framed==null||Model==framed||Model==framed.AttachedTo);
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show) { ShownSpeciesCount=0;LastSyncTrace = "ShowTags 关了"; return; }

        /* ---- 内容 ---- */
        /* 注意：ChemLab.Game 里也有一个 VesselView（容器视图组件），会遮住同名的静态类，所以这里必须全限定 */
        var lines = Chem.VesselView.AllLines(Model, TagSettings.Molecular);
        var selected=LabBootstrap.Instance!=null?LabBootstrap.Instance.SelectedVesselForTags:null;
        bool detailed=(TagSettings.ShowAllDetails||selected==Model)&&lines.Count>0;
        ShownSpeciesCount=detailed?lines.Count:0;
        var sb = new StringBuilder();
        /* 标题：容器名 + 模式 */
        string titleSource = $"{VesselNames.Name(Model.Kind)} #{Model.Id.Replace("v", "")}"
                    + (Model.Sealed?$"  密闭 {Model.PressureBar:F2} bar":(detailed?(TagSettings.Molecular ? "  具体物质" : "  离子状态"):""));
        Localization.Apply(_title, titleSource);
        Localization.Apply(_capacity, $"标称容量 {Model.Capacity:0} mL");
        _title.fontSize = Localization.English ? 14 : 20;
        _capacity.fontSize = Localization.English ? 14 : 18;

        EnsureRows(ShownSpeciesCount);
        float y = -(6 + 28 + 28 + 4);
        for (int i = 0; i < ShownSpeciesCount; i++) {
            var s = lines[i];
            var rt = (RectTransform)_rowName[i].transform;
            rt.anchoredPosition = new Vector2(0, y);
            _rowName[i].gameObject.SetActive(true);

            /* 物质名 */
            /* 用中文标记而不是 ↓/↑ —— 这两个字形在系统字体里缺失，
               会渲染成方块并盖住后面的数字（看起来像把 15.00 读成 5.00）。 */
            string tag = s.IsPrecipitate ? "沉 " : s.IsUndissolved ? "固 " : (s.IsGas ? "气 " : "");
            Localization.Apply(_rowName[i], tag + s.Name);
            _rowName[i].fontSize = Localization.English ? 12 : 18;
            _rowName[i].gameObject.SetActive(TagSettings.ShowName);

            /* 物质的量 */
            var amt = (RectTransform)_rowAmt[i].transform;
            amt.anchoredPosition = new Vector2(0, y);
            _rowAmt[i].text = Fmt(s.Mol);
            _rowAmt[i].fontSize = Localization.English ? 11 : 17;
            _rowAmt[i].gameObject.SetActive(TagSettings.ShowAmount || TagSettings.ShowConc);
            if(s.IsGas||s.IsPrecipitate||s.IsUndissolved)
                _rowAmt[i].text=Fmt(s.Mol);
            else if (TagSettings.ShowAmount && TagSettings.ShowConc)
                _rowAmt[i].text = $"{Fmt(s.Mol)}   {(double.IsNaN(s.Conc)?"无液相":$"{s.Conc:F3} M")}";
            else if (TagSettings.ShowAmount)
                _rowAmt[i].text = Fmt(s.Mol);
            else if (TagSettings.ShowConc)
                _rowAmt[i].text = double.IsNaN(s.Conc)?"无液相":$"{s.Conc:F3} M";
            y -= RowH;
        }
        for (int i = ShownSpeciesCount; i < _rowName.Count; i++) {
            _rowName[i].gameObject.SetActive(false);
            _rowAmt[i].gameObject.SetActive(false);
        }

        /* ---- 面板高度自适应 ---- */
        float h = 6 + 28 + 28 + 4 + ShownSpeciesCount * RowH + 8;
        _panel = (RectTransform)_canvas.transform;
        _panel.sizeDelta = new Vector2(detailed||Model.Sealed?PanelW:190f, h);
        /* ---- 位置 ----
           画布是 WorldSpace 的（改用世界空间是为了让两张标签的遮挡关系可控），
           所以这里必须摆**世界坐标**。
           以前这里是「投影到屏幕再写 anchoredPosition」—— 那是 ScreenSpaceOverlay
           时代的写法，画布改成世界空间之后就把人扔到看不见的地方去了：
           标签全都 active、canvas 也 enabled，就是不在画面上。 */
        var g = VesselMesh.Get(Model.Kind);
        // A 530/820 mm pipette or burette often extends above the viewport.
        // Point its information card to the visible lower shaft instead.
        float tagHeight=MeteredTransfer.IsTool(Model)?Mathf.Min(g.H,1.4f):g.H;
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + tagHeight + 0.10f);
        if (_cam == null) _cam = Camera.main;
        LastSyncTrace = "Pos=" + Model.Pos + " LiftY=" + Model.LiftY + " cam=" + (_cam == null ? "null" : "有");
        bool vis = TagLayer.Visible(basePt, _cam);
        if (_canvas != null) _canvas.enabled = vis;
        if (!vis) { LastSyncTrace += " → Visible 返回 false，没定位"; return; }

        float worldH = h / CanvasUnitsPerWorld;
        /* 按实际高度分层：面板高 worldH，槽位间距取 worldH + 0.07，两个标签才不会压在一起 */
        Vector3 anchor = basePt + Vector3.up * (Slot * (worldH + 0.07f) + worldH * 0.5f + 0.06f + ExtraLift);
        if (_cam != null && ScreenOffset.sqrMagnitude > 1e-6f) {
            var p0=_cam.WorldToScreenPoint(anchor);
            var p1=_cam.WorldToScreenPoint(anchor+_cam.transform.up*0.10f);
            float perWorld=Mathf.Max(1f,Vector2.Distance(p0,p1)/0.10f);
            anchor+=_cam.transform.right*(ScreenOffset.x/perWorld)
                   +_cam.transform.up*(ScreenOffset.y/perWorld);
        }
        var ct = _canvas.transform;
        ct.position = anchor;
        /* 正对镜头 —— 世界空间画布不会自己转向 */
        if (_cam != null) ct.rotation = _cam.transform.rotation;
        /* 这里绝不能再动 _panel 的位置属性（anchoredPosition / localPosition）——
           _panel 就是 ct 本身，任何「归零」都会把上面设好的世界位置撤销掉。 */
        /* _panel 就是 ct 本身，写 localPosition 会把上面的世界位置清零 */
        LastSyncTrace += " → anchor=" + anchor;

        /* ---- 白线：从面板底边连到容器口 ---- */
        bool showLine = TagSettings.ShowLine;
        if (_line != null) {
            _line.enabled = showLine;
            if (showLine) {
                _line.SetPosition(0, anchor - Vector3.up * (worldH * 0.5f));
                _line.SetPosition(1, basePt);
            }
        }
    }

    void EnsureRows(int n) {
        while (_rowName.Count < n) {
            var a = MakeText(_canvas.transform, "N" + _rowName.Count, 18, new Color(0.88f, 0.92f, 0.98f), TextAnchor.MiddleLeft);
            var ar = (RectTransform)a.transform;
            ar.anchorMin = new Vector2(0f, 1f); ar.anchorMax = new Vector2(0.54f, 1f);   /* 名字列 */
            ar.pivot = new Vector2(0.5f, 1f);
            ar.sizeDelta = new Vector2(-PadX, RowH - 4);
            _rowName.Add(a);

            var b = MakeText(_canvas.transform, "A" + _rowAmt.Count, 17, new Color(0.62f, 0.78f, 0.95f), TextAnchor.MiddleRight);
            var br = (RectTransform)b.transform;
            br.anchorMin = new Vector2(0.54f, 1f); br.anchorMax = new Vector2(1f, 1f);   /* 数量列 */
            br.pivot = new Vector2(0.5f, 1f);
            br.sizeDelta = new Vector2(-PadX, RowH - 4);
            _rowAmt.Add(b);
        }
    }

    /// <summary>mol 量按大小自动选单位，避免 0.0000123 这种读不出来</summary>
    static string Fmt(double mol) {
        double m = mol * 1000.0;
        return m >= 100 ? m.ToString("F1") + " mmol"
             : m >= 1   ? m.ToString("F2") + " mmol"
             : mol.ToString("E2") + " mol";
    }
}
}
