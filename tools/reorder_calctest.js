const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/calctest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Write-Host "初始: nodes=')) from = i;
  if (lines[i].includes('=== 通过 $script:PASS')) { to = i; break; }
}
if (from < 0 || to < 0) { console.log('✗ 定位失败', from, to); process.exit(1); }
console.log('替换', from + 1, '..', to, '行');

const nw = `Write-Host "初始: nodes=$(St 'nodes') edges=$(St 'edges') pending=$(St 'pending') menu=$(St 'menu')"
Shot $h "$out\\00-open.png" | Out-Null

Write-Host ''
Write-Host '=== 1. 求解未知焓变（先做，此时回路干净）==='
CXY 'btn_求解未知焓变'
Start-Sleep -Seconds 2
Shot $h "$out\\01-solved.png" | Out-Null
$t1 = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
$mm = [regex]::Match($t1, "未知焓变 = (-?[0-9.]+) kJ/mol")
$got = if ($mm.Success) { [double]$mm.Groups[1].Value } else { [double]::NaN }
Write-Host "  报出的未知焓变 = $got kJ/mol（应等于 -394 - (-111) = -283）"
Check '求解得出正确数值 -283' ([Math]::Abs($got - (-283.0)) -lt 0.5) "得到 $got"

Write-Host ''
Write-Host '=== 2. 非拉线状态下右键节点 -> 弹出选单 ==='
CXY 'node_C' 'Right'
Check '右键节点弹出选单' ((St 'menu') -eq 'open') "menu = $(St 'menu')"
Shot $h "$out\\02-menu.png" | Out-Null

Write-Host ''
Write-Host '=== 3. 左键点别处 -> 选单关闭 ==='
Click-At $h ([int]($script:CW*0.5)) ([int]($script:CH*0.85))
Start-Sleep -Milliseconds 3000
Check '左键点别处后选单关闭' ((St 'menu') -eq 'closed') "menu = $(St 'menu')"
Shot $h "$out\\03-menu-closed.png" | Out-Null

Write-Host ''
Write-Host '=== 4. 单击节点 -> 进入拉线状态 ==='
CXY 'node_C'
Check '单击节点后进入拉线状态' ((St 'pending') -eq 'yes') "pending = $(St 'pending')"
Shot $h "$out\\04-pending.png" | Out-Null

Write-Host ''
Write-Host '=== 5. 再点另一个节点 -> 连成新连线 ==='
$e0 = [int](St 'edges')
CXY 'node_CO2'
$e1 = [int](St 'edges')
Check '点另一个节点后新增连线' ($e1 -eq $e0 + 1) "edges $e0 -> $e1"
Check '连完后退出拉线状态' ((St 'pending') -eq 'no') "pending = $(St 'pending')"
Shot $h "$out\\05-connected.png" | Out-Null

Write-Host ''
Write-Host '=== 6. 拉线中点回自己 -> 取消 ==='
CXY 'node_CO'
$mid = St 'pending'
CXY 'node_CO'
Check '拉线中点回同一节点取消' ($mid -eq 'yes' -and (St 'pending') -eq 'no') "先 $mid 后 $(St 'pending')"

Write-Host ''
Write-Host '=== 7. 拉线中右键 -> 取消拉线 ==='
CXY 'node_C'
$mid2 = St 'pending'
CXY 'node_C' 'Right'
Check '拉线中右键取消拉线' ($mid2 -eq 'yes' -and (St 'pending') -eq 'no') "先 $mid2 后 $(St 'pending')"

`;

lines.splice(from, to - from, ...nw.split('\n'));
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 已重排');
