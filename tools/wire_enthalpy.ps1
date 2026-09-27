# 引擎接入焓变：解析表 + 溶解热 + 反应热 + 热容计算
$p = (Resolve-Path 'D:\ChemLabUnity\Assets\Scripts\Chem\ChemEngine.cs').Path
$e = [IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)

# ---- A) Apply：按 ΔH 改变温度（不再用那个假的 heat 跳变做热效应） ----
if ($e -notmatch 'ApplyReactionHeat') {
  $e = $e.Replace('        v.Temp += r.Heat;',
@'
        v.Temp += r.Heat;
        /* 反应焓：ΔT = n·ΔH / (m·c)。m 近似取溶液质量（按水计），c = 4.18 J/(g·K) */
        double dh = ChemData.RuleHeat(r.Eq);
        if (Math.Abs(dh) > 1e-9) ApplyReactionHeat(v, extent, dh);
'@)
  Write-Host 'OK Apply 接焓变'
}

# ---- B) 溶解焓：Introduce 里施加 ----
if ($e -notmatch 'ApplyReactionHeat') { } elseif ($e -notmatch 'ChemData.DissolveHeat') {
  $e = $e.Replace('        if (solubleHere && s.State != "g") {',
@'
        /* 溶解焓：固体溶入水中会吸热或放热（P3 热化学题的核心） */
        double dhDis = ChemData.DissolveHeat(id);
        if (Math.Abs(dhDis) > 1e-9 && inWater && v.Water > 1) ApplyReactionHeat(v, mol, dhDis);

        if (solubleHere && s.State != "g") {
'@)
  Write-Host 'OK 溶解焓已接'
}

# ---- C) 加热辅助函数 ----
if ($e -notmatch 'static void ApplyReactionHeat') {
  $e = $e.Replace('    public static void PourFraction(',
@'
    public const double HeatCapacity = 4.18;   // J/(g·K)，按水溶液近似

    /// <summary>按反应/溶解焓改变温度。ΔT = n·ΔH / (m·c)</summary>
    static void ApplyReactionHeat(Vessel v, double mol, double dhKJ) {
        double mass = Math.Max(1.0, v.Water);      // g
        double dT = mol * dhKJ * 1000.0 / (mass * HeatCapacity);
        v.Temp = Mathf.Clamp(v.Temp + (float)dT, -30f, 1200f);
    }

    /// <summary>按温度变化反算焓变（kJ/mol）—— 供测试反向验证用</summary>
    public static double EnthalpyFromTemp(Vessel v, double dT, double mol) {
        if (mol <= 1e-12) return 0;
        return dT * Math.Max(1.0, v.Water) * HeatCapacity / 1000.0 / mol;
    }

    public static void PourFraction(
'@)
  Write-Host 'OK 加热函数已加'
}
[IO.File]::WriteAllText($p, $e, (New-Object Text.UTF8Encoding($false)))
Write-Host ('ApplyReactionHeat: ' + ($e -match 'static void ApplyReactionHeat'))

# ---- D) ChemCore：解析两张焓变表并提供查询 ----
$c = (Resolve-Path 'D:\ChemLabUnity\Assets\Scripts\Chem\ChemCore.cs').Path
$cc = [IO.File]::ReadAllText($c, [Text.Encoding]::UTF8)
if ($cc -notmatch 'DissolveHeat') {
  $cc = $cc.Replace('    public static readonly Dictionary<string, string> NameToId = new Dictionary<string, string>();',
@'
    public static readonly Dictionary<string, string> NameToId = new Dictionary<string, string>();
    /* ---- 焓变数据（P3 热化学题） ---- */
    static readonly Dictionary<string, double> DissolveH = new Dictionary<string, double>();
    static readonly Dictionary<string, double> RuleH = new Dictionary<string, double>();
'@)
  $cc = $cc.Replace('        /* 中文名 → id，用于把列表里的物质名整体翻成英文 */',
@'
        foreach (var ln in Lines(TblDissolveH)) {
            var c2 = Cols(ln);
            if (c2.Length < 2) continue;
            double dv; if (double.TryParse(c2[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out dv)) DissolveH[c2[0]] = dv;
        }
        foreach (var ln in Lines(TblRuleH)) {
            var c2 = Cols(ln);
            if (c2.Length < 2) continue;
            double dv; if (double.TryParse(c2[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out dv)) RuleH[Norm(c2[0])] = dv;
        }

        /* 中文名 → id，用于把列表里的物质名整体翻成英文 */
'@)
  $cc = $cc.Replace('    static void Index(string key, string id) {',
@'
    /// <summary>把方程式规范化后查表（去掉多余空格）</summary>
    static string Norm(string eq) {
        if (string.IsNullOrEmpty(eq)) return "";
        var parts = eq.Split(' ');
        var sb = new System.Text.StringBuilder();
        foreach (var t in parts) if (t.Length > 0) { if (sb.Length > 0) sb.Append(' '); sb.Append(t); }
        return sb.ToString();
    }

    public static double DissolveHeat(string id) {
        double v; return DissolveH.TryGetValue(id, out v) ? v : 0;
    }
    public static double RuleHeat(string eq) {
        double v; return RuleH.TryGetValue(Norm(eq), out v) ? v : 0;
    }

    static void Index(string key, string id) {
'@)
  [IO.File]::WriteAllText($c, $cc, (New-Object Text.UTF8Encoding($false)))
  Write-Host 'OK ChemCore 已接'
}
Write-Host ('DissolveHeat: ' + ($cc -match 'public static double DissolveHeat'))

$ed = 'D:\Tuanjie\Editor\2022.3.62t15\Editor\Tuanjie.exe'
Remove-Item 'D:\ChemLabUnity\Logs\v.log' -Force -EA SilentlyContinue
& $ed -batchmode -quit -nographics -projectPath 'D:\ChemLabUnity' -logFile 'D:\ChemLabUnity\Logs\v.log' -executeMethod ChemLab.Editor.ChemLabTools.RunTests 2>&1 | Out-Null
Select-String -Path 'D:\ChemLabUnity\Logs\v.log' -Pattern 'error CS' -EA SilentlyContinue | Select-Object -Unique -First 8 | ForEach-Object { "  " + $_.Line.Trim() }