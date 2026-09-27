const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/dlgtest2.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function LastAudit {')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到 LastAudit'); process.exit(1); }
for (let i = from; i < from + 15; i++) { if (lines[i] === '}') { to = i; break; } }
console.log('LastAudit 在', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  '# 只取最后一块 [CalcAudit]：从最后一个 "[CalcAudit]" 到下一个 "[CalcAudit]" 之前。',
  '# 不能靠 DUMPEND —— -calcDemo 的审计没有那个标记（只有 -dumpLayout 才有）。',
  'function LastAudit {',
  '    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    if (-not $t) { return "" }',
  '    $i = $t.LastIndexOf("[CalcAudit]")',
  '    if ($i -lt 0) { return "" }',
  '    $j = $t.IndexOf("[CalcAudit]", $i + 1)',
  '    if ($j -lt 0) { $j = [Math]::Min($t.Length, $i + 6000) }',
  '    return $t.Substring($i, $j - $i)',
  '}',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ LastAudit 已改');
