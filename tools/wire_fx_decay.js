const fs = require('fs');
const C = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';
let lines = fs.readFileSync(C, 'utf8').split('\n');
let n = 0;

/* ---- 1) FlameT 字段 ---- */
let fi = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public readonly Dictionary<string,double> Gases')) break;
}
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public bool Mirror, Flame;')) { fi = i; break; }
}
if (fi < 0) { console.log('✗ 找不到 Flame 字段'); process.exit(1); }
if (!lines.slice(fi, fi + 4).some(l => l.includes('FlameT'))) {
  lines.splice(fi + 1, 0,
    '    /* 火焰剩余时长（秒）。Flame 原来是个 bool，设上之后没有任何地方清掉，',
    '       一次反应之后容器会一直「烧」下去。 */',
    '    public float FlameT;');
  n++;
  console.log('  ✓ FlameT 字段已加');
}

/* ---- 2) 每次置 Flame = true 时同时给 FlameT ---- */
const sets = [
  ['if (subId == "k") { v.Fx.Explode = 1; v.Fx.Flame = true; v.Fx.FlameColor = "#c060ff"; }',
   'if (subId == "k") { v.Fx.Explode = 1; v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = "#c060ff"; }'],
  ['v.Fx.Flame = true; v.Fx.FlameColor = "#ffbb44";',
   'v.Fx.Flame = true; v.Fx.FlameT = 8f; v.Fx.FlameColor = "#ffbb44";'],
  ['v.Fx.Flame = true; v.Fx.FlameColor = ChemData.FlameColor[ion];',
   'v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = ChemData.FlameColor[ion];'],
  ['if (!string.IsNullOrEmpty(r.Flame)) { v.Fx.Flame = true; v.Fx.FlameColor = r.Flame; }',
   'if (!string.IsNullOrEmpty(r.Flame)) { v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = r.Flame; }'],
];
for (const [oldS, newS] of sets) {
  let hit = -1;
  for (let i = 0; i < lines.length; i++) { if (lines[i].includes(oldS)) { hit = i; break; } }
  if (hit >= 0) { lines[hit] = lines[hit].replace(oldS, newS); n++; console.log('  ✓ 第', hit + 1, '行已给 FlameT'); }
  else console.log('  ✗ 未命中:', oldS.substring(0, 50));
}

/* ---- 3) 在 Tick(dt) 里每帧调用 UpdateAppearance ---- */
let ti = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public static void Tick(float dt) {')) { ti = i; break; }
}
if (ti < 0) { console.log('✗ 找不到 Tick'); process.exit(1); }
/* 插在 foreach 内、循环体末尾（heated 那行之后）*/
let ins = -1;
for (let i = ti; i < ti + 25; i++) {
  if (lines[i] !== undefined && lines[i].includes('if (heated && v.Temp > 62f')) { ins = i; break; }
}
if (ins < 0) { console.log('✗ 找不到 Tick 循环体末尾'); process.exit(1); }
if (!lines[ins + 1] || !lines[ins + 1].includes('UpdateAppearance(v, dt)')) {
  lines.splice(ins + 1, 0,
    '            /* 每帧衰减视觉强度。原来只在发生反应时才跑，',
    '               所以不反应的容器粒子强度永远停在最后一次的值 ——',
    '               烧杯倒空了还在冒气泡、火焰也一直不灭。 */',
    '            UpdateAppearance(v, dt);');
  n++;
  console.log('  ✓ Tick 里已加 UpdateAppearance');
}

fs.writeFileSync(C, lines.join('\n'), 'utf8');
console.log('改动处数:', n);
console.log('  括号差:', (fs.readFileSync(C,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(C,'utf8').match(/\}/g)||[]).length);

/* 打印 Tick 循环体核对 */
const fl = fs.readFileSync(C, 'utf8').split('\n');
let t2 = -1;
for (let i = 0; i < fl.length; i++) { if (fl[i].includes('public static void Tick(float dt) {')) { t2 = i; break; } }
console.log('--- Tick 循环体 ---');
for (let i = t2; i < t2 + 22; i++) console.log('  ', fl[i]);
