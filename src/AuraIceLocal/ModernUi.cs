using System.Drawing.Drawing2D;
using System.Reflection;

namespace AuraIceLocal;

internal enum UiButtonKind
{
    Primary,
    Secondary,
    Danger
}

internal enum UiIconKind
{
    Search,
    Play,
    Stop,
    Send,
    Refresh,
    Help,
    Info,
    Panel,
    Exit,
    Close,
    Device,
    Cpu,
    Automation,
    Update
}

internal static class UiTheme
{
    public static readonly Color AppBackground = Color.FromArgb(244, 247, 251);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color SoftBackground = Color.FromArgb(247, 249, 253);
    public static readonly Color Primary = Color.FromArgb(30, 111, 224);
    public static readonly Color PrimaryDark = Color.FromArgb(18, 75, 154);
    public static readonly Color Accent = Color.FromArgb(112, 72, 232);
    public static readonly Color Danger = Color.FromArgb(196, 55, 71);
    public static readonly Color Text = Color.FromArgb(28, 39, 55);
    public static readonly Color MutedText = Color.FromArgb(96, 109, 128);
    public static readonly Color Border = Color.FromArgb(220, 226, 235);

    public static void ApplyForm(Form form)
    {
        form.BackColor = AppBackground;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 10F);
        form.Icon = AppVisualAssets.CreateApplicationIcon();
    }

    public static void StyleButton(Button button, UiIconKind icon, UiButtonKind kind = UiButtonKind.Secondary)
    {
        (Color backColor, Color foreColor, Color borderColor) = kind switch
        {
            UiButtonKind.Primary => (Primary, Color.White, Primary),
            UiButtonKind.Danger => (Danger, Color.White, Danger),
            _ => (Color.White, Text, Border)
        };

        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(0, 38);
        button.Padding = new Padding(12, 0, 14, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = kind == UiButtonKind.Secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = borderColor;
        button.FlatAppearance.MouseOverBackColor = kind switch
        {
            UiButtonKind.Primary => PrimaryDark,
            UiButtonKind.Danger => Color.FromArgb(162, 42, 56),
            _ => Color.FromArgb(238, 243, 250)
        };
        button.FlatAppearance.MouseDownBackColor = kind switch
        {
            UiButtonKind.Primary => Color.FromArgb(13, 62, 128),
            UiButtonKind.Danger => Color.FromArgb(136, 32, 45),
            _ => Color.FromArgb(226, 233, 243)
        };
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Image = UiIconFactory.Get(icon, foreColor);
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
    }

    public static void StyleInput(Control control)
    {
        control.BackColor = Color.White;
        control.ForeColor = Text;
        control.Margin = new Padding(4, 3, 10, 3);
        if (control is ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
        }
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.ForeColor = Text;
        checkBox.Cursor = Cursors.Hand;
        checkBox.Margin = new Padding(4, 7, 14, 4);
    }

    public static void StyleListView(ListView listView)
    {
        listView.BackColor = Color.White;
        listView.ForeColor = Text;
        listView.BorderStyle = BorderStyle.None;
        listView.GridLines = false;
        listView.HideSelection = false;
        listView.Font = new Font("Segoe UI", 9.5F);
    }

    public static void StyleMenu(MenuStrip menu)
    {
        menu.BackColor = Color.White;
        menu.ForeColor = Text;
        menu.Padding = new Padding(12, 5, 0, 5);
        menu.ImageScalingSize = new Size(18, 18);
        menu.RenderMode = ToolStripRenderMode.System;
    }

    public static void StyleContextMenu(ContextMenuStrip menu)
    {
        menu.BackColor = Color.White;
        menu.ForeColor = Text;
        menu.ImageScalingSize = new Size(18, 18);
        menu.Padding = new Padding(4);
        menu.RenderMode = ToolStripRenderMode.System;
    }

    public static Label NewCaption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = MutedText,
        Margin = new Padding(0, 0, 0, 5)
    };
}

internal static class AppVisualAssets
{
    private const string PngResourceName = "AuraIceLocal.AppIcon.png";
    private const string IconResourceName = "AuraIceLocal.AppIcon.ico";

    public static Bitmap CreateApplicationBitmap()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PngResourceName)
            ?? throw new InvalidOperationException("O ícone visual incorporado não foi encontrado.");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }

    public static Icon CreateApplicationIcon()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException("O ícone do aplicativo incorporado não foi encontrado.");
        using var source = new Icon(stream);
        return (Icon)source.Clone();
    }
}

