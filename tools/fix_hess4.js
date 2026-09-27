const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(P, 'utf8').split('\n');
let n = 0;
const findLine = (needle, from) => {
  for (let i = from || 0; i < lines.length; i++) if (lines[i].includes(needle)) return i;
  return -1;
};

/* ---- 补 NodeMoved / CompletePending / CancelPending / BeginEdgeMode / Update ---- */
let i = findLine('public void BeginEdge(Node from)');
if (i < 0) {
  /* 退化：插在 Back() 之前 */
  i = findLine('void Back() {');
  if (i > 0) i -= 1;
}
if (i > 0 && !lines.some(l => l.includes('public void NodeMoved()'))) {
  const blk = [
    '    /* ================================================================',
    '       拉线状态：单击节点后，一根临时线跟着鼠标走，点另一个节点就连上',
    '       ================================================================ */',
    '    Node _pendingFrom;                 // 正在从哪个节点拉线',
    '    RectTransform _pendingRt;          // 那根跟随鼠标的临时线',
    '',
    '    public bool PendingEdge => _pendingFrom != null;',
    '    public Node PendingFrom => _pendingFrom;',
    '',
    '    /// <summary>开始拉线</summary>',
    '    public void BeginEdgeMode(Node from) {',
    '        CancelPending();',
    '        _pendingFrom = from;',
    '        var go = MkNode("PendingEdge", _lineLayer, new Color(0, 0, 0, 0));',
    '        _pendingRt = go;',
    '        DrawPending(from.Pos);',
    '    }',
    '',
    '    public void CancelPending() {',
    '        _pendingFrom = null;',
    '        if (_pendingRt != null) { Destroy(_pendingRt.gameObject); _pendingRt = null; }',
    '    }',
    '',
    '    /// <summary>把正在拉的线落到目标节点上，然后问焓变来源</summary>',
    '    public void CompletePending(Node to) {',
    '        var from = _pendingFrom;',
    '        CancelPending();',
    '        if (from == null || to == null || from == to) return;',
    '        Connect(from, to, 0, EnthalpySource.Unknown, "");',
    '        var e = _edges[_edges.Count - 1];',
    '        AskEnthalpy(e, false);',
    '    }',
    '',
    '    /// <summary>节点被移动后调它：连线立刻重画，临时线也跟上</summary>',
    '    public void NodeMoved() {',
    '        RedrawEdges();',
    '        RefreshDragEdge();',
    '    }',
    '',
    '    /// <summary>临时线跟随鼠标</summary>',
    '    public void RefreshDragEdge() {',
    '        if (_pendingFrom == null) return;',
    '        DrawPending(ToWorld(Input.mousePosition));',
    '    }',
    '',
    '    void DrawPending(Vector2 target) {',
    '        if (_pendingRt == null || _pendingFrom == null) return;',
    '        Vector2 p0 = Boundary(_pendingFrom, target);',
    '        Vector2 d = target - p0;',
    '        if (d.sqrMagnitude < 6f) {',
    '            for (int k = _pendingRt.childCount - 1; k >= 0; k--) Destroy(_pendingRt.GetChild(k).gameObject);',
    '            return;',
    '        }',
    '        Vector2 dir = d.normalized, nrm = new Vector2(-dir.y, dir.x);',
    '        Vector2 ctrl = (p0 + target) * 0.5f + nrm * Mathf.Clamp(d.magnitude * 0.20f, 24f, 120f);',
    '        DrawCurve(_pendingRt, p0, ctrl, target, new Color(0.55f, 0.88f, 1f, 0.95f));',
    '    }',
    '',
    '    void Update() { RefreshDragEdge(); }',
    '',
    '    public void BeginEdge(Node from) {',
  ];
  lines.splice(i, 1, ...blk);
  n += blk.length;
  console.log('  ✓ 拉线机制已插入');
} else console.log('  - 已存在');
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('改动处数:', n);
