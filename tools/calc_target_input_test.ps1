param(
    [string]$ProjectRoot = 'D:\ChemLabUnity',
    [int]$Width = 1280,
    [int]$Height = 800
)

$ErrorActionPreference = 'Stop'
$playerPath = Join-Path $ProjectRoot 'Build\ChemLab.exe'
$pipeSender = 'D:\ChemLabUnity\tools\send_window_target_commands.ps1'
$targetLog = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$runtimeLog = Join-Path $ProjectRoot 'Logs\calc-target-input-runtime.log'
if (-not (Test-Path -LiteralPath $playerPath)) { throw "Player missing: $playerPath" }

function Send-Target([string]$commandList) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $pipeSender -TargetProcessId $player.Id -CommandList $commandList | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Pipe send failed: $commandList" }
    Start-Sleep -Milliseconds 300
}
function Last-Calc {
    $all = Get-Content -LiteralPath $targetLog -Raw -Encoding UTF8
    $blocks = [regex]::Matches($all, '(?s)CALCSTART\r?\n(.*?)CALCEND')
    if ($blocks.Count -eq 0) { throw 'No CALC audit block' }
    return $blocks[$blocks.Count - 1].Groups[1].Value
}
function Coord([string]$block, [string]$name) {
    $pattern = 'CLICK\|' + [regex]::Escape($name) + '\|(\d+)\|(\d+)\|'
    $m = [regex]::Match($block, $pattern)
    if (-not $m.Success) { throw "Missing UI target: $name" }
    return @([int]$m.Groups[1].Value, [int]$m.Groups[2].Value)
}
function Click-Target([int[]]$at, [int]$button) {
    Send-Target "MOVE $($at[0]) $($at[1]);DOWN $button"
    Send-Target "UP $button;CALC"
    return Last-Calc
}
function Require([bool]$ok, [string]$description) {
    if (-not $ok) { throw "FAIL $description" }
    Write-Output "PASS $description"
}

$player = $null
try {
    $player = Start-Process -FilePath $playerPath -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru `
        -ArgumentList @('-windowTargetInput', '-calcDemo', '-noTut', '-screen-fullscreen', '0',
                        '-screen-width', "$Width", '-screen-height', "$Height", '-logFile', $runtimeLog)
    $ready = $false
    for ($i = 0; $i -lt 80; $i++) {
        if ($player.HasExited) { throw "Player exited before pipe ready: $($player.ExitCode)" }
        if (Test-Path -LiteralPath $targetLog) {
            $head = Get-Content -LiteralPath $targetLog -TotalCount 1 -Encoding UTF8
            if ($head -eq "READY pid=$($player.Id) size=${Width}x${Height}") { $ready = $true; break }
        }
        Start-Sleep -Milliseconds 250
    }
    Require $ready 'process-targeted calculator input ready'

    Send-Target 'CALC'
    $initial = Last-Calc
    $edge = Coord $initial 'edge_C → CO₂'
    Require ($initial -match 'STATE\|edge\|C->CO₂\|-394\|Table') 'initial C to CO2 enthalpy is table value'

    $first = Click-Target $edge 1
    $edit = Coord $first 'menu_编辑焓变值'
    Require ($first -match 'STATE\|menu\|open') 'edge right click opens menu'
    $menuPos = [regex]::Match($first, '\[CalcAudit\] Menu\s+「[^\r\n]*」 x (\d+)\.\.\d+\s+y \d+\.\.(\d+)')
    Require ($menuPos.Success -and [int]$menuPos.Groups[1].Value -eq $edge[0] -and
             [int]$menuPos.Groups[2].Value -eq $edge[1]) 'edge menu starts at right-click position'

    $second = Click-Target $edit 0
    $manual = Coord $second 'menu_手动输入…'
    Require ($second -match 'STATE\|menu\|open') 'edit action opens enthalpy source menu'
    $secondPos = [regex]::Match($second, '\[CalcAudit\] Menu\s+「[^\r\n]*」 x (\d+)\.\.\d+\s+y \d+\.\.(\d+)')
    Require ($secondPos.Success -and [int]$secondPos.Groups[1].Value -eq $edit[0] -and
             [int]$secondPos.Groups[2].Value -eq $edit[1]) 'source menu starts at latest pointer position'

    $dialog = Click-Target $manual 0
    Require ($dialog -match 'STATE\|dlg\|open') 'manual value dialog opens from source menu'
    $ok = [regex]::Match($dialog, 'STATE\|dlgOK\|(\d+),(\d+)')
    Require $ok.Success 'manual dialog exposes confirm button'
    Send-Target 'CALCTEXT -123.5'
    $done = Click-Target @([int]$ok.Groups[1].Value, [int]$ok.Groups[2].Value) 0
    Require ($done -match 'STATE\|dlg\|closed') 'confirm closes manual dialog'
    Require ($done -match 'STATE\|edge\|C->CO₂\|-124\|Manual') 'confirm commits minus 123.5 kJ per mol as manual source'
    $captureName = "calc-target-committed-${Width}x${Height}"
    Send-Target "CAPTURE $captureName"
    $imagePath = Join-Path $ProjectRoot "Logs\window-target-$captureName.png"
    Require ((Test-Path -LiteralPath $imagePath) -and (Get-Item -LiteralPath $imagePath).Length -gt 10000) 'current calculator canvas renders to nonempty image'
    Write-Output 'TOTAL 11 passed, 0 failed'
} finally {
    if ($null -ne $player) {
        $current = Get-Process -Id $player.Id -ErrorAction SilentlyContinue
        if ($null -ne $current) {
            if ($current.Path -ne $playerPath) { throw "Unexpected process path: $($current.Path)" }
            Stop-Process -Id $player.Id
        }
    }
}