internal static class UiIconFactory
{
    private static readonly Dictionary<(UiIconKind Kind, int Color), Image> Cache = [];
    private static readonly object CacheGate = new();

    public static Image Get(UiIconKind kind, Color color)
    {
        lock (CacheGate)
        {
            var key = (kind, color.ToArgb());
            if (!Cache.TryGetValue(key, out Image? image))
            {
                image = Draw(kind, color);
                Cache[key] = image;
            }
            return image;
        }
    }

    private static Bitmap Draw(UiIconKind kind, Color color)
    {
        const int size = 20;
        var bitmap = new Bitmap(size, size);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var pen = new Pen(color, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(color);

        switch (kind)
        {
            case UiIconKind.Search:
                graphics.DrawEllipse(pen, 2.5F, 2.5F, 10.5F, 10.5F);
                graphics.DrawLine(pen, 11.5F, 11.5F, 17F, 17F);
                break;
            case UiIconKind.Play:
                graphics.FillPolygon(brush, [new PointF(5, 3), new PointF(17, 10), new PointF(5, 17)]);
                break;
            case UiIconKind.Stop:
                graphics.FillRectangle(brush, 4, 4, 12, 12);
                break;
            case UiIconKind.Send:
                graphics.DrawLine(pen, 3, 10, 17, 3);
                graphics.DrawLine(pen, 17, 3, 13, 17);
                graphics.DrawLine(pen, 3, 10, 11, 11);
                graphics.DrawLine(pen, 11, 11, 13, 17);
                break;
            case UiIconKind.Refresh:
            case UiIconKind.Update:
                graphics.DrawArc(pen, 3, 3, 14, 14, 35, 285);
                graphics.FillPolygon(brush, [new PointF(15, 2), new PointF(18, 7), new PointF(12, 7)]);
                break;
            case UiIconKind.Help:
                graphics.DrawEllipse(pen, 2.5F, 2.5F, 15, 15);
                using (var font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    graphics.DrawString("?", font, brush, new RectangleF(2, 1, 16, 17), format);
                }
                break;
            case UiIconKind.Info:
                graphics.DrawEllipse(pen, 2.5F, 2.5F, 15, 15);
                graphics.FillEllipse(brush, 9, 5, 2, 2);
                graphics.DrawLine(pen, 10, 9, 10, 14);
                break;
            case UiIconKind.Panel:
                graphics.DrawRectangle(pen, 2.5F, 3.5F, 15, 13);
                graphics.DrawLine(pen, 3, 7, 17, 7);
                break;
            case UiIconKind.Exit:
                graphics.DrawRectangle(pen, 3, 3, 8, 14);
                graphics.DrawLine(pen, 8, 10, 18, 10);
                graphics.DrawLine(pen, 15, 7, 18, 10);
                graphics.DrawLine(pen, 15, 13, 18, 10);
                break;
            case UiIconKind.Close:
                graphics.DrawLine(pen, 4, 4, 16, 16);
                graphics.DrawLine(pen, 16, 4, 4, 16);
                break;
            case UiIconKind.Device:
                graphics.DrawRectangle(pen, 2.5F, 3.5F, 15, 11);
                graphics.DrawLine(pen, 7, 17, 13, 17);
                graphics.DrawLine(pen, 10, 14.5F, 10, 17);
                break;
            case UiIconKind.Cpu:
                graphics.DrawRectangle(pen, 5, 5, 10, 10);
                graphics.DrawRectangle(pen, 8, 8, 4, 4);
                for (int position = 7; position <= 13; position += 3)
                {
                    graphics.DrawLine(pen, position, 2, position, 5);
                    graphics.DrawLine(pen, position, 15, position, 18);
                    graphics.DrawLine(pen, 2, position, 5, position);
                    graphics.DrawLine(pen, 15, position, 18, position);
                }
                break;
            case UiIconKind.Automation:
                graphics.DrawEllipse(pen, 6, 6, 8, 8);
                graphics.DrawEllipse(pen, 8.5F, 8.5F, 3, 3);
                for (int angle = 0; angle < 360; angle += 45)
                {
                    double radians = angle * Math.PI / 180;
                    graphics.DrawLine(
                        pen,
                        10 + (float)Math.Cos(radians) * 5,
                        10 + (float)Math.Sin(radians) * 5,
                        10 + (float)Math.Cos(radians) * 8,
                        10 + (float)Math.Sin(radians) * 8);
                }
                break;
        }

        return bitmap;
    }
}
