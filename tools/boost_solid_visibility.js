const fs = require('fs');
let n = 0;

/* 1) 堆的表面做出明显的晶粒起伏 —— 原本 bump 只有堆高 22%，
      在白台面上完全看不出「这是一堆固体」。 */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselMesh.cs';
let v = fs.readFileSync(V, 'utf8');
const oldBump = '            float bump = ((float)rnd.NextDouble() - 0.5f) * peak * 0.22f;';
const newBump = '            /* 起伏放大到堆高的 45%，并叠一层高频颗粒 ——\n' +
                '               纯平滑的白堆放在白台面上会整片糊掉，看不出是固体。 */\n' +
                '            float bump = ((float)rnd.NextDouble() - 0.5f) * peak * 0.45f;\n' +
                '            bump += Mathf.Sin(t * 37.7f + seed) * peak * 0.055f;\n' +
                '            bump += Mathf.Sin(t * 91.3f + seed * 1.7f) * peak * 0.030f;';
if (v.includes(oldBump)) { v = v.replace(oldBump, newBump); fs.writeFileSync(V, v, 'utf8'); n++; console.log('  ✓ 晶粒起伏已加大'); }
else console.log('  ✗ bump 锚点未命中');

/* 2) 近中性色的固体压暗更多：0.86 → 0.72 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');
const oldC = '            col = new Color(col.r * 0.86f, col.g * 0.86f, col.b * 0.88f);';
const newC = '            /* 压暗得多一点：白粉和白台面几乎同色，只压 14% 看不出来 */\n' +
             '            col = new Color(col.r * 0.72f, col.g * 0.73f, col.b * 0.76f);';
if (l.includes(oldC)) { l = l.replace(oldC, newC); fs.writeFileSync(L, l, 'utf8'); n++; console.log('  ✓ 中性色压暗 0.86 → 0.72'); }
else console.log('  ✗ 颜色锚点未命中');

/* 3) 固体的不透明度拉满、加一点自发光级别的对比 */
const oldT = '                _sedMat.SetFloat("_Turbidity", 0.15f);';
const newT = '                _sedMat.SetFloat("_Turbidity", 0.42f);   /* 浑浊度提高，堆面才有明暗层次 */';
l = fs.readFileSync(L, 'utf8');
if (l.includes(oldT)) { l = l.replace(oldT, newT); fs.writeFileSync(L, l, 'utf8'); n++; console.log('  ✓ 浑浊度 0.15 → 0.42'); }
else console.log('  ✗ 浑浊度锚点未命中');

console.log('改动处数:', n);
console.log('  VesselMesh 括号差:', (fs.readFileSync(V,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(V,'utf8').match(/\}/g)||[]).length);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
