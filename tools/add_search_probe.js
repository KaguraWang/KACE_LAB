const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

if (u.includes('ProbeSearch')) { console.log('- 已存在'); process.exit(0); }

const anchor = '    public string TestCat => _cat;';
if (!u.includes(anchor)) { console.log('✗ 找不到 TestCat'); process.exit(1); }

const add = [
  '    /// <summary>诊断：搜索框那里到底有什么在挡着。</summary>',
  '    public string ProbeSearch() {',
  '        var sb = new System.Text.StringBuilder();',
  '        if (_searchField == null) { sb.AppendLine("PROBE|搜索框引用为空"); return sb.ToString(); }',
  '        var srt = (RectTransform)_searchField.transform;',
  '        var center = RectTransformUtility.WorldToScreenPoint(null, srt.TransformPoint(srt.rect.center));',
  '        sb.AppendLine("PROBE|搜索框中心屏幕坐标|" + Mathf.RoundToInt(center.x) + "," + Mathf.RoundToInt(center.y));',
  '        sb.AppendLine("PROBE|搜索框世界矩形|" + srt.rect);',
  '        sb.AppendLine("PROBE|raycastTarget 情况:");',
  '        foreach (var g in srt.GetComponentsInChildren<Graphic>(true)) {',
  '            sb.AppendLine("PROBE|   " + g.gameObject.name + " (" + g.GetType().Name + ") raycast=" + g.raycastTarget',
  '                        + " active=" + g.gameObject.activeInHierarchy);',
  '        }',
  '        /* 用 EventSystem 的射线看看这个点上命中了谁 */',
  '        var es = UnityEngine.EventSystems.EventSystem.current;',
  '        if (es != null) {',
  '            var ped = new UnityEngine.EventSystems.PointerEventData(es);',
  '            ped.position = center;',
  '            var res = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();',
  '            foreach (var cv in UnityEngine.Object.FindObjectsOfType<Canvas>()) {',
  '                var gr = cv.GetComponent<GraphicRaycaster>();',
  '                if (gr == null || !gr.enabled || !cv.gameObject.activeInHierarchy) continue;',
  '                gr.Raycast(ped, res);',
  '            }',
  '            sb.AppendLine("PROBE|该点射线命中 " + res.Count + " 个:");',
  '            for (int i = 0; i < Mathf.Min(6, res.Count); i++)',
  '                sb.AppendLine("PROBE|   " + i + ": " + res[i].gameObject.name',
  '                            + " 画布=" + (res[i].module is GraphicRaycaster ? "有" : "?")',
  '                            + " 深度=" + res[i].depth + " 排序=" + res[i].sortingOrder);',
  '        }',
  '        return sb.ToString();',
  '    }',
  '',
  anchor,
].join('\n');

u = u.replace(anchor, add);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ ProbeSearch 已加');

/* 接到 StateTick */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('ProbeSearch')) {
  const a = '            if (_ui != null) Debug.Log("[ReagentState]\\n" + _ui.ReagentAudit());';
  if (b.includes(a)) {
    b = b.replace(a, a + '\n            if (_ui != null) Debug.Log("[SearchProbe]\\n" + _ui.ProbeSearch());');
    fs.writeFileSync(B, b, 'utf8');
    console.log('  ✓ 已接进 StateTick');
  } else console.log('  ✗ ReagentState 锚点未命中');
}
console.log('  LabUI 括号差:', (fs.readFileSync(U,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(U,'utf8').match(/\}/g)||[]).length);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
