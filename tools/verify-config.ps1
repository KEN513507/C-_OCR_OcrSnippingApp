# 発行バイナリの設定とログを確認するスクリプト

$publishDir = "C:\Users\user\Documents\Projects\OcrSnippingApp\publish\win-x64"
$appDataDir = "$env:APPDATA\OcrSnippingApp"

Write-Host "=== OcrSnippingApp 検証スクリプト ===" -ForegroundColor Cyan
Write-Host ""

# 1. 環境変数の確認
Write-Host "[1] 環境変数の確認" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$userEnv = [Environment]::GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "User")
$processEnv = $env:GOOGLE_APPLICATION_CREDENTIALS

Write-Host "  User環境変数    : $userEnv" -ForegroundColor $(if ($userEnv) { "Green" } else { "Red" })
Write-Host "  Process環境変数 : $processEnv" -ForegroundColor $(if ($processEnv) { "Green" } else { "Red" })

if ($userEnv) {
    $fileExists = Test-Path $userEnv
    Write-Host "  ファイル存在    : $fileExists" -ForegroundColor $(if ($fileExists) { "Green" } else { "Red" })
    if ($fileExists) {
        Write-Host "    → $userEnv" -ForegroundColor Gray
    }
}

Write-Host ""

# 2. appsettings.json の確認
Write-Host "[2] appsettings.json の確認" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$settingsPath = Join-Path $publishDir "appsettings.json"
if (Test-Path $settingsPath) {
    Write-Host "  ✓ ファイル存在: $settingsPath" -ForegroundColor Green
    try {
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        $credPath = $settings.OcrSnipping.GoogleCredentialPath
        $hotkeyMods = $settings.OcrSnipping.HotkeyModifiers
        $hotkeyKey = $settings.OcrSnipping.HotkeyKey
        
        Write-Host ""
        Write-Host "  設定内容:" -ForegroundColor Cyan
        Write-Host "    GoogleCredentialPath : $credPath" -ForegroundColor Gray
        Write-Host "    HotkeyModifiers      : $hotkeyMods" -ForegroundColor Gray
        Write-Host "    HotkeyKey            : $hotkeyKey" -ForegroundColor Gray
        
        if ($credPath) {
            $credExists = Test-Path $credPath
            Write-Host "    認証ファイル存在     : $credExists" -ForegroundColor $(if ($credExists) { "Green" } else { "Red" })
        } else {
            Write-Host "    認証ファイル存在     : 未設定" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  エラー: JSONの解析に失敗しました" -ForegroundColor Red
        Write-Host "    $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✗ ファイルが見つかりません: $settingsPath" -ForegroundColor Red
}

Write-Host ""

# 3. 最新ログの確認
Write-Host "[3] 最新ログの確認" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$logsDir = Join-Path $appDataDir "Logs"
if (Test-Path $logsDir) {
    $latestLog = Get-ChildItem $logsDir -Filter *.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($latestLog) {
        Write-Host "  最新ログ: $($latestLog.Name)" -ForegroundColor Green
        Write-Host "  更新日時: $($latestLog.LastWriteTime)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  関連ログ抽出 (最新200行):" -ForegroundColor Cyan
        Write-Host ""
        
        Get-Content $latestLog.FullName -Tail 200 | 
            Select-String -Pattern 'Vision: using credential|Vision APIクライアント|OCR処理開始|OCR処理完了|Clipboard|エラー|ERROR' |
            ForEach-Object {
                $line = $_.Line
                if ($line -match 'ERROR|エラー') {
                    Write-Host "    $line" -ForegroundColor Red
                } elseif ($line -match 'Vision') {
                    Write-Host "    $line" -ForegroundColor Yellow
                } else {
                    Write-Host "    $line" -ForegroundColor Gray
                }
            }
    } else {
        Write-Host "  ログファイルが見つかりません" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ログディレクトリが見つかりません: $logsDir" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "検証完了" -ForegroundColor Green
Write-Host ""

# 4. 推奨アクション
Write-Host "[推奨アクション]" -ForegroundColor Yellow

$hasUserEnv = [bool]$userEnv
$hasSettingsCred = $false

if (Test-Path $settingsPath) {
    try {
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        $hasSettingsCred = [bool]$settings.OcrSnipping.GoogleCredentialPath
    } catch {}
}

if (-not $hasUserEnv -and -not $hasSettingsCred) {
    Write-Host "  ⚠ 認証情報が未設定です" -ForegroundColor Red
    Write-Host ""
    Write-Host "  次のいずれかを実行してください:" -ForegroundColor Yellow
    Write-Host "    1. appsettings.json に GoogleCredentialPath を追加 (推奨)" -ForegroundColor Gray
    Write-Host "    2. tools\setup-env-var.ps1 を実行して環境変数を設定" -ForegroundColor Gray
} elseif ($hasSettingsCred -and -not $hasUserEnv) {
    Write-Host "  ✓ appsettings.json で認証設定済み (環境変数不要)" -ForegroundColor Green
} elseif (-not $hasSettingsCred -and $hasUserEnv) {
    Write-Host "  ✓ 環境変数で認証設定済み" -ForegroundColor Green
    Write-Host "  💡 appsettings.json にも設定することを推奨します" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ 認証情報が設定されています (両方)" -ForegroundColor Green
}

Write-Host ""
