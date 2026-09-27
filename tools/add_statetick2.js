const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

if (fs.readFileSync(B, 'utf8').includes('StateTick')) { console.log('- 已存在'); process.exit(0); }

/* 在 -calcDemo 块里接上 StateTick */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (cs != null) { cs.transform.SetAsLastSibling(); Debug.Log(cs.AuditLayout()); }')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 calcDemo 的 cs 行'); process.exit(1); }
console.log('锚点在第', at + 1, '行:', lines[at]);
lines.splice(at + 1, 0, '                if (cs != null) StartCoroutine(StateTick(cs));   // 每秒打印一次状态，供外部脚本读');

/* 加 StateTick 方法 */
let ins = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('void DumpInputAudit() {')) { ins = i; break; }
}
if (ins < 0) { console.log('✗ 找不到 DumpInputAudit'); process.exit(1); }
const blk = [
  '    /// <summary>计算器状态定期打印（外部测试脚本读日志判定，不靠猜）</summary>',
  '    System.Collections.IEnumerator StateTick(EnthalpyScene cs) {',
  '        while (true) {',
  '            yield return new WaitForSeconds(1.2f);',
  '            if (cs != null) Debug.Log("[CalcState]\\n" + cs.StateAudit());',
  '        }',
  '    }',
  '',
];
lines.splice(ins, 0, ...blk);
fs.writeFileSync(B, lines.join('\n'), 'utf8');
const c = fs.readFileSync(B, 'utf8');
console.log('✓ StateTick 已加');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
