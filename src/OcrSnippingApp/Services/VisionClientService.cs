using System;
using Google.Cloud.Vision.V1;
using Google.Apis.Auth.OAuth2;

namespace OcrSnippingApp.Services
{
    /// <summary>
    /// Google Cloud Vision APIクライアントのシングルトン管理
    /// プロセス内で一度のみ初期化し、再利用する
    /// </summary>
    public sealed class VisionClientService
    {
        private static readonly Lazy<VisionClientService> _instance =
            new Lazy<VisionClientService>(() => new VisionClientService());

        private readonly ImageAnnotatorClient _client;
        private readonly DateTime _initializeTime;

        private VisionClientService()
        {
            _initializeTime = DateTime.Now;

            // 環境変数の確認
            var credPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrEmpty(credPath))
            {
                throw new InvalidOperationException(
                    "環境変数 GOOGLE_APPLICATION_CREDENTIALS が設定されていません。\n\n" +
                    "PowerShellで以下のコマンドを実行してください:\n" +
                    "$env:GOOGLE_APPLICATION_CREDENTIALS = \"C:\\Keys\\ocr-snipping-app.json\""
                );
            }

            if (!System.IO.File.Exists(credPath))
            {
                throw new InvalidOperationException(
                    $"認証ファイルが見つかりません: {credPath}\n\n" +
                    "ファイルが存在するか確認してください。"
                );
            }

            try
            {
                _client = ImageAnnotatorClient.Create();
                LogInitialization();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Vision APIクライアントの初期化に失敗しました: {ex.Message}",
                    ex
                );
            }
        }

        public static VisionClientService Instance => _instance.Value;

        public ImageAnnotatorClient Client => _client;

        public DateTime InitializeTime => _initializeTime;

        private void LogInitialization()
        {
            // デバッグ用ログ（必要に応じてファイルに出力）
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Vision APIクライアントを初期化しました。";
            System.Diagnostics.Debug.WriteLine(logMessage);

            // オプション：ログファイルに出力
            try
            {
                var logPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "OcrSnippingApp_VisionClient.log"
                );
                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // ログ出力失敗は無視
            }
        }

        public static bool IsInitialized => _instance.IsValueCreated;
    }
}