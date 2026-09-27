const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 位置往中间挪，避免被左栏挡住 ---- */
P('D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs',
`        TestSet.Add(TestItemKind.RedLitmus,  new Vector3(-1.60f, 0f, 1.75f));
        TestSet.Add(TestItemKind.BlueLitmus, new Vector3(-1.20f, 0f, 1.75f));
        TestSet.Add(TestItemKind.Splint,     new Vector3(-0.78f, 0f, 1.75f));`,
`        TestSet.Add(TestItemKind.RedLitmus,  new Vector3(-1.32f, 0f, 1.86f));
        TestSet.Add(TestItemKind.BlueLitmus, new Vector3(-1.02f, 0f, 1.86f));
        TestSet.Add(TestItemKind.Splint,     new Vector3(-0.70f, 0f, 1.86f));`);

/* ---- 2) UI：检验器材操作区 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
P(U, '    RectTransform _pourBar, _burnerBar;',
`    RectTransform _pourBar, _burnerBar, _testBar;
    Text _testLabel;
    Button _testBtn1, _testBtn2;`);

P(U, `        _pourBar.gameObject.SetActive(false);
        _burnerBar.gameObject.SetActive(false);`,
`        _pourBar.gameObject.SetActive(false);
        _burnerBar.gameObject.SetActive(false);

        /* ---- 离子检验器材区：石蕊试纸与木条 ---- */
        _testBar = Node("TestBar", info, new Vector2(0,0), new Vector2(1,0),
                        new Vector2(12, 138), new Vector2(-12, 164), new Color(0,0,0,0));
        _testLabel = Label(_testBar, "检验器材", 11, DIM, TextAnchor.MiddleLeft,
                           new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)_testLabel.transform).sizeDelta = new Vector2(74, 26);
        _testBtn1 = Btn(_testBar, "湿润", TestAction1, ACC, 12);
        { var br = (RectTransform)_testBtn1.transform; br.pivot = new Vector2(0,0);
          br.anchoredPosition = new Vector2(78, 2); br.sizeDelta = new Vector2(68, 22); }
        _testBtn2 = Btn(_testBar, "伸入", TestAction2, WARN, 12);
        { var br = (RectTransform)_testBtn2.transform; br.pivot = new Vector2(0,0);
          br.anchoredPosition = new Vector2(152, 2); br.sizeDelta = new Vector2(68, 22); }
        _testBar.gameObject.SetActive(false);`);

/* ---- 3) 交互方法 ---- */
P(U, '    void ToggleBurner() {',
`    /// <summary>当前选中的检验器材（按顺序循环）</summary>
    TestItem CurrentTest() {
        if (TestSet.All.Count == 0) return null;
        _testIdx = ((_testIdx % TestSet.All.Count) + TestSet.All.Count) % TestSet.All.Count;
        return TestSet.All[_testIdx];
    }
    int _testIdx;

    void CycleTest() {
        if (TestSet.All.Count == 0) return;
        _testIdx = (_testIdx + 1) % TestSet.All.Count;
        RefreshVesselPanel();
    }

    /// <summary>第一个按钮随器材类型变化：石蕊是「湿润」，木条是「点燃/熄灭」</summary>
    void TestAction1() {
        var t = CurrentTest();
        if (t == null || _selected == null) return;
        if (t.Kind == TestItemKind.Splint) {
            if (t.Splint == SplintState.Burning) GasTest.Extinguish(t);
            else GasTest.Light(t);
        } else {
            GasTest.Moisten(t);
        }
        RefreshVesselPanel(); RefreshLog();
    }

    /// <summary>第二个按钮：把器材伸到当前容器口</summary>
    void TestAction2() {
        var t = CurrentTest();
        if (t == null || _selected == null) return;
        GasTest.Insert(_selected, t);
        Lab.RxnCount++;
        RefreshVesselPanel(); RefreshLog();
    }

    void ToggleBurner() {`);

/* ---- 4) 面板刷新时更新这个区 ---- */
P(U, `        if (_burnerBar != null) {
            bool on = v.Burner != null;`,
`        if (_testBar != null) {
            bool on = TestSet.All.Count > 0;
            _testBar.gameObject.SetActive(on);
            if (on) {
                var t = CurrentTest();
                if (t != null) {
                    _testLabel.text = $"{t.Name}·{t.StateText}";
                    var b1 = _testBtn1.GetComponentInChildren<Text>();
                    var b2 = _testBtn2.GetComponentInChildren<Text>();
                    if (b1 != null) b1.text = t.Kind == TestItemKind.Splint
                        ? (t.Splint == SplintState.Burning ? "熄灭" : "点燃")
                        : (t.Moist ? "晾干" : "湿润");
                    if (b2 != null) b2.text = "伸入";
                }
            }
        }
        if (_burnerBar != null) {
            bool on = v.Burner != null;`);

console.log('补丁处数:', n);
