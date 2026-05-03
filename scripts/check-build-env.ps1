# A2Z 빌드 환경 자가 진단
# 사용법: 레포 루트에서 .\scripts\check-build-env.ps1
# 종료 코드: 0 (모두 OK) / 1 (실패 항목 있음)

$ErrorActionPreference = "Continue"
$repoRoot = Split-Path $PSScriptRoot -Parent

Write-Host "==== A2Z Build Environment Check ====" -ForegroundColor Cyan
Write-Host ("Repo: " + $repoRoot)
Write-Host ""

$failures = New-Object System.Collections.Generic.List[string]
$requiredManagedVersion = [Version]"1.0.26.325"

# ---------------------------------------------------------------------------
# 1. Managed dll: VIZCore3D+.NET.dll (구글 드라이브 절대 경로)
# ---------------------------------------------------------------------------
$managedPath = Join-Path $env:USERPROFILE "내 드라이브\1. 회사\Digital Wave\Vibe 3D Lab\3. API 적용 참조 파일 (.dll)\03.VIZCore3D+.NET\VIZCore3D+.NET.dll"
Write-Host "[1] Managed dll: VIZCore3D+.NET.dll" -ForegroundColor Yellow
Write-Host ("    path: " + $managedPath)
if (Test-Path $managedPath) {
    $verStr = (Get-Item $managedPath).VersionInfo.FileVersion
    try {
        $ver = [Version]$verStr
        if ($ver -ge $requiredManagedVersion) {
            Write-Host ("    OK  ver=" + $verStr) -ForegroundColor Green
        } else {
            Write-Host ("    FAIL  ver=" + $verStr + "  (need " + $requiredManagedVersion + "+)") -ForegroundColor Red
            $failures.Add("Managed dll outdated: $verStr. Update Google Drive sync, or copy 1.0.26.325+ from notebook to: $managedPath")
        }
    } catch {
        Write-Host ("    WARN  cannot parse version: " + $verStr) -ForegroundColor Yellow
    }
} else {
    Write-Host "    FAIL  not found" -ForegroundColor Red
    $failures.Add("Managed dll not found at: $managedPath. Check Google Drive sync (file may be placeholder/unsynced).")
}
Write-Host ""

# ---------------------------------------------------------------------------
# 2. Native Interop dll: VIZCore3D.NET.Interop.dll (레포 루트)
# ---------------------------------------------------------------------------
$interopPath = Join-Path $repoRoot "VIZCore3D.NET.Interop.dll"
Write-Host "[2] Native interop dll: VIZCore3D.NET.Interop.dll" -ForegroundColor Yellow
Write-Host ("    path: " + $interopPath)
if (Test-Path $interopPath) {
    $sizeMB = [Math]::Round((Get-Item $interopPath).Length / 1MB, 1)
    Write-Host ("    OK  size=" + $sizeMB + "MB") -ForegroundColor Green
} else {
    Write-Host "    FAIL  not found" -ForegroundColor Red
    $failures.Add("Interop dll missing at repo root. Copy VIZCore3D.NET.Interop.dll from notebook or SDK package to: $interopPath")
}
Write-Host ""

# ---------------------------------------------------------------------------
# 3. csproj 무결성 (참조·복사 항목 누락 점검)
# ---------------------------------------------------------------------------
$csproj = Join-Path $repoRoot "A2Z\A2Z.csproj"
Write-Host "[3] csproj integrity" -ForegroundColor Yellow
Write-Host ("    path: " + $csproj)
if (-not (Test-Path $csproj)) {
    Write-Host "    FAIL  csproj not found" -ForegroundColor Red
    $failures.Add("A2Z\A2Z.csproj not found. Repo may be corrupted.")
} else {
    $csprojContent = Get-Content $csproj -Raw
    if ($csprojContent -match "Reference Include=`"VIZCore3D\+\.NET") {
        Write-Host "    OK  Reference: VIZCore3D+.NET" -ForegroundColor Green
    } else {
        Write-Host "    FAIL  missing Reference: VIZCore3D+.NET" -ForegroundColor Red
        $failures.Add("csproj missing <Reference> for VIZCore3D+.NET. Did someone delete it?")
    }
    if ($csprojContent -match "VIZCore3D\.NET\.Interop\.dll") {
        Write-Host "    OK  None Include: VIZCore3D.NET.Interop.dll" -ForegroundColor Green
    } else {
        Write-Host "    FAIL  missing None Include: VIZCore3D.NET.Interop.dll" -ForegroundColor Red
        $failures.Add("csproj missing <None Include> for VIZCore3D.NET.Interop.dll. Native dll won't be copied to bin/.")
    }
}
Write-Host ""

# ---------------------------------------------------------------------------
# 4. csproj diff 변동 경고 (절대 경로 자동 갱신 사고 조기 탐지)
# ---------------------------------------------------------------------------
Write-Host "[4] csproj uncommitted changes (heads-up)" -ForegroundColor Yellow
$gitDiff = git -C $repoRoot diff --name-only A2Z/A2Z.csproj 2>$null
if ($LASTEXITCODE -eq 0 -and $gitDiff) {
    Write-Host "    WARN  A2Z\A2Z.csproj is modified. Inspect for accidental absolute-path changes:" -ForegroundColor Yellow
    Write-Host "          git diff A2Z\A2Z.csproj"
} else {
    Write-Host "    OK  no local changes" -ForegroundColor Green
}
Write-Host ""

# ---------------------------------------------------------------------------
# 결과 요약
# ---------------------------------------------------------------------------
if ($failures.Count -eq 0) {
    Write-Host "==== ALL OK — ready to build ====" -ForegroundColor Green
    exit 0
} else {
    Write-Host "==== FAILURES (" $failures.Count ") ====" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host ("  - " + $f) -ForegroundColor Red }
    Write-Host ""
    Write-Host "See docs/setup/build-environment.md for resolution." -ForegroundColor Yellow
    exit 1
}
