const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 容器标签的 Canvas 打开 overrideSorting ---- */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
P(V, '        t._canvas.sortingOrder = 10;   /* 低于方程式标签，让方程式永远看得见 */',
`        t._canvas.overrideSorting = true;   // 让每个标签能独立排层
        t._canvas.sortingOrder = 10;        // 低于方程式标签`);

/* ---- 2) 同一容器有方程式标签时，容器标签让位 ---- */
P(V, `        bool show = TagSettings.ShowTags;
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show) return;`,
`        /* 这个容器上正挂着方程式标签时，容器标签就让开。
           世界空间画布的透明排序按到相机的距离算、不看层级，
           两张标签挨在一起时谁盖谁不可控 —— 与其跟排序较劲，不如让位。 */
        bool show = TagSettings.ShowTags && !EquationTag.BusyFor(Model);
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show) return;`);

/* ---- 3) EquationTag 加 BusyFor + Active 列表 ---- */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EquationTag.cs';
P(E, '    public static EquationTag Create(',
`    /// <summary>正在显示的方程式标签（由 LabBootstrap 出入栈时维护）</summary>
    public static readonly System.Collections.Generic.List<EquationTag> Active =
        new System.Collections.Generic.List<EquationTag>();

    /// <summary>某个容器上是否正有方程式标签在显示</summary>
    public static bool BusyFor(Vessel v) {
        for (int i = 0; i < Active.Count; i++) {
            var t = Active[i];
            if (t != null && t.Ev != null && t.Ev.Vessel == v) return true;
        }
        return false;
    }

    public static EquationTag Create(`);

/* ---- 4) LabBootstrap 维护 Active ---- */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, `                et.Slot = sameHere;
                _eqTags.Add(et);`,
`                et.Slot = sameHere;
                EquationTag.Active.Add(et);
                _eqTags.Add(et);`);
P(B, `                if (!_eqTags[i].Sync()) {
                    Destroy(_eqTags[i].gameObject);
                    _eqTags.RemoveAt(i);
                }`,
`                if (!_eqTags[i].Sync()) {
                    EquationTag.Active.Remove(_eqTags[i]);
                    Destroy(_eqTags[i].gameObject);
                    _eqTags.RemoveAt(i);
                }`);

console.log('补丁处数:', n);
