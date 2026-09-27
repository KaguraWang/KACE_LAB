const fs = require('fs');
let n = 0;

/* =====================================================================
   器皿的聚焦高亮：从「反向壳描边」改成「玻璃着色器的菲涅尔辉光」
     反向壳必然被透明玻璃透出来 —— 描边在 Opaque 队列、玻璃在 Transparent
     队列后画，玻璃会把描边背面混进来，整个器皿泛青，怎么调细都只是减轻。
     玻璃着色器本来就有 fres 项（边缘最强），用它驱动高亮色就得到
     天然的轮廓辉光，而且是玻璃自己渲染的，没有任何排序问题。
   ===================================================================== */

/* 1) 着色器加 _Highlight / _HighlightAmt */
const G = 'D:/ChemLabUnity/Assets/Shaders/ChemLabGlass.shader';
let g = fs.readFileSync(G, 'utf8');

const oldP = `        _ReflStrength("反射强度", Range(0,2)) = 0.85
    }`;
const newP = `        _ReflStrength("反射强度", Range(0,2)) = 0.85
        _Highlight   ("聚焦高亮色", Color) = (0.30, 0.85, 1.0, 1)
        _HighlightAmt("聚焦高亮强度", Range(0,4)) = 0
    }`;
if (g.includes(oldP)) { g = g.replace(oldP, newP); n++; console.log('  ✓ 着色器属性已加'); }
else console.log('  ✗ 属性锚点未命中');

const oldD = `            float _Smoothness, _Refraction, _FresnelPow, _ReflStrength;`;
const newD = `            float _Smoothness, _Refraction, _FresnelPow, _ReflStrength;
            fixed4 _Highlight;
            float _HighlightAmt;`;
if (g.includes(oldD)) { g = g.replace(oldD, newD); n++; console.log('  ✓ 变量声明已加'); }
else console.log('  ✗ 声明锚点未命中');

const oldF = `                float alpha = saturate(_Tint.a * 2.0 + fres * 0.85 + spec * 0.9);
                return fixed4(col, alpha);`;
const newF = `                float alpha = saturate(_Tint.a * 2.0 + fres * 0.85 + spec * 0.9);

                /* --- 聚焦高亮：沿菲涅尔（轮廓最强）叠一层色 ---
                   这样描边是玻璃自己画出来的，不会像反向壳那样被透出来。 */
                if (_HighlightAmt > 0.001) {
                    float rim = pow(saturate(fres), 0.55);
                    col += _Highlight.rgb * rim * _HighlightAmt;
                    /* 边缘同时补一点不透明度，否则太透的玻璃上高亮看不出来 */
                    alpha = saturate(alpha + rim * _HighlightAmt * 0.45);
                }

                return fixed4(col, alpha);`;
if (g.includes(oldF)) { g = g.replace(oldF, newF); n++; console.log('  ✓ 菲涅尔高亮已加'); }
else console.log('  ✗ 片段锚点未命中');
fs.writeFileSync(G, g, 'utf8');

/* 2) VesselView.SetHighlight 改成驱动玻璃材质 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');
const oldH = `    /// <summary>吸附提示环（拖到别的容器附近时亮起）</summary>
    MeshRenderer _glowRing;
    public void SetHighlight(bool on) {
        if (_glowRing != null) _glowRing.enabled = on;
    }`;
const newH = `    /// <summary>吸附提示环（拖到别的容器附近时亮起）</summary>
    MeshRenderer _glowRing;
    /// <summary>聚焦高亮：走玻璃材质的面板辉光，而不是反向壳描边。
    /// 反向壳在透明物体上必然被透出来（描边在 Opaque 队列、玻璃在 Transparent
    /// 队列后画），整个器皿会泛青，调细也只是减轻、根治不了。</summary>
    public void SetHighlight(bool on) { SetFocusGlow(on ? 1.35f : 0f); }

    public void SetFocusGlow(float amt) {
        if (_glassMat != null && _glassMat.HasProperty("_HighlightAmt"))
            _glassMat.SetFloat("_HighlightAmt", amt);
    }`;
if (l.includes(oldH)) { l = l.replace(oldH, newH); fs.writeFileSync(L, l, 'utf8'); n++; console.log('  ✓ VesselView.SetHighlight 改为玻璃辉光'); }
else console.log('  ✗ VesselView 锚点未命中');

/* 3) SetFocus：器皿不再建反向壳，改用玻璃辉光 */
l = fs.readFileSync(L, 'utf8');
const oldS = `        if (tr == null) return;
        _focusOutline = OutlineFx.Attach(tr, new Color(0.30f, 0.85f, 1f), 0.0035f);
        if (_focusOutline != null) _focusOutline.SetOn(true);`;
const newS = `        if (tr == null) return;

        /* 器皿是透明的 —— 用玻璃着色器的菲涅尔辉光，
           不要用反向壳描边（会被玻璃透出来，整个器皿泛青）。
           不透明的器材/道具才走反向壳。 */
        if (target is Vessel) {
            var vv = _views.Find(x => x != null && x.Model == (Vessel)target);
            if (vv != null) vv.SetFocusGlow(1.35f);
            _focusVessel = vv;
            return;
        }
        _focusOutline = OutlineFx.Attach(tr, new Color(0.30f, 0.85f, 1f), 0.0035f);
        if (_focusOutline != null) _focusOutline.SetOn(true);`;
if (l.includes(oldS)) { l = l.replace(oldS, newS); fs.writeFileSync(L, l, 'utf8'); n++; console.log('  ✓ SetFocus 已分流'); }
else console.log('  ✗ SetFocus 锚点未命中');

/* 4) 取消聚焦时也要清掉玻璃辉光 */
l = fs.readFileSync(L, 'utf8');
const oldC = `        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        _focused = target;`;
const newC = `        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        if (_focusVessel != null) { _focusVessel.SetFocusGlow(0f); _focusVessel = null; }
        _focused = target;`;
if (l.includes(oldC)) { l = l.replace(oldC, newC); fs.writeFileSync(L, l, 'utf8'); n++; console.log('  ✓ 取消聚焦清辉光'); }
else console.log('  ✗ 取消锚点未命中');

/* 5) 字段声明 */
l = fs.readFileSync(L, 'utf8');
if (!l.includes('_focusVessel')) console.log('  ✗ _focusVessel 没声明');
else if (!l.includes('VesselView _focusVessel;')) {
  l = l.replace('    OutlineFx _focusOutline;', '    OutlineFx _focusOutline;\n    VesselView _focusVessel;');
  fs.writeFileSync(L, l, 'utf8');
  n++;
  console.log('  ✓ _focusVessel 已声明');
}
console.log('改动处数:', n);
const c = fs.readFileSync(L, 'utf8');
console.log('  LabBootstrap 括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
