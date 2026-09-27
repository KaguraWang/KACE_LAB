/* =====================================================================
   焓变计算器：把节点图做成一个整体
     三处改动：
     1) 节点与连线同挂 _world，拖背景时整层一起走
     2) 拖节点时实时重画连线（长度/弧度/箭头都跟着变）
     3) 连线的端点落在**节点边框**上而不是中心，
        并且新实现「点节点 → 拉出线 → 点另一个节点 → 连上」

     交互分工（和思维导图一致）：
       · 拖节点本体     = 挪动节点（要摆回路，必须能挪）
       · 单击节点       = 选中 + 拉出一根跟随鼠标的线
       · 再单击别的节点 = 连上，并弹出焓变来源选单
       · 右键           = 取消拉线 / 弹节点选单
   ===================================================================== */
const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(P, 'utf8');
let n = 0;
const R = (a, b, label) => {
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 50).replace(/\n/g, '⏎')); return; }
  c = c.replace(a, b); n++;
};

/* ---- 节点尺寸常量 ---- */
R(`    public class Node {
        public int Id;`,
  `    public const float NodeW = 210f, NodeH = 74f;   // 节点尺寸，算边框端点要用

    public class Node {
        public int Id;`, '节点尺寸常量');

/* ---- 边框端点 ---- */
R(`    static Vector2 Quadratic(Vector2 a, Vector2 c, Vector2 b, float t) {`,
  `    /// <summary>节点矩形边框上、朝 toward 方向的那个点。
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

    static Vector2 Quadratic(Vector2 a, Vector2 c, Vector2 b, float t) {`, '边框端点');

/* ---- RedrawEdges 用边框端点 ---- */
R(`        foreach (var e in _edges) {
            var p0 = e.A.Pos; var p1 = e.B.Pos;`,
  `        foreach (var e in _edges) {
            /* 端点贴在边框上，不是节点中心 */
            Vector2 p0 = Boundary(e.A, e.B.Pos);
            Vector2 p1 = Boundary(e.B, e.A.Pos);`, '重画用边框');

/* ---- 拉线状态 ---- */
R(`    Edge _dragging;                    // 正在拉的线`,
  `    Edge _dragging;                    // 正在拉的线（旧路径，保留兼容）
    Node _pendingFrom;                 // 单击节点后正在拉线
    RectTransform _pendingRt;          // 那根跟随鼠标的临时线`, '拉线状态');

/* ---- Update：让临时线跟随鼠标 ---- */
R(`    /* ---------------- 交互入口（由 PetDrag 调用） ---------------- */`,
  `    /* ---------------- 整体性：节点与线同层 ---------------- */
    void Update() {
        if (_pendingFrom == null) return;
        /* 临时线的终点跟着鼠标走 */
        Vector2 mouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _world, Input.mousePosition, null, out mouse);
        _dragTo = mouse;
        DrawPending(ToWorld(Input.mousePosition));
    }

    /// <summary>开始拉线：单击节点后进入这个状态</summary>
    public void BeginEdgeMode(Node from) {
        CancelPending();
        _pendingFrom = from;
        var go = MkNode("PendingEdge", _lineLayer, new Color(0, 0, 0, 0));
        _pendingRt = go;
        DrawPending(from.Pos);
    }

    public void CancelPending() {
        _pendingFrom = null;
        if (_pendingRt != null) { Destroy(_pendingRt.gameObject); _pendingRt = null; }
    }
    public bool PendingEdge => _pendingFrom != null;
    public Node PendingFrom => _pendingFrom;

    /// <summary>把临时线画到 target 处（节点边框或鼠标处）</summary>
    void DrawPending(Vector2 target) {
        if (_pendingRt == null || _pendingFrom == null) return;
        Vector2 p0 = Boundary(_pendingFrom, target);
        Vector2 p1 = target;
        Vector2 d = p1 - p0;
        if (d.sqrMagnitude < 4f) { ClearChildren(_pendingRt); return; }
        Vector2 dir = d.normalized, nrm = new Vector2(-dir.y, dir.x);
        Vector2 ctrl = (p0 + p1) * 0.5f + nrm * Mathf.Clamp(d.magnitude * 0.22f, 30f, 140f) * 0.9f;
        DrawCurve(_pendingRt, p0, ctrl, p1, new Color(0.55f, 0.85f, 1f, 0.9f));
    }

    static void ClearChildren(RectTransform rt) {
        for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
    }

    /* ---------------- 交互入口（由 PetDrag 调用） ---------------- */`, 'Update 与拉线');

