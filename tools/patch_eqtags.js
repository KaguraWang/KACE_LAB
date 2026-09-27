const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';

/* 已有 ReactionEvent 时 Localization.English 是否存在 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Chem/Localization.cs';
const lc = fs.readFileSync(L, 'utf8');
console.log('Localization.English 存在:', /public static bool English/.test(lc));

/* ---- 场景里管理方程式标签 ---- */
P(B, '    readonly List<VesselTag> _tags = new List<VesselTag>();',
`    readonly List<VesselTag> _tags = new List<VesselTag>();
    readonly List<EquationTag> _eqTags = new List<EquationTag>();
    int _eqSeen;`);

P(B, '        /* 标签必须显式同步 —— 无头环境没有 LateUpdate */\n        foreach (var tg in _tags) if (tg != null) tg.Sync();',
`        /* 标签必须显式同步 —— 无头环境没有 LateUpdate */
        foreach (var tg in _tags) if (tg != null) tg.Sync();

        /* ---- 反应方程式弹出标签 ---- */
        if (_tagRoot != null && LabUI.UiFont != null) {
            /* 新发生的反应 -> 建标签 */
            while (_eqSeen < Lab.RecentReactions.Count) {
                var ev = Lab.RecentReactions[_eqSeen++];
                if (ev == null || ev.Rule == null) continue;
                _eqTags.Add(EquationTag.Create(ev, _tagRoot, LabUI.UiFont, Cam));
            }
            if (_eqSeen > Lab.RecentReactions.Count) _eqSeen = Lab.RecentReactions.Count;
            /* 超时的移除 */
            for (int i = _eqTags.Count - 1; i >= 0; i--) {
                if (_eqTags[i] == null) { _eqTags.RemoveAt(i); continue; }
                if (!_eqTags[i].Sync()) {
                    Destroy(_eqTags[i].gameObject);
                    _eqTags.RemoveAt(i);
                }
            }
        }`);

console.log('LabBootstrap 补丁处数:', n);
