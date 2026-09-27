const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');
let n = 0;

/* 1) 时长 / 秒 两处调用（可能是 1~2 行） */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('BarLabel(_eqBar, "时长"') && !lines[i].includes('236')) {
    lines[i] = '        BarLabel(_eqBar, "时长", 24, 236, 62);';
    if (lines[i + 1] && lines[i + 1].includes('anchoredPosition')) { lines.splice(i + 1, 1); }
    n++; console.log('  ✓ 时长标签');
  }
  if (lines[i].includes('BarLabel(_eqBar, "秒"') && !lines[i].includes('336')) {
    lines[i] = '        BarLabel(_eqBar, "秒", 20, 336, 62);';
    if (lines[i + 1] && lines[i + 1].includes('anchoredPosition')) { lines.splice(i + 1, 1); }
    n++; console.log('  ✓ 秒标签');
  }
}

/* 2) MakeTagBtn 的签名与实现 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Button MakeTagBtn(string label, System.Action act, float x, float y)')) {
    lines[i] = '    Button MakeTagBtn(string label, System.Action act, float x, float y, float w = 72) {';
    n++; console.log('  ✓ MakeTagBtn 签名');
  }
  if (lines[i].includes('r.anchoredPosition = new Vector2(78 + x, y);')) {
    lines[i] = '        r.anchoredPosition = new Vector2(x, y);        /* x 是栏内绝对坐标 */';
    n++; console.log('  ✓ 去掉了 +78 偏移');
  }
  if (lines[i].includes('r.sizeDelta = new Vector2(72, 22);') && lines[i - 1] && lines[i - 1].includes('anchoredPosition = new Vector2(x, y)')) {
    lines[i] = '        r.sizeDelta = new Vector2(w, 22);';
    n++; console.log('  ✓ MakeTagBtn 宽度参数');
  }
}

/* 3) MakeEqBtn 去偏移、统一高度 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('r.anchoredPosition = new Vector2(78 + x, y);')) {
    lines[i] = '        r.anchoredPosition = new Vector2(x, y);        /* x 是栏内绝对坐标 */';
    n++; console.log('  ✓ MakeEqBtn 去偏移');
  }
}

fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('改动处数:', n);
