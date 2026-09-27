const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

const oldSr = [
  '# 搜索框实测在屏幕 x 0.78..0.96、屏幕 y（从下往上）0.055..0.10，',
  '# 换算到客户区（从上往下）就是 y 0.90..0.945',
  '$sr = @([int]($CW*0.78), [int]($CH*0.895), [int]($CW*0.18), [int]($CH*0.045))',
].join('\n');

const newSr = [
  '# 取样区域直接用游戏报出的控件坐标算 —— 靠比例猜了三次都偏。',
  '# dump 里的 search 是屏幕坐标（Y 从下往上），换到客户区要翻 Y。',
  '$sw = 0; $sh = 0',
  "if ($T.ContainsKey('search')) {",
  "    $sw = [int]($T['search'][0] * $CW / $gw)",
  "    $sh = [int](($gh - $T['search'][1]) * $CH / $gh)",
  '}',
  '$sr = @([int]($sw - $CW*0.055), [int]($sh - $CH*0.016), [int]($CW*0.11), [int]($CH*0.032))',
].join('\n');

if (t.includes(oldSr)) {
  t = t.replace(oldSr, newSr);
  fs.writeFileSync(T, t, 'utf8');
  console.log('✓ 搜索框取样区域已改为按 dump 坐标');
} else {
  console.log('✗ 锚点未命中，实际内容：');
  const i = t.indexOf('$sr = @(');
  console.log(t.substring(i - 200, i + 120));
}
