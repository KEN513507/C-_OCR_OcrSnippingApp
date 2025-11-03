using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OcrSnippingApp.Services;

namespace OcrSnippingApp
{
    public partial class MainForm : Form
    {
        private const int HotkeyId = 1;
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private SettingsService settingsService;
        private Icon? applicationIcon;
        private ToolStripMenuItem? appendModeMenuItem;
        private ToolStripMenuItem? addBlankLinesMenuItem;
        private ToolStripMenuItem? editAppendTextMenuItem;
        private ToolStripMenuItem? screenshotMenuItem;


        // テストモード関連
        private bool isInTestMode = false;
        private string testModeInput = string.Empty;
        private const string TestModeKeyword = "テストモード";
        private System.Windows.Forms.Timer? testModeTimer;

        public MainForm()
        {
            InitializeComponent();
            this.Icon = LoadApplicationIcon();
            Logger.LogInfo("MainFormを初期化中...");

            settingsService = new SettingsService();
            Logger.LogInfo($"設定を読み込みました: ホットキー={BuildHotKeyDescription(settingsService.Current.HotkeyModifiers, settingsService.Current.HotkeyKey)}");

            InitializeTrayIcon();
            Logger.LogInfo("トレイアイコンを初期化しました");

            // Vision APIクライアントを事前初期化（バックグラウンドで）
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Logger.LogInfo("Vision APIクライアントを初期化中...");
                    var client = VisionClientService.Instance;
                    Logger.LogInfo($"Vision APIクライアント初期化完了: {client.InitializeTime}");
                }
                catch (Exception ex)
                {
                    // 初期化失敗は初回OCR実行時にエラー表示される
                    Logger.LogError("Vision APIクライアント初期化警告", ex);
                }
            });

            // 古いログファイルをクリーンアップ
            Logger.CleanOldLogs();

            // フォームを非表示にしてタスクトレイのみ表示
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            
            // ウィンドウハンドルを作成（ホットキー登録に必要）
            var handle = this.Handle; // ハンドルを取得することで強制的に作成
            Logger.LogInfo($"ウィンドウハンドル強制作成: Handle={handle}, IsHandleCreated={this.IsHandleCreated}");
            
            this.Visible = false;

            Logger.LogInfo("MainForm初期化完了 - タスクトレイ常駐モード");
        }

        private void InitializeTrayIcon()
        {
            // トレイメニューの作成
            trayMenu = new ContextMenuStrip();
            InitializeAppendModeMenuItems();

            // 設定からホットキー情報を取得してメニューに表示
            screenshotMenuItem = new ToolStripMenuItem(GetHotKeyMenuLabel(), null, OnScreenshotClick!);
            trayMenu.Items.Add(screenshotMenuItem);
            trayMenu.Items.Add("-"); // セパレータ
            trayMenu.Items.Add("終了", null, OnExitClick!);

            if (appendModeMenuItem != null && addBlankLinesMenuItem != null && editAppendTextMenuItem != null)
            {
                trayMenu.Items.Insert(0, editAppendTextMenuItem);
                trayMenu.Items.Insert(0, addBlankLinesMenuItem);
                trayMenu.Items.Insert(0, appendModeMenuItem);
                trayMenu.Items.Insert(3, new ToolStripSeparator());
            }

            // トレイアイコンの作成
            trayIcon = new NotifyIcon()
            {
                Icon = LoadApplicationIcon(),
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            UpdateTrayIconText();

            trayIcon.MouseDoubleClick += OnTrayIconDoubleClick!;

            // テストモード用のタイマー初期化
            InitializeTestMode();

            // ホットキー登録はOnHandleCreatedで実行（ハンドル生成後）
            
            // 初回のみ、未設定なら情報バルーンを表示（任意）
            var s = settingsService.Current;
            if (string.IsNullOrWhiteSpace(s.HotkeyModifiers) || string.IsNullOrWhiteSpace(s.HotkeyKey))
            {
                trayIcon.ShowBalloonTip(2500, "ホットキー未設定", "設定画面からホットキーを設定できます。空欄のままでも利用可能です。", ToolTipIcon.Info);
            }
        }

        // 注: ホットキー登録はProgram.csのHotkeyWindowで実装されています

        private Keys ParseKey(string keyString)
        {
            return Enum.TryParse<Keys>(keyString, true, out var key) ? key : Keys.C;
        }

        private string GetHotKeyMenuLabel()
        {
            var current = settingsService.Current;
            return $"スクリーンショット ({BuildHotKeyDescription(current.HotkeyModifiers, current.HotkeyKey)})";
        }

        private static string BuildHotKeyDescription(string modifiers, string key)
        {
            if (string.IsNullOrWhiteSpace(modifiers) || string.IsNullOrWhiteSpace(key))
            {
                return "ホットキー未設定";
            }

            return $"{modifiers}+{key}";
        }

        private void UpdateTrayIconText()
        {
            if (trayIcon is null)
                return;

            var settings = settingsService.Current;
            var baseText = "OCR Snipping Tool";

            if (settings.AppendModeEnabled)
            {
                baseText += "（追記ON）";
            }

            var hotKeyDesc = BuildHotKeyDescription(settings.HotkeyModifiers, settings.HotkeyKey);
            if (!string.Equals(hotKeyDesc, "ホットキー未設定", StringComparison.Ordinal))
            {
                baseText += $" [{hotKeyDesc}]";
            }

            if (baseText.Length > 63)
            {
                baseText = baseText.Substring(0, 63);
            }

            trayIcon.Text = baseText;
        }

        private Icon LoadApplicationIcon()
        {
            if (applicationIcon is not null)
            {
                return applicationIcon;
            }

            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "appicon.ico");
                Logger.LogInfo($"アイコンファイル検索: {iconPath}");
                
                if (File.Exists(iconPath))
                {
                    applicationIcon = new Icon(iconPath);
                    Logger.LogInfo($"アイコン読み込み成功: {iconPath}");
                    return applicationIcon;
                }
                else
                {
                    Logger.LogWarning($"アイコンファイルが見つかりません: {iconPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"トレイアイコンの読み込みに失敗しました: {ex.Message}");
            }

            Logger.LogInfo("デフォルトアイコンを使用");
            applicationIcon = SystemIcons.Application;
            return applicationIcon;
        }

        private void UpdateHotKeyMenuLabel()
        {
            if (screenshotMenuItem != null)
            {
                screenshotMenuItem.Text = GetHotKeyMenuLabel();
            }

            UpdateTrayIconText();
        }

        private int ParseModifiers(string modifierString)
        {
            int modifiers = 0;
            var parts = modifierString.Split('+');

            foreach (var part in parts)
            {
                switch (part.Trim().ToLower())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= KeyboardHook.MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= KeyboardHook.MOD_ALT;
                        break;
                    case "windows":
                        modifiers |= KeyboardHook.MOD_WIN;
                        break;
                }
            }

            return modifiers;
        }

        // 注: WM_HOTKEYの処理はProgram.csのHotkeyWindowで実装されています

        /// <summary>
        /// ホットキーまたはメニューからスニッピングを開始
        /// </summary>
        public void TriggerSnipping()
        {
            OnScreenshotClick(this, EventArgs.Empty);
        }

        private void OnScreenshotClick(object? sender, EventArgs e)
        {
            Logger.LogInfo($"スクリーンショット処理を開始 (テストモード: {isInTestMode})");

            if (isInTestMode)
            {
                // テストモード時：CREテスターとの連携を強化
                Logger.LogInfo("CREテストモードでOCR実行");
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(2000, "CREテストモード",
                        "画像を選択してOCR実行中...\n結果はCREテスターで確認できます。",
                        ToolTipIcon.Info);
                }
            }

            // スクリーンショットとOCR処理（通常通り実行）
            var snippingForm = new SnippingForm(settingsService);
            snippingForm.ShowDialog();

            if (isInTestMode)
            {
                // テストモード時：OCR完了後にCREテスターにフォーカス
                FocusCreTester();
            }
        }

        private void FocusCreTester()
        {
            try
            {
                // CREテスターのプロセスを探してフォーカス
                var processes = System.Diagnostics.Process.GetProcessesByName("CreTester");
                if (processes.Length > 0)
                {
                    var creProcess = processes[0];
                    if (!creProcess.HasExited)
                    {
                        // ウィンドウを前面に表示
                        KeyboardHook.ShowWindow(creProcess.MainWindowHandle, KeyboardHook.SW_RESTORE);
                        KeyboardHook.SetForegroundWindow(creProcess.MainWindowHandle);
                        Logger.LogInfo("CREテスターにフォーカスを移しました");
                    }
                }
                else
                {
                    Logger.LogInfo("CREテスターのプロセスが見つかりません");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"CREテスターフォーカス処理でエラー: {ex.Message}");
            }
        }

        private void OnTrayIconDoubleClick(object? sender, MouseEventArgs e)
        {
            OnScreenshotClick(sender, e);
        }

        private void InitializeAppendModeMenuItems()
        {
            var current = settingsService.Current;

            appendModeMenuItem = new ToolStripMenuItem("追記モードを有効化")
            {
                CheckOnClick = true,
                Checked = current.AppendModeEnabled
            };
            appendModeMenuItem.CheckedChanged += OnAppendModeToggleChanged!;

            addBlankLinesMenuItem = new ToolStripMenuItem("上下に空行を挿入")
            {
                CheckOnClick = true,
                Checked = current.AddBlankLines,
                Enabled = current.AppendModeEnabled
            };
            addBlankLinesMenuItem.CheckedChanged += OnAddBlankLinesToggleChanged!;

            editAppendTextMenuItem = new ToolStripMenuItem("前後テキストを編集...");
            editAppendTextMenuItem.Click += OnEditAppendTextClicked!;
        }

        private void OnAppendModeToggleChanged(object? sender, EventArgs e)
        {
            if (appendModeMenuItem == null || addBlankLinesMenuItem == null)
                return;

            addBlankLinesMenuItem.Enabled = appendModeMenuItem.Checked;
            settingsService.UpdateAppendModeSettings(appendModeMenuItem.Checked, addBlankLinesMenuItem.Checked);

            Logger.LogInfo($"AppendModeEnabled={(appendModeMenuItem.Checked ? "true" : "false")}");
            UpdateTrayIconText();

            trayIcon?.ShowBalloonTip(1500, "設定変更",
                appendModeMenuItem.Checked ? "追記モードを有効化しました。" : "追記モードを無効化しました。",
                ToolTipIcon.Info);
        }

        private void OnAddBlankLinesToggleChanged(object? sender, EventArgs e)
        {
            if (appendModeMenuItem == null || addBlankLinesMenuItem == null)
                return;

            settingsService.UpdateAppendModeSettings(appendModeMenuItem.Checked, addBlankLinesMenuItem.Checked);
            Logger.LogInfo($"AddBlankLines={addBlankLinesMenuItem.Checked}");
        }

        private void OnEditAppendTextClicked(object? sender, EventArgs e)
        {
            var current = settingsService.Current;

            using var dialog = new AppendTextDialog(current.PrefixText ?? string.Empty,
                                                    current.SuffixText ?? string.Empty,
                                                    AppLimits.MaxAppendLen);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var (ok, message) = settingsService.SaveAppendTexts(dialog.PrefixText, dialog.SuffixText);
                if (ok)
                {
                    Logger.LogInfo($"Append texts updated: prefixLen={dialog.PrefixText.Length}, suffixLen={dialog.SuffixText.Length}");
                    if (!string.IsNullOrEmpty(message))
                    {
                        MessageBox.Show(this, message, "追記テキスト", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            Logger.LogInfo("アプリケーションを終了します");
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
            Application.Exit();
        }

        protected override void SetVisibleCore(bool value)
        {
            // フォームが表示されないようにする
            base.SetVisibleCore(false);
        }

        private void InitializeTestMode()
        {
            // テストモード用タイマーの初期化
            testModeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000 // 3秒でテストモード入力をリセット
            };
            testModeTimer.Tick += OnTestModeTimeout!;

            // グローバルキーボードフックの初期化
            SetupGlobalKeyboardHook();
        }

        private void SetupGlobalKeyboardHook()
        {
            // 簡易実装：ホットキーでテストモード切替
            // 実際のキーボード入力は、トレイアイコンのコンテキストメニューに追加
            AddTestModeMenuItems();
        }

        private void AddTestModeMenuItems()
        {
            if (trayMenu != null)
            {
                // セパレータの前に追加
                var testModeItem = new ToolStripMenuItem("CREテストモード");
                testModeItem.Click += (sender, e) => ActivateTestMode();

                // セパレータの前に挿入
                trayMenu.Items.Insert(trayMenu.Items.Count - 2, testModeItem);
            }
        }

        private void OnFormKeyPress(object sender, KeyPressEventArgs e)
        {
            // この実装は削除（グローバルフックに置き換え）
        }

        private void OnTestModeTimeout(object sender, EventArgs e)
        {
            // タイムアウトでテストモード入力をリセット
            testModeInput = string.Empty;
            testModeTimer?.Stop();
        }

        private void ActivateTestMode()
        {
            isInTestMode = !isInTestMode;

            if (isInTestMode)
            {
                // CREテスターを起動
                StartCreTester();

                // トレイアイコンのテキストを変更
                if (trayIcon != null)
                {
                    trayIcon.Text = "OCR Snipping Tool - CREテストモード";
                    trayIcon.ShowBalloonTip(3000, "CREテストモード",
                        "CREテストモードが有効になりました。\nCtrl+Alt+Cで選択した画像のCER計算ができます。",
                        ToolTipIcon.Info);
                }

                Logger.LogInfo("CREテストモードが有効になりました");
            }
            else
            {
                // 通常モードに戻す
                if (trayIcon != null)
                {
                    trayIcon.Text = "OCR Snipping Tool";
                    trayIcon.ShowBalloonTip(3000, "通常モード",
                        "通常モードに戻りました。",
                        ToolTipIcon.Info);
                }

                Logger.LogInfo("通常モードに戻りました");
            }
        }

        private void StartCreTester()
        {
            try
            {
                // CREテスターのパスを取得
                var creTestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "CreTester", "CreTester.exe");

                if (!File.Exists(creTestPath))
                {
                    // 開発環境の場合はdotnet runで起動
                    var creTestProjectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "tools", "CreTester");

                    if (Directory.Exists(creTestProjectPath))
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "run",
                            WorkingDirectory = creTestProjectPath,
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };

                        System.Diagnostics.Process.Start(startInfo);
                        Logger.LogInfo("CREテスターを起動しました (dotnet run)");
                    }
                    else
                    {
                        MessageBox.Show("CREテスターが見つかりません。\ntools/CreTesterディレクトリを確認してください。",
                                      "CREテスター起動エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // リリース版の実行ファイルで起動
                    System.Diagnostics.Process.Start(creTestPath);
                    Logger.LogInfo("CREテスターを起動しました");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("CREテスター起動エラー", ex);
                MessageBox.Show($"CREテスターの起動に失敗しました:\n{ex.Message}",
                              "起動エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (trayIcon is not null)
                {
                    trayIcon.Visible = false;
                }
                KeyboardHook.UnregisterHotKey(this.Handle, HotkeyId);
                trayIcon?.Dispose();
                trayMenu?.Dispose();
                testModeTimer?.Dispose();
                if (applicationIcon is not null && !ReferenceEquals(applicationIcon, SystemIcons.Application))
                {
                    applicationIcon.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    // 簡易ホットキー登録クラス
    public static class KeyboardHook
    {
        public const int WM_HOTKEY = 0x0312;
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
        public const int MOD_NOREPEAT = 0x4000;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public const int SW_RESTORE = 9;

        public static bool RegisterHotKey(IntPtr handle, int id, Keys key, int modifiers)
        {
            return RegisterHotKey(handle, id, modifiers, (int)key);
        }
    }

    // ホットキー解析用ヘルパークラス
    internal static class HotkeyParser
    {
        public static bool TryParse(string modsStr, string keyStr, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;

            if (string.IsNullOrWhiteSpace(modsStr) || string.IsNullOrWhiteSpace(keyStr))
                return false;

            // モディファイヤの解析
            foreach (var m in modsStr.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                switch (m.ToLowerInvariant())
                {
                    case "ctrl": mods |= 0x0002; break;
                    case "alt": mods |= 0x0001; break;
                    case "shift": mods |= 0x0004; break;
                    case "win": mods |= 0x0008; break;
                    default: return false;
                }
            }

            // キーの解析
            if (keyStr.Length == 1)
            {
                vk = char.ToUpperInvariant(keyStr[0]);
            }
            else if (Enum.TryParse(typeof(Keys), keyStr, true, out var k))
            {
                vk = (uint)(Keys)k;
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
