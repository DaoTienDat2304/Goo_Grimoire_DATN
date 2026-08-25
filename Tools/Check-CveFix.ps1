<#
    Kiem tra mot file .aab / .apk da duoc va CVE-2025-59489 hay chua.

    Lo hong nam o cho Unity Runtime doc tham so dong lenh "-xrsdk-pre-init-library"
    roi truyen thang vao dlopen(). Ban va (du la nang Editor hay chay Unity
    Application Patcher) deu lam mat chuoi "xrsdk-pre-init-library" khoi libunity.so.

    CHAY TRUOC MOI LAN UPLOAD LEN GOOGLE PLAY.

    Vi du:
        .\Tools\Check-CveFix.ps1 -Path "C:\Users\ADMIN\Downloads\GooGrimoire.aab"
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not (Test-Path $Path)) {
    Write-Host "Khong tim thay file: $Path" -ForegroundColor Red
    exit 1
}

$vulnMarker    = "xrsdk-pre-init-library"
$patchedMarker = "8rsdk-pre-init-library"

$tempDir = Join-Path $env:TEMP ("cvecheck_" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $tempDir | Out-Null

$zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
try {
    $targets = $zip.Entries | Where-Object { $_.FullName -match "libunity\.so$" }

    if ($targets.Count -eq 0) {
        Write-Host "Khong thay libunity.so trong goi - day co phai app Unity khong?" -ForegroundColor Yellow
        exit 2
    }

    $anyVulnerable = $false

    foreach ($entry in $targets) {
        $safeName = ($entry.FullName -replace "[^A-Za-z0-9\.]", "_")
        $tmpFile  = Join-Path $tempDir $safeName
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $tmpFile, $true)

        $text = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($tmpFile))
        $isVulnerable = $text.Contains($vulnMarker)
        $isPatched    = $text.Contains($patchedMarker)

        Write-Host ""
        Write-Host $entry.FullName -ForegroundColor Cyan

        if ($isVulnerable) {
            $anyVulnerable = $true
            Write-Host "  DINH LOI - van con chuoi '$vulnMarker'" -ForegroundColor Red
        }
        elseif ($isPatched) {
            Write-Host "  DA VA bang Unity Application Patcher" -ForegroundColor Green
        }
        else {
            Write-Host "  DA VA - build bang Editor da co ban va" -ForegroundColor Green
        }
    }

    Write-Host ""
    if ($anyVulnerable) {
        Write-Host "==> DUNG UPLOAD. Google se tu choi file nay." -ForegroundColor Red
        exit 1
    }

    Write-Host "==> An toan de upload." -ForegroundColor Green
    Write-Host "    Nho: version code phai lon hon moi version code da tung upload," -ForegroundColor Gray
    Write-Host "    va phai cap nhat TAT CA cac track dang co release active." -ForegroundColor Gray
    exit 0
}
finally {
    $zip.Dispose()
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
