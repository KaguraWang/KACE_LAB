const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');

/* 把节点那条改成从 _nodes 直接报化学式 ——
   节点下面有系数框和化学式框两个 Text，GetComponentInChildren 拿到的是系数（"1"）。 */
const oldLine = '                    else if (rt.name.StartsWith("Node")) { var nt = rt.GetComponentInChildren<Text>(); nm = "node_" + (nt != null && nt.text.Length > 0 ? nt.text : rt.name); }';
const newLine = '                    /* 节点不在这里报名字 —— 它下面有系数框和化学式框两个 Text，\n                       拿到的会是系数。节点的坐标在下面单独从 _nodes 遍历输出。 */\n                    else if (rt.name.StartsWith("Node")) continue;';
if (c.includes(oldLine)) { c = c.replace(oldLine, newLine); console.log('  ✓ 节点行已移除'); }
else console.log('  ✗ 节点行未命中');

/* 在控件清单之后追加节点清单 */
const marker = '        sb.AppendLine($"[CalcAudit] 画面外的控件数 = {bad}");';
if (c.includes(marker)) {
  c = c.replace(marker, `        /* 节点清单：名字直接用化学式，外部脚本按名字点，不用猜编号 */
        for (int ni = 0; ni < _nodes.Count; ni++) {
            var nd = _nodes[ni];
            if (nd == null || nd.Rt == null) continue;
            var cc = nd.Rt.TransformPoint(nd.Rt.rect.center);
            string f = nd.Formula.Length > 0 ? nd.Formula : nd.Raw;
            sb.AppendLine("[CalcAudit] CLICK|node_" + f + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)
                        + "|" + Mathf.RoundToInt(nd.Rt.rect.width) + "|" + Mathf.RoundToInt(nd.Rt.rect.height));
        }
` + marker);
  console.log('  ✓ 节点清单已加');
} else console.log('  ✗ 锚点未命中');

fs.writeFileSync(E, c, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
