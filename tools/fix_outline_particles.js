const fs = require('fs');
let n = 0;

/* ============ 1) 轮廓不要描粒子，也不要描液面/沉淀 ============
   粒子是每帧重建的公告板四边形，描它就会变成一堆青色方块飘在空中。
   只描容器的外壳（Glass）就够表达「这个东西被聚焦了」。 */
const O = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let o = fs.readFileSync(O, 'utf8');

o = o.replace(`        foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) {
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.transform.IsChildOf(_root)) continue;
            BuildShell(mf, thickness);
        }`,
`        foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) {
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.transform.IsChildOf(_root)) continue;
            if (SkipMesh(mf.transform)) continue;
            BuildShell(mf, thickness);
        }`);
o = o.replace(`        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (smr == null || smr.sharedMesh == null) continue;
            BuildShellSkinned(smr, thickness);
        }`,
`        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (smr == null || smr.sharedMesh == null) continue;
            if (SkipMesh(smr.transform)) continue;
            BuildShellSkinned(smr, thickness);
        }`);
o = o.replace(`    void BuildShell(MeshFilter src, float t) {`,
`    /// <summary>哪些网格不参与描边。
       · Fx 下的粒子：每帧重建的公告板四边形，描出来是一堆飘着的方块
       · Liquid / Sediment：在容器内部，描了会被玻璃透出来、把整个容器染蓝
       只描外壳就够了 —— 「哪个东西被聚焦了」看轮廓就明白。 </summary>
    static bool SkipMesh(Transform t) {
        while (t != null) {
            var nm = t.name;
            if (nm == "Fx" || nm == "Liquid" || nm == "Sediment") return true;
            t = t.parent;
        }
        return false;
    }

    void BuildShell(MeshFilter src, float t) {`);
fs.writeFileSync(O, o, 'utf8'); n++;
console.log('  ✓ 粒子/液面/沉淀已排除在描边之外');

/* ============ 2) 描边渲染在玻璃之前 ============
   外壳在 Opaque 队列、玻璃在 Transparent 队列后画 ——
   玻璃会把外壳透出来，于是整个容器泛蓝。
   把外壳压到 Geometry-10，让它先画；玻璃再叠上去，
   只在轮廓线上留一圈颜色。 */
const SH = 'D:/ChemLabUnity/Assets/Shaders/ChemLabOutline.shader';
let sh = fs.readFileSync(SH, 'utf8');
sh = sh.replace('Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }',
                'Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }');
fs.writeFileSync(SH, sh, 'utf8'); n++;
console.log('  ✓ 外壳队列改为 Geometry-10（先于玻璃）');

/* ============ 3) 器皿抬高一点，避免和桌面 z-fighting ============ */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (b.includes('vv.transform.localPosition = vv.Model.Pos + Vector3.up * vv.Model.LiftY;')) {
  b = b.replace('vv.transform.localPosition = vv.Model.Pos + Vector3.up * vv.Model.LiftY;',
`/* 抬高 3mm：容器底面和台面完全共面会 z-fighting，看起来一闪一闪的 */
            vv.transform.localPosition = vv.Model.Pos + Vector3.up * (vv.Model.LiftY + 0.003f);`);
  fs.writeFileSync(B, b, 'utf8'); n++;
  console.log('  ✓ 器皿抬高 3mm');
} else console.log('  ✗ 器皿定位锚点未命中');

/* ============ 4) 器材也一起抬高 ============ */
const A = 'D:/ChemLabUnity/Assets/Scripts/Game/LabApparatus.cs';
let a = fs.readFileSync(A, 'utf8');
if (a.includes('transform.localPosition = a.Pos;')) {
  a = a.replace('transform.localPosition = a.Pos;',
                'transform.localPosition = a.Pos + Vector3.up * 0.003f;   /* 同容器，避免和台面 z-fighting */');
  fs.writeFileSync(A, a, 'utf8'); n++;
  console.log('  ✓ 器材抬高 3mm');
}

/* ============ 5) 检验道具也抬高 ============ */
const T = 'D:/ChemLabUnity/Assets/Scripts/Game/TestItemView.cs';
if (fs.existsSync(T)) {
  let t = fs.readFileSync(T, 'utf8');
  const m = t.match(/transform\.localPosition = ([^;]+);/);
  if (m && !t.includes('0.003f')) {
    t = t.replace(m[0], 'transform.localPosition = ' + m[1] + ' + Vector3.up * 0.003f;');
    fs.writeFileSync(T, t, 'utf8'); n++;
    console.log('  ✓ 检验道具抬高 3mm:', m[1].trim());
  } else console.log('  - 道具定位:', m ? m[1].trim() : '没找到');
}

console.log('改动处数:', n);
