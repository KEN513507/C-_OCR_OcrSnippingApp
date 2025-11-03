# 一時的に環境変数を設定してアプリを起動し、原因を確認するスクリプト

$exe = "C:\Users\user\Documents\Projects\OcrSnippingApp\publish\win-x64\OcrSnippingApp.exe"
$credPath = "C:\Keys\ocr-snipping-app.json"

Write-Host "=== 環境変数設定テスト ===" -ForegroundColor Cyan
Write-Host ""

# 実行ファイルの存在確認
if (!(Test-Path $exe)) {
    Write-Host "エラー: 実行ファイルが見つかりません" -ForegroundColor Red
    Write-Host "  パス: $exe" -ForegroundColor Yellow
    exit 1
}

# 認証ファイルの存在確認
if (!(Test-Path $credPath)) {
    Write-Host "エラー: 認証ファイルが見つかりません" -ForegroundColor Red
    Write-Host "  パス: $credPath" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ 実行ファイル: $exe" -ForegroundColor Green
Write-Host "✓ 認証ファイル: $credPath" -ForegroundColor Green
Write-Host ""

# 環境変数を設定して起動
Write-Host "環境変数を設定してアプリを起動します..." -ForegroundColor Cyan
$env:GOOGLE_APPLICATION_CREDENTIALS = $credPath

Write-Host "  GOOGLE_APPLICATION_CREDENTIALS = $env:GOOGLE_APPLICATION_CREDENTIALS" -ForegroundColor Gray
Write-Host ""
Write-Host "アプリを起動しています..." -ForegroundColor Green

& $exe

Write-Host ""
Write-Host "アプリが終了しました。" -ForegroundColor Cyan
Write-Host "正常に動作した場合、原因は環境変数の未伝播で確定です。" -ForegroundColor Yellow
