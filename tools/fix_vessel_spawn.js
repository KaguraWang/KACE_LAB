const fs = require('fs');
const C = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';
let lines = fs.readFileSync(C, 'utf8').split('\n');

const old = '        var pos = new Vector3(-0.95f + (n % 5) * 0.92f, 0f, (n % 2 == 0) ? 0.20f : -0.16f);';
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes(old.trim())) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到摆放那行'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

/* 原来沿一条线往 x 正方向排 —— 相机是侧看的，排到第 5 个就跑到屏幕左侧、
   被左边的界面面板整个挡住了（实测 v5 落在屏幕 x=504，面板占到 500）。
   改成在台面中央的可见区里循环摆放：
     x 在 -0.9 / -0.05 / 0.8 三档循环（实测这一带屏幕 x≈1000~2100，都露着）
     z 从前往后分排，避免叠在一起 */
lines[at] = [
  '        /* 在台面中央的**可见区**里循环摆放。',
  '           原来沿 x = -0.95 + (n%5)*0.92 一直往一边排，',
  '           相机是侧看的，排到第 5 个就跑到屏幕左侧、被左边的界面面板整个挡住',
  '           （实测第 5 个落在屏幕 x=504，而面板占到 500）。 */',
  '        int k = n;',
  '        float fx = -0.90f + (k % 3) * 0.85f;      // -0.90 / -0.05 / 0.80',
  '        float fz = 0.55f - (k / 3) * 0.40f;       // 前排 → 后排，避免叠在一起',
  '        var pos = new Vector3(fx, 0f, Mathf.Clamp(fz, -2.0f, 1.4f));',
].join('\n');

fs.writeFileSync(C, lines.join('\n'), 'utf8');
console.log('✓ 摆放逻辑已改');
console.log('  括号差:', (fs.readFileSync(C,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(C,'utf8').match(/\}/g)||[]).length);
