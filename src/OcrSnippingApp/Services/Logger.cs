namespace OcrSnippingApp.Services
{
    /// <summary>
    /// アプリケーションのログ記録サービス
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath;
        private static readonly object LockObject = new object();
        private const long MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB

        static Logger()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OcrSnippingApp",
                "Logs"
            );

            Directory.CreateDirectory(logDir);

            var logFileName = $"OcrSnippingApp_{DateTime.Now:yyyyMMdd}.log";
            LogFilePath = Path.Combine(logDir, logFileName);

            // アプリケーション起動時のログ
            LogInfo("========================================");
            LogInfo($"アプリケーション起動: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo($"ログファイル: {LogFilePath}");
            LogInfo("========================================");
        }

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
            Console.WriteLine($"[INFO] {message}"); // ← 追加
        }

        public static void LogError(string message, Exception? ex = null)
        {
            WriteLog("ERROR", message);
            if (ex is not null)
            {
                WriteLog("ERROR", $"  例外: {ex.GetType().Name}");
                WriteLog("ERROR", $"  メッセージ: {ex.Message}");
                WriteLog("ERROR", $"  スタックトレース: {ex.StackTrace}");
            }
        }

        public static void LogDebug(string message)
        {
#if DEBUG
            WriteLog("DEBUG", message);
#endif
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (LockObject)
                {
                    RotateIfNeeded();

                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine, System.Text.Encoding.UTF8);

                    // デバッグビルド時はコンソールにも出力
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // ログ出力失敗は無視（無限ループ防止）
            }
        }

        public static string GetLogFilePath() => LogFilePath;

        /// <summary>
        /// 古いログファイルを削除（7日以上前のファイル）
        /// </summary>
        public static void CleanOldLogs()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-7);
                var files = Directory.GetFiles(logDir, "OcrSnippingApp_*.log");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        LogInfo($"古いログファイルを削除: {fileInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("古いログファイルの削除中にエラーが発生", ex);
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                var fileInfo = new FileInfo(LogFilePath);
                if (fileInfo.Length <= MaxLogFileSizeBytes)
                    return;

                var directory = Path.GetDirectoryName(LogFilePath);
                if (string.IsNullOrEmpty(directory))
                    return;

                var archiveName = Path.GetFileNameWithoutExtension(LogFilePath) + $"_{DateTime.Now:HHmmss}.log";
                var archivePath = Path.Combine(directory, archiveName);

                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }

                File.Move(LogFilePath, archivePath);

                var rotationMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] ログローテーション: {Path.GetFileName(archivePath)} へ退避しました。";
                File.AppendAllText(LogFilePath, rotationMessage + Environment.NewLine);
            }
            catch
            {
                // ローテーション失敗時は無視（アプリ動作への影響を避ける）
            }
        }
    }
}
