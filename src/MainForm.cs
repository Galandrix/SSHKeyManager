namespace SshKeyManager;

public sealed class MainForm : Form
{
    // --- list, details, status ---
    private readonly ListView _keys = new();
    private readonly Label _nameValue = new();
    private readonly Label _typeValue = new();
    private readonly Label _fpValue = new();
    private readonly Label _commentValue = new();
    private readonly Label _privateValue = new();
    private readonly Label _publicValue = new();
    private readonly Label _protectValue = new();
    private readonly Label _agentValue = new();
    private readonly Label _changedValue = new();
    private readonly Label _passwordValue = new();
    private readonly Label _statusKeys = new();
    private readonly Label _statusPath = new();
    // --- toolbar / agent ---
    private readonly Button _agentButton = new();
    private readonly ToolTip _tips = new();
    private readonly Image _startIcon = MakeAgentIcon(play: true);
    private readonly Image _stopIcon = MakeAgentIcon(play: false);
    private readonly ToolStripMenuItem _agentMenuItem = new();
    private readonly Label _subtitle = new();
    private readonly Button _createButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _settingsButton = new();
    private readonly GroupBox _keysBox = new();
    private readonly GroupBox _detailsBox = new();
    private readonly Label[] _detailCaptions = new Label[10];

    // --- context menu ---
    private readonly ToolStripMenuItem _copyPubItem = new();
    private readonly ToolStripMenuItem _copyFpItem = new();
    private readonly ToolStripMenuItem _openFolderItem = new();
    private readonly ToolStripMenuItem _renameItem = new();
    private readonly ToolStripMenuItem _setPasswordItem = new();
    private readonly ToolStripMenuItem _deleteKeyItem = new();
    private bool _agentRunning;

    public MainForm()
    {
        Text = "SSH Key Manager";
        Ui.ApplyIcon(this);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(940, 720);
        ClientSize = new Size(1020, 800);
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(root);

        // header, toolbar, key list, selected key, status
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        root.Controls.Add(BuildKeyList(), 0, 2);
        root.Controls.Add(BuildDetails(), 0, 3);
        root.Controls.Add(BuildStatus(), 0, 4);

        ApplyLanguage();
        LoadKeys();
        FormClosed += (_, _) =>
        {
            _startIcon.Dispose();
            _stopIcon.Dispose();
            _tips.Dispose();
        };
    }

