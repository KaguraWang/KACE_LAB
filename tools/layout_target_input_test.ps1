param(
    [string]$ProjectRoot = 'D:\ChemLabUnity',
    [int]$Width = 1280,
    [int]$Height = 800
)

# UI and camera input are addressed only to this hidden Player's named pipe.
$ErrorActionPreference = 'Stop'
$log = Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$player = $null
$checks = 0
function Require([bool]$ok, [string]$label) {
    if (-not $ok) { throw "FAIL $label" }
    $script:checks++
    Write-Output "PASS $label"
}
function AuditText { Get-Content -LiteralPath $log -Raw -Encoding UTF8 }
function Latest([string]$pattern) {
    $matches = [regex]::Matches((AuditText), $pattern)
    if ($matches.Count -eq 0) { throw "Missing audit: $pattern" }
    $matches[$matches.Count - 1]
}
function Send-Target([string]$commands, [int]$StepMilliseconds = 180) {
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
            Start-Sleep -Milliseconds $StepMilliseconds
        }
        $writer.WriteLine("PING $marker")
        $deadline=[datetime]::UtcNow.AddSeconds(15)
        while((AuditText) -notmatch "(?m)^ACK $marker\r?$"){
            if($player.HasExited -or [datetime]::UtcNow -gt $deadline){throw "Command batch not processed: $commands"}
            Start-Sleep -Milliseconds 50
        }
        $writer.Dispose()
    } finally { $client.Dispose() }
    Start-Sleep -Milliseconds 100
}
function PetPosition {
    Send-Target 'PET'
    $m = Latest '(?m)^PET center=([\d.-]+),([\d.-]+) home=[\d.-]+,[\d.-]+ visible=(True|False)\r*$'
    @([double]::Parse($m.Groups[1].Value,[cultureinfo]::InvariantCulture),
      [double]::Parse($m.Groups[2].Value,[cultureinfo]::InvariantCulture))
}
function CameraYaw {
    Send-Target 'CAMERA'
    $m = Latest '(?m)^CAMERA yaw=([\d.-]+) pitch=([\d.-]+) eye=([\d.-]+),([\d.-]+),([\d.-]+)\r*$'
    [double]::Parse($m.Groups[1].Value,[cultureinfo]::InvariantCulture)
}

