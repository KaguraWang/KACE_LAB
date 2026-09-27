const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

/* 父节点名猜错了 —— 直接按 R_ 前缀收，不限定父节点 */
const old = [
  '                if (rt.name.StartsWith("R_") && rt.parent != null && rt.parent.name == "Content")',
  '                    Emit("reagent_" + rt.name.Substring(2), rt);',
].join('\n');
const nw = [
  '                if (rt.name.StartsWith("R_"))',
  '                    Emit("reagent_" + rt.name.Substring(2), rt);',
].join('\n');

if (!u.includes(old)) { console.log('✗ 锚点未命中');
  const i = u.indexOf('reagent_'); console.log(u.substring(i-200, i+120));
  process.exit(1);
}
u = u.replace(old, nw);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ 试剂列表项条件已放宽');
console.log('  括号差:', (u.match(/\{/g) || []).length - (u.match(/\}/g) || []).length);
