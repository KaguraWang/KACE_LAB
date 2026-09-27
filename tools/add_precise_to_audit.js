const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

if (u.includes('pr_')) { console.log('- 已存在'); process.exit(0); }

const anchor = '                if (rt.name == "Search") Emit("search", rt);';
if (!u.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

/* 把精确加量那一行的控件也报出来，方便核对布局 */
u = u.replace(anchor, anchor + `
                if (rt.name.StartsWith("Pr") && rt.name.Length == 3)
                    Emit("pr_" + rt.name.Substring(2) + "_" + (rt.GetComponentInChildren<Text>() != null
                         ? rt.GetComponentInChildren<Text>().text : "?"), rt);`);

fs.writeFileSync(U, u, 'utf8');
console.log('✓ 已加进审计');
console.log('  括号差:', (u.match(/\{/g) || []).length - (u.match(/\}/g) || []).length);

/* 顺便让自检的重叠日志把坐标也打出来 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');
const a2 = '                                if (hit <= 6) Say($"   ✗ 「{all[a].name}」与「{all[b].name}」重叠");';
if (s.includes(a2)) {
  s = s.replace(a2, '                                if (hit <= 6) Say($"   ✗ 「{all[a].name}」{all[a].r} 与 「{all[b].name}」{all[b].r} 重叠");');
  fs.writeFileSync(S, s, 'utf8');
  console.log('  ✓ 自检重叠日志已带坐标');
}
