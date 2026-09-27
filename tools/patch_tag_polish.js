const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const T = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';

/* ---- 1) 阶梯错位：容器挨得近时标签会互相压住，按序号分层 ---- */
P(T, '    public Vessel Model;',
`    public Vessel Model;
    /// <summary>错位槽位（0/1/2）：容器挨得近时标签会互相压住，按槽位抬到不同高度</summary>
    public int Slot;`);

P(T, '        transform.position = center;',
`        /* 按槽位抬高，避免相邻容器的标签重叠 */
        center += Vector3.up * (Slot * 0.52f);
        transform.position = center;`);

/* ---- 2) 面板收窄一点，重叠更少 ---- */
P(T, '    public const float Width = 0.98f;', '    public const float Width = 0.92f;');
P(T, '    const float PanelW = 340f;', '    const float PanelW = 300f;');

/* ---- 3) 白线加粗 ---- */
P(T, '        t._line.startWidth = 0.012f;\n        t._line.endWidth = 0.012f;',
`        t._line.startWidth = 0.020f;
        t._line.endWidth = 0.020f;`);

/* ---- 4) 按钮文字改用字体里一定有的字符 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
P(U, '                case 0: Set(b, TagSettings.ShowTags ? "标签 ✓" : "标签 ✗"); break;',
`                case 0: Set(b, TagSettings.ShowTags ? "标签 开" : "标签 关"); break;`);
P(U, '                case 2: Set(b, TagSettings.ShowName ? "物质 ✓" : "物质 ✗"); break;',
`                case 2: Set(b, TagSettings.ShowName ? "物质 开" : "物质 关"); break;`);
P(U, '                case 3: Set(b, TagSettings.ShowAmount ? "mol ✓" : "mol ✗"); break;',
`                case 3: Set(b, TagSettings.ShowAmount ? "mol 开" : "mol 关"); break;`);
P(U, '                case 4: Set(b, TagSettings.ShowConc ? "浓度 ✓" : "浓度 ✗"); break;',
`                case 4: Set(b, TagSettings.ShowConc ? "浓度 开" : "浓度 关"); break;`);

/* ---- 5) 建 tag 时分配槽位 ---- */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, '                _tags.Add(VesselTag.Create(v, _tagRoot, LabUI.UiFont, Cam));',
`                var nt = VesselTag.Create(v, _tagRoot, LabUI.UiFont, Cam);
                /* 按容器在列表里的位置分层错位 */
                nt.Slot = _tags.Count % 3;
                _tags.Add(nt);`);

console.log('补丁处数:', n);
