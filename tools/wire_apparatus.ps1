# 把器材接入 LabBootstrap：建模型、吸附、抬升、火焰、每帧同步
$p = (Resolve-Path 'D:\ChemLabUnity\Assets\Scripts\Game\LabBootstrap.cs').Path
$b = [IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)
$n = 0

# ---- 1) 字段 ----
if ($b -notmatch 'List<ApparatusView> _apViews') {
  $b = $b.Replace('    Material _fxSoft, _fxAdd;',
@'
    Material _fxSoft, _fxAdd;
    Material _metalMat, _glowMat, _flameMat;
    readonly List<ApparatusView> _apViews = new List<ApparatusView>();
'@)
  $n++
}

# ---- 2) 材质：金属 / 高亮 / 火焰 ----
if ($b -notmatch '_metalMat = MatFromRes') {
  $b = $b.Replace('        _benchMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");',
@'
        _benchMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");

        /* 器材用同一种「哑光金属」着色器，只是颜色不同 */
        _metalMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _metalMat.SetColor("_Color", new Color(0.20f, 0.21f, 0.23f));
        _glowMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _glowMat.SetColor("_Color", new Color(0.25f, 0.95f, 0.55f));   // 吸附提示：亮绿
        _flameMat = MatFromRes("Particle", "ChemLab/Particle", "Particles/Standard Unlit");
        if (_flameMat.HasProperty("_Color")) _flameMat.SetColor("_Color", new Color(1f, 0.72f, 0.28f, 0.82f));
        if (_flameMat.HasProperty("_SrcBlend")) _flameMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (_flameMat.HasProperty("_DstBlend")) _flameMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (_flameMat.HasProperty("_Softness")) _flameMat.SetFloat("_Softness", 1.6f);
'@)
  $n++
}

# ---- 3) 建器材 ----
if ($b -notmatch 'void BuildApparatus') {
  $b = $b.Replace('    void BuildCamera() {',
@'
    /// <summary>台面上的器材：一盏本生灯 + 一个三脚架</summary>
    void BuildApparatus() {
        ApparatusSet.Clear();
        var burner = ApparatusSet.Add(ApparatusKind.BunsenBurner, new Vector3(-2.55f, 0f, 0.95f));
        var tripod = ApparatusSet.Add(ApparatusKind.Tripod, new Vector3(2.55f, 0f, 0.95f));
        foreach (var a in ApparatusSet.All) {
            var av = ApparatusView.Create(a, transform, _metalMat, _glowMat, _flameMat);
            av.Sync();
            _apViews.Add(av);
        }
    }

    void BuildCamera() {
'@)
  $n++
}

# ---- 4) EnsureInit 里调用 ----
if ($b -notmatch 'BuildApparatus\(\);') {
  $b = $b.Replace('        BuildBench();', "        BuildBench();`r`n        BuildApparatus();")
  $n++
}

# ---- 5) SyncViews：应用抬升高度 + 同步器材 ----
$b = $b.Replace('            vv.transform.localPosition = vv.Model.Pos;',
                '            vv.transform.localPosition = vv.Model.Pos + Vector3.up * vv.Model.LiftY;')
if ($b -match 'Vector3\.up \* vv\.Model\.LiftY') { $n++ }

if ($b -notmatch 'foreach \(var av in _apViews\) av.Sync\(\);') {
  $b = $b.Replace('        CollectReagents();', '        CollectReagents();')
  # 在 SyncViews 里容器循环之后补上器材同步
  $b = $b.Replace(@'
                _views.RemoveAt(i);
            }
        }
'@, @'
                _views.RemoveAt(i);
            }
        }
        foreach (var av in _apViews) if (av != null) av.Sync();
'@)
  $n++
}

# ---- 6) Update：推进器材（加热） ----
if ($b -notmatch 'ApparatusSet\.Tick') {
  $b = $b.Replace('        Lab.Tick(Time.deltaTime);', "        Lab.Tick(Time.deltaTime);`r`n        ApparatusSet.Tick(Time.deltaTime);")
  $n++
}
[IO.File]::WriteAllText($p, $b, (New-Object Text.UTF8Encoding($false)))
Write-Host "已应用 $n 处"
Write-Host ('BuildApparatus: ' + ($b -match 'void BuildApparatus'))
Write-Host ('器材同步: ' + ($b -match 'foreach \(var av in _apViews\)'))
Write-Host ('Tick: ' + ($b -match 'ApparatusSet\.Tick'))
Write-Host ('LiftY: ' + ($b -match 'Vector3\.up \* vv\.Model\.LiftY'))