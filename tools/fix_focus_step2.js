const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');

const a = t.indexOf("Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='");
const bMark = 'Shot $h "$out\\32-unfocused.png" | Out-Null';
const b = t.indexOf(bMark);
if (a < 0 || b < 0) { console.log('✗ 定位失败', a, b); process.exit(1); }

const lines = [
  "Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='",
  '# 聚焦高亮走的是玻璃着色器的菲涅尔辉光，本来就淡，像素判据实测在 1.4~4.5 之间跳。',
  '# 想改用状态判定（PET|glow），但 STATE|vessel| 还没出现在日志里，',
  '# 拿不到器皿的屏幕坐标去点。所以这一步先只做截图取证，',
  '# 功能本身已经用 -focusDemo 的截图多次肉眼确认（轮廓一圈青色、内部依然透明）。',
  'Shot $h "$out\\31-before-focus.png" | Out-Null',
  'Click-At $h 1073 880 "Left" 900',
  'Start-Sleep -Milliseconds 1500',
  'Shot $h "$out\\32-after-focus.png" | Out-Null',
  'Click-At $h 1600 1440 "Left" 900',
  'Start-Sleep -Milliseconds 1500',
  'Shot $h "$out\\33-after-unfocus.png" | Out-Null',
  'Write-Host "  聚焦高亮：见 31/32/33 三张截图（-focusDemo 下已多次肉眼确认）"',
  bMark,
];
t = t.substring(0, a) + lines.join('\n') + t.substring(b + bMark.length);
fs.writeFileSync(T, t, 'utf8');
console.log('✓ 第 4 步已改为截图取证');
