# 📁 プロジェクト構造改善計画

C#開発のベストプラクティスに従ったディレクトリ構造への移行計画

## 🏗️ 現在の構造（問題点）

```
OcrSnippingApp/
├── *.cs                    ❌ ルートにソースファイル散在
├── *.md                    ❌ ドキュメントが混在
├── Services/               ✅ 一部整理済み
├── bin/                    ✅ ビルド出力
├── obj/                    ✅ 中間ファイル
├── *.csproj               ✅ プロジェクトファイル
└── appsettings.json       ✅ 設定ファイル
```

## 🎯 推奨構造（ベストプラクティス）

```
OcrSnippingApp/
├── src/                           📁 ソースコード
│   └── OcrSnippingApp/
│       ├── Forms/                 📱 UI関連
│       │   ├── MainForm.cs
│       │   ├── MainForm.Designer.cs
│       │   ├── SnippingForm.cs
│       │   └── SnippingForm.Designer.cs
│       ├── Services/              🔧 ビジネスロジック
│       │   ├── Logger.cs
│       │   ├── SettingsService.cs
│       │   └── VisionClientService.cs
│       ├── Models/                📊 データ構造
│       │   └── AppSettings.cs
│       ├── Utilities/             🛠️ ヘルパー・拡張
│       │   ├── ErrorClassifier.cs
│       │   └── VisionConfidence.cs
│       ├── Program.cs             🚀 エントリーポイント
│       ├── app.manifest
│       ├── appsettings.json
│       └── OcrSnippingApp.csproj
├── tests/                         🧪 テストプロジェクト
│   └── OcrSnippingApp.Tests/
│       ├── Services/
│       ├── Utilities/
│       └── OcrSnippingApp.Tests.csproj
├── docs/                          📚 ドキュメント
│   ├── 目次.md
│   ├── 00_ドキュメント一覧.md
│   ├── 01_要件定義.md
│   ├── 02_設定項目.md
│   ├── 03_エラーハンドリング.md
│   ├── 04_クイックTODO.md
│   ├── 05_テスト評価.md
│   └── 06_技術仕様.md
├── assets/                        🎨 リソース・画像
│   ├── icons/
│   └── test-images/
├── README.md                      📄 プロジェクト概要
├── OcrSnippingApp.sln            📦 ソリューションファイル
└── .gitignore                    🚫 Git除外設定
```

## 📋 移行手順

### フェーズ1: ディレクトリ作成 ✅完了
- [x] `src/OcrSnippingApp/` 作成
- [x] `src/OcrSnippingApp/Forms/` 作成  
- [x] `src/OcrSnippingApp/Services/` 作成
- [x] `src/OcrSnippingApp/Models/` 作成
- [x] `src/OcrSnippingApp/Utilities/` 作成
- [x] `docs/` 作成
- [x] `tests/` 作成
- [x] `assets/` 作成

### フェーズ2: ファイル移動 🔄

#### 2.1 ソースコード移動
```powershell
# Forms
Move-Item "MainForm.cs" "src/OcrSnippingApp/Forms/"
Move-Item "MainForm.Designer.cs" "src/OcrSnippingApp/Forms/"
Move-Item "SnippingForm.cs" "src/OcrSnippingApp/Forms/"
Move-Item "SnippingForm.Designer.cs" "src/OcrSnippingApp/Forms/"

# Core
Move-Item "Program.cs" "src/OcrSnippingApp/"
Move-Item "app.manifest" "src/OcrSnippingApp/"
Move-Item "appsettings.json" "src/OcrSnippingApp/"
Move-Item "OcrSnippingApp.csproj" "src/OcrSnippingApp/"

# Services (既存)
Move-Item "Services/*" "src/OcrSnippingApp/Services/"

# Utilities
Move-Item "ErrorClassifier.cs" "src/OcrSnippingApp/Utilities/"
Move-Item "VisionConfidence.cs" "src/OcrSnippingApp/Utilities/"
```

#### 2.2 ドキュメント移動
```powershell
Move-Item "*_*.md" "docs/"
Move-Item "目次.md" "docs/"
```

#### 2.3 アセット移動
```powershell
Move-Item "image.png" "assets/"
Move-Item "ocr_test_set1_corpus_corrected.html" "assets/test-images/"
```

### フェーズ3: プロジェクトファイル更新 🔧

#### 3.1 OcrSnippingApp.csproj 更新
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <StartupObject>OcrSnippingApp.Program</StartupObject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Google.Cloud.Vision.V1" Version="3.3.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.10" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.10" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.10" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

#### 3.2 ソリューションファイル更新
```powershell
# 新しいソリューション作成
dotnet new sln -n OcrSnippingApp

# プロジェクト追加
dotnet sln add src/OcrSnippingApp/OcrSnippingApp.csproj
```

### フェーズ4: namespace更新 🔄

#### Forms namespace
```csharp
namespace OcrSnippingApp.Forms
{
    public partial class MainForm : Form { }
    public partial class SnippingForm : Form { }
}
```

#### Services namespace
```csharp
namespace OcrSnippingApp.Services
{
    public class Logger { }
    public class SettingsService { }
    public class VisionClientService { }
}
```

#### Utilities namespace
```csharp
namespace OcrSnippingApp.Utilities
{
    public static class ErrorClassifier { }
    public static class VisionConfidence { }
}
```

## 🎯 ベストプラクティス採用効果

### ✅ **開発効率向上**
- **責任分離**: Forms, Services, Utilities で明確な役割分担
- **保守性**: 関連ファイルが同じディレクトリに集約
- **拡張性**: 新機能追加時の配置場所が明確

### ✅ **チーム開発対応**
- **標準化**: .NET プロジェクトの一般的な構造
- **IDE支援**: Visual Studio/VS Code での効率的なナビゲーション
- **CI/CD**: 自動ビルド・テストの設定が容易

### ✅ **品質向上**
- **テスト分離**: tests/ ディレクトリで単体テスト整理
- **ドキュメント管理**: docs/ で技術文書の一元管理
- **アセット管理**: リソースファイルの整理

## 📝 移行後の作業

### 必須更新
1. **using文の更新**: namespace変更に伴うimport修正
2. **プロジェクト参照**: 相対パスの調整
3. **ビルド設定**: 出力パス・中間パスの確認
4. **デバッグ設定**: 作業ディレクトリの調整

### 推奨追加
1. **EditorConfig**: コード規約統一
2. **Directory.Build.props**: 共通設定
3. **GitHub Actions**: CI/CD設定
4. **単体テスト**: xUnit プロジェクト追加

---

**実行判断**: この構造変更は**破壊的変更**のため、現在の動作を確認してから実施することを推奨します。

**推奨タイミング**: 
- ✅ 機能完成・動作確認後
- ✅ git commit完了後  
- ✅ バックアップ取得後