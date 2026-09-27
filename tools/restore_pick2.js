const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 直接插在 HandleInput 的收尾 } 之后 */
let start = -1, end = -1;
for (let i = 0; i < lines.length; i++) if (lines[i].includes('void HandleInput()')) { start = i; break; }
let depth = 0, started = false;
for (let k = start; k < lines.length; k++) {
  depth += (lines[k].match(/\{/g) || []).length;
  depth -= (lines[k].match(/\}/g) || []).length;
  if (depth > 0) started = true;
  if (started && depth === 0) { end = k; break; }
}
console.log('HandleInput 结束于第', end + 1, '行');

const blk = `
    /// <summary>屏幕点选器皿：按「屏幕距离 vs 随距离缩放的半径」判定，取最近的</summary>
    Vessel PickVessel(Vector3 screen) {
        Vessel best = null; float bestT = float.MaxValue;
        foreach (var v in Lab.Vessels) {
            var g = VesselMesh.Get(v.Kind);
            var center = v.Pos + Vector3.up * g.H * 0.45f;
            var sc = Cam.WorldToScreenPoint(center);
            if (sc.z <= 0) continue;
            float r = g.R * 2.2f / Mathf.Max(0.2f, sc.z) * (Screen.height * 0.5f / 0.53f);
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Mathf.Max(r, Screen.height * 0.08f) && sc.z < bestT) { bestT = sc.z; best = v; }
        }
        return best;
    }

    /// <summary>屏幕射线打到台面（y=0）上的世界坐标</summary>
    Vector3 PickPlane(Vector3 screen) {
        var ray = Cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.zero);
        return plane.Raycast(ray, out float t) ? ray.GetPoint(t) : Vector3.zero;
    }
`;
lines.splice(end + 1, 0, ...blk.split('\n'));
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ PickVessel / PickPlane 已插回');

const chk = fs.readFileSync(B, 'utf8');
console.log('  PickVessel:', chk.includes('Vessel PickVessel(Vector3 screen)'));
console.log('  PickPlane:', chk.includes('Vector3 PickPlane(Vector3 screen)'));
