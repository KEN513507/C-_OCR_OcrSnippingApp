param(
    [int]$Tail = 200
)

$logPath = Join-Path $env:APPDATA "OcrSnippingApp\Logs\OcrSnippingApp_$(Get-Date -Format 'yyyyMMdd').log"
if (-not (Test-Path $logPath)) {
    Write-Host "Log file not found:" $logPath
    return
}

Write-Host "Log File:" $logPath
Write-Host "--- Tail (last $Tail lines) ---"
Get-Content $logPath -Tail $Tail

$hotkeyDisabled = Select-String -Path $logPath -Pattern "Hotkey disabled" -SimpleMatch
$hotkeyTriggered = Select-String -Path $logPath -Pattern "スクリーンショット処理を開始" -SimpleMatch
$creWarnings = Select-String -Path $logPath -Pattern "CREテスター" -SimpleMatch
$appendLogs = Select-String -Path $logPath -Pattern "AppendModeEnabled" -SimpleMatch
$blankLogs = Select-String -Path $logPath -Pattern "blank=" -SimpleMatch
$langHintLogs = Select-String -Path $logPath -Pattern "言語ヒント適用" -SimpleMatch
$mainFormReady = Select-String -Path $logPath -Pattern "MainForm初期化完了" -SimpleMatch
$visionReady = Select-String -Path $logPath -Pattern "Vision APIクライアント初期化完了" -SimpleMatch

Write-Host "--- Summary ---"
Write-Host ("Hotkey triggered: {0}" -f $hotkeyTriggered.Count)
Write-Host ("Hotkey disabled entries: {0}" -f $hotkeyDisabled.Count)
Write-Host ("CRE tester messages: {0}" -f $creWarnings.Count)
Write-Host ("Append mode entries: {0}" -f $appendLogs.Count)
Write-Host ("Add-blank-line entries: {0}" -f $blankLogs.Count)
Write-Host ("Language hint entries: {0}" -f $langHintLogs.Count)
Write-Host ("MainForm ready entries: {0}" -f $mainFormReady.Count)
Write-Host ("Vision API ready entries: {0}" -f $visionReady.Count)
