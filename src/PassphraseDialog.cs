namespace SshKeyManager;

// passphrase prompt for ssh-add (empty = no password)
internal sealed class PassphraseDialog : Form
{
    private readonly TextBox _password = new();

    public string Passphrase => _password.Text;

    public PassphraseDialog(string keyName)
    {
        Text = Lang.KeyPassword;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(460, 176);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));

        var caption = new Label
        {
            Text = Lang.EnterPasswordFor(keyName),
            Dock = DockStyle.Fill,
            UseCompatibleTextRendering = true,
        };
        Ui.StyleInput(_password);
        _password.UseSystemPasswordChar = true;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        var cancel = Ui.TextButton(Lang.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Margin = new Padding(0);
        var ok = Ui.TextButton("OK");
        ok.DialogResult = DialogResult.OK;

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        root.Controls.Add(caption, 0, 0);
        root.Controls.Add(_password, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
