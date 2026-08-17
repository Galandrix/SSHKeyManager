namespace SshKeyManager;

// rename private + .pub together; does not edit ~/.ssh/config
internal sealed class RenameKeyDialog : Form
{
    private readonly TextBox _name = new();

    public string NewName => _name.Text.Trim();

    public RenameKeyDialog(string currentName)
    {
        Text = Lang.RenameKey;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(460, 16 + 56 + Ui.FieldRow + Ui.ButtonRow + 16);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));

        var caption = new Label
        {
            Text = Lang.RenameCaption,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 4),
            UseCompatibleTextRendering = true,
        };
        Ui.StyleInput(_name);
        _name.Text = currentName;
        _name.SelectAll();

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
        var ok = Ui.TextButton("OK");
        ok.DialogResult = DialogResult.OK;

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        root.Controls.Add(caption, 0, 0);
        root.Controls.Add(_name, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
