param(
    [string]$ProjectRoot = 'D:\ChemLabUnity',
    [int]$Width = 1280,
    [int]$Height = 800
)

# All events go to this hidden Player's named pipe, never the desktop cursor.
$ErrorActionPreference = 'Stop'
$playerPath = Join-Path $ProjectRoot 'Build\ChemLab.exe'
$targetLog = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$runtimeLog = Join-Path $ProjectRoot 'Logs\splint-target-input-runtime.log'
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
function PickPoint([string]$id, [string]$type, [int]$x, [int]$y) {
    foreach ($dy in @(0, 12, -12, 24, -24, 36, -36, 48, -48)) {
        foreach ($dx in @(0, 6, -6, 12, -12)) {
            $px = [math]::Clamp($x + $dx, 1, $Width - 2)
            $py = [math]::Clamp($y + $dy, 1, $Height - 70)
            Send-Target "PICK $px $py"
            $m = Latest "(?m)^PICK $px,$py => .* apparatus=(ap\d+|-) test=(t\d+|-)\r*$"
            $picked = if ($type -eq 'apparatus') { $m.Groups[1].Value } else { $m.Groups[2].Value }
            if ($picked -eq $id) { return @($px, $py) }
        }
    }
    throw "No exposed pick point on $type $id"
}
function OpenMenu([int[]]$point) {
    Send-Target "MOVE $($point[0]) $($point[1]);DOWN 1;UP 1;MENU"
    Latest '(?m)^MENU open upperLeft=([\d.-]+),([\d.-]+) lowerRight=([\d.-]+),([\d.-]+)\r*$'
}
function MenuItem([string]$label) {
    $m = Latest "(?m)^MENUITEM $([regex]::Escape($label)) at=(\d+),(\d+)\r*$"
    @([int]$m.Groups[1].Value, [int]$m.Groups[2].Value)
}
function Click([int[]]$point) {
    Send-Target "MOVE $($point[0]) $($point[1]);DOWN 0;UP 0"
}
function SplintState([string]$id) {
    Send-Target 'STATE'
    Latest "(?m)^SPLINT $id state=(\w+) attached=(ap\d+|-) flame=(True|False) glow=(True|False)\r*$"
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
    Require $ready 'hidden target Player is ready'
    Send-Target 'OBJECTS'
    $item = Latest '(?m)^TESTITEM (t\d+) Splint screen=([\d.-]+),([\d.-]+),[\d.-]+\r*$'
    $splintId = $item.Groups[1].Value
    $itemPoint = PickPoint $splintId 'test' ([int][double]$item.Groups[2].Value) ([int][double]$item.Groups[3].Value)
    Require ($itemPoint.Count -eq 2) 'real 3D splint is pickable'
    $menu = OpenMenu $itemPoint
    # The animated pet can wander over the same screen point. Dismiss it via
    # its own menu, then exercise the 3D object menu without an overlay.
    $tail = (AuditText).Substring($menu.Index)
    if ($tail -match '(?m)^MENUITEM 先躲起来 at=') {
        Click (MenuItem '先躲起来')
        $menu = OpenMenu $itemPoint
    }
    Require ($menu.Success -and [math]::Abs([double]$menu.Groups[1].Value - $itemPoint[0]) -le 2 -and
             [math]::Abs([double]$menu.Groups[2].Value - $itemPoint[1]) -le 2) 'right-click menu opens at splint pointer'
    $lightPoint = MenuItem '点燃木条'
    Click $lightPoint
    $state = SplintState $splintId
    Require ($state.Groups[1].Value -eq 'Burning' -and $state.Groups[3].Value -eq 'True') 'menu click lights splint with visible flame'
    $menu = OpenMenu $itemPoint
    $outPoint = MenuItem '熄灭木条（保留余烬20秒）'
    Click $outPoint
    $state = SplintState $splintId
    Require ($state.Groups[1].Value -eq 'JustOut' -and $state.Groups[3].Value -eq 'False' -and $state.Groups[4].Value -eq 'True') 'context extinguish leaves glowing ember'

    Send-Target 'OBJECTS'
    $burner = Latest '(?m)^APPARATUS (ap\d+) BunsenBurner screen=([\d.-]+),([\d.-]+),[\d.-]+\r*$'
    $burnerId = $burner.Groups[1].Value
    $burnerPoint = PickPoint $burnerId 'apparatus' ([int][double]$burner.Groups[2].Value) ([int][double]$burner.Groups[3].Value)
    $menu = OpenMenu $burnerPoint
    $onPoint = MenuItem '点燃本生灯'
    Click $onPoint
    Send-Target "DRAGSPLINT $splintId $burnerId $($itemPoint[0]) $($itemPoint[1])"
    $drop = Latest "(?m)^DRAGSPLINT $splintId->$burnerId to=(\d+),(\d+)\r*$"
    $dropX = [int]$drop.Groups[1].Value
    $dropY = [int]$drop.Groups[2].Value
    Send-Target "MOVE $($itemPoint[0]) $($itemPoint[1]);DOWN 0;MOVE $dropX $dropY;UP 0;STATE"
    $state = Latest "(?m)^SPLINT $splintId state=(\w+) attached=(ap\d+|-) flame=(True|False) glow=(True|False)\r*$"
    # Drop/attachment resolves after this frame's contact simulation. Observe
    # subsequent ticks before asserting the fire contact has taken effect.
    for($i=0;$i -lt 10 -and $state.Groups[1].Value -ne 'Burning';$i++){
        $state=SplintState $splintId
    }
    Require ($state.Groups[2].Value -eq $burnerId -and $state.Groups[1].Value -eq 'Burning' -and $state.Groups[3].Value -eq 'True') 'actual drag snaps splint to burner and flame ignites it'
    if ($Width -eq 1280 -and $Height -eq 800) {
        Send-Target 'PICK 460 400'
        $tripodPick = Latest '(?m)^PICK 460,400 => .* apparatus=(ap\d+|-) test=(t\d+|-)\r*$'
        Require ($tripodPick.Groups[1].Value -ne '-') 'exposed tripod leg is really pickable'
        $tripodMenu = OpenMenu @(460, 400)
        $menuTail = (AuditText).Substring($tripodMenu.Index)
        Require ($menuTail -match '(?m)^MENUITEM 放回台面 at=') 'tripod right-click shows its object menu'
        Send-Target 'MOVE 800 600;DOWN 0;UP 0;MENU'
        Require ((AuditText) -match '(?m)^MENU closed\r*$') 'outside click closes tripod menu'
    }
    Send-Target 'CAPTURE splint-snapped'
    Write-Output "TOTAL $checks/$checks"
} finally {
    if ($player -ne $null -and -not $player.HasExited) {
        Stop-Process -Id $player.Id -Force
        $player.WaitForExit()
    }
}
