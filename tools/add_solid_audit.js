const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

if (u.includes('CLICK|reagent_')) { console.log('- 已存在'); process.exit(0); }

const anchor = '                if (rt.name == "Search") Emit("search", rt);';
if (!u.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

/* 试剂列表项也报出来：名字用 R_ 后面的 id，测试按 id 点 */
u = u.replace(anchor, anchor + `
                if (rt.name.StartsWith("R_") && rt.parent != null && rt.parent.name == "Content")
                    Emit("reagent_" + rt.name.Substring(2), rt);`);

fs.writeFileSync(U, u, 'utf8');
console.log('✓ 试剂列表项已加进审计');
console.log('  括号差:', (u.match(/\{/g) || []).length - (u.match(/\}/g) || []).length);

/* 顺便加一个「固体体积」审计，看模型层面到底算出来多少 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');
if (!l.includes('SOLID|')) {
  const a2 = '            sb.AppendLine("STATE|vessel|" + vv2.Model.Id + "|"';
  if (l.includes(a2)) {
    l = l.replace(a2, `            /* 固体体积：诊断「加了固体/沉淀却看不到」用 */
            {
                float sv = 0f;
                foreach (var kv in vv2.Model.Undissolved) { var ss = ChemData.Sub[kv.Key]; if (ss != null) sv += (float)(kv.Value * ss.Mass); }
                foreach (var kv in vv2.Model.Precipitates) { var ss = ChemData.Sub[kv.Key]; if (ss != null) sv += (float)(kv.Value * ss.Mass); }
                float sf = sv > 0 ? Mathf.Clamp(0.03f + sv / 60f, 0.02f, 0.34f) : 0f;
                bool sedOn = vv2.TestSedimentOn;
                sb.AppendLine("SOLID|" + vv2.Model.Id + "|未溶" + vv2.Model.Undissolved.Count
                            + " 沉淀" + vv2.Model.Precipitates.Count
                            + " 体积" + sv.ToString("F2") + " 比例" + sf.ToString("F3")
                            + " 渲染" + (sedOn ? "开" : "关"));
            }
` + a2);
    fs.writeFileSync(L, l, 'utf8');
    console.log('  ✓ SOLID 诊断已加');
  } else console.log('  ✗ STATE|vessel 锚点未命中');
}

/* VesselView 暴露沉淀渲染开关 */
l = fs.readFileSync(L, 'utf8');
if (!l.includes('public bool TestSedimentOn')) {
  l = l.replace('    public Material TestGlassMat => _glassMat;',
    '    public Material TestGlassMat => _glassMat;\n    /// <summary>沉淀渲染开没开（诊断用）</summary>\n    public bool TestSedimentOn => _sedMR != null && _sedMR.enabled;');
  fs.writeFileSync(L, l, 'utf8');
  console.log('  ✓ TestSedimentOn 已加');
}
console.log('  LabBootstrap 括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
