const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/dlgtest2.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

/* Ctrl 加等待重试：审计每 1.2 秒才打一次，控件刚出现时要等它刷新 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Ctrl([string]$name) {')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 Ctrl'); process.exit(1); }
let to = -1;
for (let i = at; i < at + 15; i++) { if (lines[i] === '}') { to = i; break; } }
console.log('Ctrl 在', at + 1, '..', to + 1);

const nw = [
  'function Ctrl([string]$name) {',
  '    # 审计每 1.2 秒才打一次，控件刚出现时要等它刷新 —— 直接读会拿到上一块',
  '    for ($try = 0; $try -lt 6; $try++) {',
  '        $blk = LastAudit',
  '        $m = [regex]::Matches($blk, "CLICK\\|" + [regex]::Escape($name) + "\\|(\\d+)\\|(\\d+)\\|")',
  '        if ($m.Count -gt 0) {',
  '            $x = [int]$m[$m.Count - 1].Groups[1].Value',
  '            $y = [int]$m[$m.Count - 1].Groups[2].Value',
  '            if ($x -ge 0 -and $x -le 2560 -and $y -ge 0 -and $y -le 1600) { return @($x, $y) }',
  '        }',
  '        Start-Sleep -Milliseconds 900',
  '    }',
  '    return $null',
  '}',
];
lines.splice(at, to - at + 1, ...nw);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ Ctrl 已加等待重试');