try {
    $player = Start-Process -FilePath (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $ProjectRoot `
        -WindowStyle Hidden -PassThru -ArgumentList @('-windowTargetInput','-noTut','-screen-fullscreen','0',
            '-screen-width',"$Width",'-screen-height',"$Height",'-logFile',(Join-Path $ProjectRoot 'Logs\layout-target-runtime.log'))
    $ready = $false
    for ($i=0; $i -lt 80; $i++) {
        if ($player.HasExited) { throw 'Player exited before pipe ready' }
        if (Test-Path -LiteralPath $log) {
            $head = Get-Content -LiteralPath $log -TotalCount 1 -Encoding UTF8
            if ($head -eq "READY pid=$($player.Id) size=${Width}x${Height}") { $ready = $true; break }
        }
        Start-Sleep -Milliseconds 250
    }
    Require $ready 'hidden target Player is ready'
    if ($Width -ge 1180) {
        Send-Target 'RACK'
        $rack = Latest '(?m)^RACK width=([\d.-]+) screen=([\d.-]+),([\d.-]+) visible=True\r*$'
        $rackWidth = [double]::Parse($rack.Groups[1].Value,[cultureinfo]::InvariantCulture)
        Require ([math]::Abs($rackWidth-318) -lt 1) 'reagent shelf uses narrow 318-unit desktop width'
    }
    $start = PetPosition
    # Grab off-centre; a correct drag preserves this mouse-to-sprite offset.
    $x0 = [int][math]::Round($start[0] + 24); $y0 = [int][math]::Round($start[1] - 14)
    # A corner dock is near the right edge on narrow windows: keep the entire
    # drag inside the client area instead of testing the input clamp.
    $dx = if ($x0 + 120 -lt $Width - 2) { 120 } else { -120 }
    $x1 = $x0 + $dx; $y1 = $y0 + 60
    Send-Target "MOVE $x0 $y0;DOWN 0;MOVE $x1 $y1;UP 0"
    $end = PetPosition
    Require ([math]::Abs(($end[0]-$start[0])-$dx) -lt 2 -and
             [math]::Abs(($end[1]-$start[1])-60) -lt 2) 'pet sprite follows a real pointer drag without scale offset'
    $before = (AuditText).Length
    Send-Target "MOVE $x1 $y1;DOWN 1;UP 1;MENU"
    $delta = (AuditText).Substring($before)
    $homeButton = [regex]::Match($delta,'(?m)^MENUITEM 回到右下角 at=(\d+),(\d+)\r*$')
    Require $homeButton.Success 'pet right-click works immediately after dragging'
    Send-Target "MOVE $($homeButton.Groups[1].Value) $($homeButton.Groups[2].Value);DOWN 0;UP 0"
    $returned = PetPosition
    Require ([math]::Abs($returned[0]-$start[0]) -lt 2 -and [math]::Abs($returned[1]-$start[1]) -lt 2) 'pet context action returns to responsive corner dock'
    if ($Width -eq 1280 -and $Height -eq 800) {
        $yaw0 = CameraYaw
        Send-Target 'MOVE 900 550;DOWN 0;MOVE 1000 550;UP 0'
        $yaw1 = CameraYaw
        $turn = $yaw1-$yaw0
        if ($turn -gt 180) { $turn -= 360 }
        if ($turn -lt -180) { $turn += 360 }
        Require ($turn -lt -18 -and $turn -gt -22) '100-pixel left-button view drag rotates about 20 degrees in corrected direction'
    }
    Send-Target 'INFO'
    $info = Latest '(?m)^INFO at=(\d+),(\d+) open=(True|False) text=(.+)\r*$'
    Require ($info.Groups[4].Value.Contains('神楽風和') -and $info.Groups[4].Value.Contains('支持風和谢谢喵') -and $info.Groups[4].Value.Contains('v0.1.0_CE_Beta')) 'version information contains user-specified build, author and requested message'
    Send-Target "MOVE $($info.Groups[1].Value) $($info.Groups[2].Value);INFO"
    $info = Latest '(?m)^INFO at=(\d+),(\d+) open=(True|False) text=(.+)\r*$'
    Require ($info.Groups[3].Value -eq 'True') 'hovering the bottom-right information button shows its panel'
    Send-Target "CAPTURE version-${Width}x${Height}"
    Send-Target "MOVE $([int]($Width/2)) $([int]($Height/2));INFO"
    $info = Latest '(?m)^INFO at=(\d+),(\d+) open=(True|False) text=(.+)\r*$'
    Require ($info.Groups[3].Value -eq 'False') 'version tooltip closes when pointer leaves'
    Send-Target "CAPTURE layout-${Width}x${Height}"
    foreach ($entry in @(@('器材','LeftPanel'),@('试剂','RightPanel'),@('记录','BottomPanel'))) {
        $caption = $entry[0]; $panelName = $entry[1]
        Send-Target 'BUTTONS;PANELS'
        $button = Latest "(?m)^BUTTON Btn_$caption (\d+),(\d+)\r*$"
        $click = "MOVE $($button.Groups[1].Value) $($button.Groups[2].Value);DOWN 0;UP 0"
        $pattern = "(?m)^PANEL $panelName active=(True|False) alpha=([\d.]+) blocks=(True|False) origin=([\d.-]+),([\d.-]+)\r*$"
        $state = Latest $pattern
        if ($state.Groups[1].Value -eq 'False') {
            Send-Target $click
            Send-Target 'PANELS'
            $state = Latest $pattern
        }
        $originX = [double]$state.Groups[4].Value; $originY = [double]$state.Groups[5].Value
        Send-Target "$click;PANELS" 40
        $state = Latest $pattern
        Require ([double]$state.Groups[2].Value -gt 0 -and [double]$state.Groups[2].Value -lt 1 -and $state.Groups[3].Value -eq 'False') "$caption closing animates and immediately releases pointer blocking"
        Start-Sleep -Milliseconds 150
        Send-Target 'PANELS'
        $state = Latest $pattern
        Require ($state.Groups[1].Value -eq 'False' -and [double]$state.Groups[2].Value -eq 0) "$caption finishes fully collapsed"
        Send-Target "$click;PANELS" 40
        $state = Latest $pattern
        Require ([double]$state.Groups[2].Value -gt 0 -and [double]$state.Groups[2].Value -lt 1) "$caption expands with intermediate animation frames"
        Start-Sleep -Milliseconds 150
        Send-Target 'PANELS'
        $state = Latest $pattern
        Require ($state.Groups[1].Value -eq 'True' -and [double]$state.Groups[2].Value -eq 1 -and $state.Groups[3].Value -eq 'True' -and
            [math]::Abs([double]$state.Groups[4].Value-$originX) -lt 1 -and [math]::Abs([double]$state.Groups[5].Value-$originY) -lt 1) "$caption restores interaction and original layout without drift"
    }
    foreach($key in @('W','S','A','D')){
        Send-Target 'CAMERA'
        $before=Latest '(?m)^CAMERA yaw=([\d.-]+) pitch=([\d.-]+) eye=([\d.-]+),([\d.-]+),([\d.-]+)\r*$'
        $yaw=[double]$before.Groups[1].Value*[math]::PI/180
        Send-Target "KEYDOWN $key;KEYUP $key;CAMERA"
        $after=Latest '(?m)^CAMERA yaw=([\d.-]+) pitch=([\d.-]+) eye=([\d.-]+),([\d.-]+),([\d.-]+)\r*$'
        $dx=[double]$after.Groups[3].Value-[double]$before.Groups[3].Value
        $dz=[double]$after.Groups[5].Value-[double]$before.Groups[5].Value
        $length=[math]::Sqrt($dx*$dx+$dz*$dz)
        $dot=if($key -in @('W','S')){$dx*[math]::Sin($yaw)+$dz*[math]::Cos($yaw)}else{$dx*[math]::Cos($yaw)-$dz*[math]::Sin($yaw)}
        if($key -in @('S','A')){$dot=-$dot}
        Require ($length -gt .01 -and $dot/$length -gt .99 -and [math]::Abs([double]$after.Groups[4].Value-[double]$before.Groups[4].Value) -lt .002) "real $key input follows the camera-relative horizontal direction"
    }
    Send-Target "CAPTURE panels-${Width}x${Height}"
    Write-Output "TOTAL $checks/$checks"
} finally {
    if ($player -ne $null -and -not $player.HasExited) {
        Stop-Process -Id $player.Id -Force
        $player.WaitForExit()
    }
}
