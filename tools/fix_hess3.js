const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(P, 'utf8').split('\n');
let n = 0;

const findLine = (needle, from) => {
  for (let i = from || 0; i < lines.length; i++) if (lines[i].includes(needle)) return i;
  return -1;
};
const insertAfter = (idx, arr) => { lines.splice(idx + 1, 0, ...arr); n += arr.length; };
const replaceLine = (idx, s) => { lines[idx] = s; n++; };

/* ---- 1) 节点尺寸常量：插在 class Node 声明之前 ---- */
let i = findLine('public class Node {');
if (i >= 0 && !lines.slice(0, i).some(l => l.includes('NodeW = 210f'))) {
  insertAfter(i - 1, [
    '    /// <summary>节点尺寸常量，算边框端点要用</summary>',
    '    public const float NodeW = 210f, NodeH = 74f;',
    '',
  ]);
  console.log('  ✓ 节点尺寸常量');
} else console.log('  - 常量已存在或找不到锚点');

/* ---- 2) Boundary 函数：插在 Quadratic 之前 ---- */
i = findLine('static Vector2 Quadratic(');
if (i >= 0 && !lines.slice(0, i).some(l => l.includes('public static Vector2 Boundary('))) {
  insertAfter(i - 1, [
    '    /// <summary>节点矩形边框上、朝 toward 方向的那个点。',
    '    /// 连线端点贴在边框上而不是中心 —— 否则线会戳进节点里，箭头也碰不到边框。</summary>',
    '    public static Vector2 Boundary(Node n, Vector2 toward) {',
    '        Vector2 d = toward - n.Pos;',
    '        if (d.sqrMagnitude < 1e-6f) return n.Pos + new Vector2(NodeW * 0.5f, 0f);',
    '        Vector2 dir = d.normalized;',
    '        float hw = NodeW * 0.5f, hh = NodeH * 0.5f;',
    '        float tx = hw / Mathf.Max(1e-4f, Mathf.Abs(dir.x));',
    '        float ty = hh / Mathf.Max(1e-4f, Mathf.Abs(dir.y));',
    '        return n.Pos + dir * Mathf.Min(tx, ty);',
    '    }',
    '',
  ]);
  console.log('  ✓ Boundary');
} else console.log('  - Boundary 已存在');

/* ---- 3) RedrawEdges 用边框端点 ---- */
i = findLine('var p0 = e.A.Pos; var p1 = e.B.Pos;');
if (i >= 0) {
  replaceLine(i, '            Vector2 p0 = Boundary(e.A, e.B.Pos);   /* 端点贴在边框上，不是中心 */');
  lines.splice(i + 1, 0, '            Vector2 p1 = Boundary(e.B, e.A.Pos);');
  n++;
  console.log('  ✓ 边框端点');
}

/* ---- 4) DrawCurve：把 RebuildEdgeMesh 改成调用通用函数 ---- */
i = findLine('void RebuildEdgeMesh(Edge e, Vector2 p0, Vector2 ctrl, Vector2 p1) {');
if (i >= 0) {
  /* 找到这个函数体的起止 */
  let end = i;
  let depth = 0, started = false;
  for (let k = i; k < lines.length; k++) {
    depth += (lines[k].match(/\{/g) || []).length;
    depth -= (lines[k].match(/\}/g) || []).length;
    if (depth > 0) started = true;
    if (started && depth === 0) { end = k; break; }
  }
  const body = [
    '    void RebuildEdgeMesh(Edge e, Vector2 p0, Vector2 ctrl, Vector2 p1) {',
    '        DrawCurve(e.Rt, p0, ctrl, p1, Color.white);',
    '    }',
    '',
    '    /// <summary>把一条二次贝塞尔画成细条 + 末端箭头。连线与临时拉线共用。</summary>',
    '    void DrawCurve(RectTransform rt, Vector2 p0, Vector2 ctrl, Vector2 p1, Color col) {',
    '        if (rt == null) return;',
    '        for (int i = rt.childCount - 1; i >= 0; i--) {',
    '            var ch = rt.GetChild(i);',
    '            if (ch.name == "Seg" || ch.name == "Head") Destroy(ch.gameObject);',
    '        }',
  ];
  /* 复用原来的绘制循环：从原函数体里抠出 "const int N = 14;" 到结尾 */
  let s = -1;
  for (let k = i; k <= end; k++) if (lines[k].includes('const int N = 14;')) { s = k; break; }
  if (s > 0) {
    const tail = lines.slice(s, end);
    const fixed = tail.map(l => l
      .replace(/MkNode\("Seg", rt, Color\.white\)/g, 'MkNode("Seg", rt, col)')
      .replace(/MkNode\("Head", rt, Color\.white\)/g, 'MkNode("Head", rt, col)')
      .replace(/\bVector2 (\w+) = MkNode\("Seg", rt, col\);/, 'var $1 = MkNode("Seg", rt, col);'));
    lines.splice(i, end - i + 1, ...body, ...fixed, '    }');
    n += body.length;
    console.log('  ✓ DrawCurve 已抽出（复用原绘制循环）');
  } else console.log('  ✗ 找不到绘制循环');
}

fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('改动处数:', n);
