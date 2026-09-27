const fs = require('fs');
let n = 0;

/* ---- EnthalpyScene：暴露测试接口 ---- */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
if (!e.includes('TestNodeWorld')) {
  e = e.replace('    public void MarkSolved(bool ok) { LastSolvedOk = ok; }',
`    public void MarkSolved(bool ok) { LastSolvedOk = ok; }

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
        var t = _nodes[idx].Rt;
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.parent.name + "/" + path; }
        return path.StartsWith("World/");
    }
    public int TestNodeCount => _nodes.Count;
    public int TestEdgeCount => _edges.Count;
    public Vector2 TestNodePos(int idx) => (idx >= 0 && idx < _nodes.Count) ? _nodes[idx].Pos : Vector2.zero;
    public void TestMoveNode(int idx, Vector2 d) {
        if (idx < 0 || idx >= _nodes.Count) return;
        _nodes[idx].Pos += d;
        _nodes[idx].Rt.anchoredPosition = _nodes[idx].Pos;
        NodeMoved();
    }
    /// <summary>连线上所有线段/箭头端点的世界坐标（用来判断线有没有跟着节点动）</summary>
    public List<Vector3> TestEdgePoints(int idx) {
        var res = new List<Vector3>();
        if (idx < 0 || idx >= _edges.Count) return res;
        var rt = _edges[idx].Rt;
        if (rt == null) return res;
        for (int i = 0; i < rt.childCount; i++) {
            var ch = rt.GetChild(i) as RectTransform;
            if (ch == null) continue;
            if (ch.name != "Seg" && ch.name != "Head") continue;
            res.Add(ch.TransformPoint(ch.rect.center));
        }
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
    }`);
  fs.writeFileSync(E, e, 'utf8'); n++;
  console.log('  ✓ EnthalpyScene 测试接口');
}

/* ---- 自检：加一节焓变场景交互测试 ---- */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('焓变场景：节点与连线是一个整体')) {
  const anchor = '            /* ---------- 宠物精灵图 ---------- */';
  const add = `            /* ================================================================
               焓变场景：节点与连线必须是一个整体
                 用户报过：拖背景只有白线动、拖节点白线不动。
                 根因是节点挂在画布根、连线挂在 World 层，不在同一坐标系。
                 这里逐条验证。
               ================================================================ */
            {
                var host = new GameObject("HessTestHost");
                var scn = ChemLab.Game.EnthalpyScene.Show(host, ChemLab.Game.LabUI.UiFont);
                if (scn == null) Say("   ✗ 无法建立焓变场景");
                else {
                    /* 示例场景给了 3 个节点 3 条边 */
                    Say($"   节点 {scn.TestNodeCount} 个，连线 {scn.TestEdgeCount} 条");
                    TCheck("示例场景至少有 3 个节点", scn.TestNodeCount >= 3);
                    TCheck("示例场景至少有 3 条连线", scn.TestEdgeCount >= 3);

                    /* ① 节点必须在 World 层下，拖背景才会一起走 */
                    bool allUnder = true;
                    for (int i = 0; i < scn.TestNodeCount; i++)
                        if (!scn.TestNodeUnderWorld(i)) { allUnder = false; Say($"   ✗ 节点 {i} 不在 World 下"); }
                    TCheck("所有节点都挂在 World 层下（拖背景时节点要跟着走）", allUnder);

                    /* ② 拖背景：World 位移后，节点的世界坐标必须同步变化 */
                    var before0 = scn.TestNodeWorldPoint(1);
                    scn.TestPanWorld(new Vector2(120, -80));
                    var after0 = scn.TestNodeWorldPoint(1);
                    float dx = after0.x - before0.x, dy = after0.y - before0.y;
                    Say($"   平移 World (120,-80) → 节点世界坐标位移 ({dx:F0},{dy:F0})");
                    TCheck("拖背景时节点跟着动", Mathf.Abs(dx - 120) < 1f && Mathf.Abs(dy + 80) < 1f);
                    scn.TestPanWorld(new Vector2(-120, 80));      // 还原

                    /* ③ 拖节点：连线的端点必须跟着变 */
                    var ptsBefore = scn.TestEdgePoints(0);
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
                    } else TCheck("拖节点时连线实时跟着变（端点位移 > 10px）", false);

                    /* ④ 单击节点 → 拉线 → 点另一个节点 */
                    bool pulled = scn.TestPullEdge(0, 2);
                    Say("   单击节点 0 → 点节点 2：" + (pulled ? "连上了" : "✗ 没连上"));
                    TCheck("单击节点可以拉出连线并连到另一个节点", pulled);

                    UnityEngine.Object.Destroy(host);
                }
            }

`;
  if (sc.includes(anchor)) { sc = sc.replace(anchor, add + anchor); n++; console.log('  ✓ 自检已加'); }
  else console.log('  ✗ 锚点');
  fs.writeFileSync(S, sc, 'utf8');
}
console.log('改动处数:', n);
