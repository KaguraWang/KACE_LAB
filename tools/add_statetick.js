const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');

if (b.includes('StateTick')) { console.log('- 已存在'); process.exit(0); }

const a1 = '    void DumpCalcTargets(EnthalpyScene cs) {';
if (!b.includes(a1)) { console.log('✗ 锚点1'); process.exit(1); }

const add = [
  '    /// <summary>计算器状态定期打印（外部测试脚本读日志判定，不靠猜）</summary>',
  '    System.Collections.IEnumerator StateTick(EnthalpyScene cs) {',
  '        while (true) {',
  '            yield return new WaitForSeconds(1.2f);',
  '            Debug.Log("[CalcState]\\n" + cs.StateAudit());',
  '        }',
  '    }',
  '',
  a1,
].join('\n');
b = b.replace(a1, add);

const a2 = '                    StartCoroutine(DumpCalcTargets(cs));';
if (!b.includes(a2)) { console.log('✗ 锚点2'); process.exit(1); }
b = b.replace(a2, a2 + '\n                    StartCoroutine(StateTick(cs));');

fs.writeFileSync(B, b, 'utf8');
console.log('✓ StateTick 已加');
console.log('  括号差:', (b.match(/\{/g) || []).length - (b.match(/\}/g) || []).length);
