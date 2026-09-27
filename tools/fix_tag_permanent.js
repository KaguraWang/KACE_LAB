const fs = require('fs');
let n = 0;

/* ============ 1) 撤掉「容器标签让位」 ============
   我当初加这个是为了绕开两张标签互相遮挡，但代价是容器标签会消失 ——
   而容器标签本来就该永久存在，只有反应标签才 10 秒消失。
   正确做法是让方程式标签排到容器标签**上方**，而不是把容器标签藏起来。 */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let v = fs.readFileSync(V, 'utf8');
if (v.includes('EquationTag.BusyFor')) {
  v = v.replace(`        /* 这个容器上正挂着方程式标签时，容器标签就让开。
           世界空间画布的透明排序按到相机的距离算、不看层级，
           两张标签挨在一起时谁盖谁不可控 —— 与其跟排序较劲，不如让位。 */
        bool show = TagSettings.ShowTags && !EquationTag.BusyFor(Model);`,
`        /* 容器标签是**永久**的，只在用户自己关掉标签显示时才消失。
           （以前这里还加了「有方程式标签时让位」，那是错的 ——
             容器标签会一消失就再也回不来。） */
        bool show = TagSettings.ShowTags;`);
  fs.writeFileSync(V, v, 'utf8'); n++;
  console.log('  ✓ 容器标签恢复永久显示');
}

/* ============ 2) VesselTag 暴露「面板顶端的世界 Y」给方程式标签让位 ============ */
v = fs.readFileSync(V, 'utf8');
if (!v.includes('public float TopWorldY')) {
  v = v.replace('    /// <summary>同步内容与位置</summary>',
`    /// <summary>面板顶端的世界 Y。方程式标签用它把自己排到容器标签上方，
    /// 这样两张标签不会重叠 —— 而不是靠隐藏其中一张来回避。 </summary>
    public float TopWorldY {
        get {
            if (_panel == null) return Model.Pos.y + Model.LiftY + 1.0f;
            var g = VesselMesh.Get(Model.Kind);
            float worldH = _panel.rect.height / CanvasUnitsPerWorld;
            Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);
            return basePt.y + worldH + 0.055f + Slot * 0.30f;
        }
    }

    /// <summary>同步内容与位置</summary>`);
  fs.writeFileSync(V, v, 'utf8'); n++;
  console.log('  ✓ TopWorldY 已加');
}

/* ============ 3) EquationTag 排到容器标签上方 ============ */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EquationTag.cs';
let e = fs.readFileSync(E, 'utf8');
const oldPos = `        Vector3 basePos = (Ev.Vessel != null) ? Ev.Vessel.Pos : Ev.Pos;
        float liftY = (Ev.Vessel != null) ? Ev.Vessel.LiftY : 0f;
        Vector3 center = basePos + Vector3.up * (liftY + 1.85f + Slot * 0.62f + worldH * 0.5f);`;
const newPos = `        Vector3 basePos = (Ev.Vessel != null) ? Ev.Vessel.Pos : Ev.Pos;
        float liftY = (Ev.Vessel != null) ? Ev.Vessel.LiftY : 0f;
        /* 容器标签是永久显示的，方程式标签必须排在它**上方**而不是盖住它。
           容器标签的高度跟行数有关，所以这里现问一次它的顶端世界 Y。 */
        float bottomY = basePos.y + liftY + 1.75f + Slot * 0.62f;
        var vt = FindVesselTag(Ev.Vessel);
        if (vt != null) bottomY = Mathf.Max(bottomY, vt.TopWorldY + 0.10f + Slot * 0.62f);
        Vector3 center = basePos + Vector3.up * (bottomY - basePos.y + worldH * 0.5f);`;
if (e.includes(oldPos)) { e = e.replace(oldPos, newPos); n++; console.log('  ✓ 方程式标签改为排在容器标签上方'); }
else console.log('  ✗ 定位锚点未命中');

/* FindVesselTag 辅助 */
if (!e.includes('static VesselTag FindVesselTag')) {
  e = e.replace('    public static EquationTag Create(',
`    /// <summary>找到某个容器的标签视图（用来问它的顶端位置）</summary>
    static VesselTag FindVesselTag(Vessel v) {
        if (v == null) return null;
        var host = LabBootstrap.Instance;
        if (host == null) return null;
        foreach (var t in host.GetComponentsInChildren<VesselTag>(true))
            if (t != null && t.Model == v) return t;
        return null;
    }

    public static EquationTag Create(`);
  n++;
  console.log('  ✓ FindVesselTag 已加');
}

fs.writeFileSync(E, e, 'utf8');
console.log('改动处数:', n);
