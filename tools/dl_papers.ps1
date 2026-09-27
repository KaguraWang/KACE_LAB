# Gate 0: download 2024-2026 (M/J) 9701 AS papers from papacambridge mirror
# Records URL / HTTP / size / SHA-256 / page count for manifest audit
$ErrorActionPreference = 'Continue'
$DestDir = 'D:\ChemLabUnity\caie\sources'
New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
$UA = 'User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36'
$BaseUrl = 'https://pastpapers.papacambridge.com/directories/CAIE/CAIE-pastpapers/upload/'
$ProxyUrl = 'http://127.0.0.1:7897'

$series = @(
  @{ y = 's24'; label = '2024 May/June' },
  @{ y = 's25'; label = '2025 May/June' },
  @{ y = 's26'; label = '2026 May/June' }
)
# Zone 5 components: P1=12, P2=22, P3=33/34
$comps = @('qp_12','ms_12','qp_22','ms_22','qp_33','ms_33','qp_34','ms_34','ci_33','ci_34')

function Get-PdfPages([string]$path) {
    try {
        $b = [IO.File]::ReadAllBytes($path)
        $t = [Text.Encoding]::Latin1.GetString($b)
        $m = [regex]::Matches($t, '/Type\s*/Page[^s]')
        if ($m.Count -gt 0) { return $m.Count }
        return -1
    } catch { return -1 }
}

$records = New-Object System.Collections.ArrayList
foreach ($s in $series) {
    foreach ($c in $comps) {
        $name = "9701_$($s.y)_$c.pdf"
        $url  = "$BaseUrl$name"
        $dest = Join-Path $DestDir $name
        if (Test-Path $dest) { Remove-Item $dest -Force -EA SilentlyContinue }

        $httpCode = '000'; $bytes = 0
        foreach ($useProxy in @($false, $true)) {
            if ($useProxy) {
                $curlText = & curl.exe -s -L -o $dest -w "%{http_code}|%{size_download}" --max-time 90 -A $UA --proxy $ProxyUrl $url 2>&1
            } else {
                $curlText = & curl.exe -s -L -o $dest -w "%{http_code}|%{size_download}" --max-time 60 -A $UA $url 2>&1
            }
            $fields = ("$curlText" -split '\|')
            if ($fields.Count -ge 2) {
                $httpCode = $fields[0].Trim()
                $bytes = [int]([double]$fields[1])
            }
            if ($httpCode -eq '200' -and $bytes -gt 20000) { break }
        }

        $ok = ($httpCode -eq '200' -and $bytes -gt 20000)
        $sha = ''; $pages = -1; $isPdf = $false
        if ($ok -and (Test-Path $dest)) {
            $head = [IO.File]::ReadAllBytes($dest)
            $isPdf = ($head.Length -gt 5 -and $head[0] -eq 0x25 -and $head[1] -eq 0x50 -and $head[2] -eq 0x44 -and $head[3] -eq 0x46)
            if (-not $isPdf) { $ok = $false }
            else {
                $sha = (Get-FileHash $dest -Algorithm SHA256).Hash
                $pages = Get-PdfPages $dest
            }
        }
        if (-not $ok) { Remove-Item $dest -Force -EA SilentlyContinue }

        [void]$records.Add([pscustomobject]@{
            series = $s.y; season = $s.label; component = $c
            file = $name; url = $url; http = $httpCode; bytes = $bytes
            pdf = $isPdf; pages = $pages; sha256 = $sha
            status = $(if ($ok) { 'OK' } else { 'MISSING' })
        })
        Write-Host ("  {0,-16} http={1} {2,9:N0} B  pages={3,3}  {4}" -f $name, $httpCode, $bytes, $pages, $(if ($ok) { 'OK' } else { 'MISSING' }))
    }
}
$records | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $DestDir '_download_report.json') -Encoding UTF8
Write-Host ''
$okN = ($records | Where-Object { $_.status -eq 'OK' }).Count
Write-Host "OK $okN / $($records.Count)"
