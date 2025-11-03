# OcrSnippingApp v1.0.0 リリースノート

## 追加機能

- **HotkeyWindow**: グローバルホットキーでOCR実行
- **追記モードUI**: OCR結果をテキスト加工して貼り付け
- **Vision認証フォールバック**: appsettings.json → 環境変数(Process/User/Machine) → デフォルトパスの順で認証情報を解決

## 変更点

- テスト資産撤去: 開発用テストコード、ツール、ドキュメントを削除
- 配布サイズ縮小: 本番環境に不要なファイルを除外

## デフォルト設定

- **ホットキー**: 未設定（appsettings.jsonで設定可能）
- **認証**: 環境変数またはappsettings.jsonで指定が必要

## 既知の制限

- **.NET Desktop Runtime 8.0** 未導入環境では起動不可
  - ダウンロード: https://dotnet.microsoft.com/download/dotnet/8.0

---

## エンドユーザー導入手順

### 1. ZIP展開

```powershell
Expand-Archive .\OcrSnippingApp-1.0.0-win-x64.zip -DestinationPath "C:\Apps\OcrSnippingApp"
cd C:\Apps\OcrSnippingApp
```

### 2. 認証設定

#### 推奨方法（環境変数）

```powershell
.\tools\setup-env-var.ps1 -CredentialPath "C:\Keys\ocr-snipping-app.json"
```

#### 代替方法（appsettings.json）

`appsettings.json` を編集:

```json
{
  "OcrSnipping": {
    "GoogleCredentialPath": "C:\\Keys\\ocr-snipping-app.json"
  }
}
```

### 3. 検証

```powershell
.\tools\verify-config.ps1
```

### 4. 起動

```powershell
.\OcrSnippingApp.exe
```

または、エクスプローラーからダブルクリック。

### 5. ホットキー設定（オプション）

`appsettings.json` を編集:

```json
{
  "OcrSnipping": {
    "HotkeyModifiers": "Ctrl+Shift",
    "HotkeyKey": "E"
  }
}
```

保存後、アプリを再起動。

---

## トラブルシューティング

### クリップボードに入らない

**症状**: OCR実行後、テキストがクリップボードにコピーされない

**確認方法**:
```powershell
# ログで "Clipboard copy OK" を確認
$log = Get-ChildItem "$env:APPDATA\OcrSnippingApp\Logs" -Filter *.log | Sort-Object LastWriteTime -Desc | Select-Object -First 1
Get-Content $log.FullName -Tail 50 | Select-String "Clipboard"
```

**原因**: RDP接続時や権限不足で失敗する場合があります。

### 認証失敗

**症状**: 起動時またはOCR実行時にエラーダイアログが表示される

**確認方法**:
```powershell
.\tools\verify-config.ps1
```

**原因**: ログに `環境変数 GOOGLE_APPLICATION_CREDENTIALS が設定されていません` が出る場合、認証情報が未設定です。

**解決策**: 「2. 認証設定」を実施してください。

### ホットキーが効かない

**症状**: 設定したホットキーを押しても反応しない

**確認方法**:
```powershell
# ログで "Hotkey registered:" を確認
$log = Get-ChildItem "$env:APPDATA\OcrSnippingApp\Logs" -Filter *.log | Sort-Object LastWriteTime -Desc | Select-Object -First 1
Get-Content $log.FullName -Tail 50 | Select-String "Hotkey"
```

**原因**: 
- 他のアプリと競合している
- 管理者権限が必要な場合がある

**解決策**: 別のキーの組み合わせで再試行してください。

### ログの場所

```
%APPDATA%\OcrSnippingApp\Logs\OcrSnippingApp_YYYYMMDD.log
```

---

## 社内展開用ワンライナー

```powershell
$dst = "C:\Apps\OcrSnippingApp"
Expand-Archive .\OcrSnippingApp-1.0.0-win-x64.zip -DestinationPath $dst -Force
& "$dst\tools\setup-env-var.ps1" -CredentialPath "C:\Keys\ocr-snipping-app.json"
Start-Process "$dst\OcrSnippingApp.exe"
```

---

## ロールバック

EXE差し替え前のZIPに戻すだけです。設定は `%APPDATA%` と `appsettings.json` に残るため復元不要です。

```powershell
# 前のバージョンに戻す
Remove-Item "C:\Apps\OcrSnippingApp" -Recurse -Force
Expand-Archive .\OcrSnippingApp-<previous-version>.zip -DestinationPath "C:\Apps\OcrSnippingApp"
```

---

## システム要件

- **OS**: Windows 10/11 (64-bit)
- **ランタイム**: .NET Desktop Runtime 8.0 以上
- **認証**: Google Cloud Vision API の認証JSONファイル

---

## ファイル構成

```
OcrSnippingApp-1.0.0-win-x64/
├── OcrSnippingApp.exe          # メイン実行ファイル
├── appsettings.json            # 設定ファイル
├── README.md                   # プロジェクト説明
└── tools/
    ├── setup-env-var.ps1       # 環境変数設定スクリプト
    └── verify-config.ps1       # 設定検証スクリプト
```
