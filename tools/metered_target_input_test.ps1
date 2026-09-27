param(
    [string]$ProjectRoot = 'D:\ChemLabUnity',
    [int]$Width = 1280,
    [int]$Height = 800
)

# Runs a hidden Player and addresses only its named pipe. No foreground focus,
# desktop cursor movement, or system-wide keyboard events are used.
$ErrorActionPreference = 'Stop'
$playerPath = Join-Path $ProjectRoot 'Build\ChemLab.exe'
$targetLog = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$runtimeLog = Join-Path $ProjectRoot 'Logs\metered-target-input-runtime.log'
if (-not (Test-Path -LiteralPath $playerPath)) { throw "Player missing: $playerPath" }
$checks = 0

function Require([bool]$ok, [string]$description) {
    if (-not $ok) { throw "FAIL $description" }
    $script:checks++
    Write-Output "PASS $description"
}
function AuditText { Get-Content -LiteralPath $targetLog -Raw -Encoding UTF8 }
function Latest([string]$pattern) {
    $matches = [regex]::Matches((AuditText), $pattern)
    if ($matches.Count -eq 0) { throw "Missing target audit: $pattern" }
    $matches[$matches.Count - 1]
}
function Send-Target([string]$commands) {
    $marker=[guid]::NewGuid().ToString('N')
    $client = [System.IO.Pipes.NamedPipeClientStream]::new('.', "ChemLabInput_$($player.Id)", [System.IO.Pipes.PipeDirection]::Out)
    try {
        $client.Connect(3000)
        $writer = [System.IO.StreamWriter]::new($client)
        $writer.AutoFlush = $true
        foreach ($command in $commands.Split(';')) {
            $line = $command.Trim()
            if ($line.Length -eq 0) { continue }
            $writer.WriteLine($line)
            Start-Sleep -Milliseconds 180
        }
        $writer.WriteLine("PING $marker")
        $deadline=[datetime]::UtcNow.AddSeconds(15)
        while((AuditText) -notmatch "(?m)^ACK $marker\r?$"){
            if($player.HasExited -or [datetime]::UtcNow -gt $deadline){throw "Target did not process command batch: $commands"}
            Start-Sleep -Milliseconds 50
        }
        $writer.Dispose()
    } finally { $client.Dispose() }
    Start-Sleep -Milliseconds 100
}
function ButtonPoint([string]$name) {
    $before = (AuditText).Length
    Send-Target 'BUTTONS'
    $currentButtons = (AuditText).Substring($before)
    $pattern = "(?m)^BUTTON Btn_$([regex]::Escape($name)) (\d+),(\d+)\r*$"
    if ($name -in @('移液管', '滴定管') -and [regex]::Matches($currentButtons, $pattern).Count -eq 0) {
        $toggle = [regex]::Match($currentButtons, '(?m)^BUTTON Btn_器材 (\d+),(\d+)\r*$')
        if (-not $toggle.Success) { throw 'Compact apparatus drawer has no toggle' }
        Click-Point @([int]$toggle.Groups[1].Value, [int]$toggle.Groups[2].Value)
        $before = (AuditText).Length
        Send-Target 'BUTTONS'
        $currentButtons = (AuditText).Substring($before)
    }
    $m = [regex]::Match($currentButtons, $pattern)
    if (-not $m.Success) { throw "Visible button missing: $name" }
    @([int]$m.Groups[1].Value, [int]$m.Groups[2].Value)
}
function Click-Point([int[]]$point) {
    Send-Target "MOVE $($point[0]) $($point[1]);DOWN 0;UP 0"
}
function FindToolPoint([string]$id, [string]$kind) {
    Send-Target 'OBJECTS'
    $object = Latest "(?m)^OBJECT $id $kind screen=([\d.-]+),([\d.-]+),[\d.-]+\r*$"
    $x = [int][double]::Parse($object.Groups[1].Value, [cultureinfo]::InvariantCulture)
    $y = [int][double]::Parse($object.Groups[2].Value, [cultureinfo]::InvariantCulture)
    foreach ($dy in @(320, 260, 180, 120, 60, 0, -60, -120)) {
        foreach ($dx in @(0, -12, 12, -24, 24)) {
            $px = [math]::Clamp($x + $dx, 1, $Width - 2)
            # Avoid the application's top bar even when a long tube extends
            # beyond the screen; PICK alone would not detect UI interception.
            $py = [math]::Clamp($y + $dy, 1, $Height - 70)
            Send-Target "PICK $px $py"
            $pick = Latest "(?m)^PICK $px,$py => (v\d+|-)"
            if ($pick.Groups[1].Value -eq $id) { return @($px, $py) }
        }
    }
    throw "No exposed point on $kind $id; a foreground object may occlude it"
}
function Drag-To([string]$from, [string]$target, [int[]]$point) {
    Send-Target "DRAGPOINT $from $target $($point[0]) $($point[1])"
    $m = Latest "(?m)^DRAGPOINT $from->$target from=\d+,\d+ to=(\d+),(\d+) height=[\d.-]+\r*$"
    $x = [int]$m.Groups[1].Value; $y = [int]$m.Groups[2].Value
    Send-Target "MOVE $($point[0]) $($point[1]);DOWN 0;MOVE $x $y;STATE;UP 0;STATE"
}
function ToolState([string]$id, [string]$kind) {
    $m = Latest "(?m)^TOOL $id $kind attached=(v\d+|-) pos=[\d.-]+,[\d.-]+ lift=[\d.-]+ volume=([\d.-]+) flow=([\d.-]+)\r*$"
    [pscustomobject]@{
        Attached = $m.Groups[1].Value
        Volume = [double]::Parse($m.Groups[2].Value, [cultureinfo]::InvariantCulture)
        Flow = [double]::Parse($m.Groups[3].Value, [cultureinfo]::InvariantCulture)
    }
}
function Water([string]$id) {
    $m = Latest "(?m)^CHEM $id h2o=[\d.E+-]+ water=([\d.-]+)"
    [double]::Parse($m.Groups[1].Value, [cultureinfo]::InvariantCulture)
}

