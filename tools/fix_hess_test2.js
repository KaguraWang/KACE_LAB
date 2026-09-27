const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');

/* 判「是否在 World 下」：直接往上找有没有叫 World 的祖先，别拼字符串 */
e = e.replace(`        Transform t = _nodes[idx].Rt;   // 必须是 Transform —— parent 返回的就是 Transform
        string path = t.name;
        while (t.parent != null) { path = t.parent.name + "/" + path; t = t.parent; }   /* 先取值再上移，否则到根节点会空引用 */
        return path.StartsWith("World/");`,
`        Transform t = _nodes[idx].Rt;
        /* 直接往上找有没有叫 World 的祖先。
           拼路径字符串的话，从下往上拼出来的开头是根节点名，
           拿 StartsWith("World/") 判会全部误判为「不在」。 */
        while (t != null) {
            if (t.name == "World") return true;
            t = t.parent;
        }
        return false;`);

/* 端点对比改成只取「起点和箭尖」，数量恒定 */
e = e.replace(`    /// <summary>连线上所有线段/箭头端点的世界坐标（用来判断线有没有跟着节点动）</summary>
    public List<Vector3> TestEdgePoints(int idx) {
        var res = new List<Vector3>();
        if (idx < 0 || idx >= _edges.Count) return res;
        var rt = _edges[idx].Rt;
        if (rt == null) return res;
        for (int i = 0; i < rt.childCount; i++) {
            var ch = rt.GetChild(i) as RectTransform;   // GetChild 返回 Transform，要显式转
            if (ch == null) continue;
            if (ch.name != "Seg" && ch.name != "Head") continue;
            res.Add(ch.TransformPoint(ch.rect.center));
        }
        return res;
    }`,
`    /// <summary>一条连线的「起点、终点（箭尖）」两个世界坐标。
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
            if (ch.name == "Head") lastHead = ch;        // 一路覆盖，拿到最后一个
        }
        if (firstSeg != null) res.Add(firstSeg.TransformPoint(firstSeg.rect.center));
        if (lastHead != null) res.Add(lastHead.TransformPoint(lastHead.rect.center));
        return res;
    }`);
fs.writeFileSync(E, e, 'utf8');
console.log('✓ EnthalpyScene 测试接口已改');

/* 自检里改用 TestEdgeEnds */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace(`                    var ptsBefore = scn.TestEdgePoints(0);
                    scn.TestMoveNode(1, new Vector2(150, 90));
                    var ptsAfter = scn.TestEdgePoints(0);
                    Say($"   移动节点 150,90 → 连线端点采样 {ptsBefore.Count} → {ptsAfter.Count} 个");
                    TCheck("连线上确实画出了线段与箭头", ptsBefore.Count >= 8);
                    bool moved = ptsBefore.Count == ptsAfter.Count && ptsBefore.Count > 0;
                    if (moved) {
                        float maxd = 0f;
                        for (int i = 0; i < ptsBefore.Count; i++)
                            maxd = Mathf.Max(maxd, Vector3.Distance(ptsBefore[i], ptsAfter[i]));
                        Say($"   连线端点最大位移 {maxd:F1} px");
                        TCheck("拖节点时连线实时跟着变（端点位移 > 10px）", maxd > 10f);
                    } else TCheck("拖节点时连线实时跟着变（端点位移 > 10px）", false);`,
`                    /* 用「起点 + 箭尖」两个点对比：中间线段数量会随弧度变，按数量比会误判 */
                    var e0Before = scn.TestEdgeEnds(0);
                    scn.TestMoveNode(1, new Vector2(150, 90));
                    var e0After = scn.TestEdgeEnds(0);
                    Say($"   连线 0 的起点/箭尖：{e0Before.Count} 个采样点");
                    TCheck("连线上确实画出了线段与箭头", e0Before.Count == 2);
                    if (e0Before.Count == 2 && e0After.Count == 2) {
                        float d0 = Vector3.Distance(e0Before[0], e0After[0]);
                        float d1 = Vector3.Distance(e0Before[1], e0After[1]);
                        Say($"   起点位移 {d0:F1} px，箭尖位移 {d1:F1} px");
                        TCheck("拖节点时连线起点跟着变", d0 > 10f);
                        TCheck("拖节点时连线箭尖跟着变", d1 > 10f);
                    } else {
                        TCheck("拖节点时连线起点跟着变", false);
                        TCheck("拖节点时连线箭尖跟着变", false);
                    }`);
fs.writeFileSync(S, sc, 'utf8');
console.log('✓ 自检已改用 TestEdgeEnds');
