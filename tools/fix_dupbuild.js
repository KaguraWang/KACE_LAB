const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 1) 删掉悬空的 foreach（它把 PetView.Show 当成了循环体） */
const del = [];
for (let i = 0; i < lines.length; i++) {
  if (lines[i].trim() === 'foreach (var arg in System.Environment.GetCommandLineArgs())'
      && lines[i + 1] && lines[i + 1].includes('PetView.Show')) {
    del.push(i);
  }
}
for (const i of del.reverse()) { lines.splice(i, 1); console.log('  删掉第', i + 1, '行的悬空 foreach'); }

/* 2) 去掉重复的 Build() + ApplyLang() 那一组 */
let removed = 0;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('_ui.Build();') && i > 0) {
    /* 往后看，如果紧接着又有一次 Build，就删掉这一组 */
    for (let k = i + 1; k < Math.min(i + 5, lines.length); k++) {
      if (lines[k].includes('_ui.Build();')) {
        /* 删掉 i..k-1 这一组（含 ApplyLang） */
        let end = k - 1;
        while (end > i && !lines[end].includes('ApplyLang')) end--;
        lines.splice(i, end - i + 1);
        removed++;
        console.log('  删掉重复的 Build 组（第', i + 1, '..', end + 1, '行）');
        break;
      }
    }
  }
}

/* 3) SetLabUiVisible 加日志，便于确认有没有被调用 */
let c = lines.join('\n');
c = c.replace(`    public void SetLabUiVisible(bool on) {
        if (_ui != null) _ui.gameObject.SetActive(on);
    }`,
`    public void SetLabUiVisible(bool on) {
        Debug.Log("[ChemLab] SetLabUiVisible(" + on + ")  _ui=" + (_ui != null));
        if (_ui != null) _ui.gameObject.SetActive(on);
    }`);
lines = c.split('\n');
fs.writeFileSync(B, lines.join('\n'), 'utf8');

console.log('改动处数:', removed + del.length + 1);
const chk = fs.readFileSync(B, 'utf8');
console.log('  Build() 次数:', (chk.match(/_ui\.Build\(\);/g) || []).length);
