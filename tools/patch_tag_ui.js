const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';

/* ---- 1) 字段 ---- */
P(U, '    Button _testBtn1, _testBtn2;',
`    Button _testBtn1, _testBtn2;
    /* 容器标签的控制 */
    RectTransform _tagBar;
    Button _tagToggle, _tagMode, _tagName, _tagAmt, _tagConc;`);

/* ---- 2) 构建控件 ---- */
P(U, '        _testBar.gameObject.SetActive(false);',
`        _testBar.gameObject.SetActive(false);

        /* ---- 容器标签控制区 ---- */
        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 138), new Vector2(-12, 222), new Color(0,0,0,0));
        var tl = Label(_tagBar, "容器标签", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)tl.transform).sizeDelta = new Vector2(74, 26);
        MakeTagBtn("标签", () => { TagSettings.ShowTags = !TagSettings.ShowTags; }, 0, 2);
        MakeTagBtn("具体/离子", () => { TagSettings.Molecular = !TagSettings.Molecular; }, 78, 2);
        MakeTagBtn("物质", () => { TagSettings.ShowName = !TagSettings.ShowName; }, 0, 26);
        MakeTagBtn("mol", () => { TagSettings.ShowAmount = !TagSettings.ShowAmount; }, 78, 26);
        MakeTagBtn("浓度", () => { TagSettings.ShowConc = !TagSettings.ShowConc; }, 156, 26);
        _tagBar.gameObject.SetActive(false);`);

/* ---- 3) 小按钮工厂 + 状态刷新 ---- */
P(U, '    void CycleTest() {',
`    /// <summary>标签区用的小按钮：Btn 会撑满父节点，所以先摆好小容器</summary>
    Button MakeTagBtn(string label, System.Action act, float x, float y) {
        var slot = Node("TB_" + label, _tagBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(78 + x, y);
        r.sizeDelta = new Vector2(72, 22);
        return Btn(slot, label, act, BG2, 11);
    }

    /// <summary>按钮文字随开关状态变，让用户看得出当前是什么</summary>
    void RefreshTagButtons() {
        if (_tagBar == null) return;
        void Set(Button b, string txt) {
            if (b == null) return;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = txt;
        }
        int k = 0;
        var kids = _tagBar.GetComponentsInChildren<Button>();
        foreach (var b in kids) {
            switch (k++) {
                case 0: Set(b, TagSettings.ShowTags ? "标签 ✓" : "标签 ✗"); break;
                case 1: Set(b, TagSettings.Molecular ? "具体物质" : "离子状态"); break;
                case 2: Set(b, TagSettings.ShowName ? "物质 ✓" : "物质 ✗"); break;
                case 3: Set(b, TagSettings.ShowAmount ? "mol ✓" : "mol ✗"); break;
                case 4: Set(b, TagSettings.ShowConc ? "浓度 ✓" : "浓度 ✗"); break;
            }
        }
    }

    void CycleTest() {`);

/* ---- 4) 刷新时显示 ---- */
P(U, '        if (_testBar != null) {\n            bool showTest = TestSet.All.Count > 0;',
`        if (_tagBar != null) {
            _tagBar.gameObject.SetActive(true);
            RefreshTagButtons();
        }
        if (_testBar != null) {
            bool showTest = TestSet.All.Count > 0;`);

console.log('UI 补丁处数:', n);
