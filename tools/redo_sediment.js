const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselMesh.cs';
let lines = fs.readFileSync(V, 'utf8').split('\n');

let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public static Mesh BuildSediment(')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到 BuildSediment'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 40; i++) {
  if (lines[i] !== undefined && lines[i].includes('return RevolveLoop(loop, segs);')) { b = i + 1; break; }
}
console.log('BuildSediment:', a + 1, '..', b + 1);
console.log('--- 旧实现 ---');
for (let i = a; i <= b; i++) console.log('  ', lines[i]);

const nw = [
  '    public static Mesh BuildSediment(VesselKind kind, float solidFrac, int segs = 40, int seed = 0) {',
  '        var g = Get(kind);',
  '        var prof = Profile(g.Type, g.R, g.H);',
  '        float w = Mathf.Max(0.012f, g.R * 0.052f);',
  '        float totalH = prof[prof.Count - 1].y;',
  '',
  '        /* 堆高。原来只有 0.03 起步、而且是个平盘 —— 少量固体在白台面上几乎看不见。',
  '           真实倒进容器的粉末会堆成一个小丘，所以这里：',
  '             ① 抬高下限（少量也要看得见一层）',
  '             ② 做成中间高、边缘低的锥形堆，而不是平板 */',
  '        float frac = Mathf.Clamp(0.06f + solidFrac * 0.85f, 0.05f, 0.42f);',
  '        float top = frac * totalH;',
  '',
  '        var rnd = new System.Random(seed * 7919 + 13);',
  '        var loop = new List<Vector2>();',
  '',
  '        /* 侧壁：从堆底贴着容器内壁往上收 */',
  '        int M = 14;',
  '        for (int i = 0; i <= M; i++) {',
  '            float t = (float)i / M;',
  '            float y = Mathf.Lerp(w, top * 0.55f, t);',
  '            float rr = RadiusAt(prof, Mathf.Clamp01(y / totalH)) - w;',
  '            /* 越往上越收，像堆起来的粉 */',
  '            rr *= Mathf.Lerp(1f, 0.72f, t);',
  '            loop.Add(new Vector2(Mathf.Max(0.02f, rr), y));',
  '        }',
  '',
  '        /* 锥顶：中间隆起，表面带颗粒起伏 */',
  '        int K = 22;',
  '        float peak = top;',
  '        for (int i = K; i >= 0; i--) {',
  '            float t = (float)i / K;                       // 1 → 0：从边缘收到中心',
  '            float baseR = Mathf.Max(0.02f, RadiusAt(prof, Mathf.Clamp01(peak / totalH)) - w) * 0.72f;',
  '            float rr = baseR * t;',
  '            /* 中心最高，边缘落到堆底；再叠一点随机颗粒感 */',
  '            float cone = peak * (1f - t * t * 0.55f);',
  '            float bump = ((float)rnd.NextDouble() - 0.5f) * peak * 0.22f;',
  '            loop.Add(new Vector2(rr, cone + bump));',
  '        }',
  '',
  '        return RevolveLoop(loop, segs);',
  '    }',
];
lines.splice(a, b - a + 1, ...nw);
fs.writeFileSync(V, lines.join('\n'), 'utf8');
console.log('✓ BuildSediment 已重做（锥形堆 + 抬高下限 + 颗粒起伏）');
console.log('  括号差:', (fs.readFileSync(V,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(V,'utf8').match(/\}/g)||[]).length);
