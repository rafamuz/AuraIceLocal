using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AuraIceLocal;

internal sealed class TrayTemperatureIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private Icon? _currentIcon;
    private int? _lastRoundedTemperature;
    private bool _disposed;

    public event Action? PanelRequested;
    public event Action? ExitRequested;

    public TrayTemperatureIcon()
    {
        var panelItem = new ToolStripMenuItem("Painel");
        var exitItem = new ToolStripMenuItem("Sair");
        panelItem.Click += (_, _) => PanelRequested?.Invoke();
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.Add(panelItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Text = "RM Aura Ice Display — temperatura indisponível",
            Visible = false
        };
        _notifyIcon.DoubleClick += (_, _) => PanelRequested?.Invoke();
        UpdateTemperature(null);
        _notifyIcon.Visible = true;
    }

    public void UpdateTemperature(double? temperature)
    {
        int? rounded = temperature.HasValue && double.IsFinite(temperature.Value)
            ? (int)Math.Round(temperature.Value, MidpointRounding.AwayFromZero)
            : null;

        _notifyIcon.Text = rounded.HasValue
            ? $"RM Aura Ice Display — {rounded.Value} °C"
            : "RM Aura Ice Display — temperatura indisponível";

        if (_lastRoundedTemperature == rounded && _currentIcon is not null)
        {
            return;
        }

        Icon replacement = CreateIcon(rounded);
        Icon? previous = _currentIcon;
        _currentIcon = replacement;
        _notifyIcon.Icon = replacement;
        _lastRoundedTemperature = rounded;
        previous?.Dispose();
    }

    internal static Icon CreateIcon(int? temperature)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            Color background = temperature switch
            {
                >= 80 => Color.FromArgb(207, 45, 45),
                >= 70 => Color.FromArgb(224, 124, 32),
                >= 50 => Color.FromArgb(35, 139, 88),
                not null => Color.FromArgb(38, 112, 184),
                null => Color.FromArgb(95, 104, 112)
            };

            using var brush = new SolidBrush(background);
            graphics.FillEllipse(brush, 1, 1, size - 2, size - 2);

            string text = temperature?.ToString() ?? "--";
            float fontSize = text.Length >= 3 ? 11f : 13f;
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(text, font, textBrush, new RectangleF(0, 0, size, size - 1), format);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
        _currentIcon = null;
        _disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