    // --- layout ---

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill };
        header.Paint += (_, e) =>
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                header.ClientRectangle,
                Color.FromArgb(52, 52, 52),
                Color.FromArgb(24, 24, 24),
                90f);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };

        var title = new Label
        {
            AutoSize = true,
            Text = "SSH Key Manager",
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            Location = new Point(18, 16),
        };
        _subtitle.AutoSize = true;
        _subtitle.ForeColor = Color.Gainsboro;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = new Font("Segoe UI", 9f);
        _subtitle.Location = new Point(20, 56);
        header.Controls.Add(title);
        header.Controls.Add(_subtitle);
        return header;
    }

    private Control BuildToolbar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 10, 14, 8),
            Margin = new Padding(0),
            WrapContents = false,
        };
        InitToolbarButton(_createButton, (_, _) => CreateNewKey());
        InitToolbarButton(_refreshButton, (_, _) => LoadKeys());
        InitToolbarButton(_settingsButton, (_, _) => OpenSettings());
        bar.Controls.Add(_createButton);
        bar.Controls.Add(_refreshButton);
        bar.Controls.Add(BuildAgentButton());
        bar.Controls.Add(_settingsButton);
        return bar;
    }

    private Control BuildAgentButton()
    {
        _agentButton.AutoSize = false;
        _agentButton.Size = new Size(160, Ui.ButtonHeight);
        _agentButton.MinimumSize = new Size(160, Ui.ButtonHeight);
        _agentButton.MaximumSize = new Size(400, Ui.ButtonHeight);
        _agentButton.Padding = new Padding(28, 0, 12, 0);
        _agentButton.Margin = new Padding(0, 0, 10, 0);
        _agentButton.UseVisualStyleBackColor = true;
        _agentButton.TextAlign = ContentAlignment.MiddleLeft;
        _agentButton.Paint += AgentButtonPaint;
        _agentButton.Click += (_, _) => ToggleAgent();
        UpdateAgentButton();
        return _agentButton;
    }

    private Control BuildKeyList()
    {
        _keysBox.Dock = DockStyle.Fill;
        _keysBox.Margin = new Padding(12, 2, 12, 6);
        _keysBox.Padding = new Padding(10, 8, 10, 10);

        _keys.Dock = DockStyle.Fill;
        _keys.View = View.Details;
        _keys.FullRowSelect = true;
        _keys.GridLines = true;
        _keys.HideSelection = false;
        _keys.MultiSelect = false;
        _keys.SmallImageList = new ImageList { ImageSize = new Size(1, Ui.ListRowHeight) };
        _keys.Columns.Add("", 170);
        _keys.Columns.Add("", 120);
        _keys.Columns.Add("", 320);
        _keys.Columns.Add("", 90);
        _keys.Columns.Add("", 120);
        _keys.Columns.Add("", 120);
        _keys.SelectedIndexChanged += (_, _) => ShowSelected();
        _keys.MouseDown += KeysMouseDown;
        _keys.ContextMenuStrip = BuildKeyMenu();

        _keysBox.Controls.Add(_keys);
        return _keysBox;
    }

    private Control BuildDetails()
    {
        _detailsBox.Dock = DockStyle.Fill;
        _detailsBox.Margin = new Padding(12, 4, 12, 8);
        _detailsBox.Padding = new Padding(12, 10, 12, 16);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(4, 4, 4, 4),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddDetail(grid, 0, _nameValue);
        AddDetail(grid, 1, _typeValue);
        AddDetail(grid, 2, _fpValue);
        AddDetail(grid, 3, _commentValue);
        AddDetail(grid, 4, _privateValue);
        AddDetail(grid, 5, _publicValue);
        AddDetail(grid, 6, _protectValue);
        AddDetail(grid, 7, _passwordValue);
        AddDetail(grid, 8, _agentValue);
        AddDetail(grid, 9, _changedValue);

        _detailsBox.Controls.Add(grid);
        return _detailsBox;
    }

    private Control BuildStatus()
    {
        var status = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(236, 236, 236),
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0),
        };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _statusKeys.AutoSize = true;
        _statusKeys.Text = Lang.KeysFound(0);
        _statusKeys.Anchor = AnchorStyles.Left;
        _statusKeys.Margin = new Padding(0, 1, 8, 0);

        _statusPath.AutoSize = true;
        _statusPath.Text = Lang.Folder + SshKeyScanner.SshDirectory;
        _statusPath.Anchor = AnchorStyles.Right;
        _statusPath.Margin = new Padding(8, 1, 0, 0);

        status.Controls.Add(_statusKeys, 0, 0);
        status.Controls.Add(_statusPath, 1, 0);
        return status;
    }

    private void AddDetail(TableLayoutPanel grid, int row, Label value)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.DetailRowHeight));
        var label = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(0, 2, 0, 0),
            UseCompatibleTextRendering = true,
        };
        _detailCaptions[row] = label;
        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.AutoEllipsis = true;
        value.Margin = new Padding(0);
        value.Padding = new Padding(0, 2, 0, 2);
        value.UseCompatibleTextRendering = true;
        grid.Controls.Add(label, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private static void InitToolbarButton(Button button, EventHandler click)
    {
        Ui.StyleTextButton(button);
        button.Margin = new Padding(0, 0, 10, 0);
        button.Click += click;
    }

    // --- language ---

    private void ApplyLanguage()
    {
        _subtitle.Text = Lang.Subtitle;
        Ui.StyleTextButton(_createButton, Lang.CreateKey);
        Ui.StyleTextButton(_refreshButton, Lang.Refresh);
        Ui.StyleTextButton(_settingsButton, Lang.Settings);
        _createButton.Margin = new Padding(0, 0, 10, 0);
        _refreshButton.Margin = new Padding(0, 0, 10, 0);
        _settingsButton.Margin = new Padding(0, 0, 10, 0);
        _agentButton.Text = Lang.SshAgent;
        _keysBox.Text = Lang.KeysOnThisPc;
        _detailsBox.Text = Lang.SelectedKey;
        _keys.Columns[0].Text = Lang.ColName;
        _keys.Columns[1].Text = Lang.ColType;
        _keys.Columns[2].Text = Lang.ColFingerprint;
        _keys.Columns[3].Text = Lang.ColAgent;
        _keys.Columns[4].Text = Lang.ColProtection;
        _keys.Columns[5].Text = Lang.ColState;
        _detailCaptions[0].Text = Lang.Name;
        _detailCaptions[1].Text = Lang.Type;
        _detailCaptions[2].Text = Lang.Fingerprint;
        _detailCaptions[3].Text = Lang.Comment;
        _detailCaptions[4].Text = Lang.PrivateKey;
        _detailCaptions[5].Text = Lang.PublicKey;
        _detailCaptions[6].Text = Lang.Protection;
        _detailCaptions[7].Text = Lang.Password;
        _detailCaptions[8].Text = Lang.SshAgentLabel;
        _detailCaptions[9].Text = Lang.Changed;
        _copyPubItem.Text = Lang.CopyPublicKey;
        _copyFpItem.Text = Lang.CopyFingerprint;
        _openFolderItem.Text = Lang.OpenFolder;
        _renameItem.Text = Lang.Rename;
        _setPasswordItem.Text = Lang.ChangePassword;
        _deleteKeyItem.Text = Lang.DeleteKey;
        UpdateAgentButton();
    }

    // --- context menu ---

    private ContextMenuStrip BuildKeyMenu()
    {
        var menu = new ContextMenuStrip();
        _copyPubItem.Click += (_, _) => CopyPublicKey();
        _copyFpItem.Click += (_, _) => CopyFingerprint();
        _openFolderItem.Click += (_, _) => OpenKeyFolder();
        _renameItem.Click += (_, _) => RenameKey();
        _agentMenuItem.Click += (_, _) => ToggleKeyInAgent();
        _setPasswordItem.Click += (_, _) => SetKeyPassword();
        _deleteKeyItem.Click += (_, _) => DeleteKey();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _copyPubItem,
            _copyFpItem,
            _openFolderItem,
            _renameItem,
            new ToolStripSeparator(),
            _agentMenuItem,
            _setPasswordItem,
            _deleteKeyItem,
        });
        menu.Opening += (_, e) =>
        {
            if (_keys.SelectedItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            var key = SelectedKey;
            var loaded = key?.InAgent == true;
            var hasPrivate = key is not null && File.Exists(key.PrivatePath);
            var hasFiles = hasPrivate || (key is not null && File.Exists(key.PublicPath));
            _agentMenuItem.Text = loaded ? Lang.RemoveFromAgent : Lang.AddToAgent;
            _agentMenuItem.Enabled = hasPrivate;
            _setPasswordItem.Enabled = hasPrivate;
            _deleteKeyItem.Enabled = hasFiles;
        };
        return menu;
    }

    private void KeysMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var hit = _keys.HitTest(e.Location);
        if (hit.Item is not null)
        {
            hit.Item.Selected = true;
        }
    }

    private SshKeyInfo? SelectedKey =>
        _keys.SelectedItems.Count > 0 ? _keys.SelectedItems[0].Tag as SshKeyInfo : null;

    // --- scan and fill the list ---

    private void LoadKeys(string? selectName = null)
    {
        UseWaitCursor = true;
        try
        {
            var selected = selectName ?? SelectedKey?.Name;
            _keys.BeginUpdate();
            _keys.Items.Clear();
            foreach (var key in SshKeyScanner.Scan())
            {
                var item = new ListViewItem(key.Name) { Tag = key };
                item.SubItems.Add(key.Bits is "—" or "" ? key.Type : $"{key.Type} {key.Bits}");
                item.SubItems.Add(key.Fingerprint);
                item.SubItems.Add(key.AgentMark);
                item.SubItems.Add(key.Protection);
                item.SubItems.Add(key.State);
                _keys.Items.Add(item);
            }
            _keys.EndUpdate();

            _statusKeys.Text = Lang.KeysFound(_keys.Items.Count);
            _statusPath.Text = Lang.Folder + SshKeyScanner.SshDirectory;

            if (_keys.Items.Count == 0)
            {
                ClearDetails();
                return;
            }

            var match = selected is null
                ? null
                : _keys.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text.Equals(selected, StringComparison.OrdinalIgnoreCase));
            (match ?? _keys.Items[0]).Selected = true;
        }
        finally
        {
            UseWaitCursor = false;
            UpdateAgentButton();
        }
    }

    // --- Windows ssh-agent service ---

    private void ToggleAgent()
    {
        UseWaitCursor = true;
        try
        {
            var error = SshAgentService.Toggle();
            if (error is not null)
            {
                MessageBox.Show(this, error, Lang.SshAgent, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            UseWaitCursor = false;
        }

        LoadKeys();
    }

    private void UpdateAgentButton()
    {
        var status = SshAgentService.GetStatus();
        _agentButton.Enabled = status != SshAgentStatus.Missing;
        _agentRunning = status == SshAgentStatus.Running;
        _agentButton.Invalidate();
        _tips.SetToolTip(_agentButton, status switch
        {
            SshAgentStatus.Running => Lang.AgentStopTip,
            SshAgentStatus.Stopped => Lang.AgentStartTip,
            _ => Lang.AgentMissingTip,
        });
    }

    private void AgentButtonPaint(object? sender, PaintEventArgs e)
    {
        var icon = _agentRunning ? _stopIcon : _startIcon;
        var y = (_agentButton.ClientSize.Height - 16) / 2;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        e.Graphics.DrawImage(icon, 8, y, 16, 16);
    }

    private static Image MakeAgentIcon(bool play)
    {
        var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        if (play)
        {
            using var brush = new SolidBrush(Color.FromArgb(39, 174, 96));
            g.FillPolygon(brush, new[]
            {
                new Point(2, 1),
                new Point(2, 15),
                new Point(14, 8),
            });
        }
        else
        {
            using var brush = new SolidBrush(Color.FromArgb(192, 57, 43));
            g.FillRectangle(brush, 2, 2, 12, 12);
        }

        return bmp;
    }

    // --- key actions ---

    private void CopyPublicKey()
    {
        var key = SelectedKey;
        if (key is null || key.PublicPath is "—" || !File.Exists(key.PublicPath))
        {
            MessageBox.Show(this, Lang.NoPubFile, Lang.CopyPublicKey,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Clipboard.SetText(File.ReadAllText(key.PublicPath).Trim());
    }

    private void RenameKey()
    {
        var key = SelectedKey;
        if (key is null)
        {
            return;
        }

        using var dialog = new RenameKeyDialog(key.Name);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var error = SshKeyScanner.Rename(key, dialog.NewName);
        if (error is not null)
        {
            MessageBox.Show(this, error, Lang.Rename, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadKeys(dialog.NewName);
    }

    private void CreateNewKey()
    {
        using var dialog = new CreateKeyDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var privatePath = Path.Combine(dialog.Folder, dialog.KeyName);
        var error = SshKeyScanner.CreateKey(dialog.KeyType, privatePath, dialog.Comment, dialog.Passphrase);
        if (error is not null)
        {
            MessageBox.Show(this, error, Lang.CreateSshKey, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadKeys(dialog.KeyName);
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ApplyLanguage();
        LoadKeys();
    }

    private void SetKeyPassword()
    {
        var key = SelectedKey;
        if (key is null || !File.Exists(key.PrivatePath))
        {
            MessageBox.Show(this, Lang.NoPrivateKey, Lang.ChangePassword,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SetPasswordDialog(key.Name, key.HasPassphrase);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var next = dialog.RemovePassword ? "" : dialog.NewPassword;
        var error = SshKeyScanner.ChangePassphrase(key.PrivatePath, dialog.CurrentPassword, next);
        if (error is not null)
        {
            MessageBox.Show(this, error, Lang.ChangePassword, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadKeys(key.Name);
    }

    private void DeleteKey()
    {
        var key = SelectedKey;
        if (key is null)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            Lang.DeleteKeyConfirm(key.Name),
            Lang.DeleteKey,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        var error = SshKeyScanner.Delete(key);
        if (error is not null)
        {
            MessageBox.Show(this, error, Lang.DeleteKey, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadKeys();
    }

    private void ToggleKeyInAgent()
    {
        var key = SelectedKey;
        if (key is null || !File.Exists(key.PrivatePath))
        {
            MessageBox.Show(this, Lang.NoPrivateKey, Lang.SshAgent,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string? error;
        if (key.InAgent)
        {
            error = SshAgentService.RemoveIdentity(key.PrivatePath);
        }
        else
        {
            string? passphrase = null;
            if (key.HasPassphrase)
            {
                using var dialog = new PassphraseDialog(key.Name);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                passphrase = dialog.Passphrase;
            }

            error = SshAgentService.AddIdentity(key.PrivatePath, passphrase);
        }

        if (error is not null)
        {
            MessageBox.Show(this, error, Lang.SshAgent, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LoadKeys(key.Name);
    }

    private void CopyFingerprint()
    {
        var key = SelectedKey;
        if (key is null || string.IsNullOrWhiteSpace(key.Fingerprint) || key.Fingerprint is "—")
        {
            MessageBox.Show(this, Lang.NoFingerprint, Lang.CopyFingerprint,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Clipboard.SetText(key.Fingerprint);
    }

    private void OpenKeyFolder()
    {
        var key = SelectedKey;
        var path = key is null ? SshKeyScanner.SshDirectory
            : File.Exists(key.PrivatePath) ? key.PrivatePath
            : File.Exists(key.PublicPath) ? key.PublicPath
            : SshKeyScanner.SshDirectory;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = File.Exists(path) ? $"/select,\"{path}\"" : $"\"{SshKeyScanner.SshDirectory}\"",
            UseShellExecute = true,
        });
    }

    // --- selected key pane ---

    private void ShowSelected()
    {
        var row = SelectedKey;
        if (row is null)
        {
            ClearDetails();
            return;
        }

        _nameValue.Text = row.Name;
        _typeValue.Text = row.Bits is "—" or "" ? row.Type : $"{row.Type} {row.Bits}";
        _fpValue.Text = row.Fingerprint;
        _commentValue.Text = row.Comment;
        _privateValue.Text = row.PrivatePath;
        _publicValue.Text = row.PublicPath;
        _protectValue.Text = row.Protection;
        _passwordValue.Text = row.HasPassphrase ? Lang.PasswordNotSaved : "—";
        _agentValue.Text = row.Agent;
        _changedValue.Text = row.Changed;
    }

    private void ClearDetails()
    {
        _nameValue.Text = "—";
        _typeValue.Text = "—";
        _fpValue.Text = "—";
        _commentValue.Text = "—";
        _privateValue.Text = "—";
        _publicValue.Text = "—";
        _protectValue.Text = "—";
        _passwordValue.Text = "—";
        _agentValue.Text = "—";
        _changedValue.Text = "—";
    }
}
