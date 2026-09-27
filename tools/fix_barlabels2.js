const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* 按行替换：每个栏标签是 2 行（Label 调用 + 可能的 sizeDelta 行） */
const jobs = [
  { key: '倾倒量',     repl: ['        _pourLabel = BarLabel(_pourBar, "倾倒量", 74, 12);'], skipNext: 1 },
  { key: '本生灯火力', repl: ['        _burnerLabel = BarLabel(_burnerBar, "本生灯火力", 86, 12);'], skipNext: 1 },
  { key: '检验器材',   repl: ['        _testLabel = BarLabel(_testBar, "检验器材", 74);'], skipNext: 1 },
  { key: '容器标签',   repl: ['        BarLabel(_tagBar, "容器标签", 74);'], skipNext: 2 },
  { key: '方程式标签', repl: ['        BarLabel(_eqBar, "方程式标签", 74);'], skipNext: 2 },
  { key: '时长',       repl: ['        var dl = BarLabel(_eqBar, "时长", 24);',
                              '        ((RectTransform)dl.transform).anchoredPosition = new Vector2(78, 0);'], skipNext: 0 },
  { key: '"秒"',       repl: ['        var ul = BarLabel(_eqBar, "秒", 20);',
                              '        ((RectTransform)ul.transform).anchoredPosition = new Vector2(172, 0);'], skipNext: 0 },
];

let n = 0;
for (const j of jobs) {
  for (let i = 0; i < lines.length; i++) {
    if (!lines[i].includes('Label(') || !lines[i].includes(j.key)) continue;
    /* 确认下一行是接续的参数 */
    let consume = 1;                       // 至少要吃掉第一行
    if (lines[i + 1] && /new Vector2\(/.test(lines[i + 1])) consume = 2;
    /* 再吃掉紧随其后的 sizeDelta 行 */
    let extra = consume;
    if (lines[i + consume] && lines[i + consume].includes('sizeDelta')) extra = consume + 1;
    lines.splice(i, extra, ...j.repl);
    n++;
    console.log('  ✓', j.key, '（吃了', extra, '行）');
    break;
  }
}
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('替换处数:', n);
