param(
    [Parameter(Mandatory=$true)][int]$TargetProcessId,
    [Parameter(Mandatory=$true)][string]$CommandList
)

# Local named pipe addressed to exactly one ChemLab Player; no OS input/focus.
$target=Get-Process -Id $TargetProcessId -ErrorAction Stop
if($target.ProcessName -ne 'ChemLab' -or $target.Path -notlike '*\ChemLabUnity*\Build\ChemLab.exe'){
    throw "PID $TargetProcessId is not an expected ChemLab Player"
}
$client=[System.IO.Pipes.NamedPipeClientStream]::new('.',"ChemLabInput_$TargetProcessId",[System.IO.Pipes.PipeDirection]::Out)
try{
    $client.Connect(3000)
    $writer=[System.IO.StreamWriter]::new($client)
    $writer.AutoFlush=$true
    foreach($command in $CommandList.Split(';')){
        $line=$command.Trim()
        if($line.Length -eq 0){continue}
        $writer.WriteLine($line)
        Start-Sleep -Milliseconds 180
    }
    $writer.Dispose()
}finally{$client.Dispose()}
"SENT pipe=ChemLabInput_$TargetProcessId"
