const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');
if (!c.includes('AuditInput')) {
  c = c.replace('    /// <summary>最近一次求解是否成功（教程用它判断用户点没点对）</summary>',
`    /* ================================================================
       输入诊断：计算器打开后调它，把「为什么点不动」直接打出来
         · EventSystem 还在不在、是不是被禁用了
         · 场景里有哪些画布、排序号、有没有 GraphicRaycaster
         · 在屏幕中心做一次真实射线，看命中谁
       ================================================================ */
    public string AuditInput() {
        var sb = new System.Text.StringBuilder();
        var es = UnityEngine.EventSystems.EventSystem.current;
        sb.AppendLine("[InputAudit] EventSystem.current = " + (es == null ? "null！" : es.gameObject.name));
        if (es != null) {
            sb.AppendLine("[InputAudit]   activeInHierarchy = " + es.gameObject.activeInHierarchy
                        + "  enabled = " + es.enabled
                        + "  父节点 = " + (es.transform.parent != null ? es.transform.parent.name : "(根)"));
            var im = es.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
            sb.AppendLine("[InputAudit]   输入模块 = " + (im == null ? "没有！" : im.GetType().Name
                        + (im.enabled ? " (启用)" : " (禁用！)")));
        }
        foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
            var gr = cv.GetComponent<GraphicRaycaster>();
            sb.AppendLine("[InputAudit] 画布 " + cv.gameObject.name
                + " 模式=" + cv.renderMode + " 排序=" + cv.sortingOrder
                + " 激活=" + cv.gameObject.activeInHierarchy
                + " Raycaster=" + (gr == null ? "无" : (gr.enabled ? "有" : "有但禁用")));
        }
        /* 在屏幕中心做一次真实射线 */
        if (es != null) {
            var ped = new UnityEngine.EventSystems.PointerEventData(es);
            ped.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var res = new List<UnityEngine.EventSystems.RaycastResult>();
            foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
                var gr = cv.GetComponent<GraphicRaycaster>();
                if (gr == null || !gr.enabled || !cv.gameObject.activeInHierarchy) continue;
                gr.Raycast(ped, res);
            }
            sb.AppendLine("[InputAudit] 屏幕中心射线命中 " + res.Count + " 个：");
            for (int i = 0; i < Mathf.Min(5, res.Count); i++)
                sb.AppendLine("[InputAudit]   " + i + ": " + res[i].gameObject.name
                            + " (depth " + res[i].depth + ", sortingOrder " + res[i].sortingOrder + ")");
        }
        return sb.ToString();
    }

    /// <summary>最近一次求解是否成功（教程用它判断用户点没点对）</summary>`);
  fs.writeFileSync(E, c, 'utf8');
  console.log('✓ AuditInput 已加');
}

/* 打开计算器后延迟打一次 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('AuditInput')) {
  b = b.replace('            if (arg == "-calcDemo") {\n                var cs = EnthalpyScene.Show(gameObject, LabUI.UiFont);',
`            if (arg == "-calcDemo") {
                var cs = EnthalpyScene.Show(gameObject, LabUI.UiFont);
                if (cs != null) Invoke(nameof(DumpInputAudit), 3f);`);
  b = b.replace('    void UpdateCamera(float dt) {',
`    void DumpInputAudit() {
        var s = EnthalpyScene.Instance;
        if (s != null) Debug.Log(s.AuditInput());
    }

    void UpdateCamera(float dt) {`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('✓ 打开计算器 3 秒后自动 dump');
}
