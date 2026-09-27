const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(P, 'utf8').split('\n');

const blk = [
  '    /* ================================================================',
  '       拉线机制：单击节点 → 一根线跟着鼠标 → 再点另一个节点就连上',
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
  '        _pendingRt = MkNode("PendingEdge", _lineLayer, new Color(0, 0, 0, 0));',
  '        DrawPending(from.Pos);',
  '    }',
  '',
  '    public void CancelPending() {',
  '        _pendingFrom = null;',
  '        if (_pendingRt != null) { Destroy(_pendingRt.gameObject); _pendingRt = null; }',
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
  '        for (int k = _pendingRt.childCount - 1; k >= 0; k--) Destroy(_pendingRt.GetChild(k).gameObject);',
  '        if (d.sqrMagnitude < 6f) return;',
  '        Vector2 dir = d.normalized, nrm = new Vector2(-dir.y, dir.x);',
  '        Vector2 ctrl = (p0 + target) * 0.5f + nrm * Mathf.Clamp(d.magnitude * 0.20f, 24f, 120f);',
  '        DrawCurve(_pendingRt, p0, ctrl, target, new Color(0.55f, 0.88f, 1f, 0.95f));',
  '    }',
  '',
  '    void Update() { RefreshDragEdge(); }',
  '',
];

/* 插在 "连线" 分节注释之前 */
let i = -1;
for (let k = 0; k < lines.length; k++) if (lines[k].includes('/* ---------------- 连线 ---------------- */')) { i = k; break; }
if (i < 0) { console.log('✗ 找不到锚点'); process.exit(1); }
lines.splice(i, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 插入', blk.length, '行（位置', i + 1, '）');

/* 顺带把旧的 BeginEdge（无人调用的空壳）删掉 */
let c = fs.readFileSync(P, 'utf8');
c = c.replace('    public void BeginEdge(Node from) { _dragFrom = from; _dragTo = from.Pos; }\n', '');
fs.writeFileSync(P, c, 'utf8');
console.log('✓ 旧的 BeginEdge 空壳已删');
