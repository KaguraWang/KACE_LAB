const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(B, 'utf8');

if (c.includes('重叠对数')) { console.log('- 已存在'); process.exit(0); }

const anchor = '        sb.AppendLine("[TagAudit] 方程式标签数 = " + _eqTags.Count';
if (!c.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = `        /* 标签重叠检查 */
        var liveTags = new List<VesselTag>();
        foreach (var t in _tags) if (t != null && t.gameObject.activeSelf) liveTags.Add(t);
        int lap = 0;
        for (int i = 0; i < liveTags.Count; i++)
            for (int j = i + 1; j < liveTags.Count; j++) {
                var ra = liveTags[i].ScreenRect; var rb = liveTags[j].ScreenRect;
                if (ra.width <= 0f || rb.width <= 0f) continue;
                if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) lap++;
            }
        sb.AppendLine("[TagAudit] 存活标签 " + liveTags.Count + " 个，重叠对数 = " + lap);
        foreach (var t in liveTags)
            sb.AppendLine("[TagAudit]   " + t.gameObject.name + " ExtraLift=" + t.ExtraLift.ToString("F3")
                        + " 屏幕矩形=" + t.ScreenRect);
`;
c = c.replace(anchor, add + anchor);
fs.writeFileSync(B, c, 'utf8');
console.log('✓ 诊断已加');
console.log('  大括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
