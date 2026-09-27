const fs = require('fs');
let n = 0;

/* 1) 自检：无头环境没有 LabBootstrap，改成可识别地跳过 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
const oldA = `                        var tagHost = ChemLab.Game.LabBootstrap.Instance;
                        int tagOk = 0, tagBad = 0;
                        if (tagHost != null) {`;
const newA = `                        var tagHost = ChemLab.Game.LabBootstrap.Instance;
                        int tagOk = 0, tagBad = 0;
                        /* 无头批处理不跑游戏循环，也就没有 LabBootstrap —— 真实场景
                           （标签有没有显示在画面上）由窗口级测试用 -tagAudit 判定，
                           这里只在有宿主时才查。 */
                        if (tagHost == null) Say("   （无头环境没有 LabBootstrap，标签的显示位置改由窗口测试 -tagAudit 验证）");
                        if (tagHost != null) {`;
if (sc.includes(oldA)) { sc = sc.replace(oldA, newA); fs.writeFileSync(S, sc, 'utf8'); n++; console.log('  ✓ 自检加了跳过说明'); }
else console.log('  ✗ A 锚点');

sc = fs.readFileSync(S, 'utf8');
const oldB = 'TCheck("容器标签都真的显示出来了（不是只有 active）", tagBad == 0 && tagOk > 0);';
const newB = 'TCheck("容器标签都真的显示出来了（不是只有 active）", tagHost == null || (tagBad == 0 && tagOk > 0));';
if (sc.includes(oldB)) { sc = sc.replace(oldB, newB); fs.writeFileSync(S, sc, 'utf8'); n++; console.log('  ✓ 判定改为无头时跳过'); }
else console.log('  ✗ B 锚点');

/* 2) 窗口测试：解析 -tagAudit，把「标签真的显示了」变成硬断言 */
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');
if (!t.includes('tagAudit')) {
  const anchor = "Write-Host ''`nWrite-Host '=== 7. 返回实验台 ==='";
  const add = `Write-Host ''
Write-Host '=== 6b. 容器标签必须真的显示、且互不重叠 ==='
# 用 -tagAudit 让游戏自己报：存活几个、重叠几对、每个标签的世界 Y
Stop-App
Start-Sleep -Seconds 2
$h2 = Start-App -ExtraArgs @('-tagAudit')
$log2 = "$env:USERPROFILE\\AppData\\LocalLow\\DefaultCompany\\ChemLabUnity\\Player.log"
Start-Sleep -Seconds 6
$txt2 = Get-Content $log2 -Encoding UTF8 -Raw -EA SilentlyContinue
$lines2 = ($txt2 -split "\`n") | Where-Object { $_ -match '\\[TagAudit\\]' }
$live = ($lines2 | Where-Object { $_ -match '存活标签 (\\d+) 个' } | ForEach-Object { [int]($_.Trim() -replace '.*存活标签 (\\d+) 个.*','$1') } | Select-Object -First 1)
$over = ($lines2 | Where-Object { $_ -match '重叠对数 = (\\d+)' } | ForEach-Object { [int]($_.Trim() -replace '.*重叠对数 = (\\d+).*','$1') } | Select-Object -First 1)
Check '容器标签存活数 > 0（不是只有 active）' ($live -gt 0) "存活 $live 个"
Check '容器标签之间不重叠' ($over -eq 0) "重叠 $over 对"
# 每个标签的世界 Y 必须大于 0.2（被清零过就说明又退化）
$badY = ($lines2 | Where-Object { $_ -match 'anchor=\\([^)]*?,\\s*([0-9.]+),' } | Where-Object { [double]($_.Trim() -replace '.*anchor=\\([^)]*?,\\s*([0-9.]+),.*','$1') -lt 0.2 } ).Count
Check '容器标签的世界 Y 都正常（没被清零）' ($badY -eq 0) "异常 $badY 个"
Shot $h2 "$out\\70-tags.png" | Out-Null
Stop-App
Start-Sleep -Seconds 2
$h = Start-App -ExtraArgs @('-dumpLayout')
Start-Sleep -Seconds 3

Write-Host ''
Write-Host '=== 7. 返回实验台 ==='`;
  if (t.includes(anchor)) { t = t.replace(anchor, add); fs.writeFileSync(T, t, 'utf8'); n++; console.log('  ✓ 窗口测试加了标签硬断言'); }
  else console.log('  ✗ 窗口测试锚点未命中');
}
console.log('改动处数:', n);
