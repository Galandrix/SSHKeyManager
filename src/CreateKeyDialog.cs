namespace SshKeyManager;

internal sealed class CreateKeyDialog : Form
{
    private readonly ComboBox _type = new();
    private readonly TextBox _name = new();
    private readonly TextBox _comment = new();
    private readonly TextBox _password = new();
    private readonly TextBox _folder = new();
    private readonly Button _eye = Ui.FieldIconButton();
    private readonly Image _eyeHide;
    private readonly Image _eyeShow;
    private readonly Image _cube;
    private readonly Image _copy;
    private bool _passwordVisible;

    public string KeyType => _type.SelectedItem as string ?? "ED25519";
    public string KeyName => _name.Text.Trim();
    public string Comment => _comment.Text.Trim();
    public string Passphrase => _password.Text;
    public string Folder => _folder.Text.Trim();

    public CreateKeyDialog()
    {
        Ui.ApplyIcon(this);
        // type, name, comment, password, folder
        _eyeHide = Ui.EyeIcon(open: false);
        _eyeShow = Ui.EyeIcon(open: true);
        _cube = Ui.CubeIcon();
        _copy = Ui.CopyIcon();

        Text = Lang.CreateSshKey;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(560, 16 + (5 * (Ui.LabelHeight + Ui.FieldRow)) + Ui.ButtonRow + 16);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 10,
            Margin = new Padding(0),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
        };
        for (var i = 0; i < 5; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        }

        var row = 0;
        fields.Controls.Add(Ui.FieldLabel(Lang.Type), 0, row++);
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        Ui.StyleInput(_type);
        _type.Items.AddRange(new object[] { "ED25519", "RSA 4096" });
        _type.SelectedIndex = 0;
        _type.SelectedIndexChanged += (_, _) => SuggestName();
        fields.Controls.Add(_type, 0, row++);

        fields.Controls.Add(Ui.FieldLabel(Lang.Name), 0, row++);
        Ui.StyleInput(_name);
        _name.Text = "id_ed25519";
        fields.Controls.Add(_name, 0, row++);

        fields.Controls.Add(Ui.FieldLabel(Lang.Comment), 0, row++);
        Ui.StyleInput(_comment);
        fields.Controls.Add(_comment, 0, row++);

        fields.Controls.Add(Ui.FieldLabel(Lang.Password), 0, row++);
        fields.Controls.Add(BuildPasswordRow(), 0, row++);

        fields.Controls.Add(Ui.FieldLabel(Lang.SaveTo), 0, row++);
        fields.Controls.Add(BuildFolderRow(), 0, row++);

        var tips = new ToolTip();

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
        var create = Ui.TextButton(Lang.Create);
        create.Click += (_, _) => OnCreate();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(create);

        root.Controls.Add(fields, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
        AcceptButton = create;
        CancelButton = cancel;
        FormClosed += (_, _) =>
        {
            _eyeHide.Dispose();
            _eyeShow.Dispose();
            _cube.Dispose();
            _copy.Dispose();
            tips.Dispose();
        };
    }

    // --- password: show / generate / copy ---

    private Control BuildPasswordRow()
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

        _password.Dock = DockStyle.Fill;
        _password.AutoSize = false;
        _password.UseSystemPasswordChar = true;
        _password.Margin = new Padding(0, 0, 6, 0);

        _eye.Image = _eyeHide;
        _eye.Dock = DockStyle.Fill;
        _eye.Click += (_, _) => TogglePassword();

        var cube = Ui.FieldIconButton();
        cube.Image = _cube;
        cube.Dock = DockStyle.Fill;
        cube.Click += (_, _) =>
        {
            var generated = PasswordOptions.Generate();
            if (generated.Length == 0)
            {
                MessageBox.Show(this, Lang.EmptyCharset, Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _password.Text = generated;
        };
        var copy = Ui.FieldIconButton();
        copy.Image = _copy;
        copy.Dock = DockStyle.Fill;
        copy.Click += (_, _) =>
        {
            if (_password.Text.Length == 0)
            {
                return;
            }

            Clipboard.SetText(_password.Text);
        };
        var tip = new ToolTip();
        tip.SetToolTip(cube, Lang.GenerateTip(PasswordOptions.Length));
        tip.SetToolTip(_eye, Lang.ShowHidePassword);
        tip.SetToolTip(copy, Lang.CopyPassword);

        row.Controls.Add(_password, 0, 0);
        row.Controls.Add(_eye, 1, 0);
        row.Controls.Add(cube, 2, 0);
        row.Controls.Add(copy, 3, 0);
        return row;
    }

    // --- save path ---

    private Control BuildFolderRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Ui.FieldHeight,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Ui.FieldMargin,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));
        _folder.Dock = DockStyle.Fill;
        _folder.AutoSize = false;
        _folder.Margin = new Padding(0, 0, 6, 0);
        _folder.Text = SshKeyScanner.SshDirectory;
        var browse = Ui.FieldIconButton();
        browse.Text = "…";
        browse.TabStop = true;
        browse.Dock = DockStyle.Fill;
        browse.Click += (_, _) => BrowseFolder();
        row.Controls.Add(_folder, 0, 0);
        row.Controls.Add(browse, 1, 0);
        return row;
    }

    private void SuggestName()
    {
        var current = _name.Text.Trim();
        if (current is "" or "id_ed25519" or "id_rsa")
        {
            _name.Text = KeyType.StartsWith("RSA", StringComparison.OrdinalIgnoreCase) ? "id_rsa" : "id_ed25519";
        }
    }

    private void TogglePassword()
    {
        _passwordVisible = !_passwordVisible;
        _password.UseSystemPasswordChar = !_passwordVisible;
        _eye.Image = _passwordVisible ? _eyeShow : _eyeHide;
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Lang.KeyFolder,
            SelectedPath = Directory.Exists(Folder) ? Folder : SshKeyScanner.SshDirectory,
            ShowNewFolderButton = true,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dialog.SelectedPath;
        }
    }

    // --- validate name / folder, then caller runs ssh-keygen ---

    private void OnCreate()
    {
        if (KeyName.Length == 0 || KeyName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || KeyName.Contains('\\') || KeyName.Contains('/'))
        {
            MessageBox.Show(this, Lang.InvalidFileName, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (Folder.Length == 0)
        {
            MessageBox.Show(this, Lang.EnterFolder, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

}
