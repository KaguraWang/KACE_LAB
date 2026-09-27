const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';

/* ---- 1) 字段 ---- */
P(B, '    readonly List<TestItemView> _testViews = new List<TestItemView>();',
`    readonly List<TestItemView> _testViews = new List<TestItemView>();
    readonly List<VesselTag> _tags = new List<VesselTag>();
    Transform _tagRoot;`);

/* ---- 2) 建 tag 根节点 ---- */
P(B, '        ChemAudio.Ensure(gameObject);',
`        ChemAudio.Ensure(gameObject);
        /* 标签单独一个根节点，方便整体开关 */
        if (_tagRoot == null) {
            var tg = new GameObject("Tags");
            tg.transform.SetParent(transform, false);
            _tagRoot = tg.transform;
        }`);

/* ---- 3) SyncViews 里补 tag ---- */
P(B, '        foreach (var av in _apViews) if (av != null) av.Sync();',
`        foreach (var av in _apViews) if (av != null) av.Sync();

        /* 容器标签：新容器补 tag，消失的移除 */
        if (_tagRoot != null && LabUI.UiFont != null) {
            foreach (var v in Lab.Vessels) {
                if (_tags.Any(x => x != null && x.Model == v)) continue;
                _tags.Add(VesselTag.Create(v, _tagRoot, LabUI.UiFont, Cam));
            }
            for (int i = _tags.Count - 1; i >= 0; i--) {
                if (_tags[i] == null) { _tags.RemoveAt(i); continue; }
                if (!Lab.Vessels.Contains(_tags[i].Model)) {
                    Destroy(_tags[i].gameObject);
                    _tags.RemoveAt(i);
                }
            }
            foreach (var tg in _tags) if (tg != null) tg.Sync();
        }`);

/* ---- 4) 无头截图时也要同步一次（RenderToFile 里已调 SyncViews） ---- */
P(B, '        foreach (var av in _apViews) if (av != null) av.Sync();\n    }\n\n\n    void Update() {',
`        foreach (var av in _apViews) if (av != null) av.Sync();
        /* 标签必须显式同步 —— 无头环境没有 LateUpdate */
        foreach (var tg in _tags) if (tg != null) tg.Sync();
    }


    void Update() {`);

console.log('LabBootstrap 补丁处数:', n);
