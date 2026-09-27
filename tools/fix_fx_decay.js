const fs = require('fs');
const C = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';
let lines = fs.readFileSync(C, 'utf8').split('\n');

/* ---- 1) UpdateAppearance 改成按时间衰减 ---- */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('static void UpdateAppearance(Vessel v) {')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到 UpdateAppearance'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 15; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '}') { b = i; break; }
}
console.log('UpdateAppearance:', a + 1, '..', b + 1);
for (let i = a; i <= b; i++) console.log('   ', lines[i]);

const nw = [
  '    /// <summary>每帧调用，按**时间**衰减视觉强度。</summary>',
  '    public static void UpdateAppearance(Vessel v, float dt = 1f / 60f) {',
  '        /* 这里的衰减必须按时间算，而且要每帧都跑。',
  '           原来它只在 Apply()（发生反应时）里被调用 ——',
  '           于是**不反应的容器，粒子强度永远停在最后一次的值**，',
  '           烧杯倒空之后还在冒黄色的气泡和烟雾。 */',
  '        float kb = Mathf.Pow(0.90f, dt * 60f);',
  '        float ks = Mathf.Pow(0.94f, dt * 60f);',
  '        v.Turbidity *= Mathf.Pow(0.985f, dt * 60f);',
  '        if (v.Precipitates.Count > 0)',
  '            v.Turbidity = Mathf.Max(v.Turbidity, Mathf.Min(0.85f, 0.25f + v.Precipitates.Count * 0.12f));',
  '',
  '        v.Fx.Bubble *= kb;',
  '        v.Fx.Smoke *= ks;',
  '        v.Fx.Glow *= kb;',
  '        v.Fx.Pop *= ks;',
  '        if (v.Fx.Bubble < 0.02f) v.Fx.Bubble = 0;',
  '        if (v.Fx.Smoke < 0.02f) v.Fx.Smoke = 0;',
  '        if (v.Fx.Glow < 0.02f) v.Fx.Glow = 0;',
  '        if (v.Fx.Pop < 0.02f) v.Fx.Pop = 0;',
  '',
  '        /* 容器里没有液体就不可能有气泡 —— 空烧杯冒泡是说不通的。',
  '           原来只要 Gases 里还剩一点东西，气泡就被钉在 0.3 不降。 */',
  '        bool hasLiquid = v.Volume() > 0.5;',
  '        if (!hasLiquid) { v.Fx.Bubble = 0; }',
  '        else if (v.Gases.Count > 0 && v.Fx.Bubble < 0.3f) v.Fx.Bubble = 0.3f;',
  '',
  '        /* 火焰：让它自然熄灭。原来 Flame 是个 bool，设上之后没有任何地方清掉，',
  '           除非整体 Reset —— 一次反应之后容器会一直「烧」下去。 */',
  '        if (v.Fx.Flame) {',
  '            v.Fx.FlameT -= dt;',
  '            if (v.Fx.FlameT <= 0f) { v.Fx.Flame = false; v.Fx.FlameColor = null; }',
  '        }',
  '    }',
];
lines.splice(a, b - a + 1, ...nw);
fs.writeFileSync(C, lines.join('\n'), 'utf8');
console.log('✓ UpdateAppearance 改成按时间衰减');
console.log('  括号差:', (fs.readFileSync(C,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(C,'utf8').match(/\}/g)||[]).length);
