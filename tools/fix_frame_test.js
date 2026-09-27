const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let c = fs.readFileSync(p, 'utf8');

const start = c.indexOf('/* 关键：动画不得包含空格子');
if (start < 0) { console.log('✗ 找不到旧断言'); process.exit(1); }
const endMark = 'TCheck("每个动画的所有帧都非空（这是之前闪烁的根因）", bad == 0);';
const end = c.indexOf(endMark);
if (end < 0) { console.log('✗ 找不到结束标记'); process.exit(1); }
const stop = end + endMark.length;

const nw = `/* 关键：动画里不能混进空格子，否则循环到那一帧就会闪一下。
                   光判断 sprite 非 null 不够 —— 空格子也能建出 sprite，
                   所以要真的去数不透明像素的占比。 */
                int bad = 0; float worst = 1f; string worstName = "";
                foreach (var kv in ChemLab.Game.PetSprite.Anims) {
                    int fc = ChemLab.Game.PetSprite.FrameCount(kv.Value.Row);
                    if (fc < 2) { bad++; Say($"   ✗ 动画 {kv.Key} 只有 {fc} 帧"); continue; }
                    for (int f = 0; f < fc; f++) {
                        float fill = ChemLab.Game.PetSprite.FrameFill(kv.Value.Row, f);
                        if (fill < worst) { worst = fill; worstName = kv.Key + "#" + f; }
                        if (fill < 0.06f) { bad++; Say($"   ✗ 动画 {kv.Key} 第 {f} 帧几乎是空的（填充率 {fill:P1}）"); }
                    }
                }
                Say($"   最稀疏的一帧：{worstName} 填充率 {worst:P1}");
                TCheck("每个动画的每一帧都有实际内容（这是之前闪烁的根因）", bad == 0);`;

c = c.slice(0, start) + nw + c.slice(stop);
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 自检断言已替换');
