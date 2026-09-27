param([string]$ProjectRoot='D:\ChemLabUnity',[int]$Width=1280,[int]$Height=800)
# Hidden Player and private input only; never OS mouse/keyboard or foreground APIs.
$ErrorActionPreference='Stop'
$run=Join-Path ([IO.Path]::GetTempPath()) ('ChemLabBilingual-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$log=Join-Path $run 'player.log'
$p=Start-Process (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $run -WindowStyle Hidden -PassThru -ArgumentList @('-bilingualUiTest','-windowTargetInput','-noTut','-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$Height",'-logFile',$log)
try {
    $deadline=[datetime]::UtcNow.AddSeconds(300)
    while(!$p.HasExited){
        if([datetime]::UtcNow -gt $deadline){throw "Bilingual audit timeout; PID=$($p.Id); evidence=$run"}
        Start-Sleep -Milliseconds 500
    }
    $report=Join-Path $run 'Logs\bilingual-ui-audit.txt'
    if(!(Test-Path -LiteralPath $report)){throw "Audit produced no report: $log"}
    Copy-Item -LiteralPath $report -Destination (Join-Path $ProjectRoot "Logs\bilingual-ui-${Width}x${Height}.txt")
    Copy-Item -LiteralPath $log -Destination (Join-Path $ProjectRoot "Logs\bilingual-ui-${Width}x${Height}-player.log")
    Get-ChildItem -LiteralPath (Join-Path $run 'Logs') -Filter 'window-target-bilingual-*.png' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $ProjectRoot ("Logs\bilingual-${Width}x${Height}-"+$_.Name))
    }
    Get-Content -LiteralPath $report | Select-String '^(FAIL|TOTAL|METRIC)'
    "Natural exit=$($p.ExitCode); evidence=$run"
    if($p.ExitCode -ne 0){throw 'Bilingual UI acceptance failed; inspect preserved report'}
} finally {if(!$p.HasExited){Stop-Process -Id $p.Id}}
