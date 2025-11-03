using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace OcrSnippingApp.Services
{
    public sealed class AppSettings
    {
        public string HotkeyModifiers { get; set; } = string.Empty;
        public string HotkeyKey { get; set; } = string.Empty;
        public string OcrMode { get; set; } = "DocumentTextDetection"; // or TextDetection
        public bool Notifications { get; set; } = true;
        public int TimeoutMs { get; set; } = 10000;
        public string? LanguageHint { get; set; } = "ja";

        // 追記モード設定
        public bool AppendModeEnabled { get; set; } = false;
        public string PrefixText { get; set; } = string.Empty;
        public string SuffixText { get; set; } = string.Empty;
        public bool AddBlankLines { get; set; } = true;
    }

    // 制限値の定数
    public static class AppLimits
    {
        public const int MaxAppendLen = 5000;
    }

    public class SettingsService
    {
        private readonly IConfiguration _configuration;
        private readonly AppSettings _settings;

        public SettingsService()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            _settings = new AppSettings();
            _configuration.GetSection("OcrSnipping").Bind(_settings);
            ValidateSettings();
        }

        public AppSettings Current => _settings;

        public void SaveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var json = JsonConvert.SerializeObject(new { OcrSnipping = _settings }, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"設定の保存に失敗しました。\n\nエラー: {ex.Message}",
                    "設定エラー",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }
        }

        public void UpdateHotKey(string modifiers, string key)
        {
            // 片肺入力は無効へ正規化（両方空にする）
            var mods = modifiers?.Trim() ?? string.Empty;
            var k = key?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(mods) || string.IsNullOrEmpty(k))
            {
                if (!(string.IsNullOrEmpty(mods) && string.IsNullOrEmpty(k)))
                {
                    Logger.LogWarning("Hotkey partial input -> normalized to disabled.");
                }
                mods = string.Empty;
                k = string.Empty;
            }

            _settings.HotkeyModifiers = mods;
            _settings.HotkeyKey = k;
            SaveSettings();
        }

        public void UpdateOCRSettings(string mode, int timeoutMs, string? languageHint)
        {
            _settings.OcrMode = mode;
            _settings.TimeoutMs = timeoutMs;
            _settings.LanguageHint = languageHint;
            SaveSettings();
        }

        public void UpdateAppendModeSettings(bool enabled, bool addBlankLines)
        {
            _settings.AppendModeEnabled = enabled;
            _settings.AddBlankLines = addBlankLines;
            SaveSettings();
        }

        public (bool ok, string? message) SaveAppendTexts(string? prefixText, string? suffixText, int maxLen = AppLimits.MaxAppendLen)
        {
            bool truncated = false;

            var sanitizedPrefix = SanitizeAppendText(prefixText, maxLen, "Prefix", ref truncated);
            var sanitizedSuffix = SanitizeAppendText(suffixText, maxLen, "Suffix", ref truncated);

            _settings.PrefixText = sanitizedPrefix;
            _settings.SuffixText = sanitizedSuffix;
            SaveSettings();

            if (truncated)
            {
                var msg = $"追記テキストは最大{maxLen}文字に切り詰められました。";
                Logger.LogWarning(msg);
                return (true, msg);
            }

            return (true, null);
        }

        private static string SanitizeAppendText(string? text, int maxLen, string label, ref bool truncated)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text!.Length > maxLen)
            {
                Logger.LogWarning($"{label}文字列が制限値{maxLen}文字を超過したため、切り詰めました。");
                truncated = true;
                return text.Substring(0, maxLen);
            }

            return text;
        }

        public string BuildAugmentedText(string ocrText)
        {
            var text = ocrText ?? string.Empty;
            if (!_settings.AppendModeEnabled)

                return text;

            var prefix = _settings.PrefixText ?? string.Empty;
            var suffix = _settings.SuffixText ?? string.Empty;

            // 実行時ガード: 長すぎる場合は制限
            if (prefix.Length > AppLimits.MaxAppendLen)
            {
                Logger.LogWarning("Prefix文字列が制限値を超過しているため、切り詰めて実行します。");
                prefix = prefix.Substring(0, AppLimits.MaxAppendLen);
            }

            if (suffix.Length > AppLimits.MaxAppendLen)
            {
                Logger.LogWarning("Suffix文字列が制限値を超過しているため、切り詰めて実行します。");
                suffix = suffix.Substring(0, AppLimits.MaxAppendLen);
            }

            if (_settings.AddBlankLines)
            {
                var newline = Environment.NewLine;
                return string.Concat(prefix, newline, text, newline, suffix);
            }

            return string.Concat(prefix, text, suffix);
        }

        private void ValidateSettings()
        {
            bool hasCorrection = false;

            // ホットキーは未設定(空)を許容。無効な値の場合は空に正規化して無効化。
            if (!string.IsNullOrWhiteSpace(_settings.HotkeyKey) && !Enum.TryParse<Keys>(_settings.HotkeyKey, true, out _))
            {
                Logger.LogWarning($"設定値 HotkeyKey '{_settings.HotkeyKey}' が無効なため、ホットキーを無効化します。");
                _settings.HotkeyKey = string.Empty;
                _settings.HotkeyModifiers = string.Empty;
                hasCorrection = true;
            }

            if (_settings.TimeoutMs <= 0 || _settings.TimeoutMs > 60000)
            {
                Logger.LogWarning("設定値 TimeoutMs が許容範囲外のため、10000ms にフォールバックします。");
                _settings.TimeoutMs = 10000;
                hasCorrection = true;
            }

            if (string.IsNullOrWhiteSpace(_settings.OcrMode) ||
                (_settings.OcrMode != "DocumentTextDetection" && _settings.OcrMode != "TextDetection"))
            {
                Logger.LogWarning("設定値 OcrMode が無効なため、DocumentTextDetection を使用します。");
                _settings.OcrMode = "DocumentTextDetection";
                hasCorrection = true;
            }

            if (!string.IsNullOrWhiteSpace(_settings.LanguageHint))
            {
                _settings.LanguageHint = _settings.LanguageHint.Trim();
                if (_settings.LanguageHint.Length > 8)
                {
                    Logger.LogWarning("設定値 LanguageHint が長すぎるため、先頭8文字に切り詰めます。");
                    _settings.LanguageHint = _settings.LanguageHint.Substring(0, 8);
                    hasCorrection = true;
                }
            }

            // 追記モード設定の検証
            if (_settings.PrefixText != null && _settings.PrefixText.Length > AppLimits.MaxAppendLen)
            {
                Logger.LogWarning($"設定値 PrefixText が制限値{AppLimits.MaxAppendLen}文字を超過したため、切り詰めます。");
                _settings.PrefixText = _settings.PrefixText.Substring(0, AppLimits.MaxAppendLen);
                hasCorrection = true;
            }

            if (_settings.SuffixText != null && _settings.SuffixText.Length > AppLimits.MaxAppendLen)
            {
                Logger.LogWarning($"設定値 SuffixText が制限値{AppLimits.MaxAppendLen}文字を超過したため、切り詰めます。");
                _settings.SuffixText = _settings.SuffixText.Substring(0, AppLimits.MaxAppendLen);
                hasCorrection = true;
            }

            if (hasCorrection)
            {
                Logger.LogWarning("appsettings.json に不正な項目が見つかったため、デフォルト値にフォールバックしました。必要であれば設定画面から再設定してください。");
            }
        }
    }
}
