# LogInspector

## 概要
OCR 実行時のログを手早く確認する PowerShell スクリプト群です。

### 収録スクリプト
- `InspectOcrLogs.ps1` : 当日ログを解析し、CRE テストモード、追記モード、ホットキー稼働などの概要を表示します。

### 使い方
```powershell
cd tools\LogInspector
.InspectOcrLogs.ps1 -Tail 200
```

### 出力例
```
Log File: C:\Users\user\AppData\Roaming\OcrSnippingApp\Logs\OcrSnippingApp_20251103.log
--- Tail (last 200 lines) ---
...
--- Summary ---
Hotkey triggered: 3
Hotkey disabled entries: 1
CRE tester messages: 0
Append mode entries: 2
Add-blank-line entries: 2
Language hint entries: 4
MainForm ready entries: 1
Vision API ready entries: 1
```
