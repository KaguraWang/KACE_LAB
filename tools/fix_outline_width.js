const fs = require('fs');
let n = 0;

/* 1) 材质资源加 _Outline 默认值 */
const mp = 'D:/ChemLabUnity/Assets/Resources/OutlineMat.mat';
let m = fs.readFileSync(mp, 'utf8');
m = m.replace(`    m_Floats: []`, `    m_Floats:
    - _Outline: 0.006`);
fs.writeFileSync(mp, m, 'utf8'); n++;
console.log('  ✓ 材质加了 _Outline 0.006');

/* 2) OutlineFx：外壳不再按百分比放大，缩放保持 1.0；
      描边宽度交给着色器的 _Outline（物体空间，恒定粗细） */
const O = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let o = fs.readFileSync(O, 'utf8');

o = o.replace(`    void Build(Color color, float thickness) {`, `    void Build(Color color, float thickness) {
        _width = thickness;`);
o = o.replace(`        _mat = MakeMat(color);`, `        _mat = MakeMat(color, thickness);`);

o = o.replace(`        go.transform.localPosition = transform.InverseTransformPoint(src.transform.position);
        go.transform.localRotation = Quaternion.Inverse(transform.rotation) * src.transform.rotation;
        go.transform.localScale = src.transform.lossyScale * (1f + t * 12f);`,
`        go.transform.localPosition = transform.InverseTransformPoint(src.transform.position);
        go.transform.localRotation = Quaternion.Inverse(transform.rotation) * src.transform.rotation;
        /* 缩放保持 1:1 —— 描边粗细由着色器沿法线外扩控制，
           按百分比放大会把小物体放大成一整块实心色。 */
        go.transform.localScale = src.transform.lossyScale;`);

o = o.replace(`            if (p.dt != null && p.st != null) {
                p.dt.localPosition = transform.InverseTransformPoint(p.st.position);
                p.dt.localRotation = Quaternion.Inverse(transform.rotation) * p.st.rotation;
                p.dt.localScale = p.st.lossyScale * 1.22f;`,
`            if (p.dt != null && p.st != null) {
                p.dt.localPosition = transform.InverseTransformPoint(p.st.position);
                p.dt.localRotation = Quaternion.Inverse(transform.rotation) * p.st.rotation;
                p.dt.localScale = p.st.lossyScale;`);

o = o.replace(`    static Material MakeMat(Color c) {`, `    float _width = 0.006f;

    static Material MakeMat(Color c, float width) {`);
o = o.replace(`        var m = new Material(tpl);
        m.SetColor("_Color", c);
        return m;`,
`        var m = new Material(tpl);
        m.SetColor("_Color", c);
        m.SetFloat("_Outline", width);
        return m;`);

fs.writeFileSync(O, o, 'utf8'); n++;
console.log('  ✓ OutlineFx 已改用着色器外扩');

/* 3) 描边宽度改成绝对距离 */
o = fs.readFileSync(O, 'utf8');
o = o.replace('public static OutlineFx Attach(GameObject target, Color color, float thickness = 0.018f) {',
              'public static OutlineFx Attach(GameObject target, Color color, float thickness = 0.006f) {');
o = o.replace('public static OutlineFx Attach(Transform target, Color color, float thickness = 0.018f) {',
              'public static OutlineFx Attach(Transform target, Color color, float thickness = 0.006f) {');
o = o.replace('public void Rebuild(Color color, float thickness = 0.018f) {',
              'public void Rebuild(Color color, float thickness = 0.006f) {');
fs.writeFileSync(O, o, 'utf8'); n++;

/* 4) 调用处也改成 0.006 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
b = b.replace('OutlineFx.Attach(tr, new Color(0.35f, 0.85f, 1f), 0.02f)',
              'OutlineFx.Attach(tr, new Color(0.35f, 0.85f, 1f), 0.006f)');
fs.writeFileSync(B, b, 'utf8'); n++;

console.log('改动处数:', n);
const chk = fs.readFileSync(O, 'utf8');
console.log('  缩放 1:1:', !chk.includes('* 1.22f') && !chk.includes('* (1f + t * 12f)'));
console.log('  _Outline 设置:', chk.includes('SetFloat("_Outline"'));