$player = $null
try {
    $player = Start-Process -FilePath $playerPath -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru `
        -ArgumentList @('-windowTargetInput', '-noTut', '-screen-fullscreen', '0',
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
    Require $ready 'hidden process-targeted Player input ready'

    Click-Point (ButtonPoint '移液管')
    $pipettePoint = FindToolPoint 'v5' 'Pipette'
    Send-Target "MOVE $($pipettePoint[0]) $($pipettePoint[1]);DOWN 1;UP 1;MENU"
    $contextMenu = Latest '(?m)^MENU open upperLeft=([\d.-]+),([\d.-]+) lowerRight=[\d.-]+,[\d.-]+'
    Require ($contextMenu.Success) 'right click on 3D pipette opens its action menu'
    Require ([math]::Abs([double]$contextMenu.Groups[1].Value - $pipettePoint[0]) -lt 2 -and
             [math]::Abs([double]$contextMenu.Groups[2].Value - $pipettePoint[1]) -lt 2) '3D action menu opens at the pointer'
    Send-Target 'KEYDOWN Escape;MENU;KEYUP Escape'
    Require ((Latest '(?m)^MENU (open|closed)').Groups[1].Value -eq 'closed') 'Escape closes the 3D action menu'
    Send-Target "MOVE $($pipettePoint[0]) $($pipettePoint[1]);DOWN 1;UP 1;MENU"
    Require ((Latest '(?m)^MENU (open|closed)').Groups[1].Value -eq 'open') '3D action menu can reopen'
    $awayX = [int]($Width / 2); $awayY = [int]($Height / 2)
    Send-Target "MOVE $awayX $awayY;DOWN 0;UP 0;MENU"
    Require ((Latest '(?m)^MENU (open|closed)').Groups[1].Value -eq 'closed') 'click outside closes the 3D action menu'
    Drag-To 'v5' 'v3' $pipettePoint
    Require ((ToolState 'v5' 'Pipette').Attached -eq 'v3') 'mouse drag attaches pipette above flask'
    Send-Target 'CHEM v3 h2o'
    Require ([math]::Abs((Water 'v3') - 180) -lt 0.01) 'starter flask begins below its 250 mL capacity'

    Click-Point (ButtonPoint '抽取')
    $extracted = $false
    for ($i = 0; $i -lt 35; $i++) {
        Send-Target 'STATE;CHEM v3 h2o'
        if ((ToolState 'v5' 'Pipette').Volume -ge 24.99) { $extracted = $true; break }
        Start-Sleep -Milliseconds 250
    }
    Require $extracted 'pipette extract button fills to the 25 mL hard limit'
    Require ([math]::Abs((Water 'v3') - 155) -lt 0.08) 'extraction removes the same 25 mL from source flask'

    Click-Point (ButtonPoint '释放')
    $released = $false
    for ($i = 0; $i -lt 35; $i++) {
        Send-Target 'STATE;CHEM v3 h2o'
        if ((ToolState 'v5' 'Pipette').Volume -le 0.01) { $released = $true; break }
        Start-Sleep -Milliseconds 250
    }
    Require $released 'pipette release button empties back into attached flask'
    Require ([math]::Abs((Water 'v3') - 180) -lt 0.08) 'extract then release conserves source liquid volume'
    Send-Target 'CAPTURE metered-pipette-release'

    Click-Point (ButtonPoint '滴定管')
    $burettePoint = FindToolPoint 'v6' 'Burette'
    Drag-To 'v6' 'v2' $burettePoint
    Require ((ToolState 'v6' 'Burette').Attached -eq 'v2') 'mouse drag attaches burette above beaker'
    Send-Target 'CHEM v2 h2o;SEED v6 10;CONTROLS'
    $startingWater = Water 'v2'
    $dial = Latest '(?m)^DIAL (\d+),(\d+)\r*$'
    Send-Target "MOVE $($dial.Groups[1].Value) $($dial.Groups[2].Value);SCROLL 10;STATE"
    Require ((ToolState 'v6' 'Burette').Flow -gt 0) 'mouse wheel on burette dial starts timed flow'
    $delivered = $false
    for ($i = 0; $i -lt 20; $i++) {
        Send-Target 'STATE;CHEM v2 h2o'
        if ((Water 'v2') -gt $startingWater + 0.5) { $delivered = $true; break }
        Start-Sleep -Milliseconds 200
    }
    Require $delivered 'burette dial transfers liquid into attached beaker'
    Click-Point (ButtonPoint '停止')
    Send-Target 'STATE'
    Require ((ToolState 'v6' 'Burette').Flow -eq 0) 'stop button closes burette flow'
    Send-Target 'CAPTURE metered-burette-stopped'
    Write-Output "TOTAL $checks passed, 0 failed"
} finally {
    if ($null -ne $player) {
        $current = Get-Process -Id $player.Id -ErrorAction SilentlyContinue
        if ($null -ne $current) {
            if ($current.Path -ne $playerPath) { throw "Unexpected process path: $($current.Path)" }
            Stop-Process -Id $player.Id
        }
    }
}
