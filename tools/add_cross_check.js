const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('连线压过节点')) { console.log('已存在'); process.exit(0); }

const anchor = `        sb.AppendLine($"[CalcAudit] 画面外的控件数 = {bad}");
        return sb.ToString();`;
const add = `        sb.AppendLine($"[CalcAudit] 画面外的控件数 = {bad}");

        /* ---- 几何检查：连线不得压过任何节点（这是硬要求） ---- */
        int cross = 0;
        foreach (var e in _edges) {
            var p0 = e.A.Pos; var p1 = e.B.Pos;
            Vector2 dd = p1 - p0;
            float len = Mathf.Max(1f, dd.magnitude);
            Vector2 dir = dd / len, nrm = new Vector2(-dir.y, dir.x);
            float off = Mathf.Clamp(len * 0.22f, 40f, 150f);
            Vector2 ctrl = (p0 + p1) * 0.5f + nrm * off * 2f;   // 与 RedrawEdges 同样的路由
            foreach (var n in _nodes) {
                if (n == e.A || n == e.B) continue;
                /* 沿曲线采样，看有没有落进节点矩形（含 6px 余量）里 */
                for (int k = 1; k <= 20; k++) {
                    Vector2 pt = Quadratic(p0, ctrl, p1, k / 21f);
                    if (Mathf.Abs(pt.x - n.Pos.x) < 105 + 6 && Mathf.Abs(pt.y - n.Pos.y) < 37 + 6) {
                        cross++;
                        sb.AppendLine($"[CalcAudit] 连线「{EdgeEquation(e)}」压过节点「{n.Formula}」");
                        break;
                    }
                }
            }
        }
        sb.AppendLine($"[CalcAudit] 连线压过节点的次数 = {cross}");
        return sb.ToString();`;
if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, add);
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 连线-节点交叉检查已加');
