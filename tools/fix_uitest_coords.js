const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let lines = fs.readFileSync(T, 'utf8').split('\n');

/* 1) 分类按钮：用审计里的 cat_氧化物 */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Click-At $h 2223 260 "Left" 1500')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到分类点击行'); process.exit(1); }
console.log('分类点击在第', a + 1, '行:', lines[a].trim());
lines[a] = [
  '# 用游戏自报的坐标，别写死 —— 试剂架布局一改，写死的坐标就点不准了。',
  'function Ctrl([string]$n) {',
  '    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    $mm = [regex]::Matches($tt, "CLICK\\|" + [regex]::Escape($n) + "\\|(\\d+)\\|(\\d+)\\|")',
  '    if ($mm.Count -eq 0) { return $null }',
  '    return @([int]$mm[$mm.Count-1].Groups[1].Value, [int]$mm[$mm.Count-1].Groups[2].Value)',
  '}',
  '$cp = Ctrl "cat_氧化物"',
  'Write-Host "  「氧化物」按钮屏幕坐标: $($cp -join \',\')"',
  'if ($cp -ne $null) { Click-At $h $cp[0] (1600 - $cp[1]) "Left" 1500 } else { Write-Host "  没拿到坐标" }',
].join('\n');

/* 2) 搜索框也用审计坐标 */
let b = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Click-At $h 2266 131')) { b = i; break; }
}
if (b >= 0) {
  console.log('搜索点击在第', b + 1, '行:', lines[b].trim());
  lines[b] = '$sp = Ctrl "search"\nif ($sp -ne $null) { Click-At $h $sp[0] (1600 - $sp[1]) "Left" 1200 } else { Write-Host "  没拿到搜索框坐标" }';
} else console.log('- 搜索点击行没找到（可能已改）');

fs.writeFileSync(T, lines.join('\n'), 'utf8');
console.log('✓ 已改用审计坐标');
