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
        private readonly SettingsService _settings;

        private VisionClientService()
        {
            _initializeTime = DateTime.Now;
            _settings = new SettingsService();

            try
            {
                _client = CreateClient();
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

        /// <summary>
        /// 認証情報を解決してクライアントを作成
        /// 優先順位:
        /// 1. appsettings.json の GoogleCredentialPath
        /// 2. 環境変数 GOOGLE_APPLICATION_CREDENTIALS (Process → User → Machine)
        /// 3. デフォルトパス: google-credentials.json
        /// </summary>
        private ImageAnnotatorClient CreateClient()
        {
            // デバッグ情報をログ出力
            Logger.LogInfo($"AppDir={AppContext.BaseDirectory}");
            Logger.LogInfo($"AppSettings.GoogleCredentialPath={_settings.Current.GoogleCredentialPath ?? "<null>"}");

            // 1) appsettings.json から取得
            string? path = _settings.Current.GoogleCredentialPath;

            // ヘルパー関数: 環境変数を取得
            string GetEnv(EnvironmentVariableTarget target)
            {
                return Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", target) ?? string.Empty;
            }

            // 2) 環境変数から取得 (Process → User → Machine の順)
            if (string.IsNullOrWhiteSpace(path))
                path = GetEnv(EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(path))
                path = GetEnv(EnvironmentVariableTarget.User);
            if (string.IsNullOrWhiteSpace(path))
                path = GetEnv(EnvironmentVariableTarget.Machine);

            // 3) デフォルトパス
            if (string.IsNullOrWhiteSpace(path))
                path = System.IO.Path.Combine(AppContext.BaseDirectory, "google-credentials.json");

            // 検証
            if (!System.IO.File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Vision credentials not found: '{path}'\n\n" +
                    "設定方法:\n" +
                    "1. appsettings.json に GoogleCredentialPath を追加\n" +
                    "2. 環境変数 GOOGLE_APPLICATION_CREDENTIALS を設定\n" +
                    "3. google-credentials.json をアプリと同じフォルダに配置"
                );
            }

            Logger.LogInfo($"Vision: using credential '{path}'");
            return new ImageAnnotatorClientBuilder { CredentialsPath = path }.Build();
        }

        private void LogInitialization()
        {
            var logMessage = $"Vision APIクライアントを初期化しました。 ({_initializeTime:yyyy-MM-dd HH:mm:ss})";
            Logger.LogInfo(logMessage);
        }

        public static bool IsInitialized => _instance.IsValueCreated;
    }
}