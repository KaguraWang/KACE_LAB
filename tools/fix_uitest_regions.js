const fs = require('fs');
let n = 0;

/* 1) Object 歧义 → 全限定 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');
if (s.includes('Object.FindObjectOfType<ChemLab.Game.VesselView>()')) {
  s = s.replace('Object.FindObjectOfType<ChemLab.Game.VesselView>()', 'UnityEngine.Object.FindObjectOfType<ChemLab.Game.VesselView>()');
  fs.writeFileSync(S, s, 'utf8');
  n++;
  console.log('  ✓ Object 已全限定');
}

/* 2) uitest：搜索框与分类列表的取样区域直接用游戏报出的坐标算，
      不再靠比例猜（前面三次都猜偏了）。 */
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

/* 把搜索框判据改成「用 dump 里的 search 坐标」 */
const oldSr = `# 搜索框实测在屏幕 x 0.78..0.96、屏幕 y（从下往上）0.055..0.10，
# 换算到客户区（从上往下）就是 y 0.90..0.945
$sr = @([int]($CW*0.78), [int]($CH*0.895), [int]($CW*0.18), [int]($CH*0.045))`;
const newSr = `# 取样区域直接用游戏报出的控件坐标算 —— 前面靠比例猜了三次都偏。
# dump 里的 search 是屏幕坐标（Y 从下往上），换到客户区要翻 Y。
$sw = 0; $sh = 0
if ($T.ContainsKey('search')) {
    $sw = [int]($T['search'][0] * $CW / $gw)
    $sh = [int](($gh - $T['search'][1]) * $CH / $gh)
}
$sr = @([int]($sw - $CW*0.055), [int]($sh - $CH*0.016), [int]($CW*0.11), [int]($CH*0.032))`;
if (t.includes(oldSr)) { t = t.Replace(oldSr, newSr); fs.writeFileSync(T, t, 'utf8'); n++; console.log('  ✓ 搜索框取样改为按 dump 坐标'); }
else console.log('  ✗ 搜索框取样锚点未命中');

/* 3) 分类列表取样：用 dump 里 cat_ 的坐标推算列表区域 */
t = fs.readFileSync(T, 'utf8');
const oldList = `$listR = @([int]($CW*0.80), [int]($CH*0.16), [int]($CW*0.19), [int]($CH*0.55))`;
const newList = `# 列表区域从分类按钮的位置往下推 —— 同样不猜比例
$lx = 0; $ly = 0
if ($T.ContainsKey('cat_氧化物')) {
    $lx = [int]($T['cat_氧化物'][0] * $CW / $gw)
    $ly = [int](($gh - $T['cat_氧化物'][1]) * $CH / $gh)
}
$listR = @([int]($lx - $CW*0.06), [int]($ly + $CH*0.03), [int]($CW*0.12), [int]($CH*0.42))`;
if (t.includes(oldList)) { t = t.Replace(oldList, newList); fs.writeFileSync(T, t, 'utf8'); n++; console.log('  ✓ 列表取样改为按 dump 坐标'); }
else console.log('  ✗ 列表取样锚点未命中');

console.log('改动处数:', n);
