const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 1) 删掉后面那份重复的 SetLabVisible（约 601 行那个 static 版本） */
const dups = [];
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('static void SetLabVisible(bool labOn)')) dups.push(i);
  if (lines[i].includes('public static void SetLabVisible(bool labOn)')) dups.push(i);
}
console.log('SetLabVisible 定义在:', dups.map(x => x + 1).join(', '));
if (dups.length >= 2) {
  /* 删掉最后一个（连同它的三行体） */
  const at = dups[dups.length - 1];
  let end = at;
  let depth = 0, started = false;
  for (let k = at; k < lines.length; k++) {
    depth += (lines[k].match(/\{/g) || []).length;
    depth -= (lines[k].match(/\}/g) || []).length;
    if (depth > 0) started = true;
    if (started && depth === 0) { end = k; break; }
  }
  lines.splice(at, end - at + 1);
  console.log('  删掉了第', at + 1, '..', end + 1, '行的重复定义');
}

/* 2) 在 Show 的两处出口加 SetLabVisible(false) */
let c = lines.join('\n');
c = c.replace('        if (Instance != null) { Instance.gameObject.SetActive(true); return Instance; }',
`        if (Instance != null) {
            Instance.gameObject.SetActive(true);
            SetLabVisible(false);      // 进计算器就藏起实验台界面（不只是透明，是不可交互）
            return Instance;
        }`);
c = c.replace(`        s.Build(font);
        Instance = s;`,
`        s.Build(font);
        Instance = s;
        SetLabVisible(false);`);
lines = c.split('\n');
fs.writeFileSync(E, lines.join('\n'), 'utf8');

/* 查验 */
const chk = fs.readFileSync(E, 'utf8');
console.log('  SetLabVisible 定义次数:', (chk.match(/(public )?static void SetLabVisible/g) || []).length);
console.log('  Show 里调用 false:', (chk.match(/SetLabVisible\(false\)/g) || []).length);
console.log('  Back 里调用 true:', (chk.match(/SetLabVisible\(true\)/g) || []).length);
