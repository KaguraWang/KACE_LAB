const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

const src = fs.readFileSync(B, 'utf8');
if (src.includes('cs.AuditLayout()')) { console.log('- 已存在'); process.exit(0); }

/* 在 StateTick 的 StateAudit 之后加一行 AuditLayout */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('cs.StateAudit()')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 StateAudit 调用'); process.exit(1); }
console.log('StateTick 里的 StateAudit 在第', at + 1, '行:');
for (let i = at - 2; i <= at + 3; i++) console.log('   ', lines[i]);

lines.splice(at + 1, 0,
  '                /* 选单是临时出现的，控件清单必须每秒重打一次，',
  '                   否则外部脚本拿不到菜单项的位置 */',
  '                Debug.Log(cs.AuditLayout());');
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ AuditLayout 已接进 StateTick');
const c = fs.readFileSync(B, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
