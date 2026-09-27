const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let lines = fs.readFileSync(T, 'utf8').split('\n');

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('$mm = [regex]::Matches($tt, "CLICK\\|" + [regex]::Escape($n)')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到那行'); 
  for (let i = 0; i < lines.length; i++) if (lines[i].includes('function Ctrl')) { for (let k=i;k<i+8;k++) console.log('  ', lines[k]); break; }
  process.exit(1);
}
console.log('第', at + 1, '行:', lines[at]);
/* 审计输出是 CLICK|名|x|y，末尾没有竖线 */
lines[at] = '    $mm = [regex]::Matches($tt, "CLICK\\|" + [regex]::Escape($n) + "\\|(\\d+)\\|(\\d+)")';
fs.writeFileSync(T, lines.join('\n'), 'utf8');
console.log('✓ 已改:', lines[at]);
