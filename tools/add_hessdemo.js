const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 找 -calcDemo 处理块的结尾 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (arg == "-calcDemo") {')) {
    /* 找到这个 if 块结束 */
    let depth = 0, started = false;
    for (let k = i; k < lines.length; k++) {
      depth += (lines[k].match(/\{/g) || []).length;
      depth -= (lines[k].match(/\}/g) || []).length;
      if (depth > 0) started = true;
      if (started && depth === 0) { at = k; break; }
    }
    break;
  }
}
console.log('-calcDemo 块结束于第', at + 1, '行');
if (at < 0) process.exit(1);

if (lines.slice(at, at + 8).some(l => l.includes('hessDemo'))) {
  console.log('- 已有 hessDemo 处理');
} else {
  lines.splice(at + 1, 0,
    '            /* -hessDemo：摆出 KCl 的 Born-Haber 循环（教程第 7 步用的那个），',
    '               单独抽出来便于截图验证，不用每次走完整段教程 */',
    '            if (arg == "-hessDemo") {',
    '                var cs2 = EnthalpyScene.Show(gameObject, LabUI.UiFont);',
    '                if (cs2 != null) { cs2.transform.SetAsLastSibling(); cs2.SeedBornHaberDemo(); }',
    '            }');
  fs.writeFileSync(B, lines.join('\n'), 'utf8');
  console.log('✓ hessDemo 处理块已插入');
}
