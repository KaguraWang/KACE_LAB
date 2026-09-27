const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');

if (s.includes('没有液体时不该有气泡')) { console.log('- 已存在'); process.exit(0); }

const anchor = 'Check("氯化铝 + 氢氧化钠过量（沉淀溶解）", v => {';
if (!s.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = `Check("没有液体时不该有气泡（倒空后特效必须停）", v => {
                /* 原来 UpdateAppearance 只在发生反应时被调用，而且
                   「有气体就把气泡钉在 0.3」—— 容器倒空之后还一直冒泡。
                   现在它每帧按时间衰减，并且没有液体时气泡强制归零。 */
                v.Fx.Bubble = 3f; v.Fx.Smoke = 2f; v.Fx.Flame = true; v.Fx.FlameT = 0.01f;
                Lab.AddReagent(v, "zn", 2.0);
                Lab.AddReagent(v, "hcl", 5.0);          // 冒氢气
                double b0 = v.Fx.Bubble;
                v.Gases.Clear(); v.Solutes.Clear(); v.Undissolved.Clear();
                v.Water = 0;                            // 倒空
                for (int i = 0; i < 240; i++) Lab.UpdateAppearance(v, 1f / 60f);
                Say($"     倒空前气泡 {b0:F2} → 倒空后 {v.Fx.Bubble:F2}，火焰 {(v.Fx.Flame ? "还开着（错误）" : "已熄灭")}");
                if (v.Fx.Bubble > 0.001) return false;
                if (v.Fx.Flame) return false;
                return true;
            });

            ` + anchor;

s = s.replace(anchor, add);
fs.writeFileSync(S, s, 'utf8');
console.log('✓ 自检断言已加');
console.log('  括号差:', (s.match(/\{/g) || []).length - (s.match(/\}/g) || []).length);
