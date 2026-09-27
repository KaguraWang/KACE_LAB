const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');

if (l.includes('FX|')) { console.log('- 已存在'); process.exit(0); }

const anchor = '        /* 器皿的屏幕位置 —— 测试要点器皿时直接用，不要猜比例坐标';
if (!l.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

/* 粒子/火焰强度：验证「容器空了特效要停」 */
l = l.replace(anchor, `        /* 视觉特效强度：验证「容器空了特效必须停」 */
        foreach (var vv3 in _views) {
            if (vv3 == null || vv3.Model == null) continue;
            var fx = vv3.Model.Fx;
            if (fx.Bubble > 0.01f || fx.Smoke > 0.01f || fx.Glow > 0.01f || fx.Flame)
                sb.AppendLine("FX|" + vv3.Model.Id + "|泡" + fx.Bubble.ToString("F2")
                            + " 烟" + fx.Smoke.ToString("F2") + " 光" + fx.Glow.ToString("F2")
                            + " 焰" + (fx.Flame ? "开" : "关")
                            + " 液" + vv3.Model.Volume().ToString("F0"));
        }
` + anchor);
fs.writeFileSync(L, l, 'utf8');
console.log('✓ FX 诊断已加');
console.log('  括号差:', (l.match(/\{/g) || []).length - (l.match(/\}/g) || []).length);
