const fs = require('fs');
let n = 0;

/* ---- LabUI：加射线检测接口 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestRaycastAt')) {
  const a = '    public string TestCat => _cat;';
  const add = `    /// <summary>射线检测：模拟 EventSystem 在按钮中心会命中谁。
    /// 这是判断「按钮为什么点不动」最直接的办法 ——
    /// 程序化 onClick.Invoke() 绕过了射线，测不出被遮挡的问题。 </summary>
    public string TestRaycastAt(string buttonNamePart) {
        var gr = _canvas.GetComponent<GraphicRaycaster>();
        if (gr == null) return "✗ 画布上没有 GraphicRaycaster";
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) {
            var ego = new GameObject("EventSystem_Test");
            es = ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }
        Button target = null;
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name.Contains(buttonNamePart)) { target = b; break; }
        if (target == null) return "✗ 找不到按钮 " + buttonNamePart;

        var rt = (RectTransform)target.transform;
        Vector3 world = rt.TransformPoint(rt.rect.center);
        var ped = new UnityEngine.EventSystems.PointerEventData(es);
        ped.position = new Vector2(world.x, world.y);   // Overlay 画布下世界坐标即屏幕坐标

        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        gr.Raycast(ped, results);
        if (results.Count == 0) return $"按钮「{target.name}」在 {ped.position} 处**射线打不到任何东西**";

        var top = results[0].gameObject;
        bool ok = top == target.gameObject || top.transform.IsChildOf(target.transform);
        return ok
            ? $"✓ 按钮「{target.name}」可点（命中最上层 = {top.name}）"
            : $"✗ 按钮「{target.name}」被「{top.name}」挡住（层级路径 {Path(top.transform)}）";
    }
    static string Path(Transform t) {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    public string TestCat => _cat;`;
  if (u.includes(a)) { u = u.replace(a, add); n++; console.log('  ✓ TestRaycastAt 已加'); }
  else console.log('  ✗ 锚点');
  fs.writeFileSync(U, u, 'utf8');
}

/* ---- 自检：对试剂架的按钮做射线检测 ---- */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('被挡住')) {
  const a2 = '                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);';
  const add2 = `                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);

                    /* --- 射线检测：这些按钮真实鼠标点得到吗 --- */
                    string[] targets = { "Btn_酸碱指示剂", "Btn_金属", "Btn_少量", "Btn_适量" };
                    foreach (var tn in targets) {
                        var line = lui.TestRaycastAt(tn);
                        Say("   " + line);
                        if (line.StartsWith("✗")) { ov++; }
                    }
                    TCheck("试剂架上的按钮都能被射线命中（没被别的控件挡住）", ov == 0);`;
  if (sc.includes(a2)) { sc = sc.replace(a2, add2); n++; console.log('  ✓ 射线自检已加'); }
  else console.log('  ✗ 锚点2');
  fs.writeFileSync(S, sc, 'utf8');
}
console.log('补丁处数:', n);
