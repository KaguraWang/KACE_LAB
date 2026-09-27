const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';

/* ---- 1) 修重叠：检验器材 138-164、容器标签 172-234 ---- */
P(U, `        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 138), new Vector2(-12, 222), new Color(0,0,0,0));`,
`        /* 容器标签区 —— 与检验器材区分开，不能重叠 */
        _tagBar = Node("TagBar", info, new Vector2(0,0), new Vector2(1,0),
                       new Vector2(12, 172), new Vector2(-12, 234), new Color(0,0,0,0));`);

/* ---- 2) 字段 ---- */
P(U, '    Button _tagToggle, _tagMode, _tagName, _tagAmt, _tagConc;',
`    Button _tagToggle, _tagMode, _tagName, _tagAmt, _tagConc;
    /* 方程式弹出标签的控制 */
    RectTransform _eqBar;
    Button _eqShow, _eqIonic;
    InputField _eqDur;`);

/* ---- 3) 构建方程式标签控制区 ---- */
P(U, '        _tagBar.gameObject.SetActive(false);',
`        _tagBar.gameObject.SetActive(false);

        /* ---- 反应方程式标签控制区 ---- */
        _eqBar = Node("EqBar", info, new Vector2(0,0), new Vector2(1,0),
                      new Vector2(12, 242), new Vector2(-12, 312), new Color(0,0,0,0));
        var el = Label(_eqBar, "方程式标签", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)el.transform).sizeDelta = new Vector2(74, 26);
        _eqShow  = MakeEqBtn("显示 开", () => { EqTagSettings.Show = !EqTagSettings.Show; }, 78, 2, 74);
        _eqIonic = MakeEqBtn("离子式 关", () => { EqTagSettings.Ionic = !EqTagSettings.Ionic; }, 0, 32, 74);
        /* 时长：数字输入框（按要求放在界面上，不放进标签里） */
        var dl = Label(_eqBar, "时长", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), new Vector2(78, 30), new Vector2(106, 56));
        _eqDur = MakeNumField(_eqBar, new Vector2(112, 30), new Vector2(56, 26), EqTagSettings.Duration,
                              v => { EqTagSettings.Duration = Mathf.Clamp(v, 1f, 120f); });
        var ul = Label(_eqBar, "秒", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), new Vector2(172, 30), new Vector2(200, 56));
        _eqBar.gameObject.SetActive(false);`);

/* ---- 4) 两个小工厂 ---- */
P(U, '    /// <summary>标签区用的小按钮：Btn 会撑满父节点，所以先摆好小容器</summary>',
`    Button MakeEqBtn(string label, System.Action act, float x, float y, float w) {
        var slot = Node("EQ_" + label, _eqBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(78 + x, y);
        r.sizeDelta = new Vector2(w, 24);
        return Btn(slot, label, act, BG2, 11);
    }

    /// <summary>数字输入框 —— 显示时长用。InputField 要有背景图和一个文本子节点。</summary>
    InputField MakeNumField(Transform parent, Vector2 pos, Vector2 size, float initial, System.Action<float> onChange) {
        var go = new GameObject("NumField");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0,0); rt.anchorMax = new Vector2(0,0);
        rt.pivot = new Vector2(0,0);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.16f, 0.24f, 1f);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.font = _font; txt.fontSize = 12;
        txt.color = INK;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.supportRichText = false;
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(3,0); trt.offsetMax = new Vector2(-3,0);

        var f = go.AddComponent<InputField>();
        f.targetGraphic = img;
        f.textComponent = txt;
        f.contentType = InputField.ContentType.DecimalNumber;
        f.characterLimit = 5;
        f.text = initial.ToString("0.#");
        f.onEndEdit.AddListener(s => {
            float v;
            if (float.TryParse(s, out v)) { onChange(v); f.text = EqTagSettings.Duration.ToString("0.#"); }
            else f.text = EqTagSettings.Duration.ToString("0.#");
        });
        return f;
    }

    /// <summary>标签区用的小按钮：Btn 会撑满父节点，所以先摆好小容器</summary>`);

/* ---- 5) 刷新时同步按钮文字与输入框 ---- */
P(U, '        if (_tagBar != null) {\n            _tagBar.gameObject.SetActive(true);\n            RefreshTagButtons();\n        }',
`        if (_tagBar != null) {
            _tagBar.gameObject.SetActive(true);
            RefreshTagButtons();
        }
        if (_eqBar != null) {
            _eqBar.gameObject.SetActive(true);
            var e1 = _eqShow  != null ? _eqShow.GetComponentInChildren<Text>()  : null;
            var e2 = _eqIonic != null ? _eqIonic.GetComponentInChildren<Text>() : null;
            if (e1 != null) e1.text = EqTagSettings.Show ? "显示 开" : "显示 关";
            if (e2 != null) e2.text = EqTagSettings.Ionic ? "离子式 开" : "离子式 关";
            if (_eqDur != null && !_eqDur.isFocused)
                _eqDur.text = EqTagSettings.Duration.ToString("0.#");
        }`);

console.log('UI 补丁处数:', n);
