using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Cloud.Vision.V1;
using VisionImage = Google.Cloud.Vision.V1.Image;
using OcrSnippingApp.Services;

namespace OcrSnippingApp
{
    public partial class SnippingForm : Form
    {
        private bool isSelecting = false;
        private Point startPoint;
        private Rectangle selectionRect;
        private Bitmap? screenShot;
        private readonly SettingsService settingsService;

        public SnippingForm(SettingsService settings)
        {
            InitializeComponent();
            settingsService = settings;
            Logger.LogInfo("スニッピングフォームを初期化");
            CaptureScreen();
        }

        private void CaptureScreen()
        {
            Logger.LogInfo("画面キャプチャを開始");
            // 全画面キャプチャ
            Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            Logger.LogInfo($"キャプチャサイズ: {bounds.Width}x{bounds.Height}");
            screenShot = new Bitmap(bounds.Width, bounds.Height);


            using (Graphics g = Graphics.FromImage(screenShot))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            // フォームを全画面表示
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackgroundImage = screenShot;
            this.BackgroundImageLayout = ImageLayout.Stretch;
            this.Cursor = Cursors.Cross;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                // 選択範囲の計算
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(e.X - startPoint.X);
                int height = Math.Abs(e.Y - startPoint.Y);


                selectionRect = new Rectangle(x, y, width, height);
                this.Invalidate(); // 再描画
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                isSelecting = false;


                if (selectionRect.Width > 10 && selectionRect.Height > 10)
                {
                    Logger.LogInfo($"選択範囲確定: {selectionRect.Width}x{selectionRect.Height} at ({selectionRect.X}, {selectionRect.Y})");
                    // 選択範囲をキャプチャしてOCR処理
                    ProcessSelectedArea();
                }
                else
                {
                    Logger.LogInfo($"選択範囲が小さすぎるためキャンセル: {selectionRect.Width}x{selectionRect.Height}");
                    this.Close();
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);


            if (isSelecting && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                // 選択範囲の枠を描画（太い赤線）
                using (Pen pen = new Pen(Color.Red, 5))
                {
                    e.Graphics.DrawRectangle(pen, selectionRect);
                }

                // 選択範囲外を暗くする

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    Rectangle screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

                    // 上部

                    e.Graphics.FillRectangle(brush, 0, 0, screen.Width, selectionRect.Y);
                    // 下部
                    e.Graphics.FillRectangle(brush, 0, selectionRect.Bottom, screen.Width, screen.Height - selectionRect.Bottom);
                    // 左部
                    e.Graphics.FillRectangle(brush, 0, selectionRect.Y, selectionRect.X, selectionRect.Height);
                    // 右部
                    e.Graphics.FillRectangle(brush, selectionRect.Right, selectionRect.Y, screen.Width - selectionRect.Right, selectionRect.Height);
                }
            }
        }

        private async void ProcessSelectedArea()
        {
            var startTime = DateTime.Now;
            var settings = settingsService.Current;

            try
            {
                Logger.LogInfo("OCR処理を開始");
                if (screenShot == null) return;

                using var selectedImage = new Bitmap(selectionRect.Width, selectionRect.Height);
                using (Graphics g = Graphics.FromImage(selectedImage))
                {
                    g.DrawImage(screenShot, 0, 0, selectionRect, GraphicsUnit.Pixel);
                }

                Logger.LogInfo("画像抽出完了、Vision APIに送信中...");

                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Math.Max(1000, settings.TimeoutMs)));
                var response = await PerformOcrRequestAsync(selectedImage, settings.LanguageHint, cts.Token);

                var text = ExtractText(response);
                var confidence = VisionConfidence.TryComputeAverageConfidence(response);

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                Logger.LogInfo($"OCR処理完了: {elapsed:F0}ms, テキスト長: {text?.Length ?? 0}文字, 信頼度={(confidence.HasValue ? confidence.Value.ToString("0.00") : "n/a")}");

