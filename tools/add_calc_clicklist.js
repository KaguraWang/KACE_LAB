const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');

if (c.includes('[CalcAudit] CLICK|')) { console.log('- 已存在'); process.exit(0); }

/* 在 AuditLayout 末尾追加一份可机读的控件清单 */
const marker = 'sb.AppendLine("[CalcAudit] 画面外的控件数 = 0");';
let anchor = null;
for (const cand of [
  'sb.AppendLine("[CalcAudit] 画面外的控件数 = " + off);',
  'sb.AppendLine("[CalcAudit] 画面外的控件数 = 0");',
]) { if (c.includes(cand)) { anchor = cand; break; } }

if (!anchor) {
  /* 找 AuditLayout 里任何一行 */
  const i = c.indexOf('画面外的控件数');
  if (i > 0) {
    const ls = c.lastIndexOf('\n', i) + 1;
    const le = c.indexOf('\n', i);
    anchor = c.substring(ls, le);
    console.log('  用实际行做锚点:', anchor);
  }
}

if (!anchor) { console.log('✗ 找不到锚点'); process.exit(1); }

const add = `${anchor}
        /* 可机读的控件清单：外部自动化脚本按名字点击，不用猜坐标 */
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null) {
                foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true)) {
                    var txt = rt.GetComponentInChildren<Text>();
                    string nm = null;
                    if (rt.name == "B" && txt != null) nm = "btn_" + txt.text;
                    else if (rt.name.StartsWith("Node")) nm = "node_" + rt.name;
                    else if (txt != null && txt.text != null && (txt.text.Contains("ΔH") || txt.text.Contains("→")))
                        nm = "label_" + rt.name;
                    if (nm == null) continue;
                    var cc = rt.TransformPoint(rt.rect.center);
                    sb.AppendLine("[CalcAudit] CLICK|" + nm + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)
                                + "|" + Mathf.RoundToInt(rt.rect.width) + "|" + Mathf.RoundToInt(rt.rect.height));
                }
            }
        }`;

c = c.replace(anchor, add);
fs.writeFileSync(E, c, 'utf8');
console.log('✓ 可机读控件清单已加');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
