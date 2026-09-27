const fs = require('fs');
let n = 0;

/* 1) RedrawEdges 末尾（第 350 行的 }）插入第二遍标注放置 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);')) {
    /* 往后找到主循环的收尾（第 350 行的 }） */
    let close = i;
    for (let k = i; k < i + 8; k++) {
      if (lines[k].trim() === '}' && lines[k - 1] && lines[k - 1].includes('new Color(0.85f')) { close = k; break; }
    }
    const blk = [
      '',
      '        /* 第二遍：第一遍是顺序贪心，先放的不知道后放的会在哪，',
      '           于是两个标注可能横向压在一起。带着所有人的既有位置重排一次就分开了。 */',
      '        for (int pass = 0; pass < 2; pass++) {',
      '            foreach (var e2 in _edges) {',
      '                if (e2.Rt == null) continue;',
      '                var t2 = e2.Rt.GetComponentInChildren<Text>();',
      '                if (t2 == null) continue;',
      '                Vector2 q0 = Boundary(e2.A, e2.B.Pos), q1 = Boundary(e2.B, e2.A.Pos);',
      '                Vector2 dd2 = q1 - q0;',
      '                float len2 = Mathf.Max(1f, dd2.magnitude);',
      '                Vector2 dir2 = dd2 / len2, nrm3 = new Vector2(-dir2.y, dir2.x);',
      '                float off2 = Mathf.Clamp(len2 * 0.22f, 40f, 150f);',
      '                Vector2 ctrl2 = (q0 + q1) * 0.5f + nrm3 * off2 * 2f;',
      '                ((RectTransform)t2.transform).anchoredPosition = PickLabelPos(e2, q0, ctrl2, q1);',
      '            }',
      '        }',
    ];
    lines.splice(close, 0, ...blk);
    fs.writeFileSync(E, lines.join('\n'), 'utf8');
    n++;
    console.log('  ✓ 第二遍标注放置已插在第', close + 1, '行前');
    break;
  }
}

/* 2) 标注互不重叠的判据改成「矩形相交」而不是「中心距 < 60」 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace(`                        int lap = 0;
                        var ls = scn.TestLabelPositions();
                        for (int x = 0; x < ls.Count; x++)
                            for (int y = x + 1; y < ls.Count; y++) {
                                float dd = Vector2.Distance(ls[x], ls[y]);
                                if (dd < 60f) { lap++; Say($"   ✗ 标注 {x} 与 {y} 相距只有 {dd:F0}px"); }
                            }
                        TCheck("连线标注之间不重叠", lap == 0);`,
`                        int lap = 0;
                        var ls = scn.TestLabelPositions();
                        /* 判据要用「矩形相交」，不能用「中心距」——
                           标注本身宽 186px，中心距 60px 看着合格其实早就叠上了。 */
                        const float LW = 186f, LH2 = 40f;
                        for (int x = 0; x < ls.Count; x++)
                            for (int y = x + 1; y < ls.Count; y++) {
                                float ox = Mathf.Abs(ls[x].x - ls[y].x);
                                float oy = Mathf.Abs(ls[x].y - ls[y].y);
                                if (ox < LW && oy < LH2) {
                                    lap++;
                                    Say($"   ✗ 标注 {x} 与 {y} 矩形相交（横向 {ox:F0} < {LW:F0}）");
                                }
                            }
                        TCheck("连线标注之间不重叠", lap == 0);`);
fs.writeFileSync(S, sc, 'utf8'); n++;
console.log('  ✓ 判据改成矩形相交');
console.log('改动处数:', n);
