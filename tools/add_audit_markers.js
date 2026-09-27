const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

if (fs.readFileSync(B, 'utf8').includes('AUDITSTART')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Debug.Log(cs.AuditLayout());')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 AuditLayout 调用'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

lines[at] = '                Debug.Log("AUDITSTART\\n" + cs.AuditLayout() + "AUDITEND\\n");';
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ 审计已加起止标记');
console.log('  括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);

/* 测试脚本改用 AUDITSTART/AUDITEND */
const P = 'D:/ChemLabUnity/tools/dlgtest2.ps1';
let p = fs.readFileSync(P, 'utf8').split('\n');
let from = -1, to = -1;
for (let i = 0; i < p.length; i++) {
  if (p[i].includes('function LastAudit {')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到 LastAudit'); process.exit(1); }
for (let i = from; i < from + 15; i++) { if (p[i] === '}') { to = i; break; } }
const nw = [
  '# 只取最后一块审计。用 AUDITSTART / AUDITEND 标记 ——',
  '# 不能靠 "[CalcAudit]" 定位：审计每行都以它开头，LastIndexOf 找到的是最后一行。',
  'function LastAudit {',
  '    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    if (-not $t) { return "" }',
  '    $i = $t.LastIndexOf("AUDITSTART")',
  '    if ($i -lt 0) { return "" }',
  '    $j = $t.IndexOf("AUDITEND", $i)',
  '    if ($j -lt 0) { $j = [Math]::Min($t.Length, $i + 6000) }',
  '    return $t.Substring($i, $j - $i)',
  '}',
];
p.splice(from, to - from + 1, ...nw);
fs.writeFileSync(P, p.join('\n'), 'utf8');
console.log('✓ dlgtest2 的 LastAudit 已改用 AUDITSTART');
