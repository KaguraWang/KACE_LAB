const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');

const old = `                /* ② 和其它标注太近：中罚 */
                foreach (var oe in _edges) {
                    if (oe == self || oe.Rt == null) continue;
                    var ot = oe.Rt.GetComponentInChildren<Text>();
                    if (ot == null) continue;
                    var ort = (RectTransform)ot.transform;
                    float d = Vector2.Distance(cand, ort.anchoredPosition);
                    if (d < LH * 1.6f) score -= (LH * 1.6f - d) * 1.5f;
                }`;

const nw = `                /* ② 和其它标注重叠：重罚。
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
                }`;

if (c.includes(old)) {
  c = c.replace(old, nw);
  fs.writeFileSync(E, c, 'utf8');
  console.log('✓ 标注互斥判据改为矩形分离');
} else {
  console.log('✗ 锚点未命中，尝试宽松匹配');
  /* 宽松匹配：直接定位到那段 */
  const a = c.indexOf('② 和其它标注太近');
  if (a > 0) {
    const b = c.indexOf('}', c.indexOf('foreach (var oe in _edges)', a)) + 1;
    /* 找到整个 foreach 块 */
    let k = c.indexOf('foreach (var oe in _edges)', a);
    let depth = 0, started = false, end = k;
    for (let i = k; i < c.length; i++) {
      if (c[i] === '{') { depth++; started = true; }
      if (c[i] === '}') { depth--; if (started && depth === 0) { end = i; break; } }
    }
    c = c.slice(0, k) + nw.trim().replace('                /* ②', '/* ②') + c.slice(end + 1);
    fs.writeFileSync(E, c, 'utf8');
    console.log('✓ 用宽松匹配替换了');
  }
}
