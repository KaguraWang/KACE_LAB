const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('eqDemo')) { console.log('已存在'); process.exit(0); }

const old = '    void SeedDemo() {';
const nw = `    /// <summary>命令行 -eqDemo 时用的验证场景：一只烧杯放正中，触发一个会放气的反应，
    /// 便于截图核对弹出的方程式标签（连接符、条件、上下标）。</summary>
    void SeedEqDemo() {
        var b = Lab.AddVessel(VesselKind.Beaker);
        b.AddWater(120);
        Lab.AddReagent(b, "na2co3", 3.0);
        Lab.AddLiquid(b, "hcl", 12);
        Lab.SetHeat(b, true);
        EqTagSettings.Duration = 120f;      // 截图期间不消失
        Lab.Log.Clear();
        Lab.Note(null, "方程式标签验证场景", "setup");
    }

    void SeedDemo() {`;

if (!c.includes(old)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(old, nw);

/* 在 EnsureInit 里按命令行选择场景 */
const seed = c.indexOf('        SeedDemo();');
if (seed < 0) { console.log('✗ 找不到 SeedDemo 调用'); process.exit(1); }
c = c.slice(0, seed) +
`        bool eqDemo = false;
        foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-eqDemo") eqDemo = true;
        if (eqDemo) SeedEqDemo(); else SeedDemo();` +
    c.slice(seed + '        SeedDemo();'.length);

fs.writeFileSync(p, c, 'utf8');
console.log('✓ -eqDemo 验证场景已加');
console.log('  SeedEqDemo:', /void SeedEqDemo\(\)/.test(c));
console.log('  调用分支:', /if \(eqDemo\) SeedEqDemo\(\); else SeedDemo\(\);/.test(c));
