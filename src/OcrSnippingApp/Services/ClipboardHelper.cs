using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OcrSnippingApp.Services
{
    /// <summary>
    /// クリップボード操作を堅牢に行うためのヘルパークラス。
    /// UIスレッド以外からの呼び出しや、他プロセスによるロックを考慮します。
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// 指定されたテキストをクリップボードに設定します。
        /// 複数回のリトライとSTAスレッドでのフォールバック実行を試みます。
        /// </summary>
        /// <param name="text">クリップボードに設定するテキスト。</param>
        /// <param name="retries">SetDataObjectの試行回数。</param>
        /// <param name="delayMs">リトライ間の待機時間(ミリ秒)。</param>
        /// <returns>成功した場合はtrue、失敗した場合はfalse。</returns>
        public static bool ClipboardTrySet(string text, int retries = 8, int delayMs = 150)
        {
            if (string.IsNullOrEmpty(text)) return false;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // SetDataObjectは、他のプロセスがクリップボードを使用していても、
                    // 指定された回数リトライする機能を持つため、より堅牢です。
                    // 第二引数: trueはアプリケーション終了後もデータを保持する。
                    Clipboard.SetDataObject(text, true, 5, 200);
                    return true;
                }
                catch (ExternalException)
                {
                    // クリップボードが他のプロセスによってロックされている場合に発生します。
                    Thread.Sleep(delayMs);
                }
                catch
                {
                    // その他の予期せぬ例外が発生した場合は、STAスレッドでの実行にフォールバックします。
                    return ClipboardOnSta(text);
                }
            }
            // 指定回数リトライしても失敗した場合は、最終手段としてSTAスレッドで試みます。
            return ClipboardOnSta(text);
        }

        /// <summary>
        /// 新しいSTAスレッドを生成し、そのスレッドでクリップボードにテキストを設定します。
        /// これは、UIスレッド以外からクリップボードを操作する際の最終手段です。
        /// </summary>
        /// <param name="text">クリップボードに設定するテキスト。</param>
        /// <returns>成功した場合はtrue、失敗した場合はfalse。</returns>
        private static bool ClipboardOnSta(string text)
        {
            bool ok = false;
            var t = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                    ok = true;
                }
                catch { ok = false; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join(); // スレッドの完了を待機
            return ok;
        }
    }
}
