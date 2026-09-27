param(
    [string]$ProjectRoot = 'D:\KaceLab_TeacherCopy1',
    [int]$Width = 1280,
    [int]$Height = 800
)

# Developer-console acceptance through the hidden Player's named pipe only.
# Nothing here moves the desktop cursor, takes foreground focus or injects
# system-wide keyboard events; every event is addressed to one process.
#
# The file is deliberately ASCII only: PowerShell 7 reads .ps1 as UTF-8 without a
# BOM, so Chinese literals in older tools of this project cannot be parsed there.
$ErrorActionPreference = 'Stop'
$playerPath = Join-Path $ProjectRoot 'Build\ChemLab.exe'
$targetLog = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$runtimeLog = Join-Path $ProjectRoot 'Logs\console-target-input-runtime.log'
if (-not (Test-Path -LiteralPath $playerPath)) { throw "Player missing: $playerPath" }
$checks = 0
$player = $null

function Require([bool]$ok, [string]$description) {
    if (-not $ok) { throw "FAIL $description" }
    $script:checks++
    Write-Output "PASS $description"
}
function AuditText { Get-Content -LiteralPath $targetLog -Raw -Encoding UTF8 }
function Send-Target([string]$commands) {
    $client = [System.IO.Pipes.NamedPipeClientStream]::new('.', "ChemLabInput_$($player.Id)", [System.IO.Pipes.PipeDirection]::Out)
    try {
        $client.Connect(3000)
        $writer = [System.IO.StreamWriter]::new($client)
        $writer.AutoFlush = $true
        foreach ($command in $commands.Split(';')) {
            $line = $command.Trim()
            if ($line.Length -eq 0) { continue }
            $writer.WriteLine($line)
            Start-Sleep -Milliseconds 170
        }
        $writer.Dispose()
    } finally { $client.Dispose() }
    Start-Sleep -Milliseconds 120
}

# Sends commands and returns only the audit text appended afterwards. The answer
# usually lands while Send-Target is still sleeping, so the "before" length has to
# be taken first and the wait has to be driven by the expected marker.
function Send-Fresh([string]$commands, [string]$marker, [int]$timeoutMs = 10000) {
    $before = (AuditText).Length
    Send-Target $commands
    $deadline = (Get-Date).AddMilliseconds($timeoutMs)
    while ($true) {
        $text = AuditText
        if ($text.Length -gt $before) {
            $tail = $text.Substring($before)
            if ([regex]::IsMatch($tail, $marker)) { return $tail }
        }
        if ((Get-Date) -gt $deadline) { throw "No fresh audit '$marker' after '$commands'" }
        Start-Sleep -Milliseconds 120
    }
}

$consolePattern = '(?m)^CONSOLE open=(True|False) captured=(True|False) rate=([\d.]+) david=(True|False) focus=([\d.-]+),([\d.-]+) drag=(\S+) segments=(-?\d+)\r*$'
$cameraPattern = '(?m)^CAMERA yaw=([\d.-]+) pitch=([\d.-]+) eye=([\d.-]+),([\d.-]+),([\d.-]+)\r*$'
$davidPattern = '(?m)^DAVID enabled=(True|False) active=(True|False) phase=(\w+) segment=(-?\d+) offset=([\d.]+) intent=[\d.,-]+ visual=[\d.,-]+ ghosts=(\d+) visible=(\d+) visibleParts=(\d+) visualSets=(\d+) copiedMeshes=(\d+) spacing=([\d.]+) pause=([\d.]+)\r*$'

