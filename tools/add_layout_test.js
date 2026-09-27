const fs = require('fs');
let n = 0;

/* ---- 自检：左栏矩形重叠检查 ---- */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('左栏布局')) {
  const anchor = '            /* ---------- 宠物精灵图 ---------- */';
  const add = `            /* ================================================================
               左栏布局：卡片里的各个区段不得重叠
                 用户反馈「排版乱七八糟」。根因是卡片高度固定、东西越塞越多，
                 内容物列表压在了标签栏与方程式栏上。这里用矩形相交来验。
               ================================================================ */
            {
                var lui = ChemLab.Game.LabUI.CreateForTest();
                if (lui == null) Say("   ✗ 无法建立测试用界面");
                else {
                    var rs = lui.TestCardRects();
                    Say("   卡片内区段 " + rs.Count + " 个");
                    int ov = 0;
                    for (int a = 0; a < rs.Count; a++)
                        for (int b = a + 1; b < rs.Count; b++) {
                            var ra = rs[a].r; var rb = rs[b].r;
                            bool hit = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;
                            if (hit) { ov++; Say("   ✗ 「" + rs[a].name + "」与「" + rs[b].name + "」重叠"); }
                        }
                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);
                    UnityEngine.Object.Destroy(lui.gameObject);
                }
            }

`;
  if (sc.includes(anchor)) { sc = sc.replace(anchor, add + anchor); n++; console.log('  ✓ 自检已加'); }
  else console.log('  ✗ 锚点');
  fs.writeFileSync(S, sc, 'utf8');
} else console.log('  - 自检已存在');

/* ---- LabUI：暴露矩形 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let uc = fs.readFileSync(U, 'utf8');
if (!uc.includes('TestCardRects')) {
  const anchor2 = '    public string TestCat => _cat;';
  const add2 = `    /// <summary>卡片内各区段的矩形（本地坐标），供布局重叠检查用</summary>
    public List<(string name, Rect r)> TestCardRects() {
        var res = new List<(string, Rect)>();
        var card = _canvas.transform.Find("LeftPanel/VesselCard") as RectTransform;
        if (card == null) return res;
        string[] want = { "Contents", "PourBar", "BurnerBar", "TestBar", "TagBar", "EqBar" };
        foreach (var w in want) {
            var t = card.Find(w) as RectTransform;
            if (t != null && t.gameObject.activeSelf) res.Add((w, t.rect));
        }
        for (int i = 0; i < 6; i++) {
            var t = card.Find("Act" + i) as RectTransform;
            if (t != null) res.Add(("Act" + i, t.rect));
        }
        return res;
    }

    public string TestCat => _cat;`;
  if (uc.includes(anchor2)) { uc = uc.replace(anchor2, add2); n++; console.log('  ✓ TestCardRects 已加'); }
  else console.log('  ✗ 锚点2');
  fs.writeFileSync(U, uc, 'utf8');
} else console.log('  - TestCardRects 已存在');

console.log('补丁处数:', n);
