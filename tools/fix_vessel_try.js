const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

const oldPick = [
  'function VesselPos {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "STATE\\|vessel\\|[^|]+\\|(\\d+),(\\d+)")',
  '    if ($mm.Count -eq 0) { return $null }',
  '    # 取最新一组的第一个',
  '    return @([int]$mm[$mm.Count - 1].Groups[1].Value, [int]$mm[$mm.Count - 1].Groups[2].Value)',
  '}',
].join('\n');
const newPick = [
  '# 所有器皿的屏幕坐标。注意坐标列表里可能混着已经在左面板下面、点不中的，',
  '# 所以下面要逐个试。',
  'function VesselPositions {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "STATE\\|vessel\\|[^|]+\\|(\\d+),(\\d+)")',
  '    $o = @()',
  '    for ($i = 0; $i -lt $mm.Count; $i++) {',
  '        $o += ,@([int]$mm[$i].Groups[1].Value, [int]$mm[$i].Groups[2].Value)',
  '    }',
  '    return $o',
  '}',
].join('\n');
if (!t.includes(oldPick)) { console.log('✗ 找不到 VesselPos'); process.exit(1); }
t = t.replace(oldPick, newPick);

const oldClick = [
  '$vp = VesselPos',
  'Write-Host "  器皿屏幕坐标: $($vp -join \',\')"',
  'if ($vp -ne $null) { Click-At $h $vp[0] (1600 - $vp[1]) "Left" 900 } else { Write-Host "  没拿到器皿坐标" }',
  'Start-Sleep -Milliseconds 2500',
  '$g1 = GlowCount',
  'Write-Host "  点器皿后 glow 数 = $g1"',
].join('\n');
const newClick = [
  '$vps = VesselPositions',
  'Write-Host "  拿到 $($vps.Count) 个器皿坐标"',
  '$g1 = 0',
  '# 逐个试：坐标列表里可能混着已经在左面板下面、点不中的',
  'foreach ($vp in ($vps | Select-Object -First 4)) {',
  '    Write-Host "  点 ($($vp[0]), $(1600 - $vp[1]))"',
  '    Click-At $h $vp[0] (1600 - $vp[1]) "Left" 1200',
  '    Start-Sleep -Milliseconds 1800',
  '    $g1 = GlowCount',
  '    Write-Host "      glow = $g1"',
  '    if ($g1 -ge 1) { break }',
  '}',
].join('\n');
if (!t.includes(oldClick)) { console.log('✗ 找不到点击段'); process.exit(1); }
t = t.replace(oldClick, newClick);

fs.writeFileSync(T, t, 'utf8');
console.log('✓ 已改成逐个试');

/* 顺便修掉那个预热位置错误：删掉开头那个 Warm-Up，在 Start-App 之后重加 */
let lines = t.split('\n');
let w = -1, aa = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Warm-Up $h')) w = i;
  if (/^\$h = Start-App/.test(lines[i])) aa = i;
}
if (w >= 0 && aa >= 0 && w < aa) {
  const txt = lines[w];
  lines.splice(w, 1);
  let a2 = -1;
  for (let i = 0; i < lines.length; i++) { if (/^\$h = Start-App/.test(lines[i])) { a2 = i; break; } }
  lines.splice(a2 + 1, 0, txt);
  fs.writeFileSync(T, lines.join('\n'), 'utf8');
  console.log('✓ Warm-Up 已挪到 Start-App 之后');
}
