const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

const a = t.indexOf("Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='");
const bMark = 'Shot $h "$out\\33-after-unfocus.png" | Out-Null';
const b = t.indexOf(bMark);
if (a < 0 || b < 0) { console.log('✗ 定位失败', a, b); process.exit(1); }
console.log('替换第', t.substring(0, a).split('\n').length, '行起');

const lines = [
  "Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='",
  '# 用状态判定：读 PET|glow（聚焦高亮的强度）和 STATE|vessel（器皿的屏幕坐标）。',
  '# 不猜比例坐标 —— 器皿每次生成的位置都不同，猜出来的坐标时灵时不灵。',
  '# 也不用像素判据 —— 菲涅尔辉光本来就淡，实测在 1.4~4.5 之间跳。',
  'function GlowCount {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  "    $mm = [regex]::Matches($tt, 'PET\\|glow\\|(\\d+)\\|')",
  '    if ($mm.Count -eq 0) { return -1 }',
  '    return [int]$mm[$mm.Count - 1].Groups[1].Value',
  '}',
  'function VesselPos {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "STATE\\|vessel\\|[^|]+\\|(\\d+),(\\d+)")',
  '    if ($mm.Count -eq 0) { return $null }',
  '    # 取最新一组的第一个',
  '    return @([int]$mm[$mm.Count - 1].Groups[1].Value, [int]$mm[$mm.Count - 1].Groups[2].Value)',
  '}',
  'Write-Host "  点之前 glow 数 = $(GlowCount)"',
  '$vp = VesselPos',
  'Write-Host "  器皿屏幕坐标: $($vp -join \',\')"',
  'if ($vp -ne $null) { Click-At $h $vp[0] (1600 - $vp[1]) "Left" 900 } else { Write-Host "  没拿到器皿坐标" }',
  'Start-Sleep -Milliseconds 2500',
  '$g1 = GlowCount',
  'Write-Host "  点器皿后 glow 数 = $g1"',
  "Check '点器皿后出现高亮' (\$g1 -ge 1) \"glow 数 = \$g1\"",
  'Shot $h "$out\\31-focused.png" | Out-Null',
  'Click-At $h 1600 1440 "Left" 900',
  'Start-Sleep -Milliseconds 2500',
  '$g2 = GlowCount',
  'Write-Host "  点空白后 glow 数 = $g2"',
  "Check '点空白后高亮消失' (\$g2 -eq 0) \"glow 数 = \$g2\"",
  bMark,
];
t = t.substring(0, a) + lines.join('\n') + t.substring(b + bMark.length);
fs.writeFileSync(T, t, 'utf8');
console.log('✓ 第 4 步已改回状态判定');
