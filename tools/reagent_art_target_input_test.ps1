param([string]$ProjectRoot='D:\ChemLabUnity',[int]$Width=1280,[int]$Height=800)
# Private Player pipe only. Text setters target Unity InputField.onValueChanged;
# pointer/scroll/Enter go through the game's input module, never the desktop.
$ErrorActionPreference='Stop'
$targetLog=Join-Path $ProjectRoot 'Logs\window-target-input.txt'
$checks=0
function Require([bool]$ok,[string]$label){if(-not $ok){throw "FAIL $label"};$script:checks++;Write-Output "PASS $label"}
function Audit {Get-Content -LiteralPath $targetLog -Raw -Encoding UTF8}
function Latest([string]$pattern){$m=[regex]::Matches((Audit),$pattern);if($m.Count-eq 0){throw "Missing $pattern"};$m[$m.Count-1]}
function Send-Target([string]$commands){
    $marker=[guid]::NewGuid().ToString('N')
    $pipe=[System.IO.Pipes.NamedPipeClientStream]::new('.',"ChemLabInput_$($player.Id)",[System.IO.Pipes.PipeDirection]::Out)
    try{$pipe.Connect(3000);$writer=[System.IO.StreamWriter]::new($pipe);$writer.AutoFlush=$true
        foreach($line in $commands.Split(';')){$writer.WriteLine($line.Trim());Start-Sleep -Milliseconds 180}
        $writer.WriteLine("PING $marker")
        $deadline=[datetime]::UtcNow.AddSeconds(15)
        while((Audit) -notmatch "(?m)^ACK $marker\r?$"){
            if($player.HasExited -or [datetime]::UtcNow -gt $deadline){throw "Command batch not processed: $commands"}
            Start-Sleep -Milliseconds 50
        }
        $writer.Dispose()
    }finally{$pipe.Dispose()}
    Start-Sleep -Milliseconds 120
}
function Click([int]$x,[int]$y){Send-Target "MOVE $x $y;DOWN 0;UP 0"}
function ClickButton([string]$name){
    $before=(Audit).Length;Send-Target 'BUTTONS'
    $m=[regex]::Match((Audit).Substring($before),"(?m)^BUTTON Btn_$([regex]::Escape($name)) (\d+),(\d+)\r*$")
    Require $m.Success "button $name is visible";Click ([int]$m.Groups[1].Value) ([int]$m.Groups[2].Value)
}
function Mass([string]$id){Send-Target "MASS $id";$m=Latest "(?m)^MASS $id ([\d.Ee+-]+)\r*$";[double]::Parse($m.Groups[1].Value,[cultureinfo]::InvariantCulture)}
try{
    $player=Start-Process (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $ProjectRoot -WindowStyle Hidden -PassThru -ArgumentList @('-windowTargetInput','-noTut','-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',(Join-Path $ProjectRoot "Logs\reagent-art-input-${Width}x${Height}.log"))
    $ready=$false
    for($i=0;$i-lt 80;$i++){if($player.HasExited){throw 'Player exited'};if((Test-Path $targetLog)-and (Get-Content $targetLog -TotalCount 1)-eq "READY pid=$($player.Id) size=${Width}x${Height}"){$ready=$true;break};Start-Sleep -Milliseconds 250}
    Require $ready 'hidden Player ready'
    if($Width-lt 1180){ClickButton '试剂'}
    Send-Target 'RACKCONTENT'
    $rack=Latest '(?m)^RACKCONTENT cat=metal query= count=(\d+) scroll=([\d.-]+) viewport=(\d+),(\d+),(\d+),(\d+)\r*$'
    $cx=[int](([int]$rack.Groups[3].Value+[int]$rack.Groups[5].Value)/2)
    $cy=[int](([int]$rack.Groups[4].Value+[int]$rack.Groups[6].Value)/2)
    $top=[double]$rack.Groups[2].Value
    Send-Target "MOVE $cx $cy;SCROLL -8;RACKCONTENT"
    $scrolled=Latest '(?m)^RACKCONTENT cat=metal query= count=\d+ scroll=([\d.-]+)'
    Require ([double]$scrolled.Groups[1].Value-lt $top) 'wheel scroll reaches additional rows'
    Send-Target 'SEARCHTEXT zn;RACKCONTENT'
    Require ((Latest '(?m)^RACKCONTENT cat=metal query=zn count=(\d+)').Groups[1].Value-ne '0') 'search uses InputField change event'
    Require ((Audit)-match '(?m)^RACKCARD zn center=[\d.-]+,[\d.-]+ image=False placeholder=暂无图片') 'missing zinc image is explicitly labeled'
    Send-Target "CAPTURE reagent-missing-${Width}x${Height}"
    Send-Target 'SEARCHTEXT no_such_reagent_xyz;RACKCONTENT'
    Require ((Audit)-match '(?m)^RACKCONTENT cat=metal query=no_such_reagent_xyz count=0 ') 'empty search removes old cards'
    ClickButton '有机物'
    Send-Target 'RACKCONTENT'
    Require ((Audit)-match '(?m)^RACKCONTENT cat=organic query= count=') 'category click clears search and restores category'
    Send-Target 'SEARCHTEXT hg;RACKCONTENT'
    Send-Target 'REAGENT hg'
    $card=Latest '(?m)^REAGENT hg (\d+),(\d+)\r*$'
    $sx=[int]$card.Groups[1].Value;$sy=[int]$card.Groups[2].Value
    Require ($sy-gt [int]$rack.Groups[4].Value-and $sy-lt [int]$rack.Groups[6].Value) 'searched picture remains inside visible viewport'
    Send-Target 'OBJECTS'
    $beakers=[regex]::Matches((Audit),'(?m)^OBJECT (v\d+) Beaker screen=')
    $destination=$null
    foreach($beaker in $beakers){$id=$beaker.Groups[1].Value;Send-Target "FINDPICK $id";$m=[regex]::Match((Audit),"(?m)^FINDPICK $id at=(\d+),(\d+)");if($m.Success){$destination=$id;$dx=[int]$m.Groups[1].Value;$dy=[int]$m.Groups[2].Value;break}}
    Require ($null-ne $destination) 'a real vessel is pickable outside UI'
    $beforeMass=Mass $destination
    Click $sx $sy;Send-Target 'DOSE'
    Require ((Latest '(?m)^DOSE open=(True|False)').Groups[1].Value-eq 'False') 'clicking picture does not directly dose'
    Send-Target "MOVE $sx $sy;DOWN 0;MOVE $($sx-12) $sy;MOVE $dx $dy;DOSE"
    Require ((Latest '(?m)^DOSE open=False drop=(v\d+)').Groups[1].Value-eq $destination) 'drag picture selects intended vessel'
    Require ([double](Latest '(?m)^DOSE highlight=([\d.]+)').Groups[1].Value-gt 0) 'target outline glows before release'
    Send-Target 'UP 0;DOSE'
    Require ((Audit)-match "(?m)^DOSE open=True drop=- target=$destination substance=hg mol=False") 'release opens gram amount dialog'
    Require ([math]::Abs((Mass $destination)-$beforeMass)-lt 0.000001) 'release alone does not add material'
    ClickButton '取消'
    Require ([math]::Abs((Mass $destination)-$beforeMass)-lt 0.000001) 'cancel preserves contents'
    # Compact layout closes its drawer on a completed drop.
    if($Width-lt 1180){ClickButton '试剂'}
    Send-Target "REAGENT hg;MOVE $sx $sy;DOWN 0;MOVE $($sx-12) $sy;MOVE $dx $dy;UP 0;DOSETEXT 0.01;KEYDOWN Return;KEYUP Return;DOSE"
    Require ((Latest '(?m)^DOSE open=(True|False)').Groups[1].Value-eq 'False') 'Enter confirms and closes amount dialog'
    Require ([math]::Abs((Mass $destination)-$beforeMass-0.01)-lt 0.00001) 'Enter adds exactly 0.01 grams once'
    Send-Target "CAPTURE reagent-dose-${Width}x${Height}"
    Write-Output "TOTAL $checks/$checks"
}finally{
    if($null-ne $player-and -not $player.HasExited){Stop-Process -Id $player.Id -Force;$player.WaitForExit()}
    if(Test-Path $targetLog){Copy-Item -LiteralPath $targetLog -Destination (Join-Path $ProjectRoot "Logs\reagent-art-input-${Width}x${Height}.txt")}
}
