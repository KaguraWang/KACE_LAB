/* =====================================================================
   反应方程式弹出标签
     反应一发生，就在容器处弹出一块标签写出方程式。
     布局（连接符长度跟着条件文字走，所以条件一定写得下）：

              [ 反应条件 ]
       反应物 [=======→] 生成物

     · 中文规范：无机「=」、有机「→」、可逆「⇌」，条件写在正上方，
                 气体标 ↑、沉淀标 ↓，不标状态符号
     · 英文规范：一律「→」、可逆「⇌」，条件写在正上方，
                 每种物质后标 (s)/(l)/(g)/(aq)
     · 可切换总方程式 / 离子方程式
     · 默认显示 10 秒后自动消失（时长在界面上设，不放在标签里）
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

public static class EqTagSettings {
    public static bool Show = true;
    /// <summary>显示时长（秒），界面可改</summary>
    public static float Duration = 10f;
    /// <summary>是否显示成离子方程式（写得出离子式的才有效）</summary>
    public static bool Ionic = false;
}

public class EquationTag : MonoBehaviour {
    public ReactionEvent Ev;
    /// <summary>错位槽位：同一容器连续发生多个反应时会各弹一个标签，
    /// 不错开就会完全叠在一起。</summary>
    public int Slot;
    /// <summary>屏幕排布器写入的像素位移，不改变方程式所属的容器。</summary>
    public Vector2 ScreenOffset;
    double _born;

    Canvas _canvas;
    Image _bg;
    Text _cond, _left, _right;
    ConnectorGraphic _conn;
    Camera _cam;

    const float CanvasUnitsPerWorld = 340f;
    const float PadX = 18f, PadY = 10f;
    const float Gap = 12f;          // 文字与连接符之间的间隙
    const float MinConn = 34f;

    public Rect ScreenRect {
        get {
            if (_canvas == null || _cam == null) return new Rect(0, 0, 0, 0);
            var rt = (RectTransform)_canvas.transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float x0 = float.MaxValue, y0 = float.MaxValue;
            float x1 = float.MinValue, y1 = float.MinValue;
            foreach (var corner in corners) {
                var p = _cam.WorldToScreenPoint(corner);
                if (p.z <= 0.01f) return new Rect(0, 0, 0, 0);
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }

    /// <summary>正在显示的方程式标签（由 LabBootstrap 出入栈时维护）</summary>
    public static readonly System.Collections.Generic.List<EquationTag> Active =
        new System.Collections.Generic.List<EquationTag>();

    /// <summary>某个容器上是否正有方程式标签在显示</summary>
    public static bool BusyFor(Vessel v) {
        for (int i = 0; i < Active.Count; i++) {
            var t = Active[i];
            if (t != null && t.Ev != null && t.Ev.Vessel == v) return true;
        }
        return false;
    }

    /// <summary>找到某个容器的标签视图（用来问它的顶端位置）</summary>
    static VesselTag FindVesselTag(Vessel v) {
        if (v == null) return null;
        var host = LabBootstrap.Instance;
        if (host == null) return null;
        foreach (var t in host.GetComponentsInChildren<VesselTag>(true))
            if (t != null && t.Model == v) return t;
        return null;
    }

    public static EquationTag Create(ReactionEvent ev, Transform parent, Font font, Camera cam) {
        var go = new GameObject("EqTag");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<EquationTag>();
        t.Ev = ev;
        t._born = ev.At;
        t._cam = cam;

        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);
        t._canvas = cgo.AddComponent<Canvas>();
        t._canvas.renderMode = RenderMode.WorldSpace;
        t._canvas.overrideSorting = true;
        /* 世界空间画布的透明排序按「到相机的距离」来，不看层级 ——
           方程式标签在容器标签上方（离相机更远），会被压在下面。
           用 sortingOrder 强制它盖在容器标签之上。 */
        t._canvas.sortingOrder = 50;
        var crt = (RectTransform)cgo.transform;
        crt.sizeDelta = new Vector2(400f, 80f);
        crt.localScale = Vector3.one * (1f / CanvasUnitsPerWorld);
        cgo.layer = LabBootstrap.UiLayerConst;

        var bg = new GameObject("BG");
        bg.transform.SetParent(cgo.transform, false);
        t._bg = bg.AddComponent<Image>();
        t._bg.color = LabUiTheme.Panel;
        if (OverlayTagMaterial.Value != null) t._bg.material = OverlayTagMaterial.Value;
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        /* 条件：居中，位于连接符正上方 */
        t._cond = MakeText(cgo.transform, "Cond", 19, new Color(1f, 0.80f, 0.35f), TextAnchor.MiddleCenter);
        /* 反应物右对齐 / 生成物左对齐，夹住中间自适应长度的连接符 */
        t._left  = MakeText(cgo.transform, "Left", 21, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleRight);
        t._right = MakeText(cgo.transform, "Right", 21, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleLeft);

        /* 必须带 RectTransform 创建：UI 控件挂在普通 Transform 上时
           整个渲染链路是断的（取 RectTransform 会直接抛 InvalidCastException）。 */
        var cg = new GameObject("Conn", typeof(RectTransform));
        cg.transform.SetParent(cgo.transform, false);
        t._conn = ConnectorGraphic.Attach(cg);
        t._conn.color = LabUiTheme.Ink;

        return t;
    }

    static Text MakeText(Transform parent, string name, int size, Color col, TextAnchor anchor) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tx = go.AddComponent<Text>();
        tx.font = LabUI.UiFont;
        tx.fontSize = size;
        tx.color = LabUiTheme.Ink;
        tx.alignment = anchor;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        tx.raycastTarget = false;
        /* 分子式里的上下标是用富文本标出来的，必须显式打开 */
        tx.supportRichText = true;
        if (OverlayTagMaterial.Value != null) tx.material = OverlayTagMaterial.Value;
        return tx;
    }

    /// <summary>无头环境没有 Update，所以由外部显式调用。返回 false 表示该销毁了。</summary>
    public bool Sync() {
        if (Ev == null || Ev.Rule == null || Ev.Vessel == null || !Lab.Vessels.Contains(Ev.Vessel)) return false;
        double age = Lab.Clock - _born;
        if (!EqTagSettings.Show || age > EqTagSettings.Duration) return false;

        bool english = Localization.English;
        var r = Ev.Rule;

        /* ---- 取方程式的三段：条件 / 左侧 / 右侧 ---- */
        string full = EqFormat.Format(r, english, EqTagSettings.Ionic);
        string conn, left, right;
        SplitEquation(full, out left, out conn, out right);
        _cond.text = EqFormat.CondLabel(r.Cond, english);

        _left.text  = left;
        _right.text = right;
        /* 连接符自己画，宽度跟着条件文字走 */

        /* ---- 量文字宽度，连接符按条件长度自适应 ---- */
        Canvas.ForceUpdateCanvases();
        float lw = _left.preferredWidth;
        float rw = _right.preferredWidth;
        float cw = _cond.preferredWidth;
        float connW = Mathf.Max(MinConn, cw + 14f);
        float totalW = lw + Gap + connW + Gap + rw;
        float panelW = totalW + PadX * 2;
        float panelH = 30f + 34f + PadY * 2;      // 条件行 + 方程式行

        _conn.Set(conn == "=" ? ConnectorGraphic.Kind.Equals
                : conn == "⇌" ? ConnectorGraphic.Kind.Equilibrium
                : ConnectorGraphic.Kind.Arrow, connW);

        var crt = (RectTransform)_canvas.transform;
        crt.sizeDelta = new Vector2(panelW, panelH);

        /* 关键：面板要**以整个方程式为中心**，连接符再按左侧宽度偏移。
           反过来让连接符居中、两侧各自贴过去的话，左边一长就会被面板裁掉
           （CO₃²⁻ 会被切成 CCO₂ 那样）。 */
        float eqLeft = -totalW * 0.5f;                       // 方程式左缘（相对面板中心）
        float connCx = eqLeft + lw + Gap + connW * 0.5f;     // 连接符中心
        float cyEq = -PadY - 30f - 17f;            // 方程式行中心（相对面板顶部）
        float condY = -PadY - 15f;

        /* 左侧：右端对齐到连接符左缘 */
        var lr = (RectTransform)_left.transform;
        lr.anchorMin = new Vector2(0.5f, 1); lr.anchorMax = new Vector2(0.5f, 1);
        lr.pivot = new Vector2(1f, 0.5f);
        lr.sizeDelta = new Vector2(lw + 6f, 30f);
        lr.anchoredPosition = new Vector2(connCx - connW * 0.5f - Gap, cyEq);

        var rr = (RectTransform)_right.transform;
        rr.anchorMin = new Vector2(0.5f, 1); rr.anchorMax = new Vector2(0.5f, 1);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.sizeDelta = new Vector2(rw + 6f, 30f);
        rr.anchoredPosition = new Vector2(connCx + connW * 0.5f + Gap, cyEq);

        /* 连接符：居中，宽度自适应 */
        var gr = (RectTransform)_conn.transform;
        gr.anchorMin = new Vector2(0.5f, 1); gr.anchorMax = new Vector2(0.5f, 1);
        gr.pivot = new Vector2(0.5f, 0.5f);
        gr.sizeDelta = new Vector2(connW, 26f);
        gr.anchoredPosition = new Vector2(connCx, cyEq);

        /* 条件：居中在连接符正上方 */
        var cr = (RectTransform)_cond.transform;
        cr.anchorMin = new Vector2(0.5f, 1); cr.anchorMax = new Vector2(0.5f, 1);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(cw + 8f, 26f);
        cr.anchoredPosition = new Vector2(connCx, condY);

        /* ---- 位置：容器上方，面朝镜头 ---- */
        float worldH = panelH / CanvasUnitsPerWorld;
        /* 容器标签本身也在容器上方，两者会重叠；方程式标签再往上抬一截，
           并且同一容器的多个反应按槽位继续错开。 */
        /* 用容器的**当前位置**，不是 Ev.Pos ——
           Ev.Pos 是反应发生那一刻的快照，容器被拖走后标签会留在原地。
           容器已经删掉时才退回快照。 */
        Vector3 basePos = (Ev.Vessel != null) ? Ev.Vessel.Pos : Ev.Pos;
        float liftY = (Ev.Vessel != null) ? Ev.Vessel.LiftY : 0f;
        /* 容器标签是永久显示的，方程式标签必须排在它**上方**而不是盖住它。
           容器标签的高度跟行数有关，所以这里现问一次它的顶端世界 Y。 */
        float bottomY = basePos.y + liftY + 1.75f + Slot * 0.62f;
        var vt = FindVesselTag(Ev.Vessel);
        if (vt != null) bottomY = Mathf.Max(bottomY, vt.TopWorldY + 0.10f + Slot * 0.62f);
        Vector3 center = basePos + Vector3.up * (bottomY - basePos.y + worldH * 0.5f);
        if (_cam == null) _cam = Camera.main;
        if (_cam != null && ScreenOffset.sqrMagnitude > 1e-6f) {
            var p0 = _cam.WorldToScreenPoint(center);
            var p1 = _cam.WorldToScreenPoint(center + _cam.transform.up * 0.10f);
            float pixelsPerWorld = Mathf.Max(1f, Vector2.Distance(p0, p1) / 0.10f);
            center += _cam.transform.right * (ScreenOffset.x / pixelsPerWorld)
                    + _cam.transform.up * (ScreenOffset.y / pixelsPerWorld);
        }
        if (_cam != null) transform.rotation = _cam.transform.rotation;   // 永远面朝镜头
        transform.position = center;

        /* ---- 快消失时淡出 ---- */
        float left01 = Mathf.Clamp01((float)(EqTagSettings.Duration - age));
        float a = Mathf.Min(1f, left01 / 1.2f);
        var c1 = _bg.color;  c1.a = LabUiTheme.Panel.a * a; _bg.color = c1;
        SetAlpha(_cond, a); SetAlpha(_left, a); SetAlpha(_right, a);
        var cc = _conn.color; cc.a = a; _conn.color = cc;
        return true;
    }

    static void SetAlpha(Text t, float a) {
        var c = t.color; c.a = a; t.color = c;
    }

    /// <summary>把「左 = 右」拆成三段，以便把连接符单独画出来</summary>
    public static void SplitEquation(string full, out string left, out string conn, out string right) {
        left = full; conn = "→"; right = "";
        int i = full.IndexOf(" ⇌ ");
        if (i >= 0) { left = full.Substring(0, i); right = full.Substring(i + 3); conn = "⇌"; return; }
        i = full.IndexOf(" = ");
        if (i >= 0) { left = full.Substring(0, i); right = full.Substring(i + 3); conn = "="; return; }
        i = full.IndexOf(" → ");
        if (i >= 0) { left = full.Substring(0, i); right = full.Substring(i + 3); conn = "→"; return; }
    }
}

}
