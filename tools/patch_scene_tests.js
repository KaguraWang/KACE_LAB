const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 44).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const F = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';

/* ---- 1) 字段 ---- */
P(F, '    readonly List<ApparatusView> _apViews = new List<ApparatusView>();',
`    readonly List<ApparatusView> _apViews = new List<ApparatusView>();
    readonly List<TestItemView> _testViews = new List<TestItemView>();
    Material _paperMat, _woodMat;
    float _lastPopPlayed;`);

/* ---- 2) 材质 ---- */
P(F, `        _flameMat = MatFromRes("Particle", "ChemLab/Particle", "Particles/Standard Unlit");`,
`        /* 检验器材：试纸与木条 */
        _paperMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _paperMat.SetColor("_Color", new Color(0.85f, 0.23f, 0.23f));
        _woodMat  = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _woodMat.SetColor("_Color", new Color(0.62f, 0.47f, 0.29f));

        _flameMat = MatFromRes("Particle", "ChemLab/Particle", "Particles/Standard Unlit");`);

/* ---- 3) 建检验器材 ---- */
P(F, '    void BuildCamera() {',
`    /// <summary>台面上的检验器材：红/蓝石蕊试纸各一、木条一根</summary>
    void BuildTestItems() {
        TestSet.Clear();
        TestSet.Add(TestItemKind.RedLitmus,  new Vector3(-1.60f, 0f, 1.75f));
        TestSet.Add(TestItemKind.BlueLitmus, new Vector3(-1.20f, 0f, 1.75f));
        TestSet.Add(TestItemKind.Splint,     new Vector3(-0.78f, 0f, 1.75f));
        foreach (var t in TestSet.All) {
            var tv = TestItemView.Create(t, transform, _paperMat, _woodMat, _flameMat);
            tv.Sync();
            _testViews.Add(tv);
        }
    }

    void BuildCamera() {`);

P(F, '        BuildApparatus();', `        BuildApparatus();
        BuildTestItems();`);

/* ---- 4) 每帧同步 + 播音 ---- */
P(F, '        foreach (var av in _apViews) if (av != null) av.Sync();',
`        foreach (var av in _apViews) if (av != null) av.Sync();
        /* 检验器材：新出现的补视图 */
        for (int i = 0; i < TestSet.All.Count; i++) {
            var t = TestSet.All[i];
            if (_testViews.Any(x => x != null && x.Model == t)) continue;
            _testViews.Add(TestItemView.Create(t, transform, _paperMat, _woodMat, _flameMat));
        }
        for (int i = _testViews.Count - 1; i >= 0; i--) {
            if (_testViews[i] == null || !TestSet.All.Contains(_testViews[i].Model)) {
                if (_testViews[i] != null) Destroy(_testViews[i].gameObject);
                _testViews.RemoveAt(i);
                continue;
            }
            _testViews[i].Sync();
        }`);

P(F, '        FollowAttachments();',
`        FollowAttachments();
        TestSet.Tick(Time.deltaTime);
        /* 爆鸣：引擎里立了标记，这里负责真的发出声音 */
        foreach (var v in Lab.Vessels) {
            if (v.Fx.Pop > 0f) {
                ChemAudio.Play("pop", Mathf.Clamp01(v.Fx.Pop));
                v.Fx.Pop = 0f;
            }
        }
        foreach (var t in TestSet.All) {
            if (t.PendingSound > 0f) {
                ChemAudio.Play(t.PendingSoundKind, t.PendingSound);
                t.PendingSound = 0f;
            }
        }`);

/* ---- 5) 音频初始化 ---- */
P(F, '        BuildUI();', `        BuildUI();
        ChemAudio.Ensure(gameObject);`);

console.log('补丁处数:', n);
