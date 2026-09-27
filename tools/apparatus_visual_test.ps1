param([string]$ProjectRoot='D:\ChemLabUnity')
$ErrorActionPreference='Stop'
$run=Join-Path ([IO.Path]::GetTempPath()) ('ChemLabModels-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$log=Join-Path $run 'player.log'
$p=Start-Process (Join-Path $ProjectRoot 'Build\ChemLab.exe') -WorkingDirectory $run -WindowStyle Hidden -PassThru -ArgumentList @('-apparatusVisualTest','-windowTargetInput','-noTut','-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-logFile',$log)
try {
    $deadline=[datetime]::UtcNow.AddSeconds(300)
    while(!$p.HasExited){
        if([datetime]::UtcNow -gt $deadline){throw "Model audit timeout; PID=$($p.Id); evidence=$run"}
        Start-Sleep -Milliseconds 500
    }
    $source=Join-Path $run 'Logs\apparatus-visual'
    if(!(Test-Path -LiteralPath (Join-Path $source 'report.txt'))){throw "No model report; inspect $log"}
    $destination=Join-Path $ProjectRoot ('Logs\apparatus-visual-'+[datetime]::Now.ToString('yyyyMMdd-HHmmss'))
    Copy-Item -LiteralPath $source -Destination $destination -Recurse
    Copy-Item -LiteralPath $log -Destination (Join-Path $destination 'player.log')
    Get-Content -LiteralPath (Join-Path $destination 'report.txt') | Select-String '^(FAIL|TOTAL|CAPACITY)'
    "Natural exit=$($p.ExitCode); evidence=$destination"
    if($p.ExitCode -ne 0){throw 'Apparatus structural checks failed; inspect preserved evidence'}
} finally {if(!$p.HasExited){Stop-Process -Id $p.Id}}
