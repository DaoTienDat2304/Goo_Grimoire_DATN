<#
    Kiem tra mot file .aab / .apk da an toan truoc CVE-2025-59489 hay chua.

    CO HAI CACH VA, va chung de lai dau vet KHAC NHAU:

    1. Build bang Editor da co ban va (cach dung).
       Unity sua CACH XU LY tham so, KHONG xoa chuoi "xrsdk-pre-init-library"
       khoi libunity.so - do la ten tham so XR hop le. Nen KHONG the dua vao
       su co mat cua chuoi do de ket luan. Dau hieu dung la CHUOI PHIEN BAN
       nhung trong libunity.so.

    2. Chay Unity Application Patcher len file da build.
       Cong cu nay be chuoi thanh "8rsdk-pre-init-library" de chan dlopen().

    Script nay kiem tra ca hai.

    CHAY TRUOC MOI LAN UPLOAD LEN GOOGLE PLAY.

    Vi du:
        .\Tools\Check-CveFix.ps1 -Path "C:\Users\ADMIN\Downloads\GooGrimoire.aab"
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Phien ban Editor toi thieu da co ban va, theo tung nhanh.
# Nguon: https://unity.com/security/sept-2025-01
$minPatched = @{
    "2021.3" = @(2021, 3, 56)
    "2022.3" = @(2022, 3, 67)
    "6000.0" = @(6000, 0, 58)
    "6000.1" = @(6000, 1, 17)
    "6000.2" = @(6000, 2,  6)
}

if (-not (Test-Path $Path)) {
    Write-Host "Khong tim thay file: $Path" -ForegroundColor Red
    exit 1
}

$tempDir = Join-Path $env:TEMP ("cvecheck_" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $tempDir | Out-Null

$zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
try {
    $targets = $zip.Entries | Where-Object { $_.FullName -match "libunity\.so$" }

    if ($targets.Count -eq 0) {
        Write-Host "Khong thay libunity.so trong goi - day co phai app Unity khong?" -ForegroundColor Yellow
        exit 2
    }

    $anyUnsafe = $false

    foreach ($entry in $targets) {
        $tmpFile = Join-Path $tempDir (($entry.FullName -replace "[^A-Za-z0-9\.]", "_"))
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $tmpFile, $true)
        $text = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($tmpFile))

        Write-Host ""
        Write-Host $entry.FullName -ForegroundColor Cyan

        # Cach 2: da chay Patcher tool.
        if ($text.Contains("8rsdk-pre-init-library")) {
            Write-Host "  AN TOAN - da va bang Unity Application Patcher" -ForegroundColor Green
            continue
        }

        # Cach 1: doc phien ban Editor da build ra file nay.
        # Binary co lan nhieu chuoi dang phien ban (vd moc serialization "2018.3.0a1"),
        # nhung phien ban THAT su build ra file la cai lap lai nhieu lan nhat.
        $counts = @{}
        foreach ($m in [regex]::Matches($text, "\b\d{4}\.\d+\.\d+[abfp]\d+\b")) {
            $counts[$m.Value] = 1 + $(if ($counts.ContainsKey($m.Value)) { $counts[$m.Value] } else { 0 })
        }

        if ($counts.Count -eq 0) {
            Write-Host "  KHONG XAC DINH - khong doc duoc phien ban Unity trong binary." -ForegroundColor Yellow
            Write-Host "  Hay tu kiem tra ProjectSettings/ProjectVersion.txt." -ForegroundColor Yellow
            $anyUnsafe = $true
            continue
        }

        $version = ($counts.GetEnumerator() | Sort-Object -Property Value -Descending | Select-Object -First 1).Key

        if ($version -notmatch "^(\d{4})\.(\d+)\.(\d+)[abfp]\d+$") {
            Write-Host "  KHONG XAC DINH - chuoi phien ban la '$version'." -ForegroundColor Yellow
            $anyUnsafe = $true
            continue
        }

        $major = [int]$Matches[1]; $minor = [int]$Matches[2]; $patch = [int]$Matches[3]
        $stream = "$major.$minor"

        if (-not $minPatched.ContainsKey($stream)) {
            Write-Host "  Unity $version - nhanh $stream khong co trong bang doi chieu." -ForegroundColor Yellow
            Write-Host "  Kiem tra tay tai https://unity.com/security/sept-2025-01" -ForegroundColor Yellow
            $anyUnsafe = $true
            continue
        }

        $min = $minPatched[$stream]
        if ($patch -ge $min[2]) {
            Write-Host "  AN TOAN - build bang Unity $version (nhanh $stream can >= $($min[0]).$($min[1]).$($min[2]))" -ForegroundColor Green
        }
        else {
            Write-Host "  DINH LOI - build bang Unity $version, nhanh $stream can >= $($min[0]).$($min[1]).$($min[2])" -ForegroundColor Red
            $anyUnsafe = $true
        }
    }

    Write-Host ""
    if ($anyUnsafe) {
        Write-Host "==> DUNG UPLOAD. Google se tu choi file nay." -ForegroundColor Red
        exit 1
    }

    Write-Host "==> An toan truoc CVE-2025-59489." -ForegroundColor Green
    Write-Host "    Nho kiem tra them: version code lon hon moi ban da tung upload," -ForegroundColor Gray
    Write-Host "    va phai cap nhat TAT CA cac track dang co release active." -ForegroundColor Gray
    exit 0
}
finally {
    $zip.Dispose()
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
