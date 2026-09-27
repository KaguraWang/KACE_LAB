const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(B, 'utf8');

if (!c.includes('Vessel PickVessel(Vector3 screen)')) {
  const anchor = '    /* ================================================================\n       无头渲染一张图（命令行验证用）';
  const add = `    Vessel PickVessel(Vector3 screen) {
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

    Vector3 PickPlane(Vector3 screen) {
        var ray = Cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.zero);
        return plane.Raycast(ray, out float t) ? ray.GetPoint(t) : Vector3.zero;
    }

`;
  if (c.includes(anchor)) {
    c = c.replace(anchor, add + anchor);
    fs.writeFileSync(B, c, 'utf8');
    console.log('✓ PickVessel / PickPlane 已补回');
  } else console.log('✗ 锚点未命中');
} else console.log('- 已存在');
