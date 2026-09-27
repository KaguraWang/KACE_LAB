const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');

/* 审计里的 TransformPoint 报的是**画布坐标**，画布有 CanvasScaler 缩放，
   必须换成屏幕坐标，否则外部脚本按它点会点偏。 */
const pairs = [
  ['                    var cc = rt.TransformPoint(rt.rect.center);\n                    sb.AppendLine("[CalcAudit] CLICK|" + nm + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)',
   '                    var cc = ScreenOf(rt);\n                    sb.AppendLine("[CalcAudit] CLICK|" + nm + "|" + Mathf.RoundToInt(cc.x) + "|" + Mathf.RoundToInt(cc.y)'],
  ['            var cc = nd.Rt.TransformPoint(nd.Rt.rect.center);',
   '            var cc = ScreenOf(nd.Rt);'],
];
let n = 0;
for (const [a, b] of pairs) {
  if (c.includes(a)) { c = c.replace(a, b); n++; console.log('  ✓ 替换', n); }
  else console.log('  ✗ 锚点未命中:', a.split('\n')[0].trim().substring(0, 60));
}

/* 加 ScreenOf 辅助 */
if (!c.includes('static Vector2 ScreenOf(')) {
  c = c.replace('    public string StateAudit() {',
`    /// <summary>RectTransform 中心点的**屏幕坐标**。
    /// 注意：不能直接用 TransformPoint —— 那是画布坐标，
    /// 画布带 CanvasScaler（这台机器上 scaleFactor ≈ 1.4），
    /// 报出去外部脚本照着点会整片偏掉。 </summary>
    public static Vector2 ScreenOf(RectTransform rt) {
        if (rt == null) return Vector2.zero;
        Vector3 wp = rt.TransformPoint(rt.rect.center);
        var cv = rt.GetComponentInParent<Canvas>();
        if (cv != null && cv.renderMode == RenderMode.ScreenSpaceOverlay)
            return RectTransformUtility.WorldToScreenPoint(null, wp);
        return RectTransformUtility.WorldToScreenPoint(Camera.main, wp);
    }

    public string StateAudit() {`);
  n++;
  console.log('  ✓ ScreenOf 已加');
}
fs.writeFileSync(E, c, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
console.log('改动处数:', n);
