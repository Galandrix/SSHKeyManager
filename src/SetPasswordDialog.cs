namespace SshKeyManager;

internal sealed class SetPasswordDialog : Form
{
    private readonly TextBox _current = new();
    private readonly TextBox _new = new();
    private readonly TextBox _confirm = new();
    private readonly bool _hasPassword;
    private readonly Image _eyeHide = Ui.EyeIcon(open: false);
    private readonly Image _eyeShow = Ui.EyeIcon(open: true);
    private readonly Image _cube = Ui.CubeIcon();
    private readonly Image _copy = Ui.CopyIcon();
    private readonly Image _paste = Ui.PasteIcon();
    private readonly Button _eye = Ui.FieldIconButton();
    private readonly Button _currentEye = Ui.FieldIconButton();
    private bool _passwordVisible;
    private bool _currentVisible;

    public string CurrentPassword => _current.Text;
    public string NewPassword => _new.Text;
    public bool RemovePassword { get; private set; }

    public SetPasswordDialog(string keyName, bool hasPassword)
    {
        _hasPassword = hasPassword;
        Text = Lang.ChangePassword;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(520, 16 + Ui.FieldHeight + (3 * (Ui.LabelHeight + Ui.FieldRow)) + Ui.ButtonRow + 16);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));

        var caption = new Label
        {
            Text = Lang.KeyLabel(keyName),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 2),
            UseCompatibleTextRendering = true,
        };

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0),
            Margin = new Padding(0),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
        };
        for (var i = 0; i < 3; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        }

        // current: eye + paste; new: eye + generate + copy (eye also opens confirm)
        fields.Controls.Add(Ui.FieldLabel(Lang.CurrentPassword), 0, 0);
        fields.Controls.Add(BuildCurrentPasswordRow(), 0, 1);
        fields.Controls.Add(Ui.FieldLabel(Lang.NewPassword), 0, 2);
        fields.Controls.Add(BuildNewPasswordRow(), 0, 3);
        fields.Controls.Add(Ui.FieldLabel(Lang.RepeatPassword), 0, 4);
        Ui.StyleInput(_confirm);
        _confirm.UseSystemPasswordChar = true;
        fields.Controls.Add(_confirm, 0, 5);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var remove = Ui.TextButton(Lang.Delete);
        remove.Anchor = AnchorStyles.Left;
        remove.Click += (_, _) => OnRemove();

        var right = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Right,
        };
        var save = Ui.TextButton(Lang.Save);
        save.Click += (_, _) => OnOk();
        var cancel = Ui.TextButton(Lang.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Margin = new Padding(0);
        right.Controls.Add(save);
        right.Controls.Add(cancel);

        buttons.Controls.Add(remove, 0, 0);
        buttons.Controls.Add(right, 1, 0);

        root.Controls.Add(caption, 0, 0);
        root.Controls.Add(fields, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
        FormClosed += (_, _) =>
        {
            _eyeHide.Dispose();
            _eyeShow.Dispose();
            _cube.Dispose();
            _copy.Dispose();
            _paste.Dispose();
        };
    }

    // --- current password: show + paste ---

    private Control BuildCurrentPasswordRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Ui.FieldHeight,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Ui.FieldMargin,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));

        _current.Dock = DockStyle.Fill;
        _current.AutoSize = false;
        _current.UseSystemPasswordChar = true;
        _current.Margin = new Padding(0, 0, 6, 0);

        _currentEye.Image = _eyeHide;
        _currentEye.Dock = DockStyle.Fill;
        _currentEye.Click += (_, _) => ToggleCurrent();

        var paste = Ui.FieldIconButton();
        paste.Image = _paste;
        paste.Dock = DockStyle.Fill;
        paste.Click += (_, _) =>
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            _current.Text = Clipboard.GetText();
        };

        var tip = new ToolTip();
        tip.SetToolTip(_currentEye, Lang.ShowHidePassword);
        tip.SetToolTip(paste, Lang.PastePassword);

        row.Controls.Add(_current, 0, 0);
        row.Controls.Add(_currentEye, 1, 0);
        row.Controls.Add(paste, 2, 0);
        return row;
    }

    // --- new password: show (both fields) + generate + copy ---

    private Control BuildNewPasswordRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Ui.FieldHeight,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Ui.FieldMargin,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));

        _new.Dock = DockStyle.Fill;
        _new.AutoSize = false;
        _new.UseSystemPasswordChar = true;
        _new.Margin = new Padding(0, 0, 6, 0);

        _eye.Image = _eyeHide;
        _eye.Dock = DockStyle.Fill;
        _eye.Click += (_, _) => TogglePassword();

        var cube = Ui.FieldIconButton();
        cube.Image = _cube;
        cube.Dock = DockStyle.Fill;
        cube.Click += (_, _) => FillGenerated();

        var copy = Ui.FieldIconButton();
        copy.Image = _copy;
        copy.Dock = DockStyle.Fill;
        copy.Click += (_, _) =>
        {
            if (_new.Text.Length == 0)
            {
                return;
            }

            Clipboard.SetText(_new.Text);
        };

        var tip = new ToolTip();
        tip.SetToolTip(_eye, Lang.ShowHidePassword);
        tip.SetToolTip(cube, Lang.GenerateTip(PasswordOptions.Length));
        tip.SetToolTip(copy, Lang.CopyPassword);

        row.Controls.Add(_new, 0, 0);
        row.Controls.Add(_eye, 1, 0);
        row.Controls.Add(cube, 2, 0);
        row.Controls.Add(copy, 3, 0);
        return row;
    }

    private void TogglePassword()
    {
        _passwordVisible = !_passwordVisible;
        _new.UseSystemPasswordChar = !_passwordVisible;
        _confirm.UseSystemPasswordChar = !_passwordVisible;
        _eye.Image = _passwordVisible ? _eyeShow : _eyeHide;
    }

    private void ToggleCurrent()
    {
        _currentVisible = !_currentVisible;
        _current.UseSystemPasswordChar = !_currentVisible;
        _currentEye.Image = _currentVisible ? _eyeShow : _eyeHide;
    }

    private void FillGenerated()
    {
        var generated = PasswordOptions.Generate();
        if (generated.Length == 0)
        {
            MessageBox.Show(this, Lang.EmptyCharset, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _new.Text = generated;
        _confirm.Text = generated;
    }

    private void OnOk()
    {
        if (_new.Text != _confirm.Text)
        {
            MessageBox.Show(this, Lang.PasswordMismatch, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_new.Text.Length == 0)
        {
            MessageBox.Show(this, Lang.EnterNewOrRemove, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_hasPassword && _current.Text.Length == 0)
        {
            MessageBox.Show(this, Lang.EnterCurrentPassword, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RemovePassword = false;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnRemove()
    {
        if (_hasPassword && _current.Text.Length == 0)
        {
            MessageBox.Show(this,
                Lang.EnterCurrentToRemove,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (!_hasPassword)
        {
            MessageBox.Show(this, Lang.KeyHasNoPassword, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        RemovePassword = true;
        DialogResult = DialogResult.OK;
        Close();
    }
}
