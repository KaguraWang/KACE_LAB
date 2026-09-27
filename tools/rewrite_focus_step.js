const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

const a = t.indexOf('function Glow {');
const bMark = 'Shot $h "$out\\32-unfocused.png" | Out-Null';
const b = t.indexOf(bMark);
if (a < 0 || b < 0) { console.log('✗ 定位失败', a, b); process.exit(1); }
console.log('替换第', t.substring(0, a).split('\n').length, '行到第', t.substring(0, b).split('\n').length, '行');
console.log('--- 旧内容 ---');
console.log(t.substring(a, b + bMark.length));

const lines = [
  'function Glow {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  "    $mm = [regex]::Matches($tt, 'PET\\|glow\\|(\\d+)\\|([0-9.]+)')",
  "    if ($mm.Count -eq 0) { return '' }",
  '    return $mm[$mm.Count - 1].Value',
  '}',
  '# 器皿的屏幕坐标由游戏自报（STATE|vessel|）—— 器皿每次生成的位置都不同，',
  '# 猜比例坐标时灵时不灵。',
  'function VesselPos {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "STATE\\|vessel\\|[^|]+\\|(\\d+),(\\d+)")',
  '    if ($mm.Count -eq 0) { return $null }',
  '    return @([int]$mm[0].Groups[1].Value, [int]$mm[0].Groups[2].Value)',
  '}',
  'Write-Host "  点之前: $(Glow)"',
  '$vp = VesselPos',
  'Write-Host "  器皿屏幕坐标: $($vp -join \',\')"',
  'if ($vp -ne $null) { Click-At $h $vp[0] (1600 - $vp[1]) "Left" 900 }',
  'Start-Sleep -Milliseconds 2000',
  '$g1 = Glow',
  'Write-Host "  点器皿后: $g1"',
  "Check '点器皿后出现高亮（glow 数 > 0）' ($g1 -match '\\|[1-9]') \"$g1\"",
  'Shot $h "$out\\31-focused.png" | Out-Null',
  'Click-At $h 1600 1440 "Left" 900',
  'Start-Sleep -Milliseconds 2000',
  '$g2 = Glow',
  'Write-Host "  点空白后: $g2"',
  "Check '点空白后高亮消失（glow 数 = 0）' ($g2 -match '\\|0\\|') \"$g2\"",
  bMark,
];
t = t.substring(0, a) + lines.join('\n') + t.substring(b + bMark.length);
fs.writeFileSync(T, t, 'utf8');
console.log('✓ 第 4 步已重写');
