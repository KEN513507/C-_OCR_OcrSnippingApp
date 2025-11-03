# Vision API 認証設定ガイド

## 問題の原因

発行バイナリを起動したプロセス環境に `GOOGLE_APPLICATION_CREDENTIALS` 環境変数が存在しないため、Vision API の認証に失敗します。

PowerShell で `$env:GOOGLE_APPLICATION_CREDENTIALS` を設定しても、エクスプローラーやショートカットから起動したプロセスには伝播しません。

## 解決策

### 方法1: appsettings.json で設定 (推奨)

環境変数に依存せず、アプリケーション設定ファイルで認証情報を管理します。

#### 設定手順

1. `publish/win-x64/appsettings.json` を編集:

```json
{
  "OcrSnipping": {
    "GoogleCredentialPath": "C:\\Keys\\ocr-snipping-app.json",
    ...
  }
}
```

2. 認証ファイルが正しいパスに存在することを確認:

```powershell
Test-Path "C:\Keys\ocr-snipping-app.json"
```

#### メリット
- 環境変数不要
- ポータブル性が高い
- 設定が明示的

### 方法2: 環境変数で設定

システム全体で認証情報を共有する場合に使用します。

#### 一時的な確認

```powershell
.\tools\test-with-env.ps1
```

このスクリプトは環境変数を設定してアプリを起動します。正常に動作すれば、原因は環境変数の未伝播で確定です。

#### 恒久的な設定

```powershell
.\tools\setup-env-var.ps1
```

このスクリプトはユーザー環境変数を設定し、必要に応じてエクスプローラーを再起動します。

手動で設定する場合:

```powershell
[Environment]::SetEnvironmentVariable(
  "GOOGLE_APPLICATION_CREDENTIALS",
  "C:\Keys\ocr-snipping-app.json",
  "User"
)

# エクスプローラー再起動で反映
Stop-Process -Name explorer -Force
Start-Process explorer.exe
```

#### 確認

```powershell
[Environment]::GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS","User")
```

## 認証情報の解決順序

`VisionClientService` は次の順序で認証情報を検索します:

1. **appsettings.json** の `GoogleCredentialPath`
2. **環境変数 (Process)** `GOOGLE_APPLICATION_CREDENTIALS`
3. **環境変数 (User)** `GOOGLE_APPLICATION_CREDENTIALS`
4. **環境変数 (Machine)** `GOOGLE_APPLICATION_CREDENTIALS`
5. **デフォルトパス** `google-credentials.json` (アプリと同じフォルダ)

最初に見つかった有効なパスが使用されます。

### 初期化ログ

起動時に以下のログが出力されます:

```
[INFO] AppDir=C:\Users\user\Documents\Projects\OcrSnippingApp\publish\win-x64\
[INFO] AppSettings.GoogleCredentialPath=C:\Keys\ocr-snipping-app.json
[INFO] Vision: using credential 'C:\Keys\ocr-snipping-app.json'
[INFO] Vision APIクライアント初期化完了: 2025/11/03 16:30:55
```

これにより、実際にどのパスが使用されたかを確認できます。

## 検証スクリプト

```powershell
.\tools\verify-config.ps1
```

このスクリプトは以下を確認します:

- 環境変数の設定状態
- appsettings.json の内容
- 認証ファイルの存在
- 最新ログの抽出

## トラブルシューティング

### 認証ファイルが見つからない

```
認証ファイルが見つかりません: C:\Keys\ocr-snipping-app.json
```

**原因**: パスが間違っているか、ファイルが存在しない

**解決策**:
1. ファイルの実際のパスを確認
2. appsettings.json または環境変数のパスを修正

### 環境変数が反映されない

**原因**: エクスプローラー起動時の環境変数が古い

**解決策**:
1. サインアウト/サインイン
2. PCを再起動
3. `setup-env-var.ps1` でエクスプローラー再起動

### 文字列に全角スペースや引用符ミス

**確認**:
```powershell
$path = "C:\Keys\ocr-snipping-app.json"
$path.Length  # 期待値と一致するか
$path -match '\s'  # 余計な空白がないか
```

## ログの確認

初期化ログを確認:

```powershell
$log = Get-ChildItem "$env:APPDATA\OcrSnippingApp\Logs" -Filter *.log |
       Sort-Object LastWriteTime -Desc | Select-Object -First 1

Get-Content $log.FullName -Tail 200 |
  Select-String -Pattern 'Vision: using credential|OCR処理開始|OCR処理完了|Clipboard'
```

成功時のログ例:

```
[INFO] Vision: using credential 'C:\Keys\ocr-snipping-app.json'
[INFO] Vision APIクライアントを初期化しました。 (2025-11-03 13:45:00)
```

## 注意事項

- 認証ファイルのパスは絶対パスで指定
- JSON 内でバックスラッシュは `\\` とエスケープ
- 認証ファイルには読み取り権限が必要
- appsettings.json の変更後は再ビルド不要、再起動のみ
