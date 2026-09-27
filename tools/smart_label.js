/* =====================================================================
   焓变连线标注的智能摆放
     之前：曲线中点 + 固定法向偏移 26px。
     问题：换条线就会压到别的节点、或者压在线自身上。

     现在：在曲线中段取几个候选点，每个候选点试正反两侧法向，
     逐个打分（撞节点扣分、离别的标注太近扣分、离线太近扣分），
     取分数最高的位置。这是思维导图里标注摆放的常规做法。
   ===================================================================== */
const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');
let n = 0;

/* 找标注定位那一段，整段换成打分选位 */
const startMark = '            var trt = (RectTransform)t.transform;';
const endMark = 'trt.anchoredPosition = midPt + nrm2 * 26f;';
const a = c.indexOf(startMark);
const b = c.indexOf(endMark);
if (a < 0 || b < 0) { console.log('✗ 找不到标注定位段'); process.exit(1); }

const nw = `            var trt = (RectTransform)t.transform;
            trt.sizeDelta = new Vector2(186f, 40f);
            /* 在曲线中段试几个候选位置，挑一个不撞节点、不压线的。
               固定往法向推 26px 是不够的 —— 换个布局就会压到别的节点上。 */
            trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);`;

c = c.slice(0, a) + nw + c.slice(b + endMark.length);
/* 去掉上面遗留的 midPt/nrm2 计算（现在都在 PickLabelPos 里算了） */
c = c.replace(`            /* 标注沿曲线法向偏移。固定往 Y 上抬的话会压在节点上 ——
               法向取「曲线中点附近的切线」转 90°，再统一取朝上的一侧。 */
            Vector2 midPt = Quadratic(p0, ctrl, p1, 0.5f);
            Vector2 tan = Quadratic(p0, ctrl, p1, 0.56f) - Quadratic(p0, ctrl, p1, 0.44f);
            Vector2 nrm2 = tan.sqrMagnitude > 1e-6f
                         ? new Vector2(-tan.y, tan.x).normalized
                         : new Vector2(0, 1);
            if (nrm2.y < 0) nrm2 = -nrm2;
`, '');
fs.writeFileSync(E, c, 'utf8');
console.log('  ✓ 标注定位改为打分选位');

/* 加 PickLabelPos 实现 */
c = fs.readFileSync(E, 'utf8');
if (!c.includes('Vector2 PickLabelPos(')) {
  c = c.replace('    static Vector2 Quadratic(Vector2 a, Vector2 c, Vector2 b, float t) {',
`    /// <summary>给一条连线的标注挑位置。
    /// 在曲线中段取几个 t，每个 t 试正反两侧法向，逐个打分：
    ///   撞节点          重罚
    ///   压到别的标注    中罚
    ///   离线太近        轻罚
    /// 取分最高的。这样标注总能落在相对干净的地方。 </summary>
    Vector2 PickLabelPos(Edge self, Vector2 p0, Vector2 ctrl, Vector2 p1) {
        const float LW = 186f, LH = 40f;      // 标注尺寸
        const float Off = 30f;                // 距曲线的基准偏移
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
                /* ② 和其它标注太近：中罚 */
                foreach (var oe in _edges) {
                    if (oe == self || oe.Rt == null) continue;
                    var ot = oe.Rt.GetComponentInChildren<Text>();
                    if (ot == null) continue;
                    var ort = (RectTransform)ot.transform;
                    float d = Vector2.Distance(cand, ort.anchoredPosition);
                    if (d < LH * 1.6f) score -= (LH * 1.6f - d) * 1.5f;
                }
                /* ③ 离线太近（标注会压住线）：轻罚 */
                float minD = float.MaxValue;
                for (int k = 1; k <= 12; k++) {
                    Vector2 q = Quadratic(p0, ctrl, p1, k / 13f);
                    minD = Mathf.Min(minD, Vector2.Distance(cand, q));
                }
                score -= Mathf.Max(0f, 26f - minD) * 0.8f;
                /* ④ 稍微偏好「落在画面中上部」，读起来顺 */
                score += cand.y * 0.02f;
                if (score > bestScore) { bestScore = score; best = cand; }
            }
        }
        return best;
    }

    static Vector2 Quadratic(Vector2 a, Vector2 c, Vector2 b, float t) {`);
  fs.writeFileSync(E, c, 'utf8');
  console.log('  ✓ PickLabelPos 已加');
}

console.log('改动处数:', n + 1);
