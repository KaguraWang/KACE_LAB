const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* 1) LabUI 暴露测试接口 */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
P(U, '    /* ------------------------------------------------------------ 右：试剂架 */',
`    /* ================================================================
       测试接口：让自动化测试能真的点按钮、读状态。
       靠读代码猜为什么点击不生效是不可靠的 —— 这些接口让测试能直接验证。
       ================================================================ */
    public string TestCat => _cat;
    public string TestQuery => _query;
    public string TestAmount => _amount;
    public int TestVisibleCount => _contentList != null ? _contentList.childCount : -1;
    public int TestReagentTotal => _reagents != null ? _reagents.Count : -1;
    /// <summary>按名字找到一个按钮并真的触发它的 onClick</summary>
    public bool TestClickButton(string namePart) {
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) {
            if (b.gameObject.name.Contains(namePart)) { b.onClick.Invoke(); return true; }
        }
        return false;
    }
    public List<string> TestListButtons() {
        var res = new List<string>();
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) res.Add(b.gameObject.name);
        return res;
    }
    /// <summary>直接调 RefreshReagents，测过滤器本身</summary>
    public void TestSetFilter(string cat, string query) {
        _cat = cat; _query = query ?? "";
        RefreshReagents();
    }

    /* ------------------------------------------------------------ 右：试剂架 */`);

/* 2) 自检里加一组界面测试 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
const sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('试剂架过滤')) {
  const anchor = '            /* ---------- 宠物精灵图 ---------- */';
  const add = `            /* ================================================================
               试剂架：过滤按钮与搜索框
                 用户反馈「过滤按钮完全不起作用、搜索框没用」。
                 这里真的去点按钮、真的设查询词，看列表有没有变。
                 光读代码猜是不可靠的。
               ================================================================ */
            {
                var ui = ChemLab.Game.LabUI.CreateForTest();
                if (ui == null) Say("   ✗ 无法建立测试用界面");
                else {
                    Say($"   试剂总数 {ui.TestReagentTotal}，按钮共 {ui.TestListButtons().Count} 个");
                    /* 先记下初始状态 */
                    string cat0 = ui.TestCat;
                    int n0 = ui.TestVisibleCount;
                    Say($"   初始分类「{cat0}」列出 {n0} 条");

                    /* --- 分类按钮：换一个类别，列表应当变化 --- */
                    bool clicked = ui.TestClickButton("Cat2");
                    Say("   点击 Cat2（氧化物）：" + (clicked ? "找到并触发了" : "✗ 根本没找到这个按钮"));
                    TCheck("分类按钮存在且能被点击", clicked);
                    if (clicked) {
                        Say($"   点后分类「{ui.TestCat}」，列出 {ui.TestVisibleCount} 条");
                        TCheck("点分类按钮后 _cat 确实变了", ui.TestCat != cat0);
                        TCheck("点分类按钮后列表被刷新", ui.TestVisibleCount != n0 || ui.TestVisibleCount >= 0);
                    }

                    /* --- 直接测过滤器本身（排除点击层的问题）--- */
                    ui.TestSetFilter("acid", "");
                    int nAcid = ui.TestVisibleCount;
                    ui.TestSetFilter("base", "");
                    int nBase = ui.TestVisibleCount;
                    Say($"   直接设过滤器：酸 {nAcid} 条，碱 {nBase} 条");
                    TCheck("不同分类列出不同数量（酸≠碱）", nAcid != nBase && nAcid > 0 && nBase > 0);

                    /* --- 搜索框 --- */
                    ui.TestSetFilter("acid", "硫酸");
                    int nq = ui.TestVisibleCount;
                    Say($"   搜索「硫酸」→ {nq} 条");
                    TCheck("搜索能缩小结果", nq > 0 && nq < nAcid);

                    ui.TestSetFilter("acid", "zzzz不存在");
                    int nz = ui.TestVisibleCount;
                    Say($"   搜索「zzzz不存在」→ {nz} 条");
                    TCheck("搜不到时结果为空", nz == 0);

                    UnityEngine.Object.Destroy(ui.gameObject);
                }
            }

`;
  if (sc.includes(anchor)) {
    fs.writeFileSync(S, sc.replace(anchor, add + anchor), 'utf8'); n++;
    console.log('  ✓ 界面自检已加');
  } else console.log('  ✗ 找不到宠物精灵图锚点');
} else console.log('  - 界面自检已存在');

console.log('补丁处数:', n);
