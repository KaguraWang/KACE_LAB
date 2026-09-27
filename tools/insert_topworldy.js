const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let lines = fs.readFileSync(V, 'utf8').split('\n');

if (lines.some(l => l.includes('TopWorldY'))) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public void Sync() {')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 Sync'); process.exit(1); }

const blk = [
  '    /// <summary>面板顶端的世界 Y。方程式标签用它把自己排到容器标签上方，',
  '    /// 这样两张标签不会重叠 —— 而不是靠隐藏其中一张来回避。',
  '    /// 容器标签是永久显示的，只有反应标签才有时限。 </summary>',
  '    public float TopWorldY {',
  '        get {',
  '            if (Model == null) return 1.0f;',
  '            var g = VesselMesh.Get(Model.Kind);',
  '            float baseY = Model.Pos.y + Model.LiftY + g.H + 0.10f;',
  '            float worldH = 0.42f;                      // 面板高度的保守估计',
  '            if (_panel != null && _panel.rect.height > 1f)',
  '                worldH = _panel.rect.height / CanvasUnitsPerWorld;',
  '            return baseY + worldH + 0.055f + Slot * 0.30f;',
  '        }',
  '    }',
  '',
];
lines.splice(at, 0, ...blk);
fs.writeFileSync(V, lines.join('\n'), 'utf8');
console.log('✓ TopWorldY 已插在 Sync 之前（第', at + 1, '行）');

/* 查验 */
const c = fs.readFileSync(V, 'utf8');
console.log('  存在:', c.includes('public float TopWorldY'));
console.log('  大括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
