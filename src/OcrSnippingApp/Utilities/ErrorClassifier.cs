using System;
using System.Net.Http;
using Grpc.Core;

namespace OcrSnippingApp
{
    public enum OcrIssue
    {
        None,
        NetworkUnreachable,
        Timeout,
        QuotaExceeded,
        PermissionDenied,
        CredentialsMissing,
        InvalidRequest,
        ServiceUnavailable,
        UnknownError,
        LowConfidence,
        NoTextDetected
    }

    public static class OcrTroubleMessage
    {
        public static (OcrIssue kind, string userMessage, string? detailTip) FromException(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return (OcrIssue.Timeout,
                    "Vision API の応答がタイムアウトしました。",
                    "回線状況を確認し、数秒後に再試行してください。TimeoutMs を上げることも検討。");
            }

            if (ex is HttpRequestException)
            {
                return (OcrIssue.NetworkUnreachable,
                    "ネットワークに接続できませんでした。",
                    "Wi-Fi/有線、VPN/プロキシ設定、社内FWを確認してください。");
            }

            if (ex is RpcException rpc)
            {
                return rpc.Status.StatusCode switch
                {
                    StatusCode.DeadlineExceeded => (OcrIssue.Timeout,
                        "Vision API の応答がタイムアウトしました。",
                        "回線状況を確認し、数秒後に再試行してください。TimeoutMs を上げることも検討。"),

                    StatusCode.Unavailable => (OcrIssue.ServiceUnavailable,
                        "Vision サービスに一時的に接続できません。",
                        "しばらく待ってから再試行してください。ステータスページや社内ネットのメンテも確認。"),

                    StatusCode.PermissionDenied => (OcrIssue.PermissionDenied,
                        "Vision API の権限がありません（PermissionDenied）。",
                        "サービスアカウントに roles/visionai.user 以上が付与されているか確認。"),

                    StatusCode.ResourceExhausted => (OcrIssue.QuotaExceeded,
                        "Vision API のクォータを超過しました（ResourceExhausted）。",
                        "クォータ/課金設定を見直すか、時間をおいて実行してください。"),

                    StatusCode.InvalidArgument => (OcrIssue.InvalidRequest,
                        "要求が不正です（InvalidArgument）。",
                        "画像サイズ・形式やリクエスト構造を確認してください。PNG/JPEG推奨。"),

                    _ => (OcrIssue.UnknownError,
                        $"OCR で不明なエラーが発生しました（{rpc.Status.StatusCode}）。",
                        rpc.Status.Detail)
                };
            }

            if (ex.Message.Contains("Application Default Credentials", StringComparison.OrdinalIgnoreCase))
            {
                return (OcrIssue.CredentialsMissing,
                    "認証情報が見つかりません（ADC 未設定）。",
                    "環境変数 GOOGLE_APPLICATION_CREDENTIALS に JSON の絶対パスを設定してください。");
            }

            return (OcrIssue.UnknownError, "OCR 実行中にエラーが発生しました。", ex.Message);
        }

        public static (OcrIssue kind, string userMessage, string? tip) FromResult(string? text, float? avgConfidence)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (OcrIssue.NoTextDetected,
                    "選択範囲にテキストが見つかりませんでした。",
                    "もう少し広めに選択してください。");
            }

            if (avgConfidence.HasValue && avgConfidence.Value < 0.55f)
            {
                return (OcrIssue.LowConfidence,
                    "認識精度が低い可能性があります。",
                    "コントラスト/解像度を上げる、LanguageHint を調整して再試行。");
            }

            return (OcrIssue.None, string.Empty, null);
        }
    }
}
