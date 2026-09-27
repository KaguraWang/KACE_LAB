const fs = require('fs');
let n = 0;

/* 1) 暴露聚焦高亮的强度，供测试读状态 —— 菲涅尔辉光太淡，像素判据不稳 */
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');
if (!l.includes('public float TestFocusGlow')) {
  l = l.replace('    public Material TestGlassMat => _glassMat;',
`    public Material TestGlassMat => _glassMat;
    /// <summary>当前聚焦高亮强度，给窗口测试当客观证据（辉光太淡，像素判据不稳）</summary>
    public float TestFocusGlow => (_glassMat != null && _glassMat.HasProperty("_HighlightAmt"))
                                ? _glassMat.GetFloat("_HighlightAmt") : -1f;`);
  fs.writeFileSync(L, l, 'utf8');
  n++;
  console.log('  ✓ TestFocusGlow 已加');
}

/* 2) 审计里报出来 */
l = fs.readFileSync(L, 'utf8');
if (!l.includes('PET|focusGlow')) {
  l = l.replace('        sb.AppendLine("PET|aiUnlocked|" + (PetProgress.AiUnlocked ? "yes" : "no"));',
`        sb.AppendLine("PET|aiUnlocked|" + (PetProgress.AiUnlocked ? "yes" : "no"));
        /* 聚焦高亮强度：0 = 没聚焦，>0 = 有器皿被聚焦 */
        float maxGlow = 0f; int glowCount = 0;
        foreach (var vv in _views) {
            if (vv == null) continue;
            float g0 = vv.TestFocusGlow;
            if (g0 > 0.01f) { glowCount++; if (g0 > maxGlow) maxGlow = g0; }
        }
        sb.AppendLine("PET|glow|" + glowCount + "|" + maxGlow.ToString("F2"));`);
  fs.writeFileSync(L, l, 'utf8');
  n++;
  console.log('  ✓ PET|glow 已加');
}

/* 3) uitest 的第 4 步改成读状态 */
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');
const oldA = t.indexOf("Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='");
if (oldA < 0) { console.log('✗ 找不到第 4 步'); process.exit(1); }
const oldB = t.indexOf("Write-Host '=== 5. 打开焓变计算器");
if (oldB < 0) { console.log('✗ 找不到第 5 步'); process.exit(1); }
console.log('第 4 步在第', t.substring(0, oldA).split('\n').length, '行');

const nw = [
  "Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='",
  "# 用状态判定而不是像素：聚焦高亮走的是玻璃的菲涅尔辉光，本来就淡，",
  "# 像素判据实测在 1.4~4.5 之间跳，时过时不过。",
  "function Glow {",
  "    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue",
  "    $mm = [regex]::Matches($tt, 'PET\\|glow\\|(\\d+)\\|([0-9.]+)')",
  "    if ($mm.Count -eq 0) { return '' }",
  "    return $mm[$mm.Count - 1].Value",
  "}",
  "Write-Host \"  点之前: $(Glow)\"",
  "Click-At $h 1073 864 'Left' 900      # 点台面上的器皿",
  "Start-Sleep -Milliseconds 2000",
  "$g1 = Glow",
  "Write-Host \"  点器皿后: $g1\"",
  "Check '点器皿后出现高亮（glow 数 > 0）' ($g1 -match '\\|([1-9]\\d*)\\|') \"$g1\"",
  "Shot $h \"$out\\31-focused.png\" | Out-Null",
  "Click-At $h 1600 1440 'Left' 900     # 点空白",
  "Start-Sleep -Milliseconds 2000",
  "$g2 = Glow",
  "Write-Host \"  点空白后: $g2\"",
  "Check '点空白后高亮消失（glow 数 = 0）' ($g2 -match '\\|0\\|') \"$g2\"",
  "Shot $h \"$out\\32-unfocused.png\" | Out-Null",
  '',
  '',
];
t = t.substring(0, oldA) + nw.join('\n') + t.substring(oldB);
fs.writeFileSync(T, t, 'utf8');
n++;
console.log('  ✓ uitest 第 4 步已改为状态判定');
console.log('改动处数:', n);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
