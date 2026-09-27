const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselMesh.cs';
let lines = fs.readFileSync(V, 'utf8').split('\n');

if (fs.readFileSync(V, 'utf8').includes('// 中轴：必须正好是 0')) { console.log('- 已修'); process.exit(0); }

let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('loop.Add(new Vector2(0.004f, fillH));')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到那两行'); process.exit(1); }
console.log('第', a + 1, '..', a + 2, '行:');
console.log('   ', lines[a]);
console.log('   ', lines[a + 1]);

/* 半径写 0.004 而不是 0，于是「收回轴心」其实生成了一根有面积的细柱；
   RevolveLoop 又把近轴顶点的法线强制成轴向，这根柱子就按水平盖面打光，
   在液体正中显出一条深色竖线（试管里那根「杆」就是这么来的）。
   改成正好 0：所有旋转顶点重合，三角形退化成零面积，什么都不画。 */
lines[a]     = '        loop.Add(new Vector2(0f, fillH));        // 顶面内侧';
lines[a + 1] = '        loop.Add(new Vector2(0f, 0f));           // 中轴：必须正好是 0（见下）';

/* 在上面补一段说明 */
lines.splice(a, 0,
  '        /* 中轴：半径必须**正好是 0**。',
  '           原来写 0.004f，想着「收到轴心」，但那是一根有面积的细柱；',
  '           RevolveLoop 对 pt.x < 0.006f 的顶点会把法线强制成轴向，',
  '           于是这根柱子按「水平盖面」打光，在液体正中显出一条深色竖线 ——',
  '           试管里那根来路不明的「杆」就是它。正好 0 时顶点全部重合，',
  '           三角形退化成零面积，什么都不画。 */');

fs.writeFileSync(V, lines.join('\n'), 'utf8');
console.log('✓ 已改成 0');
console.log('  括号差:', (fs.readFileSync(V,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(V,'utf8').match(/\}/g)||[]).length);

/* 核对 */
const fl = fs.readFileSync(V, 'utf8').split('\n');
let b = -1;
for (let i = 0; i < fl.length; i++) { if (fl[i].includes('public static Mesh BuildLiquid')) { b = i; break; } }
console.log('--- 改后的 BuildLiquid 收尾 ---');
for (let i = b + 16; i < b + 32; i++) console.log('  ', fl[i]);