function Parse-Console([string]$tail) {
    $m = [regex]::Match($tail, $consolePattern)
    if (-not $m.Success) { throw 'Unparsable CONSOLE line' }
    [pscustomobject]@{
        Open = $m.Groups[1].Value
        Captured = $m.Groups[2].Value
        Rate = [double]::Parse($m.Groups[3].Value, [cultureinfo]::InvariantCulture)
        David = $m.Groups[4].Value
        Drag = $m.Groups[7].Value
        Segments = [int]$m.Groups[8].Value
    }
}
function ConsoleState { Parse-Console (Send-Fresh 'CONSOLE' 'CONSOLE open=') }
function Parse-Camera([string]$tail) {
    $m = [regex]::Match($tail, $cameraPattern)
    if (-not $m.Success) { throw 'Unparsable CAMERA line' }
    [pscustomobject]@{
        Yaw = [double]::Parse($m.Groups[1].Value, [cultureinfo]::InvariantCulture)
        Pitch = [double]::Parse($m.Groups[2].Value, [cultureinfo]::InvariantCulture)
        X = [double]::Parse($m.Groups[3].Value, [cultureinfo]::InvariantCulture)
        Y = [double]::Parse($m.Groups[4].Value, [cultureinfo]::InvariantCulture)
        Z = [double]::Parse($m.Groups[5].Value, [cultureinfo]::InvariantCulture)
    }
}
function CameraState { Parse-Camera (Send-Fresh 'CAMERA' 'CAMERA yaw=') }
function Parse-David([string]$tail) {
    $m = [regex]::Match($tail, $davidPattern)
    if (-not $m.Success) { throw 'Unparsable DAVID line' }
    [pscustomobject]@{
        Enabled = $m.Groups[1].Value
        Active = $m.Groups[2].Value
        Phase = $m.Groups[3].Value
        Segment = [int]$m.Groups[4].Value
        Offset = [double]::Parse($m.Groups[5].Value, [cultureinfo]::InvariantCulture)
        Ghosts = [int]$m.Groups[6].Value
        Visible = [int]$m.Groups[7].Value
        Parts = [int]$m.Groups[8].Value
        VisualSets = [int]$m.Groups[9].Value
        CopiedMeshes = [int]$m.Groups[10].Value
        Spacing = [double]::Parse($m.Groups[11].Value, [cultureinfo]::InvariantCulture)
        Pause = [double]::Parse($m.Groups[12].Value, [cultureinfo]::InvariantCulture)
    }
}
function DavidState { Parse-David (Send-Fresh 'DAVID' 'DAVID enabled=') }
function ToggleConsole { Send-Target 'KEYDOWN BackQuote;KEYUP BackQuote' }
function TypeCommand([string]$text) {
    Send-Target "CONSOLETEXT $text;KEYDOWN Return;KEYUP Return"
}
function FirstVesselPoint {
    # Some seeded vessels sit behind a HUD panel, where no pick point exists, so
    # use the first vessel that actually exposes one. Must be called while the
    # console is closed: its overlay blocks every UI raycast on purpose.
    $tail = Send-Fresh 'OBJECTS' '(?m)^OBJECT v\d+ \w+ screen='
    $objects = [regex]::Matches($tail, '(?m)^OBJECT (v\d+) \w+ screen=([\d.-]+),([\d.-]+),([\d.-]+)\r*$')
    foreach ($candidate in $objects) {
        $id = $candidate.Groups[1].Value
        $tail = Send-Fresh "FINDPICK $id" "(?m)^FINDPICK $id (at|none)"
        $point = [regex]::Match($tail, "(?m)^FINDPICK $id at=(\d+),(\d+)")
        if ($point.Success) {
            return @{ Id = $id; X = [int]$point.Groups[1].Value; Y = [int]$point.Groups[2].Value }
        }
    }
    throw 'No vessel exposes a reachable pick point'
}

