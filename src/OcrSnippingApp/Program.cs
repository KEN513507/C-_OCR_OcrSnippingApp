using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OcrSnippingApp.Services;

namespace OcrSnippingApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // ログフォルダを必ず作成してからロガー初期化を行う
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OcrSnippingApp",
                "Logs");
            Directory.CreateDirectory(logDir);

            // 単一インスタンスを保証
            using var mutex = new Mutex(true, "OcrSnippingApp.Singleton", out var created);
            if (!created)
            {
                Logger.LogWarning("既に別のインスタンスが起動しているため終了します。");
                return;
            }

            Logger.LogInfo("UI初期化シーケンス開始");

            // グローバル例外ハンドラ
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                Logger.LogError("UIスレッドで未処理の例外が発生しました。", e.Exception);
                MessageBox.Show(
                    $"予期しないエラーが発生しました。\n\n{e.Exception.Message}\n\nアプリを再起動するか、ログを確認してください。",
                    "アプリケーションエラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.LogError("バックグラウンドスレッドで未処理の例外が発生しました。", ex);
                }
                else
                {
                    Logger.LogError("バックグラウンドスレッドで未処理の例外が発生しました。(詳細不明)");
                }
            };

            Logger.LogInfo("アプリケーション起動シーケンス開始");

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); // DPI設定
            ApplicationConfiguration.Initialize();

            // ホットキーウィンドウの作成（フォーム表示状態に依存しない）
            var hotkeyWindow = new HotkeyWindow();
            var settingsService = new SettingsService();
            var settings = settingsService.Current;

            // MainFormの作成
            var mainForm = new MainForm();

            // ホットキー押下時のイベント接続
            hotkeyWindow.HotkeyPressed += () =>
            {
                // UIスレッドで実行
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => mainForm.TriggerSnipping()));
                }
                else
                {
                    mainForm.TriggerSnipping();
                }
            };

            // ホットキー登録
            var mods = HotkeyWindow.ParseModifiers(settings.HotkeyModifiers);
            var key = HotkeyWindow.ParseKey(settings.HotkeyKey);

            if (mods == 0 || key == Keys.None)
            {
                Logger.LogInfo("Hotkey disabled: configuration empty. Registration skipped");
            }
            else
            {
                hotkeyWindow.Register(mods, key);
            }

            // アプリケーション終了時のクリーンアップ
            Application.ApplicationExit += (_, __) =>
            {
                try
                {
                    hotkeyWindow.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"HotkeyWindow dispose error: {ex.Message}");
                }
            };

            Application.Run(mainForm);
        }
    }
}
