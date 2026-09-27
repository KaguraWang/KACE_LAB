const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(B, 'utf8');

if (c.includes('CLICK|tut_')) { console.log('- 已存在'); process.exit(0); }

/* 在 PetAudit 里追加教程栏按钮与阿侬自身的屏幕位置（可机读 CLICK| 行） */
const anchor = '        sb.AppendLine("PET|aiUnlocked|" + (PetProgress.AiUnlocked ? "yes" : "no"));';
if (!c.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = `${anchor}
        /* 可机读的点击目标：教程栏按钮、阿侬自己 */
        var tb = FindObjectOfType<TutorialBar>();
        if (tb != null) {
            foreach (var rt in tb.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name != "B") continue;
                var t = rt.GetComponentInChildren<Text>();
                if (t == null) continue;
                var sc2 = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
                sb.AppendLine("PET|CLICK|tut_" + t.text + "|" + Mathf.RoundToInt(sc2.x) + "|" + Mathf.RoundToInt(sc2.y)
                            + "|" + Mathf.RoundToInt(rt.rect.width) + "|" + Mathf.RoundToInt(rt.rect.height));
            }
        }
        if (pv != null) {
            var prt = pv.GetComponent<RectTransform>();
            if (prt != null) {
                /* 阿侬的贴图在子节点里，取最靠上的那个 Image 的位置 */
                foreach (var ir in pv.GetComponentsInChildren<Image>(true)) {
                    if (ir == null || ir.sprite == null) continue;
                    var irc = RectTransformUtility.WorldToScreenPoint(null, ir.rectTransform.TransformPoint(ir.rectTransform.rect.center));
                    sb.AppendLine("PET|CLICK|pet|" + Mathf.RoundToInt(irc.x) + "|" + Mathf.RoundToInt(irc.y)
                                + "|" + Mathf.RoundToInt(ir.rectTransform.rect.width) + "|" + Mathf.RoundToInt(ir.rectTransform.rect.height));
                    break;
                }
            }
        }`;

c = c.replace(anchor, add);
fs.writeFileSync(B, c, 'utf8');
console.log('✓ 教程栏与阿侬的位置输出已加');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
