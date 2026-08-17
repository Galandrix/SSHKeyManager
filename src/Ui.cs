namespace SshKeyManager;

internal static class Ui
{
    private static Icon? _appIcon;

    public static Icon AppIcon => _appIcon ??= LoadAppIcon();

    public static void ApplyIcon(Form form)
    {
        form.ShowIcon = true;
        form.Icon = AppIcon;
    }

    private static Icon LoadAppIcon()
    {
        var stream = typeof(Ui).Assembly.GetManifestResourceStream("SshKeyManager.app.ico");
        if (stream != null)
            return new Icon(stream);

        var exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
        {
            var extracted = Icon.ExtractAssociatedIcon(exe);
            if (extracted != null)
                return extracted;
        }

        return SystemIcons.Application;
    }

    // --- shared sizes ---
    public const int ButtonHeight = 32;
    public const int FieldHeight = 28;
    public const int FieldIconSize = 28;
    public const int LabelHeight = 22;
    public const int RowGap = 8;
    public const int FieldRow = FieldHeight + RowGap;
    public const int ButtonRow = 40;
    public const int ListRowHeight = 24;
    public const int DetailRowHeight = 28;

    public static Padding FieldMargin => new(0, 0, 0, RowGap);
    public static Padding LabelMargin => new(0);

    // --- fields and buttons ---

    public static Label FieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = LabelMargin,
            Padding = new Padding(0, 2, 0, 0),
            UseCompatibleTextRendering = true,
        };
    }

    public static void StyleInput(Control control)
    {
        control.Dock = DockStyle.Top;
        control.Margin = FieldMargin;
        control.Height = FieldHeight;
        if (control is TextBox text)
        {
            text.AutoSize = false;
        }
    }

    public static Button FieldIconButton()
    {
        return new Button
        {
            Size = new Size(FieldIconSize, FieldIconSize),
            MinimumSize = new Size(FieldIconSize, FieldIconSize),
            MaximumSize = new Size(FieldIconSize, FieldIconSize),
            Margin = new Padding(2, 0, 0, 0),
            Padding = new Padding(0),
            UseVisualStyleBackColor = true,
            TabStop = false,
        };
    }

    public static Button TextButton(string text)
    {
        var button = new Button();
        StyleTextButton(button, text);
        return button;
    }

    public static void StyleTextButton(Button button, string? text = null)
    {
        if (text is not null)
        {
            button.Text = text;
        }

        var font = button.Font ?? SystemFonts.MessageBoxFont ?? new Font("Segoe UI", 9f);
        var width = Math.Max(110, TextRenderer.MeasureText(button.Text, font).Width + 28);
        button.AutoSize = false;
        button.Size = new Size(width, ButtonHeight);
        button.MinimumSize = new Size(110, ButtonHeight);
        button.MaximumSize = new Size(400, ButtonHeight);
        button.Padding = new Padding(10, 0, 10, 0);
        button.Margin = new Padding(0, 0, 8, 0);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.UseVisualStyleBackColor = true;
        button.Height = ButtonHeight;
    }

    // --- 16x16 icons: show, generate, copy, paste ---

    public static Image EyeIcon(bool open)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(50, 50, 50), 1.4f);
        using var brush = new SolidBrush(Color.FromArgb(50, 50, 50));
        g.DrawEllipse(pen, 1, 5, 14, 6);
        if (open)
        {
            g.FillEllipse(brush, 6, 6, 4, 4);
        }
        else
        {
            g.DrawLine(pen, 2, 13, 14, 3);
        }

        return bmp;
    }

    public static Image CubeIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(50, 50, 50), 1.2f);
        using var fill = new SolidBrush(Color.FromArgb(230, 230, 230));
        var face = new[] { new Point(3, 5), new Point(10, 3), new Point(13, 7), new Point(6, 9) };
        g.FillPolygon(fill, face);
        g.DrawPolygon(pen, face);
        g.DrawPolygon(pen, new[] { new Point(3, 5), new Point(6, 9), new Point(6, 14), new Point(3, 10) });
        g.DrawPolygon(pen, new[] { new Point(6, 9), new Point(13, 7), new Point(13, 12), new Point(6, 14) });
        using var dot = new SolidBrush(Color.FromArgb(50, 50, 50));
        g.FillEllipse(dot, 7, 6, 2, 2);
        return bmp;
    }

    public static Image CopyIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(50, 50, 50), 1.2f);
        g.DrawRectangle(pen, 5, 2, 8, 9);
        g.DrawRectangle(pen, 2, 5, 8, 9);
        return bmp;
    }

    public static Image PasteIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(50, 50, 50), 1.2f);
        g.DrawRectangle(pen, 3, 3, 10, 11);
        g.DrawRectangle(pen, 6, 1, 4, 3);
        g.DrawLine(pen, 8, 7, 8, 12);
        g.DrawLine(pen, 6, 10, 8, 12);
        g.DrawLine(pen, 10, 10, 8, 12);
        return bmp;
    }

    public static Button IconButton()
    {
        return new Button
        {
            Size = new Size(ButtonHeight, ButtonHeight),
            MinimumSize = new Size(ButtonHeight, ButtonHeight),
            MaximumSize = new Size(ButtonHeight, ButtonHeight),
            Margin = new Padding(0),
            Padding = new Padding(0),
            UseVisualStyleBackColor = true,
            TabStop = false,
        };
    }
}
