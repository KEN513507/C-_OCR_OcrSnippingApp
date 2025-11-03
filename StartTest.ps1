param(
  [switch]$SkipBuild,
  [switch]$QuickTest,
  [switch]$AutoStartMain,
  [switch]$AutoStartCre,
  [switch]$TailLog
)

$ErrorActionPreference = 'Stop'
$projectRoot = "C:\Users\user\Documents\Projects\OcrSnippingApp"
if (!(Test-Path $projectRoot)) {
    Write-Error "not found: $projectRoot"
    exit 1
}
Set-Location $projectRoot

$mainCsproj = ".\src\OcrSnippingApp\OcrSnippingApp.csproj"
$creCsproj  = ".\tools\CreTester\CreTester.csproj"
$mainExe    = ".\src\OcrSnippingApp\bin\Release\net8.0-windows\OcrSnippingApp.exe"
$creExe     = ".\tools\CreTester\bin\Debug\net8.0-windows\CreTester.exe"
$logDir     = Join-Path $env:APPDATA 'OcrSnippingApp\Logs'
$todayLog   = Join-Path $logDir ("OcrSnippingApp_{0}.log" -f (Get-Date -Format 'yyyyMMdd'))

function Kill-Apps {
    Get-Process OcrSnippingApp,CreTester -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 1
}

function Build-All {
    if ($SkipBuild) { return }
    Kill-Apps
    Write-Host "🔨 build main..."
    dotnet build $mainCsproj -c Release --nologo
    Write-Host "🔨 build CreTester..."
    dotnet build $creCsproj -c Debug --nologo
}

function Start-Main {
    if (Test-Path $mainExe) {
        Start-Process -FilePath $mainExe -WorkingDirectory (Split-Path $mainExe) -PassThru
    } else {
        Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$projectRoot'; dotnet run --project `$mainCsproj`" -PassThru
    }
}

function Start-Cre {
    if (Test-Path $creExe) {
        Start-Process -FilePath $creExe -WorkingDirectory (Split-Path $creExe) -PassThru
    } else {
        Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$projectRoot\tools\CreTester'; dotnet run --project `$creCsproj`" -PassThru
    }
}

function Wait-Log([int]$sec = 10) {
    $limit = (Get-Date).AddSeconds($sec)
    while (!(Test-Path $todayLog) -and (Get-Date) -lt $limit) {
        Start-Sleep 0.3
    }
    if (!(Test-Path $todayLog)) {
        Write-Warning "log not found: $todayLog"
        return $false
    }
    return $true
}

Write-Host "✅ root: $projectRoot"
New-Item -ItemType Directory $logDir -Force | Out-Null
if ([string]::IsNullOrEmpty($env:GOOGLE_APPLICATION_CREDENTIALS) -or !(Test-Path $env:GOOGLE_APPLICATION_CREDENTIALS)) {
    Write-Warning "GOOGLE_APPLICATION_CREDENTIALS 未設定/未検出"
}

Build-All

Write-Host "`n📋 テスト実施手順"
$cases = if ($QuickTest) { "001","009","011" } else { "001","002","003","004","005","006","007","008","009","010","011","012" }
if ($QuickTest) { Write-Host "🏃‍♂️ クイックテスト（3件）" } else { Write-Host "🎯 フルテスト（12件）" }
$cases | ForEach-Object { " - ケース $_" } | Write-Host

Write-Host "`n🎯 合格基準"
"• 全体平均CER ≤ 5%","• タグ別しきい値遵守","• 008/010の空白保持率≥99%","• 001/009のレイテンシ≤1500ms" | Write-Host

Kill-Apps
$pMain = $null; $pCre = $null
if ($AutoStartMain) {
    $pMain = Start-Main
    if ($pMain) { Write-Host ("Main PID: {0}" -f $pMain.Id) }
}
if ($AutoStartCre) {
    $pCre = Start-Cre
    if ($pCre) { Write-Host ("Cre  PID: {0}" -f $pCre.Id) }
}

if ($TailLog) {
    if (Wait-Log 15) {
        Write-Host "`n📝 Log tail: $todayLog"
        Get-Content $todayLog -Wait -Encoding UTF8 |
            Select-String 'CREテスト|OCR処理|言語ヒント|クリップボード|Clipboard|append='
    } else {
        Write-Warning "ログ生成を待てませんでした"
    }
} else {
    Write-Host "`nヒント: ログ監視"
    Write-Host "Get-Content '$todayLog' -Wait -Encoding UTF8 | Select-String 'OCR処理|クリップボード|append='"
}
