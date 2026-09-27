/* =====================================================================
   两个工具栏：按精确坐标重排（行级替换，不做字符串猜测）
     TagBar 330×64：两行
       y=36  容器标签 | 标签 | 具体/离子
       y=8   物质 | mol | 浓度
     EqBar  330×100：三行
       y=70  方程式标签 | 显示 | 时长 [10] 秒
       y=40  离子式
       y=10  （留白）
   ===================================================================== */
const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');
let n = 0;

/* --- TagBar 的五个按钮 --- */
const tagPlan = [
  ['"标签"',      '78',  '36', '72'],
  ['"具体/离子"', '156', '36', '76'],
  ['"物质"',      '78',  '8',  '72'],
  ['"mol"',       '156', '8',  '72'],
  ['"浓度"',      '234', '8',  '72'],
];
for (let i = 0; i < lines.length; i++) {
  const L = lines[i];
  if (!L.includes('MakeTagBtn(')) continue;
  for (const [label, x, y, w] of tagPlan) {
    if (!L.includes(label)) continue;
    /* 保持原有的回调，只换坐标与宽度 */
    const m = L.match(/MakeTagBtn\(\s*(".*?")\s*,\s*(.*?)\s*,\s*[\d.]+\s*,\s*[\d.]+\s*(?:,\s*[\d.]+\s*)?\)/);
    if (m) {
      lines[i] = '        MakeTagBtn(' + m[1] + ', ' + m[2] + ', ' + x + ', ' + y + ', ' + w + ');';
      n++;
      console.log('  ✓ TagBar', label, '→', x + ',' + y);
    }
    break;
  }
}

/* --- EqBar 的两个按钮 --- */
for (let i = 0; i < lines.length; i++) {
  const L = lines[i];
  if (!L.includes('MakeEqBtn(')) continue;
  if (L.includes('"显示 开"')) {
    lines[i] = lines[i].replace(/MakeEqBtn\(\s*"显示 开"\s*,\s*(.*?)\s*,\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*\)/,
                                'MakeEqBtn("显示 开", $1, 78, 70, 74)');
    n++; console.log('  ✓ EqBar 显示 开 → 78,70');
  }
  if (L.includes('"离子式 关"')) {
    lines[i] = lines[i].replace(/MakeEqBtn\(\s*"离子式 关"\s*,\s*(.*?)\s*,\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*\)/,
                                'MakeEqBtn("离子式 关", $1, 78, 40, 74)');
    n++; console.log('  ✓ EqBar 离子式 关 → 78,40');
  }
}

/* --- 标签与数字框 --- */
for (let i = 0; i < lines.length; i++) {
  const L = lines[i];
  if (L.includes('BarLabel(_tagBar, "容器标签"')) { lines[i] = '        BarLabel(_tagBar, "容器标签", 74, 0, 36);'; n++; console.log('  ✓ 容器标签 → 0,36'); }
  if (L.includes('BarLabel(_eqBar, "方程式标签"')) { lines[i] = '        BarLabel(_eqBar, "方程式标签", 74, 0, 70);'; n++; console.log('  ✓ 方程式标签 → 0,70'); }
  if (L.includes('BarLabel(_eqBar, "时长"')) { lines[i] = '        BarLabel(_eqBar, "时长", 24, 156, 70);'; n++; console.log('  ✓ 时长 → 156,70'); }
  if (L.includes('BarLabel(_eqBar, "秒"')) { lines[i] = '        BarLabel(_eqBar, "秒", 20, 252, 70);'; n++; console.log('  ✓ 秒 → 252,70'); }
  if (L.includes('MakeNumField(_eqBar')) {
    lines[i] = lines[i].replace(/new Vector2\([\d.]+,\s*[\d.]+\)/, 'new Vector2(182, 68)');
    n++; console.log('  ✓ 数字框 → 182,68');
  }
}

fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('改动处数:', n);
