# 恒久的にユーザー環境変数を設定するスクリプト

param(
    [string]$CredentialPath = "C:\Keys\ocr-snipping-app.json"
)

Write-Host "=== 環境変数恒久設定 ===" -ForegroundColor Cyan
Write-Host ""

# 認証ファイルの存在確認
if (!(Test-Path $CredentialPath)) {
    Write-Host "エラー: 認証ファイルが見つかりません" -ForegroundColor Red
    Write-Host "  パス: $CredentialPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "ファイルを正しい場所に配置してから再実行してください。" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ 認証ファイル: $CredentialPath" -ForegroundColor Green
Write-Host ""

# 現在の設定を確認
$currentValue = [Environment]::GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "User")
if ($currentValue) {
    Write-Host "現在の設定:" -ForegroundColor Yellow
    Write-Host "  GOOGLE_APPLICATION_CREDENTIALS = $currentValue" -ForegroundColor Gray
    Write-Host ""
}

# ユーザー環境変数を設定
Write-Host "ユーザー環境変数を設定しています..." -ForegroundColor Cyan
try {
    [Environment]::SetEnvironmentVariable(
        "GOOGLE_APPLICATION_CREDENTIALS",
        $CredentialPath,
        "User"
    )
    Write-Host "✓ 環境変数を設定しました" -ForegroundColor Green
} catch {
    Write-Host "エラー: 環境変数の設定に失敗しました" -ForegroundColor Red
    Write-Host "  $_" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "設定を確認しています..." -ForegroundColor Cyan
$newValue = [Environment]::GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "User")
Write-Host "  GOOGLE_APPLICATION_CREDENTIALS = $newValue" -ForegroundColor Gray

# エクスプローラーの再起動
Write-Host ""
Write-Host "設定を反映するためにエクスプローラーを再起動しますか？ [Y/N]" -ForegroundColor Yellow
$choice = Read-Host

if ($choice -eq 'Y' -or $choice -eq 'y') {
    Write-Host "エクスプローラーを再起動しています..." -ForegroundColor Cyan
    try {
        Stop-Process -Name explorer -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
        Start-Process explorer.exe
        Write-Host "✓ エクスプローラーを再起動しました" -ForegroundColor Green
    } catch {
        Write-Host "警告: エクスプローラーの再起動に失敗しました" -ForegroundColor Yellow
        Write-Host "  手動でサインアウト/サインインしてください" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "注意: 設定を反映するには、次のいずれかを実行してください:" -ForegroundColor Yellow
    Write-Host "  - Windowsからサインアウト/サインイン" -ForegroundColor Gray
    Write-Host "  - PCを再起動" -ForegroundColor Gray
}

Write-Host ""
Write-Host "完了しました。" -ForegroundColor Green
