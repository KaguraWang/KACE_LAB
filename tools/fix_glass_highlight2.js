const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(L, 'utf8').split('\n');
let n = 0;

/* 1) VesselView.SetHighlight 改成驱动玻璃材质 */
let hit = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public void SetHighlight(bool on) {') && lines[i + 1] && lines[i + 1].includes('_glowRing.enabled')) { hit = i; break; }
}
if (hit < 0) { console.log('✗ 找不到 VesselView.SetHighlight'); process.exit(1); }
console.log('SetHighlight 在第', hit + 1, '行');
lines[hit + 1] = '        SetFocusGlow(on ? 1.35f : 0f);';
lines.splice(hit + 2, 0,
  '    }',
  '',
  '    /// <summary>聚焦高亮：走玻璃材质的菲涅尔辉光，而不是反向壳描边。',
  '    /// 反向壳在透明物体上必然被透出来（描边在 Opaque 队列、玻璃在 Transparent',
  '    /// 队列后画），整个器皿会泛青，调细也只是减轻、根治不了。</summary>',
  '    public void SetFocusGlow(float amt) {',
  '        if (_glassMat != null && _glassMat.HasProperty("_HighlightAmt"))',
  '            _glassMat.SetFloat("_HighlightAmt", amt);');
n++;
console.log('  ✓ VesselView 改成玻璃辉光');

/* 2) SetFocus：器皿走玻璃辉光 */
let sf = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('_focusOutline = OutlineFx.Attach(tr, new Color(0.30f, 0.85f, 1f), 0.0035f);')) { sf = i; break; }
}
if (sf < 0) { console.log('✗ 找不到 Attach 调用'); process.exit(1); }
console.log('Attach 在第', sf + 1, '行');
lines[sf] = '        /* 器皿是透明的 —— 用玻璃着色器的菲涅尔辉光，不要反向壳描边。',
lines.splice(sf + 1, 0,
  '           不透明的器材 / 道具才走反向壳。 */',
  '        if (target is Vessel) {',
  '            var fv = _views.Find(x => x != null && x.Model == (Vessel)target);',
  '            if (fv != null) { fv.SetFocusGlow(1.35f); _focusVessel = fv; }',
  '            return;',
  '        }',
  '        _focusOutline = OutlineFx.Attach(tr, new Color(0.30f, 0.85f, 1f), 0.0035f);');
n++;
console.log('  ✓ SetFocus 已分流');

fs.writeFileSync(L, lines.join('\n'), 'utf8');

/* 3) 取消聚焦时清辉光 + 字段声明 */
let c = fs.readFileSync(L, 'utf8');
const oldC = `        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        _focused = target;`;
const newC = `        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        if (_focusVessel != null) { _focusVessel.SetFocusGlow(0f); _focusVessel = null; }
        _focused = target;`;
if (c.includes(oldC)) { c = c.replace(oldC, newC); n++; console.log('  ✓ 取消聚焦清辉光'); }
else console.log('  ✗ 取消锚点');

if (!c.includes('VesselView _focusVessel;')) {
  c = c.replace('    OutlineFx _focusOutline;', '    OutlineFx _focusOutline;\n    VesselView _focusVessel;');
  n++;
  console.log('  ✓ _focusVessel 已声明');
}
fs.writeFileSync(L, c, 'utf8');
console.log('改动处数:', n);
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
