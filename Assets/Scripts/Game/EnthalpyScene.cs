/* =====================================================================
   焓变计算器（独立 2D 场景）
     · 可拖动的无限画布（中键或空白处右键拖动平移）
     · 右键弹出选单：新建节点
     · 节点两个输入框：系数 + 化学式/名称（回车自动匹配并显示规范化学式）
     · 左键选中高亮；右键节点 → 编辑/删除
     · 从节点拉出带单向箭头的白色曲线（自动绕开节点与其它连线）
     · 松手落到另一节点后询问：查表 / 手动 / 未知
     · 右键可取消弹窗并撤销这条线
     · 连线右键 → 删除/编辑
     · 检测闭合回路 → 盖斯定律求解未知量
   ===================================================================== */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {

public class EnthalpyScene : MonoBehaviour {
    public static EnthalpyScene Instance;

    /* ---------------- 数据模型 ---------------- */
    /// <summary>节点尺寸常量，算边框端点要用</summary>
    public const float NodeW = 210f, NodeH = 74f;

    public class Node {
        public int Id;
        public double Coef = 1;
        public string Raw = "";          // 用户输入的原文
        public string Formula = "";      // 匹配后规范化显示
        public string SpeciesId = "";    // 匹配到的物种 id（可能为空）
        public Vector2 Pos;
        public RectTransform Rt;
        public Image Bg;
        public InputField CoefField, FormulaField;
    }

    public class Edge {
        public Node A, B;
        public double DH;
        public EnthalpySource Source = EnthalpySource.Unknown;
        public string Equation = "";
        public RectTransform Rt;
        public Image Hit;                // 加粗的透明命中区，便于右键
    }

    readonly List<Node> _nodes = new List<Node>();
    readonly List<Edge> _edges = new List<Edge>();
    int _nextId = 1;

    Canvas _canvas;
    CanvasScaler _scaler;
    RectTransform _root, _world, _lineLayer, _menuLayer;
    Text _hint, _result;
    Edge _dragging;                    // 正在拉的线（旧路径，保留兼容）
    Node _dragFrom;
    Vector2 _dragTo;
    Node _selected;
    bool _panning; Vector2 _panStart; Vector2 _worldStart;

    /* ---------------- 创建 ---------------- */
    public static void SetLabVisible(bool labOn) {
        LabBootstrap.CalculatorActive = !labOn;
        if (LabBootstrap.Instance != null) LabBootstrap.Instance.SetLabUiVisible(labOn);
        /* 阿侬平时停在右下角，正好会盖住计算器下方的节点 —— 进计算器时挪到左下角 */
        PetView.Instance?.SetHomeForCalc(!labOn);
    }

    public static EnthalpyScene Show(GameObject host, Font font) {
        if (Instance != null) {
            Instance.gameObject.SetActive(true);
            SetLabVisible(false);      // 进计算器就藏起实验台界面（不只是透明，是不可交互）
            return Instance;
        }
        var go = new GameObject("EnthalpyScene");
        go.transform.SetParent(host.transform, false);
        var s = go.AddComponent<EnthalpyScene>();
        s.Build(font);
        Instance = s;
        /* 首次打开计算器 -> 教一遍怎么用（约 15 步）。
           PetTutorial 的注释里写着这条，但之前没有任何代码触发它 ——
           BornHaber 教程只能从右键选单手动开，
           而且 PetProgress.FinishCalc() 永远不会被调用，calcDone 一直是 no。 */
        if (PetProgress.LabDone && !PetProgress.CalcDone && !PetTutorial.Running)
            PetTutorial.StartDelayed(TutorialKind.BornHaber, 1.5f);
        SetLabVisible(false);      // 新建路径也要藏 —— 教程第一次打开走的就是这条
        return s;
    }

    void Build(Font font) {
        /* 无头测试时 UiFont 可能还没设 —— MkText 全都用它，为 null 会直接空引用 */
        if (LabUI.UiFont == null) LabUI.UiFont = font;

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        _scaler = gameObject.AddComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        _scaler.scaleFactor = ReadableScale();

        gameObject.AddComponent<GraphicRaycaster>();
        _root = (RectTransform)transform;

        /* 背景 */
        var bg = MkNode("BG", _root, new Color(0.06f, 0.08f, 0.12f, 1f));
        Stretch(bg);

        /* 可拖动的世界层 */
        _world = MkNode("World", _root, new Color(0, 0, 0, 0));
        Stretch(_world);
        _world.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        _world.gameObject.GetComponent<Image>().raycastTarget = true;
        var wd = _world.gameObject.AddComponent<WorldDrag>();
        wd.scene = this;

        _lineLayer = MkNode("Lines", _world, new Color(0, 0, 0, 0));
        Stretch(_lineLayer);
        _menuLayer = MkNode("Menus", _root, new Color(0, 0, 0, 0));
        Stretch(_menuLayer);

        /* 顶栏 */
        var bar = MkNode("TopBar", _root, new Color(0.10f, 0.14f, 0.20f, 0.98f));
        var brt = bar; brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
        brt.pivot = new Vector2(0.5f, 1); brt.anchoredPosition = new Vector2(0, 0);
        brt.sizeDelta = new Vector2(0, 46);
        MkText(bar, "焓变计算器 · Hess's Law", 17, new Color(0.6f, 0.92f, 0.75f), TextAnchor.MiddleLeft,
               new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 0), new Vector2(260, 0));
        MkBtn(bar, "返回实验台", () => Back(), new Vector2(-16, -8), new Vector2(110, 30), new Vector2(1, 1), new Color(0.20f, 0.30f, 0.42f));
        MkBtn(bar, "求解未知焓变", () => Solve(), new Vector2(-134, -8), new Vector2(130, 30), new Vector2(1, 1), new Color(0.20f, 0.55f, 0.35f));
        MkBtn(bar, "清空", () => ClearAll(), new Vector2(-272, -8), new Vector2(70, 30), new Vector2(1, 1), new Color(0.45f, 0.25f, 0.25f));

        _hint = MkText(_root, "空白处右键 = 新建节点 · 节点左键拖出箭头连线 · 连线/节点右键可编辑或删除", 13,
                       new Color(0.7f, 0.78f, 0.9f), TextAnchor.LowerLeft,
                       new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 12), new Vector2(-16, 40));
        _result = MkText(_root, "", 15, new Color(1f, 0.9f, 0.5f), TextAnchor.LowerLeft,
                         new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 42), new Vector2(-16, 70));

        /* 打开时先摆一个示例：三角形回路，帮助理解怎么用 */
        SeedExample();
    }

    static float ReadableScale() => Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 800f), 1f, 1.6f);
    Vector2 DiagramSpread() => new Vector2(
        Mathf.Clamp(Screen.width / Mathf.Max(1f, _scaler.scaleFactor) / 1280f, .65f, 1f),
        Mathf.Clamp(Screen.height / Mathf.Max(1f, _scaler.scaleFactor) / 800f, .72f, 1f));

    void SeedExample() {
        var spread = DiagramSpread();
        var a = AddNode(Vector2.Scale(new Vector2(-340, 60), spread), "1", "C");       // 石墨
        var b = AddNode(Vector2.Scale(new Vector2(40, 250), spread), "1", "CO2");
        var co = AddNode(Vector2.Scale(new Vector2(40, -140), spread), "1", "CO");
        Connect(a, b, -394, EnthalpySource.Table, "C + O2 -> CO2");
        Connect(a, co, -111, EnthalpySource.Table, "C + 0.5O2 -> CO");
        Connect(b, co, 0, EnthalpySource.Unknown, "");
        _result.text = "示例：C → CO2、C → CO 已知，CO → CO2 未知。点「求解未知焓变」。";
    }

    /* ---------------- 节点 ---------------- */
    public Node AddNode(Vector2 pos, string coef, string formula) {
        var n = new Node { Id = _nextId++, Pos = pos };
        /* 必须挂在 _world 下 —— 和连线同一个坐标系。
           挂在 _root 下的话，拖背景只会移动 _world（线跟着动），
           节点留在原地不动，两者就脱节了。 */
        var go = MkNode("Node" + n.Id, _world, new Color(0.12f, 0.18f, 0.26f, 0.97f));
        n.Rt = go;
        go.sizeDelta = new Vector2(210, 74);
        go.anchorMin = go.anchorMax = new Vector2(0.5f, 0.5f);
        go.pivot = new Vector2(0.5f, 0.5f);
        n.Bg = go.GetComponent<Image>();

        /* 系数输入框 */
        n.CoefField = MkInput(go, new Vector2(8, -8), new Vector2(44, 26), new Vector2(0, 1), "1");
        /* 化学式输入框 */
        n.FormulaField = MkInput(go, new Vector2(58, -8), new Vector2(144, 26), new Vector2(0, 1), "化学式");
        /* 规范化学式显示 */
        var disp = MkText(go, "", 20, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleCenter,
                          new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 12), new Vector2(-8, 46));
        n.FormulaField.onEndEdit.AddListener(s => Commit(n, s));

        var drag = go.gameObject.AddComponent<NodeDrag>();
        drag.scene = this; drag.node = n;

        _nodes.Add(n);
        Commit(n, formula);                     /* 匹配物种、写出规范化学式 */
        if (!string.IsNullOrEmpty(coef)) {
            n.CoefField.text = coef;
            /* TryParse 失败会把 out 参数置 0，那会让标注显示成「0CO₂」—— 解析不了就当 1 */
            if (!double.TryParse(coef, out n.Coef)) n.Coef = 1;
        }
        SyncNode(n);
        return n;
    }

    /// <summary>回车后匹配物种，并把纠正过上下标的化学式显示出来</summary>
    void Commit(Node n, string raw) {
        n.Raw = (raw ?? "").Trim();
        if (n.Raw.Length == 0) { n.Formula = ""; n.SpeciesId = ""; return; }
        var id = ChemData.Resolve(n.Raw);
        if (id != null) {
            ChemData.Sub.TryGetValue(id, out var s);
            n.SpeciesId = id;
            n.Formula = EqFormat.Pretty(s != null ? s.Formula : n.Raw);
            n.FormulaField.text = n.Raw;
        } else {
            /* 匹配不到就原样显示，并给出提示色 */
            n.SpeciesId = "";
            n.Formula = n.Raw;
        }
        SyncNode(n);
    }

    void SyncNode(Node n) {
        var texts = n.Rt.GetComponentsInChildren<Text>();
        if (texts.Length > 0) texts[texts.Length - 1].text = n.Formula;
        var img = n.Rt.GetComponent<Image>();
        bool matched = !string.IsNullOrEmpty(n.SpeciesId);
        img.color = n == _selected ? LabUiTheme.Accent : LabUiTheme.Card;
        if(img is LabUiSurface surface){surface.WarningBorder=!matched;surface.SetVerticesDirty();}
        n.Rt.anchoredPosition = n.Pos;
    }

    public void SelectNode(Node n) {
        _selected = n;
        foreach (var x in _nodes) SyncNode(x);
    }

    void RemoveNode(Node n) {
        for (int i = _edges.Count - 1; i >= 0; i--) if (_edges[i].A == n || _edges[i].B == n) RemoveEdge(_edges[i]);
        _nodes.Remove(n);
        Destroy(n.Rt.gameObject);
        if (_selected == n) _selected = null;
    }

    /* ================================================================
       拉线机制：单击节点 → 一根线跟着鼠标 → 再点另一个节点就连上
       ================================================================ */
    Node _pendingFrom;                 // 正在从哪个节点拉线
    RectTransform _pendingRt;          // 那根跟随鼠标的临时线

    public bool PendingEdge => _pendingFrom != null;
    public Node PendingFrom => _pendingFrom;

    /// <summary>开始拉线</summary>
    public void BeginEdgeMode(Node from) {
        CancelPending();
        _pendingFrom = from;
        _pendingRt = MkNode("PendingEdge", _lineLayer, new Color(0, 0, 0, 0));
        DrawPending(from.Pos);
    }

    public void CancelPending() {
        _pendingFrom = null;
        if (_pendingRt != null) { Destroy(_pendingRt.gameObject); _pendingRt = null; }
    }

    /// <summary>临时线跟随鼠标</summary>
    public void RefreshDragEdge() {
        if (_pendingFrom == null) return;
        DrawPending(ToWorld(LabInput.mousePosition));
    }

    void DrawPending(Vector2 target) {
        if (_pendingRt == null || _pendingFrom == null) return;
        Vector2 p0 = Boundary(_pendingFrom, target);
        Vector2 d = target - p0;
        for (int k = _pendingRt.childCount - 1; k >= 0; k--) Destroy(_pendingRt.GetChild(k).gameObject);
        if (d.sqrMagnitude < 6f) return;
        Vector2 dir = d.normalized, nrm = new Vector2(-dir.y, dir.x);
        Vector2 ctrl = (p0 + target) * 0.5f + nrm * Mathf.Clamp(d.magnitude * 0.20f, 24f, 120f);
        DrawCurve(_pendingRt, p0, ctrl, target, new Color(0.05f, 0.40f, 0.37f, 1f));
    }

    void Update() {
        /* 开发者控制台持有输入时不接受画布交互，也不继续跟随鼠标画待连的线。 */
        if (DeveloperConsole.InputCaptured) return;
        if (_scaler != null) _scaler.scaleFactor = ReadableScale();
        RefreshDragEdge();
    }

    /* ---------------- 连线 ---------------- */
    /// <summary>节点被移动后调它 —— 重画所有连线，并刷新正在拉的那根</summary>
    /// <summary>把正在拉的线落到目标节点上，然后问焓变来源</summary>
    public void CompletePending(Node to) {
        var from = _pendingFrom;
        CancelPending();
        if (from == null || to == null || from == to) return;
        Connect(from, to, 0, EnthalpySource.Unknown, "");
        var e = _edges[_edges.Count - 1];
        AskEnthalpy(e, false);
    }

    public void NodeMoved() {
        RedrawEdges();
        RefreshDragEdge();
    }

    public void BeginEdge(Node from) { _dragFrom = from; _dragTo = from.Pos; }

    public void Connect(Node a, Node b, double dh, EnthalpySource src, string eq) {
        if (a == null || b == null || a == b) return;
        var e = new Edge { A = a, B = b, DH = dh, Source = src, Equation = eq };
        var go = MkNode("Edge", _lineLayer, new Color(0, 0, 0, 0));
        e.Rt = go;
        /* 连线的点击热区：几乎全透明但必须可点（MkNode 现在只要 alpha > 0 就会建 Image） */
        var hit = MkNode("Hit", go, new Color(1, 1, 1, 0.02f));
        e.Hit = hit.GetComponent<Image>();
        var ed = go.gameObject.AddComponent<EdgeView>();
        ed.scene = this; ed.edge = e;
        _edges.Add(e);
        RedrawEdges();
    }

    void RemoveEdge(Edge e) {
        _edges.Remove(e);
        if (e.Rt != null) Destroy(e.Rt.gameObject);
        RedrawEdges();
    }

    /// <summary>重画所有连线。
    /// 路由：用一条二次贝塞尔，控制点沿「两节点连线」的法向偏移；
    /// 偏移量随距离增大，并检查是否穿过任何节点 —— 穿过了就加大偏移换一侧。</summary>
    void RedrawEdges() {
        foreach (var e in _edges) {
            Vector2 p0 = Boundary(e.A, e.B.Pos);   /* 端点贴在边框上，不是中心 */
            Vector2 p1 = Boundary(e.B, e.A.Pos);
            Vector2 d = p1 - p0;
            float len = Mathf.Max(1f, d.magnitude);
            Vector2 dir = d / len;
            Vector2 nrm = new Vector2(-dir.y, dir.x);
            /* 同一条直线上多条边时依次加大偏移，避免叠在一起 */
            int idx = _edges.Where(x => x != e && SameLine(x, e)).ToList().IndexOf(e);
            float baseOff = Mathf.Clamp(len * 0.22f, 40f, 150f) * (1f + 0.5f * Mathf.Max(0, idx));
            float off = baseOff;
            for (int side = 1; side >= -1; side -= 2) {
                bool ok = true;
                for (int k = 1; k <= 6; k++) {
                    float tt = k / 7f;
                    Vector2 pt = Quadratic(p0, p0 + nrm * (off * side) * 2f, p1, tt);
                    foreach (var n in _nodes) {
                        if (n == e.A || n == e.B) continue;
                        if (Mathf.Abs(pt.x - n.Pos.x) < 110 && Mathf.Abs(pt.y - n.Pos.y) < 45) { ok = false; break; }
                    }
                    if (!ok) break;
                }
                if (ok) { off *= side; break; }
                if (side == -1) off *= side;      // 两侧都不行就取负侧
            }
            Vector2 ctrl = (p0 + p1) * 0.5f + nrm * off * 2f;

            /* 画成多段线段构成的折线，末端加箭头 */
            RebuildEdgeMesh(e, p0, ctrl, p1);

            /* 标注两行：规范化反应式 + 焓变值。
               方程式过一遍 EqFormat，上下标与离子电荷才正确
               （CO2 → CO₂、H+ → H⁺、SO4^2- → SO₄²⁻）。 */
            var t = EnsureLabel(e);
            string eq = EdgeEquation(e);
            string dh = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                      : string.Format("ΔH = {0:F0} kJ/mol", e.DH);
            t.text = eq.Length > 0 ? eq + "\n" + dh : dh;
            t.fontSize = 13;
            var trt = (RectTransform)t.transform;
            trt.sizeDelta = new Vector2(186f, 40f);
            /* 在曲线中段试几个候选位置，挑一个不撞节点、不压线的。
               固定往法向推 26px 是不够的 —— 换个布局就会压到别的节点上。 */
            trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);
            /* 热区跟着标注走 ——
               原来 Hit 只在 Connect 里建了一次、从没重新定位，
               于是它一直停在父节点原点、尺寸也是 MkNode 的默认 100x100，
               连线根本点不到（右键连线弹出的会是空白处的「新建节点」选单）。 */
            if (e.Hit != null) {
                var hrt = (RectTransform)e.Hit.transform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
                hrt.pivot = new Vector2(0.5f, 0.5f);
                hrt.anchoredPosition = trt.anchoredPosition;
                hrt.sizeDelta = new Vector2(186f, 40f);   /* 和标注同尺寸（见下面的 LW/LH） */
            }
            t.color = LabUiTheme.Ink;

        /* 第二遍：第一遍是顺序贪心，先放的不知道后放的会在哪，
           于是两个标注可能横向压在一起。带着所有人的既有位置重排一次就分开了。 */
        for (int pass = 0; pass < 2; pass++) {
            foreach (var e2 in _edges) {
                if (e2.Rt == null) continue;
                var t2 = e2.Rt.GetComponentInChildren<Text>();
                if (t2 == null) continue;
                Vector2 q0 = Boundary(e2.A, e2.B.Pos), q1 = Boundary(e2.B, e2.A.Pos);
                Vector2 dd2 = q1 - q0;
                float len2 = Mathf.Max(1f, dd2.magnitude);
                Vector2 dir2 = dd2 / len2, nrm3 = new Vector2(-dir2.y, dir2.x);
                float off2 = Mathf.Clamp(len2 * 0.22f, 40f, 150f);
                Vector2 ctrl2 = (q0 + q1) * 0.5f + nrm3 * off2 * 2f;
                ((RectTransform)t2.transform).anchoredPosition = PickLabelPos(e2, q0, ctrl2, q1);
            }
        }
        }
    }

    static bool SameLine(Edge x, Edge y) {
        return (x.A == y.A && x.B == y.B) || (x.A == y.B && x.B == y.A);
    }
    /// <summary>节点矩形边框上、朝 toward 方向的那个点。
    /// 连线的端点要贴在边框上，不能戳进节点中心 —— 而且箭头得碰到边框才自然。</summary>
    public static Vector2 Boundary(Node n, Vector2 toward) {
        Vector2 d = toward - n.Pos;
        if (d.sqrMagnitude < 1e-6f) return n.Pos + new Vector2(NodeW * 0.5f, 0f);
        Vector2 dir = d.normalized;
        float hw = NodeW * 0.5f, hh = NodeH * 0.5f;
        float tx = hw / Mathf.Max(1e-4f, Mathf.Abs(dir.x));
        float ty = hh / Mathf.Max(1e-4f, Mathf.Abs(dir.y));
        return n.Pos + dir * Mathf.Min(tx, ty);
    }

    /// <summary>给一条连线的标注挑位置。
    /// 在曲线中段取几个 t，每个 t 试正反两侧法向，逐个打分：
    ///   撞节点          重罚
    ///   压到别的标注    中罚
    ///   离线太近        轻罚
    /// 取分最高的。这样标注总能落在相对干净的地方。 </summary>
    Vector2 PickLabelPos(Edge self, Vector2 p0, Vector2 ctrl, Vector2 p1) {
        const float LW = 186f, LH = 40f;      // 标注尺寸
        const float Off = LH * 0.5f + 34f;   // 净空 = 标注半高 + 34 —— 16 还是太近，视觉上仍压线
        float[] ts = { 0.5f, 0.38f, 0.62f, 0.28f, 0.72f };
        Vector2 best = Quadratic(p0, ctrl, p1, 0.5f) + new Vector2(0, Off);
        float bestScore = float.MinValue;

        foreach (var tt in ts) {
            Vector2 pt = Quadratic(p0, ctrl, p1, tt);
            Vector2 tan = Quadratic(p0, ctrl, p1, tt + 0.06f) - Quadratic(p0, ctrl, p1, tt - 0.06f);
            Vector2 nrm = tan.sqrMagnitude > 1e-6f
                        ? new Vector2(-tan.y, tan.x).normalized : new Vector2(0, 1);
            for (int side = 0; side < 2; side++) {
                Vector2 dir = side == 0 ? nrm : -nrm;
                Vector2 cand = pt + dir * Off;
                float score = 0f;
                /* ① 撞节点：重罚 */
                foreach (var nd in _nodes) {
                    float ox = Mathf.Max(0f, Mathf.Abs(cand.x - nd.Pos.x) - (NodeW * 0.5f + LW * 0.5f));
                    float oy = Mathf.Max(0f, Mathf.Abs(cand.y - nd.Pos.y) - (NodeH * 0.5f + LH * 0.5f));
                    float gap = Mathf.Sqrt(ox * ox + oy * oy);
                    score -= Mathf.Max(0f, 60f - gap) * 3f;
                }
                /* ② 和其它标注重叠：重罚。
                   判据必须是「矩形分离」，不能用中心距 ——
                   标注本身 186 宽，中心距 64 看着合格其实早就叠上了。 */
                foreach (var oe in _edges) {
                    if (oe == self || oe.Rt == null) continue;
                    var ot = oe.Rt.GetComponentInChildren<Text>();
                    if (ot == null) continue;
                    var ort = (RectTransform)ot.transform;
                    Vector2 opos = ort.anchoredPosition;
                    float gapX = Mathf.Max(0f, Mathf.Abs(cand.x - opos.x) - LW);
                    float gapY = Mathf.Max(0f, Mathf.Abs(cand.y - opos.y) - LH);
                    float gap = Mathf.Sqrt(gapX * gapX + gapY * gapY);
                    if (gap < 24f) score -= (24f - gap) * 6f;      // 靠太近就重罚
                }
                /* ③ 离线太近（标注会压住线）：轻罚 */
                float minD = float.MaxValue;
                for (int k = 1; k <= 12; k++) {
                    Vector2 q = Quadratic(p0, ctrl, p1, k / 13f);
                    minD = Mathf.Min(minD, Vector2.Distance(cand, q));
                }
                /* 离曲线太近 = 视觉上压线，罚得重一点 */
                score -= Mathf.Max(0f, (LH * 0.5f + 10f) - minD) * 3f;
                /* ④ 稍微偏好「落在画面中上部」，读起来顺 */
                score += cand.y * 0.02f;
                if (score > bestScore) { bestScore = score; best = cand; }
            }
        }
        return best;
    }

    static Vector2 Quadratic(Vector2 a, Vector2 c, Vector2 b, float t) {
        float u = 1 - t;
        return u * u * a + 2 * u * t * c + t * t * b;
    }

    Text EnsureLabel(Edge e) {
        var t = e.Rt.GetComponentsInChildren<Text>().FirstOrDefault();
        if (t != null) return t;
        return MkText(e.Rt, "", 14, Color.white, TextAnchor.MiddleCenter,
                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    /// <summary>把曲线画成 UI 里的白色细条 + 末端箭头</summary>
    void RebuildEdgeMesh(Edge e, Vector2 p0, Vector2 ctrl, Vector2 p1) {
        DrawCurve(e.Rt, p0, ctrl, p1, LabUiTheme.Ink);
    }

    /// <summary>把一条二次贝塞尔画成细条 + 末端箭头。连线与临时拉线共用。</summary>
    void DrawCurve(RectTransform rt, Vector2 p0, Vector2 ctrl, Vector2 p1, Color col) {
        if (rt == null) return;
        for (int i = rt.childCount - 1; i >= 0; i--) {
            var ch = rt.GetChild(i);
            if (ch.name == "Seg" || ch.name == "Head") Destroy(ch.gameObject);
        }
        const int N = 14;
        Vector2 prev = p0;
        for (int i = 1; i <= N; i++) {
            Vector2 pt = Quadratic(p0, ctrl, p1, (float)i / N);
            var seg = MkNode("Seg", rt, col);
            Vector2 mid = (prev + pt) * 0.5f;
            Vector2 dv = pt - prev;
            seg.anchoredPosition = mid;
            seg.sizeDelta = new Vector2(dv.magnitude, 2.2f);
            seg.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dv.y, dv.x) * Mathf.Rad2Deg);
            prev = pt;
        }
        /* 箭头 */
        Vector2 tip = p1;
        Vector2 back = Quadratic(p0, ctrl, p1, (float)(N - 1) / N);
        Vector2 dir = (tip - back).normalized;
        var head = MkNode("Head", rt, col);
        head.anchoredPosition = tip - dir * 9f;
        head.sizeDelta = new Vector2(16, 2.2f);
        head.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        /* 两根斜线做箭头尖 */
        for (int k = 0; k < 2; k++) {
            var b = MkNode("Head", rt, col);
            float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + (k == 0 ? 150f : -150f);
            Vector2 bd = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            b.anchoredPosition = tip + bd * 7f;
            b.sizeDelta = new Vector2(14, 2.2f);
            b.localRotation = Quaternion.Euler(0, 0, a);
        }
    }

    /* ---------------- 右键选单 ---------------- */
    void ContextMenu(Vector2 screenPos, List<(string label, System.Action act)> items) {
        Debug.Log("[MenuDbg] ContextMenu 打开，条目=" + items.Count + " 位置=" + screenPos);
        CloseMenu();
        float h = items.Count * 30 + 10;
        PetMenu.Close();

        /* 全屏「挡板」：左键点画面任意别处就把选单关掉。
           必须先建它 —— 这样选单才盖在它上面、点得到。
           挡板本身几乎全透明（alpha 0.001），看不见但能接收点击。 */
        /* alpha 必须 > 0.001 —— MkNode 里是 `if (col.a > 0f)` 才加 Image，
           写 0.001 的话 0.001 > 0.001 是 false，压根不会创建 Image，
           没有 Graphic 就没有 raycast 目标，挡板收不到任何点击（选单永远关不掉）。 */
        var catcher = MkNode("MenuCatcher", _menuLayer, new Color(0, 0, 0, 0.004f));
        Stretch(catcher);
        var cb = catcher.gameObject.AddComponent<Button>();
        cb.transition = Selectable.Transition.None;
        cb.onClick.AddListener(CloseMenu);
        catcher.gameObject.AddComponent<MenuDismiss>().Close = CloseMenu;

        var panel = MkNode("Menu", _menuLayer, new Color(0.12f, 0.16f, 0.22f, 0.99f));
        // screenPos 是输入系统的物理屏幕像素；画布由 CanvasScaler 缩放，
        // 不能直接塞进 anchoredPosition，否则高 DPI / 非参考分辨率时菜单会跑出画面。
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0, 1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_menuLayer, screenPos, null, out var localPos);
        panel.anchoredPosition = localPos;
        panel.sizeDelta = new Vector2(190, h);
        MenuPlacement.AtPointer(panel, _menuLayer, screenPos);
        for (int i = 0; i < items.Count; i++) {
            var it = items[i];
            if (it.act == null) continue;                 /* 分隔线 */
            MkBtn(panel, it.label, () => { CloseMenu(); it.act(); },
                  new Vector2(-6, -6 - i * 30), new Vector2(178, 28), new Vector2(1, 1),
                  new Color(0.18f, 0.24f, 0.32f));
        }
    }
    public void CloseMenu() {
        if (_menuLayer == null) return;
        for (int i = _menuLayer.childCount - 1; i >= 0; i--) {
            var child = _menuLayer.GetChild(i).gameObject;
            child.SetActive(false); Destroy(child);
        }
    }

    /* ---------------- 用例入口 ---------------- */
    public void OnWorldRightClick(Vector2 pos) {
        ContextMenu(pos, new List<(string, System.Action)> {
            ("在此新建节点", () => {
                var n = AddNode(ToWorld(pos), "1", "");
                SelectNode(n);
            }),
        });
    }
    public void OnNodeRightClick(Node n, Vector2 pos) {
        ContextMenu(pos, new List<(string, System.Action)> {
            ("编辑化学式", () => { SelectNode(n); n.FormulaField.ActivateInputField(); }),
            ("编辑系数", () => { n.CoefField.ActivateInputField(); }),
            ("复制节点", () => { var m = AddNode(n.Pos + new Vector2(40, -90), n.Coef.ToString("0.#"), n.Raw); SelectNode(m); }),
            ("删除节点", () => RemoveNode(n)),
        });
    }
    public void OnEdgeRightClick(Edge e, Vector2 pos) {
        ContextMenu(pos, new List<(string, System.Action)> {
            ("编辑焓变值", () => AskEnthalpy(e, true)),
            ("删除连线", () => RemoveEdge(e)),
        });
    }

    /// <summary>问用户这条边的焓变从哪来</summary>
    public void AskEnthalpy(Edge e, bool editing) {
        CloseMenu();
        /* 先尝试查表 */
        double t = double.NaN; string note = "";
        if (e.A.SpeciesId.Length > 0 && e.B.SpeciesId.Length > 0) {
            string eq = $"{e.A.Coef:0.#}{e.A.SpeciesId} -> {e.B.Coef:0.#}{e.B.SpeciesId}";
            t = HessLaw.Lookup(eq, out note);
        }
        var items = new List<(string, System.Action)>();
        if (!double.IsNaN(t)) items.Add(($"查表：{t:F0} kJ/mol（{note}）", () => { e.DH = t; e.Source = EnthalpySource.Table; RedrawEdges(); }));
        items.Add(("手动输入…", () => AskManualValue(e, !editing)));
        items.Add(("设为未知量（待求）", () => { e.Source = EnthalpySource.Unknown; e.DH = 0; RedrawEdges(); }));
        items.Add(("取消（撤销这条线）", () => { if (!editing) RemoveEdge(e); }));
        ContextMenu(LabInput.mousePosition, items);
    }

    /// <summary>把这条连线两端的节点写成一条规范化化学方程式。
    /// 离子会显示成 H⁺ / SO₄²⁻ 这种正确形式，系数也带上。</summary>
    public string EdgeEquation(Edge e) {
        if (e.A == null || e.B == null) return "";
        string L = Term(e.A), R = Term(e.B);
        if (L.Length == 0 || R.Length == 0) return "";
        return L + " → " + R;
    }
    string Term(Node n) {
        string f = n.Formula.Length > 0 ? n.Formula : n.Raw;
        if (f.Length == 0) return "";
        /* 优先用匹配到的物种分子式，再走 EqFormat 做上下标规范化 */
        string raw = n.SpeciesId.Length > 0 ? FormulaOf(n.SpeciesId) : f;
        string pretty = EqFormat.Pretty(raw);
        if (n.Coef == 1) return pretty;
        return (n.Coef == System.Math.Floor(n.Coef) ? ((int)n.Coef).ToString() : n.Coef.ToString("0.#")) + pretty;
    }
    static string FormulaOf(string id) {
        ChemData.Sub.TryGetValue(id, out var s);
        return s != null ? s.Formula : id;
    }

    /* ---------------- 手动输入数值对话框 ---------------- */
    /// <summary>弹出一个小窗口让用户输入 kJ/mol 数值。
    /// 这是「手动输入」这条路径真正缺的一环 —— 之前只是给了个占位值 −100。</summary>
    public void AskManualValue(Edge e, bool firstTime) {
        CloseMenu();
        var save = e.DH;
        var srcSave = e.Source;

        var panel = MkNode("ValueDlg", _menuLayer, new Color(0.12f, 0.16f, 0.22f, 0.99f));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(320, 150);
        panel.SetAsLastSibling();

        MkText(panel, "输入这条连线的焓变 ΔH（kJ/mol）", 14, new Color(0.85f, 0.92f, 1f),
               TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(10, -34), new Vector2(-10, -8));

        var fld = MkInput(panel, new Vector2(0, 0), new Vector2(150, 30),
                          new Vector2(0.5f, 0.5f), e.DH.ToString("0.#"));
        var frt = (RectTransform)fld.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(0, 4);
        fld.text = e.DH.ToString("0.#");
        fld.ActivateInputField();

        System.Action commit = () => {
            double v;
            if (double.TryParse(fld.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v)) {
                e.DH = v; e.Source = EnthalpySource.Manual;
                RedrawEdges();
                _result.text = $"已设为手动值 ΔH = {v:F1} kJ/mol";
            } else {
                _result.text = $"「{fld.text}」不是有效数字，已忽略";
                e.DH = save; e.Source = srcSave;
            }
            Destroy(panel.gameObject);
        };
        System.Action cancel = () => {
            e.DH = save; e.Source = srcSave;
            if (firstTime) RemoveEdge(e);       // 第一次创建时取消 = 撤销这条线
            RedrawEdges();
            _result.text = firstTime ? "已撤销这条连线。" : "已取消编辑。";
            Destroy(panel.gameObject);
        };

        MkBtn(panel, "确定", commit, new Vector2(-14, 12), new Vector2(80, 28), new Vector2(1, 0), new Color(0.20f, 0.55f, 0.35f));
        MkBtn(panel, "取消", cancel, new Vector2(-104, 12), new Vector2(80, 28), new Vector2(1, 0), new Color(0.42f, 0.26f, 0.26f));
        /* 回车 = 确定，Esc 处理不了就用右键取消 */
        fld.onEndEdit.AddListener(s => { if (LabInput.GetKeyDown(KeyCode.Return) || LabInput.GetKeyDown(KeyCode.KeypadEnter)) commit(); });
    }

    /* ---------------- 求解 ---------------- */
    public void Solve() {
        if (_edges.Count == 0) { _result.text = "还没有连线。"; return; }
        var nodes = _nodes.Select((n, i) => new { n, i }).ToDictionary(x => x.n, x => x.i);
        var es = _edges.Select(e => new HessEdge {
            From = nodes[e.A], To = nodes[e.B], DH = e.DH, Source = e.Source,
        }).ToList();
        var cyc = HessLaw.FindCycle(_nodes.Count, es);
        if (cyc == null || cyc.Count < 4) { _result.text = "没有找到闭合回路：至少要三个节点、三条首尾相接的连线。"; return; }
        /* 把求解结果写回界面上对应的边 */
        bool ok = HessLaw.SolveCycle(cyc, es, out double unk, out string why);
        if (ok) {
            for (int i = 0; i < es.Count; i++) {
                if (es[i].Source == EnthalpySource.Unknown && _edges[i].Source == EnthalpySource.Unknown)
                    _edges[i].DH = es[i].DH;
            }
            RedrawEdges();
        }
        string path = string.Join(" → ", cyc.Select(i => NameOf(i)));
        _result.text = $"回路：{path}\n{why}";
        Debug.Log("[SolveResult] " + why);   /* 同时打日志，窗口测试靠它判定 */
    }

    string NameOf(int i) {
        if (i < 0 || i >= _nodes.Count) return "?";
        var n = _nodes[i];
        return n.Formula.Length > 0 ? n.Formula : ("#" + n.Id);
    }

    public void ClearAll() {
        for (int i = _nodes.Count - 1; i >= 0; i--) RemoveNode(_nodes[i]);
        _edges.Clear(); RedrawEdges();
        _result.text = "已清空。";
    }

    void Back() {
        gameObject.SetActive(false);
        SetLabVisible(true);      // 返回实验台时把界面还回来
    }

    /// <summary>切换实验台界面的显隐（并挡住它的输入）</summary>

    /* ================================================================
       输入诊断：计算器打开后调它，把「为什么点不动」直接打出来
         · EventSystem 还在不在、是不是被禁用了
         · 场景里有哪些画布、排序号、有没有 GraphicRaycaster
         · 在屏幕中心做一次真实射线，看命中谁
       ================================================================ */
    public string AuditInput() {
        var sb = new System.Text.StringBuilder();
        var es = UnityEngine.EventSystems.EventSystem.current;
        sb.AppendLine("[InputAudit] EventSystem.current = " + (es == null ? "null！" : es.gameObject.name));
        if (es != null) {
            sb.AppendLine("[InputAudit]   activeInHierarchy = " + es.gameObject.activeInHierarchy
                        + "  enabled = " + es.enabled
                        + "  父节点 = " + (es.transform.parent != null ? es.transform.parent.name : "(根)"));
            var im = es.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
            sb.AppendLine("[InputAudit]   输入模块 = " + (im == null ? "没有！" : im.GetType().Name
                        + (im.enabled ? " (启用)" : " (禁用！)")));
        }
        foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
            var gr = cv.GetComponent<GraphicRaycaster>();
            sb.AppendLine("[InputAudit] 画布 " + cv.gameObject.name
                + " 模式=" + cv.renderMode + " 排序=" + cv.sortingOrder
                + " 激活=" + cv.gameObject.activeInHierarchy
                + " Raycaster=" + (gr == null ? "无" : (gr.enabled ? "有" : "有但禁用")));
        }
        /* 在屏幕中心做一次真实射线 */
        if (es != null) {
            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var res = new List<UnityEngine.EventSystems.RaycastResult>();
            foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
                var gr = cv.GetComponent<GraphicRaycaster>();
                if (gr == null || !gr.enabled || !cv.gameObject.activeInHierarchy) continue;
                gr.Raycast(ped, res);
            }
            sb.AppendLine("[InputAudit] 屏幕中心射线命中 " + res.Count + " 个：");
            for (int i = 0; i < Mathf.Min(5, res.Count); i++)
                sb.AppendLine("[InputAudit]   " + i + ": " + res[i].gameObject.name
                            + " (depth " + res[i].depth + ", sortingOrder " + res[i].sortingOrder + ")");
        }
        return sb.ToString();
    }

    /// <summary>把计算器里关键控件的中心点按「游戏内屏幕坐标」打出来，供外部自动化点击。
    /// 输出格式与 LabUI.TestClickTargets 一致：CLICK|名字|x|y</summary>
    public string TestClickTargets() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SCREEN|" + Screen.width + "|" + Screen.height);
        void Emit(string name, RectTransform rt) {
            if (rt == null) return;
            var c = rt.TransformPoint(rt.rect.center);
            sb.AppendLine("CLICK|" + name + "|" + Mathf.RoundToInt(c.x) + "|" + Mathf.RoundToInt(c.y));
        }
        /* 画布上的按钮（清空 / 求解 / 返回） */
        foreach (var rt in GetComponentsInChildren<RectTransform>(true)) {
            if (rt.name == "B") {
                var txt = rt.GetComponentInChildren<Text>();
                if (txt == null) continue;
                string t = txt.text;
                if (t == "清空") Emit("calc_clear", rt);
                else if (t == "求解未知焓变") Emit("calc_solve", rt);
                else if (t == "返回实验台") Emit("calc_back", rt);
                else if (t == "确定") Emit("dlg_ok", rt);
                else if (t == "取消") Emit("dlg_cancel", rt);
            }
        }
        /* 节点 */
        for (int i = 0; i < _nodes.Count; i++) Emit("node" + i, _nodes[i].Rt);
        /* 连线 */
        for (int i = 0; i < _edges.Count; i++) {
            if (_edges[i].Rt == null) continue;
            var t = _edges[i].Rt.GetComponentInChildren<Text>();
            if (t == null) continue;
            Emit("edge" + i, (RectTransform)t.transform);
        }
        return sb.ToString();
    }

    /// <summary>最近一次求解是否成功（教程用它判断用户点没点对）</summary>
    public bool LastSolvedOk { get; private set; }

    /// <summary>摆出氯化钾的 Born-Haber 循环，供教程使用。
    ///   K(s) → K(g)            +89
    ///   K(g) → K⁺(g) + e⁻      +418
    ///   ½Cl₂(g) → Cl(g)        +121
    ///   Cl(g) + e⁻ → Cl⁻(g)    −349
    ///   合起来 = KCl(s) 生成焓 −437 → 晶格能 = −437 − 279 = −716
    /// </summary>
    public void SeedBornHaberDemo() {
        ClearAll();
        var spread = DiagramSpread();
        var elem = AddNode(Vector2.Scale(new Vector2(-380, 40), spread),  "1",  "K");
        var ion  = AddNode(Vector2.Scale(new Vector2(-120, 220), spread), "1",  "K+");
        var cl   = AddNode(Vector2.Scale(new Vector2(-120, -150), spread), "1", "Cl-");
        var salt = AddNode(Vector2.Scale(new Vector2(180, 40), spread),   "1",  "KCl");
        /* 元素 → 气态离子的两步（原子化 + 电离），这里合并成一条手动边 */
        Connect(elem, ion,  89 + 418, EnthalpySource.Manual, "K(s) -> K+(g) + e-");
        Connect(elem, cl,   121 - 349, EnthalpySource.Manual, "0.5Cl2(g) + e- -> Cl-(g)");
        Connect(ion,  salt, 0, EnthalpySource.Unknown, "K+(g) + Cl-(g) -> KCl(s)");
        Connect(cl,   salt, 0, EnthalpySource.Unknown, "Cl-(g) -> KCl(s)");
        /* 为了让回路闭合，把「元素 → KCl」用生成焓连起来 */
        Connect(elem, salt, -437, EnthalpySource.Manual, "K(s) + 0.5Cl2(g) -> KCl(s)");
        _result.text = "KCl 的 Born-Haber 循环已搭好：晶格能是未知量。点「求解未知焓变」。";
    }

    /// <summary>教程用：把求解结果也记下来</summary>
    public void MarkSolved(bool ok) { LastSolvedOk = ok; }

    /* ================================================================
       测试接口：验证「节点与连线是一个整体」
         用户报的问题：拖背景只有线动、拖节点线不动。
         根因是节点挂在 _root、连线挂在 _world，两者不在同一坐标系。
         这里把关键量暴露出来，让自检能真的验证：
           · 节点是不是 _world 的子节点（拖背景才会一起走）
           · 移动节点后连线的端点有没有跟着变
       ================================================================ */
    /// <summary>节点是否挂在 _world 下（拖背景时节点要跟着动）</summary>
    public bool TestNodeUnderWorld(int idx) {
        if (idx < 0 || idx >= _nodes.Count) return false;
        Transform t = _nodes[idx].Rt;   // 必须是 Transform —— parent 返回的就是 Transform
        /* 直接往上找有没有叫 World 的祖先。
           拼路径的话，从下往上拼出来的开头是根节点名，
           拿 StartsWith("World/") 判会把全部节点误判为「不在」。 */
        while (t != null) {
            if (t.name == "World") return true;
            t = t.parent;
        }
        return false;
    }
    public int TestNodeCount => _nodes.Count;
    public int TestEdgeCount => _edges.Count;
    public Edge TestFirstEdge() => _edges.Count > 0 ? _edges[0] : null;
    public Vector2 TestNodePos(int idx) => (idx >= 0 && idx < _nodes.Count) ? _nodes[idx].Pos : Vector2.zero;
    public void TestMoveNode(int idx, Vector2 d) {
        if (idx < 0 || idx >= _nodes.Count) return;
        _nodes[idx].Pos += d;
        _nodes[idx].Rt.anchoredPosition = _nodes[idx].Pos;
        NodeMoved();
    }
    /// <summary>所有连线标注的锚点位置（用于检查标注之间有没有叠在一起）</summary>
    public List<Vector2> TestLabelPositions() {
        var res = new List<Vector2>();
        foreach (var e in _edges) {
            if (e.Rt == null) { res.Add(Vector2.zero); continue; }
            var t = e.Rt.GetComponentInChildren<Text>();
            res.Add(t != null ? ((RectTransform)t.transform).anchoredPosition : Vector2.zero);
        }
        return res;
    }

    /// <summary>某条连线的标注中心到曲线的最短距离。负数表示取不到。 </summary>
    public float TestLabelClearance(int idx) {
        if (idx < 0 || idx >= _edges.Count) return -1f;
        var e = _edges[idx];
        if (e.Rt == null) return -1f;
        var t = e.Rt.GetComponentInChildren<Text>();
        if (t == null) return -1f;
        Vector2 lp = ((RectTransform)t.transform).anchoredPosition;
        /* 重算这条线的控制点（与 RedrawEdges 同样的算法） */
        Vector2 p0 = Boundary(e.A, e.B.Pos), p1 = Boundary(e.B, e.A.Pos);
        Vector2 d = p1 - p0;
        float len = Mathf.Max(1f, d.magnitude);
        Vector2 dir = d / len, nrm = new Vector2(-dir.y, dir.x);
        float off = Mathf.Clamp(len * 0.22f, 40f, 150f);
        Vector2 ctrl = (p0 + p1) * 0.5f + nrm * off * 2f;
        float minD = float.MaxValue;
        for (int k = 0; k <= 40; k++)
            minD = Mathf.Min(minD, Vector2.Distance(lp, Quadratic(p0, ctrl, p1, k / 40f)));
        return minD;
    }

    /// <summary>一条连线的「起点、箭尖」两个世界坐标。
    /// 只取两个点 —— 中间线段的数量会随弧度变化，按数量对比会误判。</summary>
    public List<Vector3> TestEdgeEnds(int idx) {
        var res = new List<Vector3>();
        if (idx < 0 || idx >= _edges.Count) return res;
        var rt = _edges[idx].Rt;
        if (rt == null) return res;
        RectTransform firstSeg = null, lastHead = null;
        for (int i = 0; i < rt.childCount; i++) {
            var ch = rt.GetChild(i) as RectTransform;
            if (ch == null) continue;
            if (ch.name == "Seg" && firstSeg == null) firstSeg = ch;
            if (ch.name == "Head") lastHead = ch;
        }
        if (firstSeg != null) res.Add(firstSeg.TransformPoint(firstSeg.rect.center));
        if (lastHead != null) res.Add(lastHead.TransformPoint(lastHead.rect.center));
        return res;
    }
    /// <summary>World 层的位移（拖背景用）</summary>
    public Vector2 TestWorldPos => _world.anchoredPosition;
    public void TestPanWorld(Vector2 d) { _world.anchoredPosition += d; }
    /// <summary>节点屏幕坐标随 World 位移应同步变化</summary>
    public Vector3 TestNodeWorldPoint(int idx) {
        if (idx < 0 || idx >= _nodes.Count) return Vector3.zero;
        return _nodes[idx].Rt.TransformPoint(Vector3.zero);
    }
    /// <summary>走一遍「单击节点 → 拉线 → 点另一个节点」</summary>
    public bool TestPullEdge(int fromIdx, int toIdx) {
        if (fromIdx < 0 || fromIdx >= _nodes.Count) return false;
        if (toIdx < 0 || toIdx >= _nodes.Count) return false;
        int before = _edges.Count;
        BeginEdgeMode(_nodes[fromIdx]);
        bool pending = PendingEdge;
        CompletePending(_nodes[toIdx]);
        CancelPending();
        return pending && _edges.Count == before + 1;
    }

    /* ================================================================
       布局审计：把所有控件的屏幕矩形打出来。
       用它来判断按钮到底在不在画面里、有没有互相压住 ——
       比对着截图猜可靠得多。
       ================================================================ */
    /// <summary>计算器的当前状态，给外部测试脚本当客观证据。
    /// 输出格式：STATE|键|值</summary>
    /// <summary>RectTransform 中心点的**屏幕坐标**。
    /// 注意：不能直接用 TransformPoint —— 那是画布坐标，
    /// 画布带 CanvasScaler（这台机器上 scaleFactor ≈ 1.4），
    /// 报出去外部脚本照着点会整片偏掉。 </summary>
    public static Vector2 ScreenOf(RectTransform rt) {
        if (rt == null) return Vector2.zero;
        Vector3 wp = rt.TransformPoint(rt.rect.center);
        var cv = rt.GetComponentInParent<Canvas>();
        if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
            return RectTransformUtility.WorldToScreenPoint(null, wp);
        return RectTransformUtility.WorldToScreenPoint(Camera.main, wp);
    }


    public string StateAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("STATE|nodes|" + _nodes.Count);
        sb.AppendLine("STATE|edges|" + _edges.Count);
        sb.AppendLine("STATE|pending|" + (PendingEdge ? "yes" : "no"));
        sb.AppendLine("STATE|menu|" + (_menuLayer != null && _menuLayer.childCount > 0 ? "open" : "closed"));
        sb.AppendLine("STATE|solved|" + (LastSolvedOk ? "yes" : "no"));
        /* 手动输入对话框开着没有 */
        bool dlgOpen = false;
        if (_menuLayer != null) {
            foreach (var ch in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {
                if (ch.name == "ValueDlg" && ch.gameObject.activeInHierarchy) { dlgOpen = true; break; }
            }
        }
        sb.AppendLine("STATE|dlg|" + (dlgOpen ? "open" : "closed"));
        /* 对话框里各控件的屏幕坐标：外部脚本直接点，不猜 */
        if (dlgOpen && _menuLayer != null) {
            foreach (var ch in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {
                if (!ch.gameObject.activeInHierarchy) continue;
                if (ch.name == "ValueDlg") {
                    var dc = ScreenOf(ch);
                    sb.AppendLine("STATE|dlgBox|" + Mathf.RoundToInt(dc.x) + "," + Mathf.RoundToInt(dc.y));
                    continue;
                }
                var bt = ch.GetComponentInChildren<Text>();
                if (bt == null || ch.name != "B") continue;
                var bc = ScreenOf(ch);
                if (bt.text == (Localization.English ? "OK" : "确定"))
                    sb.AppendLine("STATE|dlgOK|" + Mathf.RoundToInt(bc.x) + "," + Mathf.RoundToInt(bc.y));
                else if (bt.text == (Localization.English ? "Cancel" : "取消"))
                    sb.AppendLine("STATE|dlgCancel|" + Mathf.RoundToInt(bc.x) + "," + Mathf.RoundToInt(bc.y));
            }
        }
        foreach (var e in _edges) {
            string a = e.A != null ? e.A.Formula : "?";
            string b = e.B != null ? e.B.Formula : "?";
            sb.AppendLine("STATE|edge|" + a + "->" + b + "|" + e.DH.ToString("F0") + "|" + e.Source);
        }
        return sb.ToString();
    }

    public string AuditLayout() {
        /* 必须先强制跑一次布局，否则读到的还是没解析过的矩形 */
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        var canvasRt = (RectTransform)transform;
        sb.AppendLine($"[CalcAudit] 画布 {canvasRt.rect.width:F0}x{canvasRt.rect.height:F0}  屏幕 {Screen.width}x{Screen.height}");
        int bad = 0;
        foreach (var rt in GetComponentsInChildren<RectTransform>(true)) {
            if (rt == canvasRt) continue;
            var t = rt.GetComponent<Text>();
            var img = rt.GetComponent<Image>();
            string label = t != null && !string.IsNullOrEmpty(t.text) ? t.text
                         : (img != null ? "[图]" : "");
            if (label.Length == 0) continue;
            if (rt.name == "T" || rt.name == "BG" || rt.name == "Seg" || rt.name == "Head") continue;
            /* 世界坐标（ScreenSpaceOverlay 下就是屏幕像素） */
            var c0 = rt.TransformPoint(rt.rect.min);
            var c1 = rt.TransformPoint(rt.rect.max);
            float x0 = Mathf.Min(c0.x, c1.x), x1 = Mathf.Max(c0.x, c1.x);
            float y0 = Mathf.Min(c0.y, c1.y), y1 = Mathf.Max(c0.y, c1.y);
            bool offscreen = x1 < 0 || y1 < 0 || x0 > Screen.width || y0 > Screen.height;
            if (offscreen) bad++;
            sb.AppendLine($"[CalcAudit] {rt.name,-12} 「{Trim(label),-16}」 x {x0:F0}..{x1:F0}  y {y0:F0}..{y1:F0}"
                        + (offscreen ? "   ← 在画面外！" : ""));
        }
        /* 节点清单：名字直接用化学式，外部脚本按名字点，不用猜编号 */
        for (int ni = 0; ni < _nodes.Count; ni++) {
            var nd = _nodes[ni];
            if (nd == null || nd.Rt == null) continue;
            var cc = ScreenOf(nd.Rt);
            string f = nd.Formula.Length > 0 ? nd.Formula : nd.Raw;
            sb.AppendLine("[CalcAudit] CLICK|node_" + f + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)
                        + "|" + Mathf.RoundToInt(nd.Rt.rect.width) + "|" + Mathf.RoundToInt(nd.Rt.rect.height));
        }

        /* 连线清单：名字用方程，外部脚本按方程点，编号靠不住 */
        for (int ei = 0; ei < _edges.Count; ei++) {
            var ed = _edges[ei];
            if (ed == null || ed.Rt == null) continue;
            var et = ed.Rt.GetComponentInChildren<Text>();
            if (et == null) continue;
            var cc2 = ScreenOf((RectTransform)et.transform);
            string eq2 = EdgeEquation(ed);
            if (eq2.Length == 0) eq2 = "edge" + ei;
            sb.AppendLine("[CalcAudit] CLICK|edge_" + eq2 + "|" + Mathf.RoundToInt(cc2.x) + "|" + Mathf.RoundToInt(cc2.y) + "|0|0");
        }

        /* 选单项清单：右键弹出菜单后才知道有哪些项，名字就是按钮文字 */
        if (_menuLayer != null) {
            foreach (var mrt in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {
                if (mrt.name != "B") continue;
                var mt = mrt.GetComponentInChildren<Text>();
                if (mt == null || mt.text.Length == 0) continue;
                var mc = ScreenOf(mrt);
                sb.AppendLine("[CalcAudit] CLICK|menu_" + mt.text + "|" + Mathf.RoundToInt(mc.x) + "|" + Mathf.RoundToInt(mc.y) + "|0|0");
            }
        }
        sb.AppendLine($"[CalcAudit] 画面外的控件数 = {bad}");
        /* 可机读的控件清单：外部自动化脚本按名字点击，不用猜坐标 */
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null) {
                foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true)) {
                    var txt = rt.GetComponentInChildren<Text>();
                    string nm = null;
                    if (rt.name == "B" && txt != null) nm = "btn_" + txt.text;
                    /* 节点不在这里报名字 —— 它下面有系数框和化学式框两个 Text，
                       拿到的会是系数。节点的坐标在下面单独从 _nodes 遍历输出。 */
                    else if (rt.name.StartsWith("Node")) continue;
                    else if (txt != null && txt.text != null && (txt.text.Contains("ΔH") || txt.text.Contains("→")))
                        nm = "label_" + rt.name;
                    if (nm == null) continue;
                    var cc = ScreenOf(rt);
                    sb.AppendLine("[CalcAudit] CLICK|" + nm + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)
                                + "|" + Mathf.RoundToInt(rt.rect.width) + "|" + Mathf.RoundToInt(rt.rect.height));
                }
            }
        }

        /* ---- 几何检查：连线不得压过任何节点（这是硬要求）---- */
        int cross = 0;
        foreach (var e in _edges) {
            if (e == null || e.A == null || e.B == null) continue;
            var p0 = e.A.Pos; var p1 = e.B.Pos;
            Vector2 dd = p1 - p0;
            float len = Mathf.Max(1f, dd.magnitude);
            Vector2 dir = dd / len, nrm = new Vector2(-dir.y, dir.x);
            float off = Mathf.Clamp(len * 0.22f, 40f, 150f);
            Vector2 ctrl = (p0 + p1) * 0.5f + nrm * off * 2f;   /* 与 RedrawEdges 一致的路由 */
            foreach (var n in _nodes) {
                if (n == e.A || n == e.B) continue;
                for (int k = 1; k <= 20; k++) {
                    Vector2 pt = Quadratic(p0, ctrl, p1, k / 21f);
                    if (Mathf.Abs(pt.x - n.Pos.x) < 111 && Mathf.Abs(pt.y - n.Pos.y) < 43) {
                        cross++;
                        sb.AppendLine($"[CalcAudit] 连线压过节点：{n.Formula}");
                        break;
                    }
                }
            }
        }
        sb.AppendLine($"[CalcAudit] 连线压过节点的次数 = {cross}");
        return sb.ToString();
    }
    static string Trim(string s) => s.Length > 14 ? s.Substring(0, 14) : s;


    /* ---------------- 小工具 ---------------- */
    Vector2 ToWorld(Vector2 screen) {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out local);
        return local - _world.anchoredPosition;
    }

    static RectTransform MkNode(string name, RectTransform parent, Color col) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (col.a > 0f) {
            bool line=name=="Seg"||name=="Head";
            var img = line?go.AddComponent<Image>():go.AddComponent<LabUiSurface>();
            img.color = !line&&col.a>=.8f?LabUiTheme.Panel:col;
        }
        return rt;
    }
    static void Stretch(RectTransform rt) {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    static Text MkText(RectTransform parent, string s, int size, Color col, TextAnchor anc,
                       Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax) {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = LabUI.UiFont; t.fontSize = size; t.color = LabUiTheme.Ink; t.alignment = anc;
        Localization.Apply(t, s); t.raycastTarget = false; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = omin; rt.offsetMax = omax;
        return t;
    }
    static Button MkBtn(RectTransform parent, string label, System.Action act,
                        Vector2 pos, Vector2 size, Vector2 pivot, Color col) {
        var go = new GameObject("B", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        /* 锚点用调用方传的 pivot —— 顶栏传 (1,1) 从右上角往左排，对话框传 (1,0)
           从右下角往左排。硬编码 (1,1) 会把对话框的按钮摆到面板外面去。 */
        rt.anchorMin = rt.anchorMax = pivot;
        rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<LabUiSurface>(); img.color = LabUiTheme.Card;
        var b = go.AddComponent<Button>(); b.targetGraphic = img;
        LabUiTheme.Button(b);
        if (act != null) b.onClick.AddListener(() => act());
        MkText(rt, label, 13, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }
    static InputField MkInput(RectTransform parent, Vector2 pos, Vector2 size, Vector2 pivot, string placeholder) {
        var go = new GameObject("I", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = pivot;
        rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>(); img.color = new Color(0.06f, 0.10f, 0.15f, 1f);
        var t = MkText(rt, "", 13, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));
        var f = go.AddComponent<InputField>();
        f.targetGraphic = img; f.textComponent = t; f.text = placeholder;
        LabUiTheme.Input(f);
        return f;
    }
}

/* ---------------- 拖拽与右键处理 ---------------- */
public class WorldDrag : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler {
    public EnthalpyScene scene;
    public void OnPointerClick(PointerEventData e) {
        if (e.button == PointerEventData.InputButton.Right) scene.OnWorldRightClick(e.position);
    }
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) {
        if (e.button == PointerEventData.InputButton.Middle || e.button == PointerEventData.InputButton.Left) {
            /* 空白处拖动 = 平移画布 */
            var w = scene.transform.Find("World") as RectTransform;
            if (w != null) w.anchoredPosition += e.delta;
        }
    }
}

public class NodeDrag : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public EnthalpyScene scene;
    public EnthalpyScene.Node node;
    bool _moved;
        public void OnPointerClick(PointerEventData e) {
            Debug.Log("[ClickDbg] 节点点击 button=" + e.button + " _moved=" + _moved
                    + " pending=" + scene.PendingEdge + " node=" + (node != null ? node.Formula : "null"));
            if (_moved) { _moved = false; return; }   // 拖过了就不算点击，但要复位

            if (e.button == PointerEventData.InputButton.Right) {
                /* 正在拉线时右键 = 取消拉线 */
                if (scene.PendingEdge) { scene.CancelPending(); return; }
                scene.OnNodeRightClick(node, e.position);
                return;
            }

            /* 左键：已经在拉线 → 连到本节点；否则选中并开始拉线 */
            if (scene.PendingEdge) {
                if (scene.PendingFrom != node) scene.CompletePending(node);
                else scene.CancelPending();     // 点回自己 = 取消
                return;
            }
            scene.SelectNode(node);
            scene.BeginEdgeMode(node);
        }
    public void OnBeginDrag(PointerEventData e) { _moved = false; }
    public void OnDrag(PointerEventData e) {
        _moved = true;
        node.Pos += e.delta;
        node.Rt.anchoredPosition = node.Pos;
        /* 关键：节点一动，连到它上面的线必须立刻重画。
           少了这一句，拖节点时线就停在原地 —— 看起来就是「曲线不是实时计算的」。 */
        scene.NodeMoved();
    }
    public void OnEndDrag(PointerEventData e) { }
}

public class EdgeView : MonoBehaviour, IPointerClickHandler {
    public EnthalpyScene scene;
    public EnthalpyScene.Edge edge;
    public void OnPointerClick(PointerEventData e) {
        if (e.button == PointerEventData.InputButton.Right) scene.OnEdgeRightClick(edge, e.position);
    }
}

}
