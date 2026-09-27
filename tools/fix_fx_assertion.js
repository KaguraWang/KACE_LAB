const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let lines = fs.readFileSync(S, 'utf8').split('\n');

/* 删掉刚加错的那段 */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('没有液体时不该有气泡（倒空后特效必须停）')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到刚加的断言'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 20; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '});') { b = i; break; }
}
console.log('删除', a + 1, '..', b + 1);
lines.splice(a - 1, b - a + 3);   /* 连前面那行 Check( 和后面的空行一起 */

/* 用 TCheck 重写，放在一个普通块里 */
let t = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('TCheck("检验器具里有木条')) { t = i; break; }
}
if (t < 0) { console.log('✗ 找不到 TCheck 锚点'); process.exit(1); }
let te = -1;
for (let i = t; i < t + 10; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '}') { te = i; break; }
}
console.log('TCheck 块在', t + 1, '..', te + 1);

const blk = [
  '',
  '                    /* 容器倒空之后，气泡和火焰必须停 ——',
  '                       原来 UpdateAppearance 只在发生反应时调用，',
  '                       而且「有气体就把气泡钉在 0.3」，',
  '                       烧杯烧干了还一直冒黄色的气泡。 */',
  '                    {',
  '                        var tv = Lab.AddVessel(VesselKind.Beaker);',
  '                        tv.AddWater(30);',
  '                        tv.Fx.Bubble = 3f; tv.Fx.Smoke = 2f;',
  '                        tv.Fx.Flame = true; tv.Fx.FlameT = 0.05f;',
  '                        for (int i = 0; i < 180; i++) Lab.UpdateAppearance(tv, 1f / 60f);',
  '                        float bWithLiquid = tv.Fx.Bubble;',
  '                        /* 倒空 */',
  '                        tv.Water = 0; tv.Solutes.Clear(); tv.Undissolved.Clear();',
  '                        tv.Precipitates.Clear(); tv.Gases.Clear();',
  '                        for (int i = 0; i < 240; i++) Lab.UpdateAppearance(tv, 1f / 60f);',
  '                        Say($"   有液体时气泡 {bWithLiquid:F2}，倒空后 {tv.Fx.Bubble:F2}，火焰 {(tv.Fx.Flame ? "还开着" : "已熄灭")}");',
  '                        TCheck("倒空后气泡归零", tv.Fx.Bubble <= 0.001f);',
  '                        TCheck("倒空后火焰熄灭", !tv.Fx.Flame);',
  '                        Lab.RemoveVessel(tv);',
  '                    }',
];
lines.splice(te + 1, 0, ...blk);
fs.writeFileSync(S, lines.join('\n'), 'utf8');
console.log('✓ 已改用 TCheck');
console.log('  括号差:', (fs.readFileSync(S,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(S,'utf8').match(/\}/g)||[]).length);
