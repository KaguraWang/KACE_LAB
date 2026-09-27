/* =====================================================================
   EventSystem 改到 LabBootstrap.EnsureInit 里建
     诊断结果：EventSystem.current = null —— 整个场景没有输入分发，
     所以计算器点不动、连阿侬也点不动。

     它原本在 LabUI.Build() 里创建。LabUI 一旦被 SetActive(false)
     （进计算器时要藏实验台界面），挂在它下面的 EventSystem 就一起没了。
     上一次我把它改挂 transform.root，但根因是「它的生死跟着 UI 走」这件事本身。

     现在直接在 EnsureInit 里建，和任何 UI 都解耦。
   ===================================================================== */
const fs = require('fs');
let n = 0;

/* 1) LabBootstrap.EnsureInit 里建 EventSystem */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('EnsureEventSystem')) {
  b = b.replace('        BuildCamera();',
`        EnsureEventSystem();
        BuildCamera();`);
  b = b.replace('    void BuildCamera() {',
`    /// <summary>保证场景里有且只有一个 EventSystem。
    /// 它必须独立于任何界面存在 —— 否则藏起某个界面就会连带把输入分发一起关掉。 </summary>
    static void EnsureEventSystem() {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        foreach (var e in UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>()) {
            if (e != null && e.gameObject.activeInHierarchy) return;
        }
        var ego = new GameObject("EventSystem");
        ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
        ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Debug.Log("[ChemLab] 已创建 EventSystem");
    }

    void BuildCamera() {`);
  fs.writeFileSync(B, b, 'utf8'); n++;
  console.log('  ✓ EnsureEventSystem 已加');
}

/* 2) 去掉 LabUI 里那份（避免重复） */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (u.includes('var es = UnityEngine.Object.FindObjectOfType<EventSystem>();')) {
  u = u.replace(`        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null) {
            /* 关键：EventSystem **不能**做 LabUI 的子节点。
               进焓变计算器时要把 LabUI 整个 SetActive(false)，
               挂在它下面的话 EventSystem 会被一起禁用 ——
               结果就是计算器页面所有控件都收不到输入（点不动、打不了字）。 */
            var ego = new GameObject("EventSystem");
            ego.transform.SetParent(transform.root, false);   // 挂到场景根
            ego.AddComponent<EventSystem>();
            ego.AddComponent<StandaloneInputModule>();
        }
`, `        /* EventSystem 由 LabBootstrap.EnsureEventSystem 统一负责创建 ——
           放在这里的话，LabUI 被 SetActive(false) 时会连带把输入分发一起关掉。 */
`);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('  ✓ LabUI 里那份已移除');
}

console.log('改动处数:', n);
