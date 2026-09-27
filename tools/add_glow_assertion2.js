const fs = require('fs');
let n = 0;

/* 1) 自检断言：透明器皿的聚焦高亮必须走玻璃材质 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');
if (!s.includes('透明器皿的聚焦高亮')) {
  const anchor = '                    TCheck("容器标签之间不重叠", tlap == 0);';
  if (s.includes(anchor)) {
    const add = [
      anchor, '',
      '                    /* 透明器皿的聚焦高亮必须走玻璃材质的菲涅尔辉光，',
      '                       不能建反向壳 —— 反向壳在透明物体上必然被透出来，',
      '                       整个器皿泛青，调细描边也只是减轻、根治不了。 */',
      '                    {',
      '                        var anyVessel = Object.FindObjectOfType<ChemLab.Game.VesselView>();',
      '                        if (anyVessel != null) {',
      '                            var gm = anyVessel.TestGlassMat;',
      '                            TCheck("器皿玻璃材质带 _HighlightAmt 属性（走菲涅尔辉光）",',
      '                                   gm != null && gm.HasProperty("_HighlightAmt"));',
      '                        } else Say("   （无头环境没有器皿视图，跳过玻璃高亮检查）");',
      '                    }',
    ].join('\n');
    s = s.replace(anchor, add);
    fs.writeFileSync(S, s, 'utf8');
    n++;
    console.log('  ✓ 自检断言已加');
  } else console.log('  ✗ 自测锚点未命中');
}

/* 2) VesselView 暴露玻璃材质 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');
if (!l.includes('public Material TestGlassMat')) {
  l = l.replace('    public void SetFocusGlow(float amt) {',
    '    /// <summary>给自检用的玻璃材质访问点</summary>\n    public Material TestGlassMat => _glassMat;\n\n    public void SetFocusGlow(float amt) {');
  fs.writeFileSync(L, l, 'utf8');
  n++;
  console.log('  ✓ TestGlassMat 已加');
}

/* 3) 实验台测试：搜索框取样区域重定 */
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');
const old = "$sr = @([int]($CW*0.795), [int]($CH*0.056), [int]($CW*0.170), [int]($CH*0.040))";
const nw = [
  '# 搜索框实测在屏幕 x 0.78..0.96、屏幕 y（从下往上）0.055..0.10，',
  '# 换算到客户区（从上往下）就是 y 0.90..0.945',
  '$sr = @([int]($CW*0.78), [int]($CH*0.895), [int]($CW*0.18), [int]($CH*0.045))',
].join('\n');
if (t.includes(old)) { t = t.replace(old, nw); fs.writeFileSync(T, t, 'utf8'); n++; console.log('  ✓ 搜索框取样区域已重定'); }
else console.log('  ✗ uitest 锚点未命中');

console.log('改动处数:', n);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
console.log('  ChemSelfTest 括号差:', (fs.readFileSync(S,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(S,'utf8').match(/\}/g)||[]).length);
