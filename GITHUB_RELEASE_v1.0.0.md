# OcrSnippingApp v1.0.0

Windows向けOCRスニッピングツールの初回リリースです。

## ✨ 主な機能

- **グローバルホットキー**: 任意のキーでOCR実行
- **追記モードUI**: OCR結果をテキスト加工して貼り付け
- **Vision認証フォールバック**: 複数の方法で認証情報を解決

## 📦 インストール

1. **ZIPをダウンロード**して展開
2. **認証設定**を実施（環境変数またはappsettings.json）
3. **OcrSnippingApp.exe**を起動

詳細な導入手順は [RELEASE_NOTES_v1.0.0.md](https://github.com/KEN513507/C-_OCR_OcrSnippingApp/blob/main/RELEASE_NOTES_v1.0.0.md) を参照してください。

## ⚙️ システム要件

- Windows 10/11 (64-bit)
- [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) 以上
- Google Cloud Vision API の認証JSONファイル

## 🔧 クイックセットアップ

```powershell
# ZIP展開
Expand-Archive .\OcrSnippingApp-1.0.0-win-x64.zip -DestinationPath "C:\Apps\OcrSnippingApp"

# 認証設定
cd C:\Apps\OcrSnippingApp
.\tools\setup-env-var.ps1 -CredentialPath "C:\Keys\ocr-snipping-app.json"

# 検証
.\tools\verify-config.ps1

# 起動
.\OcrSnippingApp.exe
```

## 📝 変更点

### 追加
- HotkeyWindow機能
- 追記モードUI
- Vision認証フォールバック（appsettings → 環境変数 → デフォルトパス）

### 変更
- テスト資産撤去
- 配布サイズ縮小

### デフォルト設定
- ホットキー: 未設定（手動設定が必要）
- 認証: 環境変数またはappsettings.jsonで指定

## ⚠️ 既知の制限

- .NET Desktop Runtime 8.0 未導入環境では起動不可

## 🐛 トラブルシューティング

ログの場所: `%APPDATA%\OcrSnippingApp\Logs\OcrSnippingApp_YYYYMMDD.log`

よくある問題は [RELEASE_NOTES_v1.0.0.md](https://github.com/KEN513507/C-_OCR_OcrSnippingApp/blob/main/RELEASE_NOTES_v1.0.0.md) のトラブルシューティングセクションを参照してください。

## 📊 ファイル検証

ZIPファイルのSHA256ハッシュ:
```
EAF15FF8C4FAB31AF6A1736A9306F8E3FB4C4F91457906A7B9AFB7F04D6FA36B
```

検証方法:
```powershell
Get-FileHash .\OcrSnippingApp-1.0.0-win-x64.zip -Algorithm SHA256
```
