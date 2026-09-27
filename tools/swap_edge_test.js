const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 找到 TestEdgePoints 整个方法并替换成 TestEdgeEnds */
let start = -1;
for (let i = 0; i < lines.length; i++) if (lines[i].includes('public List<Vector3> TestEdgePoints(int idx)')) { start = i; break; }
if (start < 0) { console.log('✗ 找不到 TestEdgePoints'); process.exit(1); }
let depth = 0, started = false, end = -1;
for (let k = start; k < lines.length; k++) {
  depth += (lines[k].match(/\{/g) || []).length;
  depth -= (lines[k].match(/\}/g) || []).length;
  if (depth > 0) started = true;
  if (started && depth === 0) { end = k; break; }
}
/* 连同上面那行注释一起去掉 */
let commentStart = start;
while (commentStart > 0 && lines[commentStart - 1].trim().startsWith('///')) commentStart--;

const nw = [
  '    /// <summary>一条连线的「起点、箭尖」两个世界坐标。',
  '    /// 只取两个点 —— 中间线段的数量会随弧度变化，按数量对比会误判。</summary>',
  '    public List<Vector3> TestEdgeEnds(int idx) {',
  '        var res = new List<Vector3>();',
  '        if (idx < 0 || idx >= _edges.Count) return res;',
  '        var rt = _edges[idx].Rt;',
  '        if (rt == null) return res;',
  '        RectTransform firstSeg = null, lastHead = null;',
  '        for (int i = 0; i < rt.childCount; i++) {',
  '            var ch = rt.GetChild(i) as RectTransform;',
  '            if (ch == null) continue;',
  '            if (ch.name == "Seg" && firstSeg == null) firstSeg = ch;',
  '            if (ch.name == "Head") lastHead = ch;',
  '        }',
  '        if (firstSeg != null) res.Add(firstSeg.TransformPoint(firstSeg.rect.center));',
  '        if (lastHead != null) res.Add(lastHead.TransformPoint(lastHead.rect.center));',
  '        return res;',
  '    }',
];
lines.splice(commentStart, end - commentStart + 1, ...nw);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ TestEdgePoints → TestEdgeEnds（替换了', end - commentStart + 1, '行）');

const chk = fs.readFileSync(E, 'utf8');
console.log('  TestEdgeEnds 存在:', chk.includes('public List<Vector3> TestEdgeEnds'));
console.log('  TestEdgePoints 残留:', chk.includes('TestEdgePoints'));
