using System;
using System.Drawing;
using System.Windows.Forms;

namespace OcrSnippingApp
{
    public sealed class AppendTextDialog : Form
    {
        private readonly int _maxLength;
        private readonly TextBox _tbPrefix;
        private readonly TextBox _tbSuffix;
        private readonly Label _lbPrefix;
        private readonly Label _lbSuffix;

        public string PrefixText => _tbPrefix.Text;
        public string SuffixText => _tbSuffix.Text;

        public AppendTextDialog(string prefix, string suffix, int maxLen)
        {
            _maxLength = maxLen;
            Text = "前後テキストを編集";
            Width = 720;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _lbPrefix = new Label
            {
                Left = 12,
                Top = 12,
                Width = 680,
                Text = "前置（0/" + _maxLength + ")"
            };
            _tbPrefix = new TextBox
            {
                Left = 12,
                Top = 32,
                Width = 680,
                Height = 180,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Text = prefix ?? string.Empty
            };

            _lbSuffix = new Label
            {
                Left = 12,
                Top = 224,
                Width = 680,
                Text = "後置（0/" + _maxLength + ")"
            };
            _tbSuffix = new TextBox
            {
                Left = 12,
                Top = 244,
                Width = 680,
                Height = 180,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Text = suffix ?? string.Empty
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 512,
                Top = 434,
                Width = 80
            };
            var btnCancel = new Button
            {
                Text = "キャンセル",
                DialogResult = DialogResult.Cancel,
                Left = 602,
                Top = 434,
                Width = 90
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            _tbPrefix.TextChanged += (_, _) => EnforceLimit(_tbPrefix, _lbPrefix, "前置");
            _tbSuffix.TextChanged += (_, _) => EnforceLimit(_tbSuffix, _lbSuffix, "後置");

            EnforceLimit(_tbPrefix, _lbPrefix, "前置");
            EnforceLimit(_tbSuffix, _lbSuffix, "後置");

            Controls.AddRange(new Control[]
            {
                _lbPrefix, _tbPrefix,
                _lbSuffix, _tbSuffix,
                btnOk, btnCancel
            });
        }

        private void EnforceLimit(TextBox textBox, Label label, string labelPrefix)
        {
            if (textBox.Text.Length > _maxLength)
            {
                var selection = textBox.SelectionStart;
                textBox.Text = textBox.Text.Substring(0, _maxLength);
                textBox.SelectionStart = Math.Min(selection, _maxLength);
            }
            label.Text = $"{labelPrefix}（{textBox.Text.Length}/{_maxLength}）";
        }
    }
}
