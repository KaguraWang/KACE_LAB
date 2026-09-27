const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let lines = fs.readFileSync(T, 'utf8').split('\n');

let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("=== 3. 器材架点「烧杯」 ===")) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到第 3 步'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 20; i++) {
  if (lines[i] !== undefined && lines[i].includes("Check '点「烧杯」后台面出现器皿'")) { b = i; break; }
}
if (b < 0) { console.log('✗ 找不到判据'); process.exit(1); }
console.log('第 3 步:', a + 1, '..', b + 1);
for (let i = a; i <= b; i++) console.log('   ', lines[i]);

const nw = [
  "Write-Host '=== 3. 器材架点「烧杯」 ==='",
  '# 数器皿个数，不采样像素 —— 新器皿的位置改过之后，',
  '# 原来固定的采样区域就套不中了（像素判据太脆）。',
  'function VesselCount {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "STATE\\|vessel\\|(v\\d+)\\|")',
  '    $ids = @{}',
  '    foreach ($x in $mm) { $ids[$x.Groups[1].Value] = 1 }',
  '    return $ids.Count',
  '}',
  '$n0 = VesselCount',
  'Write-Host "  点之前器皿数 = $n0"',
  'Click-At $h 125 147 "Left" 2000',
  'Start-Sleep -Seconds 3',
  '$n1 = VesselCount',
  'Write-Host "  点之后器皿数 = $n1"',
  "Check '点「烧杯」后器皿数增加' ($n1 -gt $n0) \"器皿 $n0 -> $n1\"",
  'Shot $h "$out\\21-after-beaker.png" | Out-Null',
];
lines.splice(a, b - a + 1, ...nw);
fs.writeFileSync(T, lines.join('\n'), 'utf8');
console.log('✓ 第 3 步已改为数器皿');
