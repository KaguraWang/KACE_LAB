const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');
let n = 0;

/* 1) RedrawEdges 末尾加第二遍：带着所有标注的既有位置重新摆放 */
const mark = `            trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);
        }
    }`;
if (c.includes(mark)) {
  c = c.replace(mark, `            trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);
        }

        /* 第二遍：第一遍是顺序贪心，先放的不知道后放的会在哪，
           于是两个标注可能互相压。带着所有人的既有位置重排一次就分开了。 */
        foreach (var e2 in _edges) {
            if (e2.Rt == null) continue;
            var t2 = e2.Rt.GetComponentInChildren<Text>();
            if (t2 == null) continue;
            Vector2 q0 = Boundary(e2.A, e2.B.Pos), q1 = Boundary(e2.B, e2.A.Pos);
            Vector2 dd2 = q1 - q0;
            float len2 = Mathf.Max(1f, dd2.magnitude);
            Vector2 dir2 = dd2 / len2, nrm3 = new Vector2(-dir2.y, dir2.x);
            float off2 = Mathf.Clamp(len2 * 0.22f, 40f, 150f);
            Vector2 ctrl2 = (q0 + q1) * 0.5f + nrm3 * off2 * 2f;
            ((RectTransform)t2.transform).anchoredPosition = PickLabelPos(e2, q0, ctrl2, q1);
        }
    }`);
  n++;
  console.log('  ✓ 第二遍标注放置已加');
} else console.log('  ✗ 锚点未命中');

/* 2) 自检：标注之间不得重叠 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('标注之间不重叠')) {
  sc = sc.replace('                        TCheck("所有连线标注都离线足够远（不压线）", tooClose == 0);',
`                        TCheck("所有连线标注都离线足够远（不压线）", tooClose == 0);
                        /* 标注之间也不能压在一起 */
                        int lap = 0;
                        var ls = scn.TestLabelPositions();
                        for (int x = 0; x < ls.Count; x++)
                            for (int y = x + 1; y < ls.Count; y++) {
                                float dd = Vector2.Distance(ls[x], ls[y]);
                                if (dd < 60f) { lap++; Say($"   ✗ 标注 {x} 与 {y} 相距只有 {dd:F0}px"); }
                            }
                        TCheck("连线标注之间不重叠", lap == 0);`);
  fs.writeFileSync(S, sc, 'utf8'); n++;
  console.log('  ✓ 自检加了标注互不重叠');
}

/* 3) EnthalpyScene 暴露 TestLabelPositions */
c = fs.readFileSync(E, 'utf8');
if (!c.includes('TestLabelPositions')) {
  c = c.replace('    /// <summary>某条连线的标注中心到曲线的最短距离。负数表示取不到。 </summary>',
`    /// <summary>所有连线标注的锚点位置（用于检查标注之间有没有叠在一起）</summary>
    public List<Vector2> TestLabelPositions() {
        var res = new List<Vector2>();
        foreach (var e in _edges) {
            if (e.Rt == null) { res.Add(Vector2.zero); continue; }
            var t = e.Rt.GetComponentInChildren<Text>();
            res.Add(t != null ? ((RectTransform)t.transform).anchoredPosition : Vector2.zero);
        }
        return res;
    }

    /// <summary>某条连线的标注中心到曲线的最短距离。负数表示取不到。 </summary>`);
  fs.writeFileSync(E, c, 'utf8'); n++;
  console.log('  ✓ TestLabelPositions 已加');
}

console.log('改动处数:', n);