                RunOnUiThread(() => HandleOcrOutcome(text, confidence, OcrIssue.None, null, null));
            }
            catch (Exception ex)
            {
                var root = UnwrapException(ex);
                var (kind, userMessage, tip) = OcrTroubleMessage.FromException(root);
                Logger.LogError($"OCR処理中にエラーが発生: {kind}", root);

                var errorDetails = new System.Text.StringBuilder();
                errorDetails.AppendLine("OCR処理中にエラーが発生しました。");
                errorDetails.AppendLine();
                errorDetails.AppendLine($"エラー: {root.Message}");
                errorDetails.AppendLine();
                errorDetails.AppendLine($"エラータイプ: {root.GetType().Name}");

                if (root.InnerException != null)
                {
                    errorDetails.AppendLine();
                    errorDetails.AppendLine($"内部エラー: {root.InnerException.Message}");
                }

                errorDetails.AppendLine();
                errorDetails.AppendLine("スタックトレース:");
                errorDetails.AppendLine(root.StackTrace);

                // catchブロック内で 'elapsed', 'text', 'confidence', 'credPath' は未定義なので、これらの行を削除


                MessageBox.Show(
                    errorDetails.ToString(),
                    "OCRエラー詳細",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                RunOnUiThread(() =>
                {
                    var message = userMessage ?? "OCR実行中にエラーが発生しました。";
                    if (!string.IsNullOrWhiteSpace(tip))
                    {
                        message += Environment.NewLine + tip;
                    }
                    ShowNotification("OCR失敗", message, ToolTipIcon.Error);
                    Close();
                });
            }
        }

        private async Task<AnnotateImageResponse> PerformOcrRequestAsync(Bitmap image, string? languageHint, CancellationToken cancellationToken)
        {
            var client = VisionClientService.Instance.Client;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);
                var visionImage = VisionImage.FromBytes(stream.ToArray());

                var context = new ImageContext();
                if (!string.IsNullOrWhiteSpace(languageHint))
                {
                    context.LanguageHints.Add(languageHint);
                    Logger.LogDebug($"言語ヒント適用: {languageHint}");
                }

                var request = new AnnotateImageRequest
                {
                    Image = visionImage,
                    ImageContext = context
                };
                request.Features.Add(new Feature { Type = Feature.Types.Type.DocumentTextDetection });

                Logger.LogInfo($"Vision API リクエスト開始: 画像サイズ={stream.Length}bytes, 言語ヒント={languageHint ?? "なし"}");

                try
                {
                    var response = await RetryAsync(
                        () => client.AnnotateAsync(request, cancellationToken: cancellationToken),
                        maxRetries: 2);
                    
                    sw.Stop();
                    var textLength = response?.TextAnnotations?.Count > 0 ? response.TextAnnotations[0].Description?.Length ?? 0 : 0;
                    Logger.LogInfo($"Vision API 成功: {sw.ElapsedMilliseconds}ms, テキスト={textLength}文字");
                    
                    return response!;
                }
                catch (Grpc.Core.RpcException ex)
                {
                    sw.Stop();
                    Logger.LogError($"Vision API RPC エラー: {ex.StatusCode} - {ex.Status.Detail}, {sw.ElapsedMilliseconds}ms");
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Logger.LogError($"Vision API エラー: {ex.GetType().Name} - {ex.Message}, {sw.ElapsedMilliseconds}ms");
                    throw;
                }
            }
        }

        private void HandleOcrOutcome(string? text, double? confidence, OcrIssue issue, string? userMessage, string? tip)
        {
            if (issue == OcrIssue.None && !string.IsNullOrWhiteSpace(text))
            {
                Logger.LogInfo($"OCRテキスト取得: {text.Length}文字, 信頼度={(confidence.HasValue ? confidence.Value.ToString("0.00") : "n/a")}");
                Logger.LogDebug($"テキスト内容(先頭60文字): {text.Substring(0, Math.Min(60, text.Length))}");

                var augmented = settingsService.BuildAugmentedText(text);
                if (CopyToClipboardWithRetry(augmented, out var fallbackPath))
                {
                    var current = settingsService.Current;
                    Logger.LogInfo($"OCR copied. len={augmented.Length}, append={current.AppendModeEnabled}, blank={current.AddBlankLines}, prefixLen={current.PrefixText?.Length ?? 0}, suffixLen={current.SuffixText?.Length ?? 0}");

                    var preview = augmented.Trim();
                    if (preview.Length > 60)
                    {
                        preview = preview.Substring(0, 60) + "…";
                    }

                    var modeText = current.AppendModeEnabled ? "追記モード適用済み" : "標準モード";
                    ShowNotification("OCR完了", $"{modeText}\nテキストをクリップボードにコピーしました:\n{preview}", ToolTipIcon.Info);
                }
                else
                {
                    ShowNotification("コピー失敗", fallbackPath is null
                        ? "クリップボードにコピーできませんでした。"
                        : $"クリップボードにコピーできませんでした。代わりに {fallbackPath} に保存しました。",
                        ToolTipIcon.Warning);
                }
            }
            else
            {
                var message = userMessage ?? "OCR結果に注意が必要です。";
                if (!string.IsNullOrWhiteSpace(tip))
                {
                    message += Environment.NewLine + tip;
                }

                Logger.LogWarning($"OCR結果: kind={issue}, 信頼度={(confidence.HasValue ? confidence.Value.ToString("0.00") : "n/a")}, message={message.Replace(Environment.NewLine, " ")}");
                ShowNotification("OCR注意", message, ToolTipIcon.Warning);
            }

            Close();
        }

        private void RunOnUiThread(Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private bool CopyToClipboardWithRetry(string text, out string? fallbackPath)
        {
            Logger.LogInfo($"Clipboard copy start (threadAPT={Thread.CurrentThread.GetApartmentState()})");

            const int maxTry = 5;
            fallbackPath = null;

            for (int attempt = 1; attempt <= maxTry; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true, 10, 100);
                    var back = Clipboard.GetText();
                    if (string.Equals(back, text, StringComparison.Ordinal))
                    {
                        Logger.LogInfo($"Clipboard copy OK len={text.Length} try={attempt}");
                        return true;
                    }

                    Logger.LogWarning($"Clipboard verify mismatch try={attempt}");
                }
                catch (ExternalException ex)
                {
                    Logger.LogWarning($"Clipboard copy failed try={attempt}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Clipboard unexpected error try={attempt}: {ex.Message}");
                }

                Thread.Sleep(120);
            }

            fallbackPath = Path.Combine(Path.GetTempPath(), "ocr_result.txt");
            File.WriteAllText(fallbackPath, text, System.Text.Encoding.UTF8);
            Logger.LogError($"Clipboard最終失敗。fallback保存: {fallbackPath}");
            return false;
        }

        private static bool ShouldRetry(Grpc.Core.StatusCode statusCode)
        {
            return statusCode == Grpc.Core.StatusCode.ResourceExhausted ||
                   statusCode == Grpc.Core.StatusCode.Unavailable ||
                   statusCode == Grpc.Core.StatusCode.Internal ||
                   statusCode == Grpc.Core.StatusCode.Unknown ||
                   statusCode == Grpc.Core.StatusCode.DeadlineExceeded;
        }

        private static async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 2)
        {
            var delay = 300;
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Grpc.Core.RpcException ex) when (ShouldRetry(ex.Status.StatusCode) && attempt < maxRetries)
                {
                    Logger.LogWarning($"Vision API呼び出し失敗 (Status: {ex.Status.StatusCode}). {delay}ms後に再試行します。詳細: {ex.Status.Detail}");
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay = Math.Min(delay * 2, 1000);
                }
            }
        }

        private static Exception UnwrapException(Exception ex)
        {
            while (ex is AggregateException agg && agg.InnerExceptions.Count == 1)
            {
                ex = agg.InnerExceptions[0];
            }

            return ex;
        }

        private static string? ExtractText(AnnotateImageResponse response)
        {
            var text = response?.FullTextAnnotation?.Text;
            if (string.IsNullOrWhiteSpace(text) && response?.TextAnnotations?.Count > 0)
            {
                Logger.LogDebug("FullTextAnnotationが空のためTextAnnotations[0]にフォールバック");
                text = response.TextAnnotations[0].Description;
            }

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            var settings = settingsService.Current;


            if (!settings.Notifications)
                return;

            // バルーン通知表示
            var trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Information,
                Visible = true
            };

            // メッセージを適度な長さに制限

            var displayMessage = message.Length > 100 ? message.Substring(0, 100) + "..." : message;


            trayIcon.ShowBalloonTip(3000, title, displayMessage, icon);

            // 3秒後にアイコンを非表示

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 3500;
            timer.Tick += (s, e) =>
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                timer.Dispose();
            };
            timer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Escapeキーで終了
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            base.OnKeyDown(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                screenShot?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
