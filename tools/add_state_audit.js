const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');

if (c.includes('public string StateAudit')) { console.log('- 已存在'); process.exit(0); }

/* 状态审计：给外部测试脚本读的客观证据 */
const anchor = '    public string AuditLayout() {';
if (!c.includes(anchor)) { console.log('✗ 找不到 AuditLayout'); process.exit(1); }

const add = `    /// <summary>计算器的当前状态，给外部测试脚本当客观证据。
    /// 输出格式：STATE|键|值</summary>
    public string StateAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("STATE|nodes|" + _nodes.Count);
        sb.AppendLine("STATE|edges|" + _edges.Count);
        sb.AppendLine("STATE|pending|" + (PendingEdge ? "yes" : "no"));
        sb.AppendLine("STATE|menu|" + (_menuLayer != null && _menuLayer.childCount > 0 ? "open" : "closed"));
        sb.AppendLine("STATE|solved|" + (LastSolvedOk ? "yes" : "no"));
        foreach (var e in _edges) {
            string a = e.A != null ? e.A.Formula : "?";
            string b = e.B != null ? e.B.Formula : "?";
            sb.AppendLine("STATE|edge|" + a + "->" + b + "|" + e.DH.ToString("F0") + "|" + e.Source);
        }
        return sb.ToString();
    }

    public string AuditLayout() {`;

c = c.replace(anchor, add);
fs.writeFileSync(E, c, 'utf8');
console.log('✓ StateAudit 已加');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
