param(
    [string]$ProjectRoot = 'D:\ChemLabUnity',
    [int]$Width = 1280,
    [int]$Height = 800
)

# Hidden Player only: local named-pipe pointer events, never the OS cursor.
$ErrorActionPreference = 'Stop'
$targetLog = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$checks = 0
$player = $null
function Require([bool]$ok, [string]$description) {
    if (-not $ok) { throw "FAIL $description" }
    $script:checks++
    Write-Output "PASS $description"
}
function AuditText { Get-Content -LiteralPath $targetLog -Raw -Encoding UTF8 }
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

try {
    $player = Start-Process -FilePath (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden -PassThru -ArgumentList @('-windowTargetInput','-noTut','-screen-fullscreen','0',
            '-screen-width',"$Width",'-screen-height',"$Height",'-logFile',(Join-Path $ProjectRoot 'Logs\object-menu-runtime.log'))
    $ready = $false
    for ($i=0; $i -lt 80; $i++) {
        if ($player.HasExited) { throw 'Player exited before pipe ready' }
        if (Test-Path -LiteralPath $targetLog) {
            $head = Get-Content -LiteralPath $targetLog -TotalCount 1 -Encoding UTF8
            if ($head -eq "READY pid=$($player.Id) size=${Width}x${Height}") { $ready = $true; break }
        }
        Start-Sleep -Milliseconds 250
    }
    Require $ready 'hidden target Player is ready'
    $objects = @(
        @('v1','加热'), @('v2','加热'), @('v3','加热'), @('v4','加热'),
        @('ap1','点燃本生灯'), @('ap2','放回台面'),
        @('t1','用蒸馏水湿润'), @('t2','用蒸馏水湿润'), @('t3','点燃木条')
    )
    foreach ($entry in $objects) {
        $id = $entry[0]; $action = $entry[1]
        $before = (AuditText).Length
        Send-Target "FINDPICK $id"
        $delta = (AuditText).Substring($before)
        $point = [regex]::Match($delta,"(?m)^FINDPICK $id at=(\d+),(\d+) uiBlocked=\d+ hits=\d+\r*$")
        Require $point.Success "$id has a 3D pick point unobstructed by UI"
        $x = [int]$point.Groups[1].Value; $y = [int]$point.Groups[2].Value
        $before = (AuditText).Length
        Send-Target "MOVE $x $y;DOWN 1;UP 1;MENU"
        $delta = (AuditText).Substring($before)
        $menu = [regex]::Match($delta,'(?m)^MENU open upperLeft=([\d.-]+),([\d.-]+) lowerRight=([\d.-]+),([\d.-]+)\r*$')
        Require ($menu.Success -and $delta -match "(?m)^MENUITEM $([regex]::Escape($action)) at=") "$id right-click opens its own action menu"
        $left = [double]::Parse($menu.Groups[1].Value,[cultureinfo]::InvariantCulture)
        $top = [double]::Parse($menu.Groups[2].Value,[cultureinfo]::InvariantCulture)
        Require ([math]::Abs($left-$x) -le 2 -and [math]::Abs($top-$y) -le 2) "$id menu starts at pointer"
        $before = (AuditText).Length
        Send-Target 'KEYDOWN Escape;KEYUP Escape;MENU'
        $delta = (AuditText).Substring($before)
        Require ($delta -match '(?m)^MENU closed\r*$') "$id menu closes with Escape"
        $before = (AuditText).Length
        Send-Target "MOVE $x $y;DOWN 1;UP 1;MENU"
        $delta = (AuditText).Substring($before)
        $safeAction = if ($id.StartsWith('v')) { '搅拌' } else { '放回台面' }
        $button = [regex]::Match($delta,"(?m)^MENUITEM $([regex]::Escape($safeAction)) at=(\d+),(\d+)\r*$")
        Require $button.Success "$id exposes a usable action after reopening"
        $before = (AuditText).Length
        Send-Target "MOVE $($button.Groups[1].Value) $($button.Groups[2].Value);DOWN 0;UP 0;MENU"
        $delta = (AuditText).Substring($before)
        Require ($delta -match '(?m)^MENU closed\r*$') "$id menu closes after its action"
        $before = (AuditText).Length
        Send-Target "MOVE $x $y;DOWN 1;UP 1;MENU"
        $delta = (AuditText).Substring($before)
        Require ($delta -match '(?m)^MENU open ') "$id menu reopens after its action"
        $before = (AuditText).Length
        Send-Target "MOVE $($Width-3) 3;DOWN 0;UP 0;MENU"
        $delta = (AuditText).Substring($before)
        Require ($delta -match '(?m)^MENU closed\r*$') "$id menu closes on outside click"
    }
    Send-Target "CAPTURE object-menu-${Width}x${Height}"
    Write-Output "TOTAL $checks/$checks"
} finally {
    if ($player -ne $null -and -not $player.HasExited) {
        Stop-Process -Id $player.Id -Force
        $player.WaitForExit()
    }
    if ($player -ne $null -and (Test-Path -LiteralPath $targetLog)) {
        Copy-Item -LiteralPath $targetLog -Destination (Join-Path $ProjectRoot "Logs\object-menu-${Width}x${Height}-$($player.Id).txt")
    }
}
