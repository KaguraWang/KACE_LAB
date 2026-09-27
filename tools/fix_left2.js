/* =====================================================================
   按行重排左栏 + 修掉 EN 按钮重复创建
     · 卡片 424 → 620（Y 从 -272 提到 -220）
     · 各栏按明确区段分层，互不重叠
     · RefreshReagents 里那句 BuildLangButton 是多余的 ——
       每次刷新都会再建一个 EN 按钮叠上去，删掉
   ===================================================================== */
const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');
let n = 0;
const set = (i, s) => { lines[i] = s; n++; };

/* 先定位 */
let iCard = -1, iInfo = -1, iTagBar = -1, iEqBar = -1, iTestBar = -1, iBurnerBar = -1, iPourBar = -1, iLabel = -1;
for (let i = 0; i < lines.length; i++) {
  const L = lines[i];
  if (iCard < 0 && L.includes('float y = -272;')) iCard = i;
  if (L.includes('irt.sizeDelta = new Vector2(-28, 424);')) iInfo = i;
  if (L.includes('Label(info, "内容物"')) iLabel = i;
  if (L.includes('_pourBar = Node("PourBar"')) iPourBar = i;
  if (L.includes('_burnerBar = Node("BurnerBar"')) iBurnerBar = i;
  if (L.includes('_testBar = Node("TestBar"')) iTestBar = i;
  if (L.includes('_tagBar = Node("TagBar"')) iTagBar = i;
  if (L.includes('_eqBar = Node("EqBar"')) iEqBar = i;
}
console.log('定位: 卡片', iCard + 1, '尺寸', iInfo + 1, '内容物', iLabel + 1,
            '倾倒', iPourBar + 1, '火力', iBurnerBar + 1, '检验', iTestBar + 1,
            '标签', iTagBar + 1, '方程式', iEqBar + 1);

/* 1) 卡片：上移 + 加高 */
if (iCard >= 0) set(iCard, '        float y = -220;');
if (iInfo >= 0) set(iInfo, '        irt.sizeDelta = new Vector2(-28, 620);   /* 620 才装得下所有区段，见下方分层 */');

/* 2) 温度/pH/液面格：-58 → -52 */
for (let i = 0; i < lines.length; i++)
  if (lines[i].includes('crt.anchoredPosition = new Vector2(x0, -58);'))
    set(i, '            crt.anchoredPosition = new Vector2(x0, -52);');

/* 3) pH 色条：-112 → -106 */
for (let i = 0; i < lines.length; i++)
  if (lines[i].includes('brt.anchoredPosition = new Vector2(0, -112);'))
    set(i, '        brt.anchoredPosition = new Vector2(0, -106); brt.sizeDelta = new Vector2(-24, 8);');

/* 4) 内容物标题 + 列表：往下压太多，改成只到顶部 250 */
if (iLabel >= 0) {
  set(iLabel + 1, '              new Vector2(0,1), new Vector2(1,1), new Vector2(12,-140), new Vector2(-12,-122));');
}
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('_vesselInfo.anchoredPosition = new Vector2(0, -172);'))
    set(i, '        _vesselInfo.anchoredPosition = new Vector2(0, -144);');
  if (lines[i].includes('_vesselInfo.sizeDelta = new Vector2(0, 120);'))
    set(i, '        _vesselInfo.sizeDelta = new Vector2(0, 106);   /* 到顶部 250 为止，不再压下面的栏 */');
}

/* 5) 四条工具栏：从底往上依次排开 */
if (iPourBar >= 0) set(iPourBar + 1, '                        new Vector2(12, 100), new Vector2(-12, 126), new Color(0,0,0,0));');
if (iBurnerBar >= 0) set(iBurnerBar + 1, '                          new Vector2(12, 130), new Vector2(-12, 156), new Color(0,0,0,0));');
if (iTestBar >= 0) set(iTestBar + 1, '                        new Vector2(12, 160), new Vector2(-12, 186), new Color(0,0,0,0));');
if (iTagBar >= 0) set(iTagBar + 1, '                       new Vector2(12, 192), new Vector2(-12, 254), new Color(0,0,0,0));');
if (iEqBar >= 0) set(iEqBar + 1, '                      new Vector2(12, 262), new Vector2(-12, 362), new Color(0,0,0,0));');

/* 6) 删掉 RefreshReagents 里那句多余的 BuildLangButton */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].trim() === 'BuildLangButton(_canvas.transform);' && i > 400) {
    console.log('  删除第', i + 1, '行的多余 BuildLangButton');
    lines[i] = '        /* EN 按钮只在 Build 时建一次 —— 这里再建会不断叠加 */';
    n++;
    break;
  }
}

fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('改动处数:', n);

/* 查验 */
const out = fs.readFileSync(U, 'utf8').split('\n');
console.log('');
console.log('=== 改后关键行 ===');
for (const [name, idx] of [['卡片Y', iCard], ['卡片高', iInfo], ['倾倒', iPourBar], ['火力', iBurnerBar],
                            ['检验', iTestBar], ['标签', iTagBar], ['方程式', iEqBar]]) {
  if (idx >= 0) console.log('  ' + name + ': ' + out[idx].trim().slice(0, 90));
}
