const fs = require('fs');
let n = 0;

/* 1) SetLabVisible 里加宠物让位 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('LabBootstrap.Instance.SetLabUiVisible(labOn);')) {
    lines.splice(i + 1, 0,
      '        /* 阿侬平时停在右下角，正好会盖住计算器下方的节点 —— 进计算器时挪到左下角 */',
      '        PetView.Instance?.SetHomeForCalc(!labOn);');
    fs.writeFileSync(E, lines.join('\n'), 'utf8');
    n++;
    console.log('  ✓ SetLabVisible 里加了宠物让位（第', i + 2, '行）');
    break;
  }
}

/* 2) 标注偏移再加大：从「半高+16」提到「半高+34」 */
let c = fs.readFileSync(E, 'utf8');
c = c.replace('        const float Off = LH * 0.5f + 16f;   // 净空 = 标注半高 + 16，边缘才不会压着线',
              '        const float Off = LH * 0.5f + 34f;   // 净空 = 标注半高 + 34 —— 16 还是太近，视觉上仍压线');
fs.writeFileSync(E, c, 'utf8'); n++;
console.log('  ✓ 标注偏移加大');

/* 3) 自检加一条：标注中心到曲线的距离必须够 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('标注不得压线')) {
  sc = sc.replace('                    /* ④ 单击节点 → 拉线 → 点另一个节点 */',
`                    /* ④ 标注不得压线：中心到曲线的距离要够 */
                    {
                        int tooClose = 0;
                        for (int ei = 0; ei < scn.TestEdgeCount; ei++) {
                            float d = scn.TestLabelClearance(ei);
                            if (d >= 0f && d < 40f) { tooClose++; Say($"   ✗ 连线 {ei} 的标注离曲线只有 {d:F0}px"); }
                        }
                        TCheck("所有连线标注都离线足够远（不压线）", tooClose == 0);
                    }

                    /* ⑤ 单击节点 → 拉线 → 点另一个节点 */`);
  fs.writeFileSync(S, sc, 'utf8'); n++;
  console.log('  ✓ 自检加了标注净空检查');
}

/* 4) EnthalpyScene 暴露 TestLabelClearance */
c = fs.readFileSync(E, 'utf8');
if (!c.includes('TestLabelClearance')) {
  c = c.replace('    /// <summary>一条连线的「起点、箭尖」两个世界坐标。',
`    /// <summary>某条连线的标注中心到曲线的最短距离。负数表示取不到。 </summary>
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

    /// <summary>一条连线的「起点、箭尖」两个世界坐标。`);
  fs.writeFileSync(E, c, 'utf8'); n++;
  console.log('  ✓ TestLabelClearance 已加');
}

console.log('改动处数:', n);
