/* =====================================================================
   重排左栏「容器卡片」
     病灶：卡片是固定高度 424，而往里加的东西越来越多。
     「内容物」列表从顶部 172 往下占 120，正好压在
     标签栏（从底部 172 起）和方程式栏（从底部 242 起）上 —— 全糊在一起。

     做法：把卡片加高到 620，上下两头各排各的，中间留出明确的分界：
       从顶部：标题 → 温度/pH/液面 → pH 色条 → 内容物标题 → 内容物列表（到 250）
       从底部：操作按钮（到 88）→ 倾倒 → 火力 → 检验 → 标签栏（到 254）→ 方程式栏（到 362）
       250 + 362 = 612 < 620 ✓ 两头不打架
   ===================================================================== */
const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
let n = 0;
const P = (a, b, label) => {
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 50).replace(/\n/g, '⏎')); return; }
  c = c.replace(a, b); n++;
};

/* ---- 卡片本体：位置上移、加高 ---- */
P(`        float y = -272;
        var info = Node("VesselCard", root, new Vector2(0,1), new Vector2(1,1),
                        Vector2.zero, Vector2.zero, BG2);
        var irt = (RectTransform)info;
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0, y);
        irt.sizeDelta = new Vector2(-28, 424);`,
`        /* 卡片高度要装得下所有区段 —— 见文件顶部注释里的分层表 */
        float y = -220;
        const float CardH = 620f;
        var info = Node("VesselCard", root, new Vector2(0,1), new Vector2(1,1),
                        Vector2.zero, Vector2.zero, BG2);
        var irt = (RectTransform)info;
        irt.pivot = new Vector2(0.5f, 1f);
        irt.anchoredPosition = new Vector2(0, y);
        irt.sizeDelta = new Vector2(-28, CardH);`, '卡片本体');

/* ---- 温度/pH/液面 ---- */
P(`            crt.anchoredPosition = new Vector2(x0, -58);
            crt.sizeDelta = new Vector2(80, 46);`,
`            crt.anchoredPosition = new Vector2(x0, -52);
            crt.sizeDelta = new Vector2(80, 46);`, '三个数值格');

/* ---- pH 色条 ---- */
P(`        brt.anchoredPosition = new Vector2(0, -112); brt.sizeDelta = new Vector2(-24, 8);`,
  `        brt.anchoredPosition = new Vector2(0, -106); brt.sizeDelta = new Vector2(-24, 8);`, 'pH 色条');

/* ---- 内容物标题 + 列表 ---- */
P(`        Label(info, "内容物", 11, DIM, TextAnchor.UpperLeft,
              new Vector2(0,1), new Vector2(1,1), new Vector2(12,-152), new Vector2(-12,-134));
        _vesselInfo = Node("Contents", info, new Vector2(0,1), new Vector2(1,1),
                           new Vector2(12, 0), new Vector2(-12, 0), new Color(0,0,0,0));
        _vesselInfo.pivot = new Vector2(0.5f, 1f);
        _vesselInfo.anchoredPosition = new Vector2(0, -172);
        _vesselInfo.sizeDelta = new Vector2(0, 120);`,
`        Label(info, "内容物", 11, DIM, TextAnchor.UpperLeft,
              new Vector2(0,1), new Vector2(1,1), new Vector2(12,-140), new Vector2(-12,-122));
        _vesselInfo = Node("Contents", info, new Vector2(0,1), new Vector2(1,1),
                           new Vector2(12, 0), new Vector2(-12, 0), new Color(0,0,0,0));
        _vesselInfo.pivot = new Vector2(0.5f, 1f);
        _vesselInfo.anchoredPosition = new Vector2(0, -144);
        _vesselInfo.sizeDelta = new Vector2(0, 106);      /* 到顶部 250 为止，不再压下面的栏 */`, '内容物');

/* ---- 操作按钮：底排 ---- */
P(`            rt.anchoredPosition = new Vector2(12 + col * 88, 14 + (1 - row) * 40);
            rt.sizeDelta = new Vector2(80, 34);`,
`            rt.anchoredPosition = new Vector2(12 + col * 88, 14 + (1 - row) * 40);
            rt.sizeDelta = new Vector2(80, 34);   /* 占 14..88 */`, '操作按钮');

/* ---- 四条工具栏：从底往上依次排开，互不重叠 ---- */
P(`        _pourBar = Node("PourBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 104), new Vector2(-12, 130), new Color(0,0,0,0));`,
`        _pourBar = Node("PourBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 100), new Vector2(-12, 126), new Color(0,0,0,0));`, '倾倒栏');

P(`        _burnerBar = Node("BurnerBar", info, new Vector2(0,0), new Vector2(1,0),
                          new Vector2(12, 104), new Vector2(-12, 130), new Color(0,0,0,0));`,
`        _burnerBar = Node("BurnerBar", info, new Vector2(0,0), new Vector2(1,0),
                          new Vector2(12, 130), new Vector2(-12, 156), new Color(0,0,0,0));`, '火力栏');

P(`        _testBar = Node("TestBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 138), new Vector2(-12, 164), new Color(0,0,0,0));`,
`        _testBar = Node("TestBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 160), new Vector2(-12, 186), new Color(0,0,0,0));`, '检验栏');

P(`        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 172), new Vector2(-12, 234), new Color(0,0,0,0));`,
`        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 192), new Vector2(-12, 254), new Color(0,0,0,0));`, '标签栏');

P(`        _eqBar = Node("EqBar", info, new Vector2(0,0), new Vector2(1,0),
                      new Vector2(12, 242), new Vector2(-12, 312), new Color(0,0,0,0));`,
`        _eqBar = Node("EqBar", info, new Vector2(0,0), new Vector2(1,0),
                      new Vector2(12, 262), new Vector2(-12, 362), new Color(0,0,0,0));`, '方程式栏');

fs.writeFileSync(U, c, 'utf8');
console.log('补丁处数:', n);