try {
    $player = Start-Process -FilePath $playerPath -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru `
        -ArgumentList @('-windowTargetInput', '-noTut', '-screen-fullscreen', '0',
                        '-screen-width', "$Width", '-screen-height', "$Height", '-logFile', $runtimeLog)
    $ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        if ($player.HasExited) { throw "Player exited before pipe ready: $($player.ExitCode)" }
        if (Test-Path -LiteralPath $targetLog) {
            $head = Get-Content -LiteralPath $targetLog -TotalCount 1 -Encoding UTF8
            if ($head -eq "READY pid=$($player.Id) size=${Width}x${Height}") { $ready = $true; break }
        }
        Start-Sleep -Milliseconds 250
    }
    Require $ready 'hidden process-targeted Player input ready'
    Start-Sleep -Seconds 2

    # 1. the console starts closed, both commands are registered
    $state = ConsoleState
    Require ($state.Open -eq 'False' -and $state.Captured -eq 'False') 'console starts closed'
    Require ($state.Rate -eq 1) 'experiment starts at world_speed 1'
    Require ($state.David -eq 'False') 'david starts disabled'

    # resolve the drag fixture before anything covers the screen
    $vessel = FirstVesselPoint

    # 2. ~ opens the CLI and it mirrors the log
    ToggleConsole
    $state = ConsoleState
    Require ($state.Open -eq 'True' -and $state.Captured -eq 'True') '~ opens the developer console'
    $tail = Send-Fresh 'CONSOLE' 'CONSOLELINE '
    Require ([regex]::IsMatch($tail, '(?m)^CONSOLELINE .+')) 'console displays captured log lines'

    # 3. while open, keys and pointer must not reach the laboratory
    $cameraBefore = CameraState
    Send-Target "KEYDOWN W;MOVE $($vessel.X) $($vessel.Y)"
    Start-Sleep -Milliseconds 900
    Send-Target "DOWN 0;MOVE $($vessel.X + 90) $($vessel.Y + 40)"
    Start-Sleep -Milliseconds 300
    $state = ConsoleState
    $cameraDuring = CameraState
    Require ($state.Drag -eq '-') 'open console swallows pointer dragging'
    Require ([math]::Abs($cameraDuring.X - $cameraBefore.X) -lt 0.001 -and
             [math]::Abs($cameraDuring.Z - $cameraBefore.Z) -lt 0.001) 'open console swallows camera keys'
    Send-Target 'UP 0;KEYUP W'

    # 4. negative control: the identical input works once it is closed
    ToggleConsole
    $state = ConsoleState
    Require ($state.Open -eq 'False') '~ closes the developer console'
    Send-Target 'KEYDOWN W'
    Start-Sleep -Milliseconds 900
    Send-Target 'KEYUP W'
    $cameraAfter = CameraState
    Require ([math]::Abs($cameraAfter.X - $cameraBefore.X) -gt 0.001 -or
             [math]::Abs($cameraAfter.Z - $cameraBefore.Z) -gt 0.001) 'closed console lets camera keys through again'

    # 5. the real Enter path applies a typed command line
    ToggleConsole
    Require ((ConsoleState).Open -eq 'True') 'console reopens for typed commands'
    TypeCommand 'world_speed 0'
    $state = ConsoleState
    Require ($state.Rate -eq 0) 'Enter applies a typed world_speed command'
    Require ($state.Open -eq 'True') 'console stays open after a command'
    TypeCommand 'world_speed 2.5'
    Require ((ConsoleState).Rate -eq 2.5) 'world_speed accepts a decimal rate through the CLI'
    TypeCommand 'world_speed 9'
    Require ((ConsoleState).Rate -eq 2.5) 'an out-of-range rate is refused, not clamped'
    TypeCommand 'world_speed 1'

    # 6. david: enable it, close the console, then drag a vessel
    TypeCommand 'david 1'
    Require ((ConsoleState).David -eq 'True') 'david 1 enables the dash drive'
    ToggleConsole
    Require ((ConsoleState).Open -eq 'False') 'console closes before the drag'
    $vessel = FirstVesselPoint
    Send-Target "MOVE $($vessel.X) $($vessel.Y);DOWN 0"
    $dashed = $false; $sawGhosts = $false; $maxOffset = 0.0; $maxGhosts = 0
    for ($i = 0; $i -lt 26; $i++) {
        Send-Target "MOVE $($vessel.X - $i * 7) $($vessel.Y - $i * 3)"
        $david = DavidState
        if ($david.Active -eq 'True') { $dashed = $true }
        if ($david.Offset -gt $maxOffset) { $maxOffset = $david.Offset }
        if ($david.Ghosts -gt $maxGhosts) { $maxGhosts = $david.Ghosts }
        if ($david.Ghosts -gt 0 -and $david.Visible -gt 0) { $sawGhosts = $true }
    }
    Require $dashed 'david drives the dragged vessel with real dashes'
    Require ($maxOffset -gt 0.1) 'david moves the vessel away from the pointer'
    Require ($maxOffset -le 5.001) 'david keeps every dash within 50 cm'
    Require $sawGhosts 'david renders afterimages while dashing'
    Send-Target 'UP 0'
    $settled = $false
    for ($i = 0; $i -lt 60; $i++) {
        if ((DavidState).Active -eq 'False') { $settled = $true; break }
        Start-Sleep -Milliseconds 120
    }
    Require $settled 'david finishes the release run'
    $after = DavidState
    Require ($after.Ghosts -eq 0 -and $after.CopiedMeshes -eq 0) 'david releases every afterimage and snapshot mesh'
    Require ((ConsoleState).Drag -eq '-') 'ordinary drop still resolves after a david drag'

    # 7. david 0 restores normal dragging and the world speed goes back to 1
    ToggleConsole
    TypeCommand 'david 0'
    Require ((ConsoleState).David -eq 'False') 'david 0 disables the dash drive'
    TypeCommand 'world_speed 1'
    Require ((ConsoleState).Rate -eq 1) 'world speed can be restored to 1'
    ToggleConsole
    $state = ConsoleState
    Require ($state.Open -eq 'False' -and $state.Captured -eq 'False') 'console closes and gives the laboratory its input back'

    Write-Output "TOTAL $checks passed at ${Width}x${Height}"
    exit 0
} finally {
    if ($player -and -not $player.HasExited) {
        $player.Kill()
        $player.WaitForExit(5000)
    }
}
