const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');

if (!sc.includes('Born-Haber 循环的标注')) {
  sc = sc.replace('                    /* ⑤ 单击节点 → 拉线 → 点另一个节点 */',
`                    /* ⑤ Born-Haber 循环（4 节点 5 边）的标注也要不压线、不互压。
                          示例场景只有 3 条边，覆盖不到教程用的那张图。 */
                    {
                        scn.SeedBornHaberDemo();
                        const float LW = 186f, LH2 = 40f;
                        int bad = 0;
                        var ls = scn.TestLabelPositions();
                        Say($"   Born-Haber：{scn.TestNodeCount} 个节点，{scn.TestEdgeCount} 条边，{ls.Count} 个标注");
                        for (int x = 0; x < ls.Count; x++) {
                            float cl = scn.TestLabelClearance(x);
                            if (cl >= 0f && cl < 40f) { bad++; Say($"   ✗ 标注 {x} 离曲线只有 {cl:F0}px"); }
                            for (int y = x + 1; y < ls.Count; y++) {
                                float ox = Mathf.Abs(ls[x].x - ls[y].x);
                                float oy = Mathf.Abs(ls[x].y - ls[y].y);
                                if (ox < LW && oy < LH2) { bad++; Say($"   ✗ 标注 {x} 与 {y} 矩形相交（横向 {ox:F0}）"); }
                            }
                        }
                        TCheck("Born-Haber 循环的标注不压线也不互压", bad == 0);
                    }

                    /* ⑥ 单击节点 → 拉线 → 点另一个节点 */`);
  fs.writeFileSync(S, sc, 'utf8');
  console.log('✓ Born-Haber 标注检查已加');
} else console.log('- 已存在');
