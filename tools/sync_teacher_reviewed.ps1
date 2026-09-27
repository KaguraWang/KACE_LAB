param([switch]$Apply)
$ErrorActionPreference='Stop'
$sourceRoot='D:\KaceLab_TeacherCopy1'
$targetRoot='D:\ChemLabUnity'
$files=@(foreach($folder in @('Assets','Packages','ProjectSettings','tools','docs')) {
    Get-ChildItem -LiteralPath (Join-Path $sourceRoot $folder) -Recurse -File
})
$files+=@(Get-ChildItem -LiteralPath $sourceRoot -File -Filter '*.md')
$changes=@(foreach($file in $files) {
    $relative=$file.FullName.Substring($sourceRoot.Length+1)
    $target=Join-Path $targetRoot $relative
    $before=if(Test-Path -LiteralPath $target){(Get-FileHash -LiteralPath $target).Hash}else{''}
    $after=(Get-FileHash -LiteralPath $file.FullName).Hash
    if($before -ne $after){[pscustomobject]@{Path=$relative;Before=$before;After=$after}}
})
if(!$Apply){$changes | Format-Table Path,Before; return}
$backup='D:\ChemLabUnity_SyncBackup_'+(Get-Date -Format 'yyyyMMdd_HHmmss')
New-Item -ItemType Directory -Path $backup | Out-Null
$changes | Export-Csv -LiteralPath (Join-Path $backup 'manifest.csv') -NoTypeInformation -Encoding utf8
foreach($change in $changes){
    $target=Join-Path $targetRoot $change.Path
    $source=Join-Path $sourceRoot $change.Path
    if($change.Before){
        if((Get-FileHash -LiteralPath $target).Hash -ne $change.Before){throw "Concurrent target change: $target"}
        $saved=Join-Path $backup $change.Path
        New-Item -ItemType Directory -Path (Split-Path $saved) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $saved
    }
    if((Get-FileHash -LiteralPath $source).Hash -ne $change.After){throw "Concurrent source change: $source"}
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    if((Get-FileHash -LiteralPath $target).Hash -ne $change.After){throw "Verification failed: $target"}
}
"Synced $($changes.Count) files; backup: $backup; no deletions, caches, student files or builds copied."
