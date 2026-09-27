param([string]$ProjectRoot='D:\ChemLabUnity', [int]$Width=1280, [int]$Height=800)
# All pointer/keyboard commands go to this hidden Player's private pipe, never the OS.
$ErrorActionPreference='Stop'
$auditPath=Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$player=$null
$checks=0
function Require([bool]$ok,[string]$label) {
    if(-not $ok){throw "FAIL $label"}
    $script:checks++; Write-Output "PASS $label"
}
function Send-Target([string]$commands) {
    $marker=[guid]::NewGuid().ToString('N')
    $before=(Get-Content -LiteralPath $auditPath -Raw -Encoding UTF8).Length
    $client=[IO.Pipes.NamedPipeClientStream]::new('.',"ChemLabInput_$($player.Id)",[IO.Pipes.PipeDirection]::Out)
    try {
        $client.Connect(3000);$writer=[IO.StreamWriter]::new($client);$writer.AutoFlush=$true
        foreach($line in $commands.Split(';')){$writer.WriteLine($line.Trim());Start-Sleep -Milliseconds 180}
        $writer.WriteLine("PING $marker")
        $deadline=[datetime]::UtcNow.AddSeconds(15)
        while((Get-Content -LiteralPath $auditPath -Raw -Encoding UTF8) -notmatch "(?m)^ACK $marker\r?$"){
            if($player.HasExited -or [datetime]::UtcNow -gt $deadline){throw "Command batch not processed: $commands"}
            Start-Sleep -Milliseconds 50
        }
        $writer.Dispose()
    } finally {$client.Dispose()}
    Start-Sleep -Milliseconds 100
    (Get-Content -LiteralPath $auditPath -Raw -Encoding UTF8).Substring($before)
}
function Open-Source([string]$id) {
    $found=Send-Target "FINDPICK $id"
    $point=[regex]::Match($found,"(?m)^FINDPICK $id at=(\d+),(\d+) uiBlocked=\d+ hits=\d+\r*$")
    if(-not $point.Success){throw "No visible pick point for $id : $found"}
    Send-Target "MOVE $($point.Groups[1].Value) $($point.Groups[2].Value);DOWN 1;UP 1;MENU"
}
function Click-Action([string]$menu,[string]$caption) {
    $point=[regex]::Match($menu,"(?m)^MENUITEM $([regex]::Escape($caption)) at=(\d+),(\d+)\r*$")
    if(-not $point.Success){throw "Missing menu item: $caption"}
    Send-Target "MOVE $($point.Groups[1].Value) $($point.Groups[2].Value);DOWN 0;UP 0;MENU"
}
try {
    $player=Start-Process (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru -ArgumentList @('-windowTargetInput','-noTut','-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',(Join-Path $ProjectRoot 'Logs\distillation-input-runtime.log'))
    $ready=$false
    for($i=0;$i -lt 80;$i++){
        if($player.HasExited){throw 'Player exited before ready'}
        if((Test-Path $auditPath) -and (Get-Content $auditPath -TotalCount 1) -eq "READY pid=$($player.Id) size=${Width}x${Height}"){$ready=$true;break}
        Start-Sleep -Milliseconds 250
    }
    Require $ready 'hidden target Player ready'
    foreach($kind in @('蒸馏','回流')) {
        $buttons=Send-Target 'BUTTONS'
        $button=[regex]::Match($buttons,"(?m)^BUTTON Btn_${kind}套件 250 mL ([\d.-]+),([\d.-]+)\r*$")
        if(-not $button.Success -and $Width -lt 1180){
            $drawer=[regex]::Match($buttons,'(?m)^BUTTON Btn_器材 ([\d.-]+),([\d.-]+)\r*$')
            if($drawer.Success){
                $null=Send-Target "MOVE $($drawer.Groups[1].Value) $($drawer.Groups[2].Value);DOWN 0;UP 0"
                $buttons=Send-Target 'BUTTONS'
                $button=[regex]::Match($buttons,"(?m)^BUTTON Btn_${kind}套件 250 mL ([\d.-]+),([\d.-]+)\r*$")
            }
        }
        Require $button.Success "$kind shelf button available"
        $objects=Send-Target "MOVE $($button.Groups[1].Value) $($button.Groups[2].Value);DOWN 0;UP 0;OBJECTS"
        $sources=[regex]::Matches($objects,'(?m)^OBJECT (\S+) RoundBottom screen=')
        Require ($sources.Count -gt 0) "$kind shelf pointer click creates source"
        $id=$sources[$sources.Count-1].Groups[1].Value
        # Water is a controlled fixture; shelf/menu interaction itself uses
        # pointer events. Empty boilers correctly stop instead of dry-heating.
        $seed=Send-Target "SEED $id 30"
        Require ($seed -match 'water=30.000') "$kind has water fixture for powered heating"
        $menu=Open-Source $id
        Require ($menu.Contains('打开冷凝水')) "$kind source right-click exposes cooling"
        $settings=Click-Action $menu '传热设置（水／甲醇／乙醇）'
        Require ($settings.Contains('基准功率（灯50%） 250 W')) "$kind thermal settings show current power"
        $power=Click-Action $settings '基准功率（灯50%） 250 W'
        $null=Click-Action $power '功率 W：100'
        $menu=Open-Source $id
        $settings=Click-Action $menu '传热设置（水／甲醇／乙醇）'
        Require ($settings.Contains('基准功率（灯50%） 100 W')) "$kind pointer changes actual heater setting"
        $flow=Click-Action $settings '冷却水流量 10 g/s'
        $null=Click-Action $flow '流量 g/s：1'
        $menu=Open-Source $id
        $settings=Click-Action $menu '传热设置（水／甲醇／乙醇）'
        Require ($settings.Contains('冷却水流量 1 g/s')) "$kind pointer changes coolant flow setting"
        $closed=Click-Action $settings '查看热量账本'
        Require ($closed -match 'MENU closed') "$kind ledger action closes menu"
        $ledger=Send-Target 'THERMALPANEL'
        Require ($ledger.Contains('水套储热') -and $ledger.Contains('器皿玻璃储热') -and $ledger.Contains('器皿环境散热') -and $ledger.Contains('源瓶壁') -and $ledger.Contains('壁温 15.00') -and -not $ledger.Contains('未启用') -and -not $ledger.Contains('fits=False')) "$kind visible ledger reports enabled vessel storage without clipped text"
        $ledgerKind=if($kind -eq '蒸馏'){'distillation'}else{'reflux'}
        $ledgerCapture=Send-Target "CAPTURE thermal-ledger-${ledgerKind}-${Width}x${Height}"
        Require ($ledgerCapture -match '(?m)^CAPTURE ' -and (Test-Path (Join-Path $ProjectRoot "Logs\window-target-thermal-ledger-${ledgerKind}-${Width}x${Height}.png"))) "$kind ledger frame captured"
        $dismissed=Send-Target 'KEYDOWN Return;KEYUP Return;THERMALPANEL'
        Require ($dismissed.Contains('THERMALPANEL closed')) "$kind Enter closes ledger"
        $menu=Open-Source $id
        $settings=Click-Action $menu '传热设置（水／甲醇／乙醇）'
        $closed=Click-Action $settings '传热模型说明'
        Require ($closed -match 'MENU closed') "$kind model explanation action closes menu"
        $explanation=Send-Target 'THERMALPANEL'
        Require ($explanation.Contains('BOROFLOAT') -and $explanation.Contains('不是实测仪器标定') -and -not $explanation.Contains('fits=False')) "$kind model approximation is visible without clipping"
        $dismissed=Send-Target 'KEYDOWN Escape;KEYUP Escape;THERMALPANEL'
        Require ($dismissed.Contains('THERMALPANEL closed')) "$kind Escape closes model explanation"
        $null=Send-Target 'KEYDOWN Escape;KEYUP Escape'
        $menu=Open-Source $id
        $closed=Click-Action $menu '打开冷凝水（15℃）'
        Require ($closed -match 'MENU closed') "$kind cooling action closes menu"
        $menu=Open-Source $id
        Require ($menu.Contains('关闭冷凝水')) "$kind cooling state changes"
        $closed=Click-Action $menu '启动蒸馏／回流'
        $menu=Open-Source $id
        Require ($menu.Contains('停止蒸馏／回流')) "$kind starts through pointer menu"
        $thermal=Send-Target "THERMAL $id"
        Require ($thermal.Contains('transient=True running=True') -and $thermal.Contains('power=100.0 flow=1.0')) "$kind pointer starts real default transient settings"
        $null=Click-Action $menu '停止蒸馏／回流'
        $menu=Open-Source $id
        Require ($menu.Contains('启动蒸馏／回流')) "$kind stops through pointer menu"
        $null=Send-Target 'KEYDOWN Escape;KEYUP Escape'
        $captureKind=if($kind -eq '蒸馏'){'distillation'}else{'reflux'}
        $captured=Send-Target "CAPTURE ${captureKind}-input-${Width}x${Height}"
        Require ($captured -match '(?m)^CAPTURE ' -and (Test-Path (Join-Path $ProjectRoot "Logs\window-target-${captureKind}-input-${Width}x${Height}.png"))) "$kind visible frame captured"
        if($kind -eq '蒸馏'){
            $menu=Open-Source $id
            $null=Click-Action $menu '启动蒸馏／回流'
            $current=Send-Target 'OBJECTS'
            $receivers=[regex]::Matches($current,'(?m)^OBJECT (\S+) Flask screen=')
            Require ($receivers.Count -gt 0) 'distillation receiver exists'
            $receiverId=$receivers[$receivers.Count-1].Groups[1].Value
            $found=Send-Target "FINDPICK $receiverId"
            $point=[regex]::Match($found,"(?m)^FINDPICK $receiverId at=(\d+),(\d+) uiBlocked=\d+ hits=\d+\r*$")
            Require $point.Success 'receiver has unobstructed pointer location'
            $rx=[int]$point.Groups[1].Value;$ry=[int]$point.Groups[2].Value
            $null=Send-Target "MOVE $rx $ry;DOWN 0;MOVE $($rx+3) $ry;UP 0"
            $menu=Open-Source $id
            Require ($menu.Contains('停止蒸馏／回流')) 'receiver click with subthreshold jitter keeps running connection'
            $null=Send-Target 'KEYDOWN Escape;KEYUP Escape'
            # Reobserve after closing the menu, then lift the receiver clear of
            # the outlet. Shift avoids accidentally starting a pour transaction.
            $found=Send-Target "FINDPICK $receiverId"
            $point=[regex]::Match($found,"(?m)^FINDPICK $receiverId at=(\d+),(\d+) uiBlocked=\d+ hits=\d+\r*$")
            Require $point.Success 'receiver still pickable before disconnect drag'
            $rx=[int]$point.Groups[1].Value;$ry=[int]$point.Groups[2].Value
            $null=Send-Target "MOVE $rx $ry;KEYDOWN LeftShift;DOWN 0;MOVE $rx $($ry+24);UP 0;KEYUP LeftShift"
            $menu=Open-Source $id
            Require ($menu.Contains('安装回流冷凝器') -and -not $menu.Contains('停止蒸馏／回流') -and -not $menu.Contains('拆除冷凝装置')) 'receiver pointer drag disconnects and stops the source'
            $null=Send-Target 'KEYDOWN Escape;KEYUP Escape'
            continue
        }
        $menu=Open-Source $id
        $null=Click-Action $menu '拆除冷凝装置'
        $menu=Open-Source $id
        Require ($menu.Contains('安装回流冷凝器') -and -not $menu.Contains('拆除冷凝装置')) "$kind detach restores install actions"
        $null=Send-Target 'KEYDOWN Escape;KEYUP Escape'
    }
    Write-Output "TOTAL $checks/$checks"
} finally {
    if($player -ne $null -and -not $player.HasExited){Stop-Process -Id $player.Id -Force;$player.WaitForExit()}
    if($player -ne $null -and (Test-Path $auditPath)){Copy-Item $auditPath (Join-Path $ProjectRoot "Logs\distillation-input-${Width}x${Height}-$($player.Id).txt")}
}