/* ---- 抽出通用的画曲线函数，RedrawEdges 与临时线共用 ---- */
R(`    /// <summary>把曲线画成 UI 里的白色细条 + 末端箭头</summary>
    void RebuildEdgeMesh(Edge e, Vector2 p0, Vector2 ctrl, Vector2 p1) {
        var rt = e.Rt;
        for (int i = rt.childCount - 1; i >= 0; i--) {
            var ch = rt.GetChild(i);
            if (ch.name == "Seg" || ch.name == "Head") Destroy(ch.gameObject);
        }`,
  `    /// <summary>把曲线画成 UI 里的白色细条 + 末端箭头</summary>
    void RebuildEdgeMesh(Edge e, Vector2 p0, Vector2 ctrl, Vector2 p1) {
        DrawCurve(e.Rt, p0, ctrl, p1, Color.white);
    }

    void DrawCurve(RectTransform rt, Vector2 p0, Vector2 ctrl, Vector2 p1, Color col) {
        if (rt == null) return;
        for (int i = rt.childCount - 1; i >= 0; i--) {
            var ch = rt.GetChild(i);
            if (ch.name == "Seg" || ch.name == "Head") Destroy(ch.gameObject);
        }`, '抽公共画线');

/* 把 DrawCurve 里的 Color.white 换成 col */
c = c.replace(`            var seg = MkNode("Seg", rt, Color.white);`, `            var seg = MkNode("Seg", rt, col);`);
c = c.replace(`        var head = MkNode("Head", rt, Color.white);`, `        var head = MkNode("Head", rt, col);`);
c = c.replace(`            var b = MkNode("Head", rt, Color.white);`, `            var b = MkNode("Head", rt, col);`);
n += 3;

/* ---- 节点点击：进入拉线模式 ---- */
R(`    public void OnPointerClick(PointerEventData e) {
        if (_moved) return;
        if (e.button == PointerEventData.InputButton.Right) scene.OnNodeRightClick(node, e.position);
        else scene.SelectNode(node);
    }`,
  `    public void OnPointerClick(PointerEventData e) {
        if (_moved) return;                      // 拖过了就不算点击
        if (e.button == PointerEventData.InputButton.Right) {
            if (scene.PendingEdge) { scene.CancelPending(); return; }   // 右键取消拉线
            scene.OnNodeRightClick(node, e.position);
            return;
        }
        scene.SelectNode(node);
        /* 如果已经在拉线，而且拉的不是自己 —— 就连上 */
        if (scene.PendingEdge) {
            if (scene.PendingFrom != node) scene.CompletePending(node);
            return;
        }
        /* 单击节点：拉出一根跟随鼠标的线 */
        scene.BeginEdgeMode(node);
    }`, '节点点击拉线');

/* ---- CompletePending：连上并问焓变 ---- */
R(`    public void NodeMoved() {`,
  `    /// <summary>把正在拉的线落到目标节点上，然后问焓变来源</summary>
    public void CompletePending(Node to) {
        var from = _pendingFrom;
        CancelPending();
        if (from == null || to == null || from == to) return;
        Connect(from, to, 0, EnthalpySource.Unknown, "");
        var e = _edges[_edges.Count - 1];
        AskEnthalpy(e, false);
    }

    public void NodeMoved() {`, 'CompletePending');

fs.writeFileSync(P, c, 'utf8');
console.log('补丁处数:', n);
