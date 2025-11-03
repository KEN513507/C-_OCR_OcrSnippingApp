# 🔄 ディレクトリ構造移行実行スクリプト

## ⚠️ 重要な注意事項
**このスクリプトを実行する前に:**
1. 現在のコードが正常に動作することを確認
2. git commitでコードをバックアップ
3. 移行後はnamespace更新が必要

## 📋 実行前チェックリスト
- [ ] アプリケーションが正常動作する
- [ ] ビルドエラーがない  
- [ ] git statusがクリーン
- [ ] バックアップ取得済み

## 🚀 移行実行コマンド

### Phase 1: ソースコード移動
```powershell
# Forms関連
Move-Item "MainForm.cs" "src/OcrSnippingApp/Forms/"
Move-Item "MainForm.Designer.cs" "src/OcrSnippingApp/Forms/"  
Move-Item "SnippingForm.cs" "src/OcrSnippingApp/Forms/"
Move-Item "SnippingForm.Designer.cs" "src/OcrSnippingApp/Forms/"

# Core files
Move-Item "Program.cs" "src/OcrSnippingApp/"
Move-Item "app.manifest" "src/OcrSnippingApp/"
Move-Item "appsettings.json" "src/OcrSnippingApp/"
Move-Item "OcrSnippingApp.csproj" "src/OcrSnippingApp/"
Move-Item "OcrSnippingApp.csproj.user" "src/OcrSnippingApp/"

# Services (既存フォルダから移動)
Get-ChildItem "Services" | Move-Item -Destination "src/OcrSnippingApp/Services/"
Remove-Item "Services" -Recurse

# Utilities
Move-Item "ErrorClassifier.cs" "src/OcrSnippingApp/Utilities/"
Move-Item "VisionConfidence.cs" "src/OcrSnippingApp/Utilities/"
```

### Phase 2: ドキュメント移動
```powershell
# 新しい番号付きドキュメント
Move-Item "目次.md" "docs/"
Move-Item "00_ドキュメント一覧.md" "docs/"
Move-Item "01_要件定義.md" "docs/"
Move-Item "02_設定項目.md" "docs/"
Move-Item "03_エラーハンドリング.md" "docs/"
Move-Item "04_クイックTODO.md" "docs/"
Move-Item "05_テスト評価.md" "docs/"
Move-Item "06_技術仕様.md" "docs/"

# 旧ドキュメント（整理済み）
Move-Item "TEST_EVALUATION_REPORT.md" "docs/"
Move-Item "エラーハンドリング.md" "docs/archive/"
Move-Item "クイックTODO.md" "docs/archive/"
Move-Item "要件定義.md" "docs/archive/"
Move-Item "設定項目.md" "docs/archive/"
Move-Item "非要件定義.md" "docs/archive/"
```

### Phase 3: アセット移動
```powershell
New-Item -ItemType Directory -Path "assets/test-images" -Force
Move-Item "image.png" "assets/"
Move-Item "ocr_test_set1_corpus_corrected.html" "assets/test-images/"
```

### Phase 4: プロジェクトファイル更新
```powershell
# 新しいソリューション作成
dotnet new sln -f

# プロジェクト追加
dotnet sln add "src/OcrSnippingApp/OcrSnippingApp.csproj"
```

## 🔧 移行後の必須作業

### 1. namespace更新 (手動)
```csharp
// Forms/MainForm.cs
namespace OcrSnippingApp.Forms

// Forms/SnippingForm.cs  
namespace OcrSnippingApp.Forms

// Services/*.cs
namespace OcrSnippingApp.Services

// Utilities/*.cs
namespace OcrSnippingApp.Utilities
```

### 2. using文更新 (手動)
```csharp
// Program.cs, Forms/*.cs に追加
using OcrSnippingApp.Services;
using OcrSnippingApp.Utilities;
using OcrSnippingApp.Forms;
```

### 3. ビルドテスト
```powershell
cd src/OcrSnippingApp
dotnet build -c Release
```

### 4. 動作確認
```powershell
cd src/OcrSnippingApp
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Keys\ocr-snipping-app.json"
dotnet run
```

## 📁 移行後の最終構造

```
OcrSnippingApp/
├── src/OcrSnippingApp/              📁 メインアプリケーション
│   ├── Forms/                       📱 UI関連
│   ├── Services/                    🔧 ビジネスロジック  
│   ├── Utilities/                   🛠️ ヘルパー類
│   ├── Program.cs                   🚀 エントリーポイント
│   └── OcrSnippingApp.csproj        📦 プロジェクトファイル
├── docs/                            📚 ドキュメント
├── assets/                          🎨 リソース
├── tests/                           🧪 テスト (将来)
├── README.md                        📄 プロジェクト概要
└── OcrSnippingApp.sln              📦 ソリューション
```

## ⏰ 推奨実行タイミング

**今すぐ実行**: ✅ 機能完成・テスト完了後  
**延期推奨**: ❌ 開発中・不安定時期

この構造化により、プロフェッショナルなC#プロジェクトとして管理しやすくなります！