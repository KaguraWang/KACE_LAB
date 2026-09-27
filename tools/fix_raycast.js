const fs = require('fs');
let n = 0;

/* ---- 1) CreateForTest 要建一个和运行时一致的画布 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
const oldCreate = u.slice(u.indexOf('    /// <summary>建一个只用来测试的界面实例</summary>'),
                          u.indexOf('    public string TestCat => _cat;'));
const newCreate = `    /// <summary>建一个只用来测试的界面实例。
    /// 注意：必须配上 CanvasScaler 与参考分辨率 ——
    /// 否则画布尺寸是默认值，控件世界坐标全错，射线测试毫无意义。 </summary>
    public static LabUI CreateForTest() {
        if (LabUI.UiFont == null) LabUI.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var ego = new GameObject("EventSystem_Test");
            ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
            ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        var go = new GameObject("LabUI_Test");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(DesignW, DesignH);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var ui = go.AddComponent<LabUI>();
        ui.UiLayer = 31;
        ui.Build();
        Canvas.ForceUpdateCanvases();
        return ui;
    }

`;
if (u.includes('    /// <summary>建一个只用来测试的界面实例</summary>')) {
  u = u.replace(oldCreate, newCreate);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('  ✓ CreateForTest 已配好画布');
} else console.log('  ✗ 锚点1');

/* ---- 2) 射线测试：坐标换算正确 + 用真实按钮名 ---- */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace('string[] targets = { "Btn_酸碱指示剂", "Btn_金属", "Btn_少量", "Btn_适量" };',
                'string[] targets = { "Btn_金属", "Btn_氧化物", "Btn_少量", "Btn_适量", "Btn_过量" };');
fs.writeFileSync(S, sc, 'utf8'); n++;
console.log('  ✓ 按钮名已修正');

/* ---- 3) TestRaycastAt 用正确的屏幕坐标 ---- */
u = fs.readFileSync(U, 'utf8');
const oldRay = u.slice(u.indexOf('    /// <summary>射线检测：模拟 EventSystem'),
                       u.indexOf('    static string Path(Transform t)'));
const newRay = `    /// <summary>射线检测：模拟 EventSystem 在按钮中心会命中谁。
    /// 这是判断「按钮为什么点不动」最直接的办法 ——
    /// 程序化 onClick.Invoke() 绕过了射线，测不出被遮挡的问题。 </summary>
    public string TestRaycastAt(string buttonNamePart) {
        var gr = _canvas.GetComponent<GraphicRaycaster>();
        if (gr == null) return "✗ 画布上没有 GraphicRaycaster（画布尺寸可能是 0）";
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return "✗ 场景里没有 EventSystem";

        Button target = null;
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == buttonNamePart) { target = b; break; }
        if (target == null) return "✗ 找不到按钮 " + buttonNamePart;

        Canvas.ForceUpdateCanvases();
        var rt = (RectTransform)target.transform;
        /* ScreenSpaceOverlay 下世界坐标就是屏幕坐标，
           但要先把画布的缩放算进去 —— 用 Camera.main == null 的换算函数最稳 */
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));

        var ped = new UnityEngine.EventSystems.PointerEventData(es);
        ped.position = screen;
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        gr.Raycast(ped, results);
        if (results.Count == 0)
            return "✗ 按钮「" + target.name + "」在屏幕 " + screen + " 处射线打不到任何东西";

        var top = results[0].gameObject;
        bool ok = top == target.gameObject || top.transform.IsChildOf(target.transform);
        return ok
            ? "✓ 按钮「" + target.name + "」可点"
            : "✗ 按钮「" + target.name + "」被「" + top.name + "」挡住（路径 " + Path(top.transform) + "）";
    }

`;
if (u.includes('    /// <summary>射线检测：模拟 EventSystem')) {
  u = u.replace(oldRay, newRay);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('  ✓ TestRaycastAt 已改');
} else console.log('  ✗ 锚点3');

console.log('补丁处数:', n);
