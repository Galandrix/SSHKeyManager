namespace SshKeyManager;

internal sealed class SettingsDialog : Form
{
    private readonly ComboBox _language = new();
    private readonly NumericUpDown _length = new();
    private readonly CheckBox _lower = new();
    private readonly CheckBox _upper = new();
    private readonly CheckBox _digits = new();
    private readonly CheckBox _special = new();
    private readonly TextBox _specialChars = new();

    public SettingsDialog()
    {
        Text = Lang.Settings;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(460, 420);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight + Ui.FieldRow));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));

        // language, then password generator
        var language = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        language.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        language.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        language.Controls.Add(Ui.FieldLabel(Lang.Language), 0, 0);
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        Ui.StyleInput(_language);
        _language.Items.Add(Lang.LanguageEnglish);
        _language.Items.Add(Lang.LanguageRussian);
        _language.SelectedIndex = PasswordOptions.Language == AppLanguage.Ru ? 1 : 0;
        language.Controls.Add(_language, 0, 1);

        var box = new GroupBox
        {
            Text = Lang.PasswordGeneration,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 8),
        };

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Margin = new Padding(0),
        };
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight + 6));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lengthLabel = Ui.FieldLabel(Lang.Length);
        lengthLabel.Padding = new Padding(0, 2, 0, 6);
        fields.Controls.Add(lengthLabel, 0, 0);
        _length.Minimum = 4;
        _length.Maximum = 128;
        _length.Value = PasswordOptions.Length;
        _length.AutoSize = false;
        _length.Width = 80;
        Ui.StyleInput(_length);
        _length.Dock = DockStyle.Left;
        fields.Controls.Add(_length, 0, 1);

        StyleCheck(_lower, Lang.Lower);
        StyleCheck(_upper, Lang.Upper);
        StyleCheck(_digits, Lang.Digits);
        StyleCheck(_special, Lang.Special);
        _lower.Checked = PasswordOptions.Lower;
        _upper.Checked = PasswordOptions.Upper;
        _digits.Checked = PasswordOptions.Digits;
        _special.Checked = PasswordOptions.Special;
        fields.Controls.Add(_lower, 0, 2);
        fields.Controls.Add(_upper, 0, 3);
        fields.Controls.Add(_digits, 0, 4);
        fields.Controls.Add(_special, 0, 5);

        Ui.StyleInput(_specialChars);
        _specialChars.Text = PasswordOptions.SpecialChars;
        fields.Controls.Add(_specialChars, 0, 6);
        _special.CheckedChanged += (_, _) => _specialChars.Enabled = _special.Checked;
        _specialChars.Enabled = _special.Checked;

        box.Controls.Add(fields);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
        };
        var cancel = Ui.TextButton(Lang.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Margin = new Padding(0);
        var save = Ui.TextButton(Lang.Save);
        save.Click += (_, _) => OnSave();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);

        root.Controls.Add(language, 0, 0);
        root.Controls.Add(box, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void StyleCheck(CheckBox box, string text)
    {
        box.Text = text;
        box.AutoSize = false;
        box.Dock = DockStyle.Fill;
        box.TextAlign = ContentAlignment.MiddleLeft;
        box.Margin = Ui.FieldMargin;
        box.UseCompatibleTextRendering = true;
    }

    private void OnSave()
    {
        PasswordOptions.Length = (int)_length.Value;
        PasswordOptions.Lower = _lower.Checked;
        PasswordOptions.Upper = _upper.Checked;
        PasswordOptions.Digits = _digits.Checked;
        PasswordOptions.Special = _special.Checked;
        PasswordOptions.SpecialChars = _specialChars.Text;
        PasswordOptions.Language = _language.SelectedIndex == 1 ? AppLanguage.Ru : AppLanguage.En;
        var error = PasswordOptions.Save();
        if (error is not null)
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
