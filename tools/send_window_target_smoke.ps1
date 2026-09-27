param([Parameter(Mandatory=$true)][int]$TargetProcessId)

# Sends logical input only to the test pipe owned by the selected ChemLab Player.
$target=Get-Process -Id $TargetProcessId -ErrorAction Stop
if($target.ProcessName -ne 'ChemLab'){throw "PID $TargetProcessId is not ChemLab"}
$client=[System.IO.Pipes.NamedPipeClientStream]::new('.',"ChemLabInput_$TargetProcessId",[System.IO.Pipes.PipeDirection]::Out)
try{
    $client.Connect(3000)
    $writer=[System.IO.StreamWriter]::new($client)
    $writer.AutoFlush=$true
    $writer.WriteLine('BUTTONS')
    $writer.WriteLine('STATE')
    Start-Sleep -Milliseconds 150
    $writer.WriteLine('MOVE 149 521')
    Start-Sleep -Milliseconds 150
    $writer.WriteLine('DOWN 0')
    Start-Sleep -Milliseconds 180
    $writer.WriteLine('UP 0')
    Start-Sleep -Milliseconds 180
    $writer.WriteLine('STATE')
    $writer.WriteLine('KEYDOWN W')
    Start-Sleep -Milliseconds 180
    $writer.WriteLine('KEYUP W')
    Start-Sleep -Milliseconds 180
    $writer.WriteLine('STATE')
    $writer.Dispose()
}finally{$client.Dispose()}
"SENT pipe=ChemLabInput_$TargetProcessId"
