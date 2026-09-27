const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');

if (sc.includes('容器标签在反应标签过期后仍然存在')) {
  console.log('- 断言已存在');
  process.exit(0);
}

const anchor = '                    /* ⑤ Born-Haber 循环（4 节点 5 边）的标注也要不压线、不互压。';
if (!sc.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = `                    /* ⑤ 容器标签：必须永久存在、且互不重叠。
                         这条断言是被真实 bug 逼出来的 —— 标签曾经全都 active、
                         canvas 也 enabled，就是不在画面上（世界坐标被清零了），
                         而且容器标签还会因为方程式标签而消失。 */
                    {
                        var host = ChemLab.Game.LabBootstrap.Instance;
                        int tagOk = 0, tagBad = 0;
                        if (host != null) {
                            var all = host.GetComponentsInChildren<ChemLab.Game.VesselTag>(true);
                            foreach (var tg in all) {
                                if (tg == null) continue;
                                if (!tg.gameObject.activeInHierarchy) { tagBad++; Say("   ✗ 标签 " + tg.name + " 不显示"); continue; }
                                var cv = tg.GetComponentInChildren<Canvas>(true);
                                if (cv == null || !cv.enabled) { tagBad++; Say("   ✗ 标签 " + tg.name + " 的画布没启用"); continue; }
                                float lift = tg.transform.position.y;
                                if (lift < 0.2f) { tagBad++; Say($"   ✗ 标签 {tg.name} 的世界 Y 只有 {lift:F2}（像是没定位）"); continue; }
                                tagOk++;
                            }
                            Say($"   容器标签：正常 {tagOk} 个，异常 {tagBad} 个");
                        }
                        TCheck("容器标签都真的显示出来了（不是只有 active）", tagBad == 0 && tagOk > 0);
                        /* 标签之间不许重叠 */
                        int tlap = 0;
                        if (host != null) {
                            var all = host.GetComponentsInChildren<ChemLab.Game.VesselTag>(true);
                            for (int a = 0; a < all.Length; a++)
                                for (int b = a + 1; b < all.Length; b++) {
                                    var ra = all[a].ScreenRect; var rb = all[b].ScreenRect;
                                    if (ra.width <= 0f || rb.width <= 0f) continue;
                                    if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) tlap++;
                                }
                        }
                        TCheck("容器标签之间不重叠", tlap == 0);
                    }

                    /* ⑤ Born-Haber 循环（4 节点 5 边）的标注也要不压线、不互压。`;

sc = sc.replace(anchor, add);
fs.writeFileSync(S, sc, 'utf8');
console.log('✓ 容器标签的自检断言已加');
console.log('  括号差:', (sc.match(/\{/g) || []).length - (sc.match(/\}/g) || []).length);
