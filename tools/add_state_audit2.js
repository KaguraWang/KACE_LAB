const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (lines.some(l => l.includes('public string StateAudit'))) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public string AuditLayout() {')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 AuditLayout'); process.exit(1); }
console.log('AuditLayout 在第', at + 1, '行');

const blk = [
  '    /// <summary>计算器的当前状态，给外部测试脚本当客观证据。',
  '    /// 输出格式：STATE|键|值</summary>',
  '    public string StateAudit() {',
  '        var sb = new System.Text.StringBuilder();',
  '        sb.AppendLine("STATE|nodes|" + _nodes.Count);',
  '        sb.AppendLine("STATE|edges|" + _edges.Count);',
  '        sb.AppendLine("STATE|pending|" + (PendingEdge ? "yes" : "no"));',
  '        sb.AppendLine("STATE|menu|" + (_menuLayer != null && _menuLayer.childCount > 0 ? "open" : "closed"));',
  '        sb.AppendLine("STATE|solved|" + (LastSolvedOk ? "yes" : "no"));',
  '        foreach (var e in _edges) {',
  '            string a = e.A != null ? e.A.Formula : "?";',
  '            string b = e.B != null ? e.B.Formula : "?";',
  '            sb.AppendLine("STATE|edge|" + a + "->" + b + "|" + e.DH.ToString("F0") + "|" + e.Source);',
  '        }',
  '        return sb.ToString();',
  '    }',
  '',
];
lines.splice(at, 0, ...blk);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
const c = fs.readFileSync(E, 'utf8');
console.log('✓ StateAudit 已插在 AuditLayout 之前');
console.log('  存在:', c.includes('public string StateAudit'));
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
