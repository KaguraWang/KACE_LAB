const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(B, 'utf8');

const a = c.indexOf('    public string TagAudit() {');
if (a < 0) { console.log('✗ 找不到 TagAudit'); process.exit(1); }
/* 找到方法结束 */
let depth = 0, started = false, end = -1;
for (let i = a; i < c.length; i++) {
  if (c[i] === '{') { depth++; started = true; }
  if (c[i] === '}') { depth--; if (started && depth === 0) { end = i; break; } }
}
console.log('TagAudit 范围', a, '..', end);
console.log('--- 旧实现（前 40 行）---');
console.log(c.slice(a, end + 1).split('\n').slice(0, 40).join('\n'));

const nw = `    public string TagAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[TagAudit] ShowTags = " + TagSettings.ShowTags);
        sb.AppendLine("[TagAudit] _tagRoot = " + (_tagRoot == null ? "null！" : _tagRoot.name
                    + " active=" + _tagRoot.gameObject.activeInHierarchy));
        sb.AppendLine("[TagAudit] 容器数 = " + Lab.Vessels.Count + "  标签视图数 = " + _tags.Count);

        var liveTags = new List<VesselTag>();
        foreach (var lt in _tags) if (lt != null && lt.gameObject.activeSelf) liveTags.Add(lt);

        int lap = 0;
        for (int i = 0; i < liveTags.Count; i++)
            for (int j = i + 1; j < liveTags.Count; j++) {
                var ra = liveTags[i].ScreenRect; var rb = liveTags[j].ScreenRect;
                if (ra.width <= 0f || rb.width <= 0f) continue;
                if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) lap++;
            }

        sb.AppendLine("[TagAudit] 存活标签 " + liveTags.Count + " 个，重叠对数 = " + lap);
        foreach (var lt in liveTags) {
            sb.AppendLine("[TagAudit]   " + lt.gameObject.name
                        + " ExtraLift=" + lt.ExtraLift.ToString("F3")
                        + " 屏幕矩形=" + lt.ScreenRect);
            sb.AppendLine("[TagAudit]      trace: " + lt.LastSyncTrace);
        }
        sb.AppendLine("[TagAudit] 方程式标签数 = " + _eqTags.Count
                    + "  Active = " + EquationTag.Active.Count);
        return sb.ToString();
    }`;

c = c.slice(0, a) + nw + c.slice(end + 1);
fs.writeFileSync(B, c, 'utf8');
console.log('✓ TagAudit 已整段重写');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
