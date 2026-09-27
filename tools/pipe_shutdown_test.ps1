$ErrorActionPreference='Stop'
$root=Join-Path ([IO.Path]::GetTempPath()) ('ChemLabPipeShutdown-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$jobs=@()
try {
    foreach($mode in @('unconnected','idle-client','partial-line')){
        $dir=Join-Path $root $mode
        New-Item -ItemType Directory -Path $dir | Out-Null
        $log=Join-Path $dir 'player.log'
        $p=Start-Process -FilePath 'D:\ChemLabUnity\Build\ChemLab.exe' -WorkingDirectory $dir -ArgumentList @('-batchmode','-consoleTest','-windowTargetInput','-noTut','-logFile',$log) -WindowStyle Hidden -PassThru
        $job=[pscustomobject]@{Mode=$mode;Process=$p;Client=$null;Log=$log;Started=[datetime]::UtcNow}
        $jobs+=$job
        if($mode -ne 'unconnected'){
            $job.Client=[IO.Pipes.NamedPipeClientStream]::new('.','ChemLabInput_'+$p.Id,[IO.Pipes.PipeDirection]::Out)
            $job.Client.Connect(15000)
            $probe=[Text.Encoding]::UTF8.GetBytes("CONSOLE`n")
            $job.Client.Write($probe,0,$probe.Length);$job.Client.Flush()
            if($mode -eq 'partial-line'){
                $bytes=[Text.Encoding]::UTF8.GetBytes('MOVE 100')
                $job.Client.Write($bytes,0,$bytes.Length);$job.Client.Flush()
            }
        }
    }
    while(@($jobs | Where-Object {!$_.Process.HasExited}).Count){
        foreach($job in $jobs){
            if(!$job.Process.HasExited -and ([datetime]::UtcNow-$job.Started).TotalSeconds -gt 180){throw "Shutdown timeout: $($job.Mode) PID $($job.Process.Id)"}
        }
        Start-Sleep -Milliseconds 500
    }
    foreach($job in $jobs){
        $text=Get-Content -LiteralPath $job.Log -Raw
        if($job.Process.ExitCode -ne 0 -or $text -notmatch 'TOTAL 143 passed, 0 failed' -or $text -match 'reader did not stop'){throw "Failed: $($job.Mode) exit=$($job.Process.ExitCode)"}
        if($job.Mode -ne 'unconnected'){
            $inputLog=Join-Path (Split-Path $job.Log) 'Logs\window-target-input.txt'
            if((Get-Content -LiteralPath $inputLog -Raw) -notmatch '(?m)^CONSOLE open='){throw "External pipe command was not processed: $($job.Mode)"}
        }
        "PASS $($job.Mode) natural exit=0 console=143/143"
    }
    "Evidence: $root"
} finally {
    foreach($job in $jobs){
        if($job.Client){$job.Client.Dispose()}
        if(!$job.Process.HasExited){Stop-Process -Id $job.Process.Id}
    }
}
