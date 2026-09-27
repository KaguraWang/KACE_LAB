param([string]$ProjectRoot='D:\ChemLabUnity',[int]$Width=1280,[int]$Height=800)
# Inject only into the hidden Player's private input pipe, never the OS cursor.
$ErrorActionPreference='Stop'
$auditPath=Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$player=$null
function Send-Target([string]$commands){
    $before=(Get-Content $auditPath -Raw -Encoding UTF8).Length
    $pipe=[IO.Pipes.NamedPipeClientStream]::new('.',"ChemLabInput_$($player.Id)",[IO.Pipes.PipeDirection]::Out)
    try{$pipe.Connect(3000);$writer=[IO.StreamWriter]::new($pipe);$writer.AutoFlush=$true
        foreach($line in $commands.Split(';')){$writer.WriteLine($line.Trim());Start-Sleep -Milliseconds 180}
        $writer.Dispose()
    }finally{$pipe.Dispose()}
    Start-Sleep -Milliseconds 100
    (Get-Content $auditPath -Raw -Encoding UTF8).Substring($before)
}
try{
    $player=Start-Process (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru -ArgumentList @('-windowTargetInput','-noTut','-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',(Join-Path $ProjectRoot 'Logs\burner-input-runtime.log'))
    $ready=$false
    for($i=0;$i -lt 80;$i++){
        if($player.HasExited){throw 'Player exited'}
        if((Test-Path $auditPath) -and (Get-Content $auditPath -TotalCount 1) -eq "READY pid=$($player.Id) size=${Width}x${Height}"){$ready=$true;break}
        Start-Sleep -Milliseconds 250
    }
    if(!$ready){throw 'No private input pipe ready'}
    $objects=Send-Target 'OBJECTS'
    $burner=[regex]::Match($objects,'(?m)^APPARATUS (\S+) BunsenBurner ')
    if(!$burner.Success){throw 'No burner fixture'}
    $found=Send-Target "FINDPICK $($burner.Groups[1].Value)"
    $point=[regex]::Match($found,'(?m)^FINDPICK \S+ at=(\d+),(\d+) uiBlocked=\d+ hits=\d+')
    if(!$point.Success){throw "No visible burner: $found"}
    $menu=Send-Target "MOVE $($point.Groups[1].Value) $($point.Groups[2].Value);DOWN 1;UP 1;MENU"
    $action=[regex]::Match($menu,'(?m)^MENUITEM 调节火力… at=(\d+),(\d+)')
    if(!$action.Success){throw "No power action: $menu"}
    $buttons=Send-Target "MOVE $($action.Groups[1].Value) $($action.Groups[2].Value);DOWN 0;UP 0;BUTTONS"
    if(!$buttons.Contains('Btn_完成 / Enter')){throw 'Popup not opened by pointer'}
    Write-Output 'PASS burner body right-click opens fire control popup'
    $scale=if($Width -lt 800){$Width/800.0}else{[Math]::Max(1,$Width/1280.0)}
    $y=$Height/2+23*$scale;$left=$Width/2-180*$scale;$right=$Width/2+100*$scale
    $null=Send-Target "MOVE $left $y;DOWN 0;MOVE $right $y;UP 0"
    $capture=Send-Target "CAPTURE burner-control-${Width}x${Height}"
    if(!$capture.Contains('CAPTURE ')){throw 'No fresh popup capture'}
    $buttons=Send-Target 'KEYDOWN Return;KEYUP Return;BUTTONS'
    if($buttons.Contains('Btn_完成 / Enter')){throw 'Enter did not close popup'}
    Write-Output 'PASS Enter closes fire control popup'
    Write-Output "CAPTURE ready: Logs/window-target-burner-control-${Width}x${Height}.png"
}finally{
    if($player -ne $null -and !$player.HasExited){Stop-Process -Id $player.Id -Force;$player.WaitForExit()}
    if($player -ne $null -and (Test-Path $auditPath)){Copy-Item $auditPath (Join-Path $ProjectRoot "Logs\burner-input-${Width}x${Height}-$($player.Id).txt")}
}
