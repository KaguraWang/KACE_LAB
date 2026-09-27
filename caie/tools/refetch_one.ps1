# 修复两份 REVIEW 文件：重下损坏件 + 放宽身份识别正则 + 增加完整性校验
$ErrorActionPreference = 'Continue'
$DestDir = 'D:\ChemLabUnity\caie\sources'
$UA = 'User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36'
$BaseUrl = 'https://pastpapers.papacambridge.com/directories/CAIE/CAIE-pastpapers/upload/'
$ProxyUrl = 'http://127.0.0.1:7897'

Write-Host '=== 重下 9701_s25_ms_33.pdf（原文件截断）==='
$name = '9701_s25_ms_33.pdf'
$dest = Join-Path $DestDir $name
Remove-Item $dest -Force -EA SilentlyContinue
foreach ($mode in @('proxy','direct','proxy')) {
    if ($mode -eq 'proxy') {
        $r = & curl.exe -s -L -o $dest -w "%{http_code}|%{size_download}|%{speed_download}" --max-time 300 --retry 3 --retry-delay 2 -A $UA --proxy $ProxyUrl "$BaseUrl$name" 2>&1
    } else {
        $r = & curl.exe -s -L -o $dest -w "%{http_code}|%{size_download}|%{speed_download}" --max-time 300 --retry 3 --retry-delay 2 -A $UA "$BaseUrl$name" 2>&1
    }
    Write-Host "  [$mode] $r"
    if (Test-Path $dest) {
        $len = (Get-Item $dest).Length
        if ($len -gt 100000) {
            # 试解析
            $ok = & python -c "import pypdf,sys; r=pypdf.PdfReader(sys.argv[1]); print(len(r.pages)); [p.extract_text() for p in r.pages]" $dest 2>$null
            if ($LASTEXITCODE -eq 0) { Write-Host "  重下成功 $len B, 页数 $ok"; break }
            else { Write-Host "  下载 $len B 但解析失败，重试" }
        } else { Write-Host "  下载 $len B 太小，重试" }
    }
}
if (Test-Path $dest) { Write-Host "  最终大小 $((Get-Item $dest).Length) B  SHA256 $((Get-FileHash $dest -Algorithm SHA256).Hash.Substring(0,16))..." }
