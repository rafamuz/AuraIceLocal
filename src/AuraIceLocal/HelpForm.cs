using System.Text.RegularExpressions;

namespace AuraIceLocal;

internal sealed partial class HelpForm : Form
{
    private static readonly Regex MarkdownLink = MarkdownLinkRegex();
    private readonly ListBox _sectionList = new();
    private readonly RichTextBox _content = new();
    private readonly IReadOnlyList<HelpSection> _sections;
    private readonly Font _titleFont = new("Segoe UI", 15, FontStyle.Bold);
    private readonly Font _headingFont = new("Segoe UI", 11, FontStyle.Bold);
    private readonly Font _bodyFont = new("Segoe UI", 10.5F);
    private readonly Font _codeFont = new("Consolas", 9.5F);
    private readonly Bitmap _appIconBitmap = AppVisualAssets.CreateApplicationBitmap();

    public HelpForm()
    {
        _sections = HelpContent.LoadSections();
        Text = "Ajuda — RM Aura Ice Display";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(1080, 760);
        ShowInTaskbar = false;
        UiTheme.ApplyForm(this);

        BuildLayout();
        _sectionList.DataSource = _sections.ToArray();
        _sectionList.DisplayMember = nameof(HelpSection.Title);
        _sectionList.DrawMode = DrawMode.OwnerDrawFixed;
        _sectionList.ItemHeight = 38;
        _sectionList.DrawItem += DrawSectionItem;
        _sectionList.SelectedIndexChanged += (_, _) => ShowSelectedSection();
        if (_sections.Count > 0)
        {
            _sectionList.SelectedIndex = 0;
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = UiTheme.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new PictureBox
        {
            Image = _appIconBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(52, 52),
            Margin = new Padding(0, 0, 12, 0)
        }, 0, 0);
        var headerText = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        headerText.Controls.Add(new Label
        {
            Text = "Manual do usuário",
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 2, 0, 1)
        }, 0, 0);
        headerText.Controls.Add(new Label
        {
            Text = "Recursos, segurança e solução de problemas",
            AutoSize = true,
            ForeColor = UiTheme.MutedText,
            Margin = Padding.Empty
        }, 0, 1);
        header.Controls.Add(headerText, 1, 0);
        root.Controls.Add(header, 0, 0);

        var contentArea = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.AppBackground,
            Margin = Padding.Empty
        };
        contentArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        contentArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var navigationCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 12, 0)
        };

        _sectionList.Dock = DockStyle.Fill;
        _sectionList.BorderStyle = BorderStyle.None;
        _sectionList.IntegralHeight = false;
        _sectionList.Font = new Font("Segoe UI", 10);
        _sectionList.BackColor = UiTheme.CardBackground;
        navigationCard.Controls.Add(_sectionList);
        contentArea.Controls.Add(navigationCard, 0, 0);

        var articleCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(20),
            Margin = Padding.Empty
        };

        _content.Dock = DockStyle.Fill;
        _content.ReadOnly = true;
        _content.BorderStyle = BorderStyle.None;
        _content.BackColor = UiTheme.CardBackground;
        _content.ForeColor = UiTheme.Text;
        _content.DetectUrls = true;
        _content.HideSelection = false;
        _content.ScrollBars = RichTextBoxScrollBars.Vertical;
        articleCard.Controls.Add(_content);
        contentArea.Controls.Add(articleCard, 1, 0);
        root.Controls.Add(contentArea, 0, 1);

        var closeButton = new Button
        {
            Text = "Fechar",
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 12, 0, 0)
        };
        UiTheme.StyleButton(closeButton, UiIconKind.Close);
        closeButton.Click += (_, _) => Close();
        root.Controls.Add(closeButton, 0, 2);

        Controls.Add(root);
        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private void DrawSectionItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _sections.Count)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color backColor = selected ? Color.FromArgb(229, 239, 255) : UiTheme.CardBackground;
        Color foreColor = selected ? UiTheme.PrimaryDark : UiTheme.Text;
        using var background = new SolidBrush(backColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        Rectangle textBounds = new(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 18, e.Bounds.Height);
        TextRenderer.DrawText(
            e.Graphics,
            _sections[e.Index].Title,
            _sectionList.Font,
            textBounds,
            foreColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (selected)
        {
            using var accent = new SolidBrush(UiTheme.Primary);
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 5, 4, e.Bounds.Height - 10);
        }
    }

    private void ShowSelectedSection()
    {
        if (_sectionList.SelectedItem is not HelpSection section)
        {
            return;
        }

        _content.Clear();
        AppendText(section.Title, _titleFont, Color.FromArgb(31, 55, 78));
        _content.AppendText(Environment.NewLine + Environment.NewLine);

        bool codeBlock = false;
        foreach (string sourceLine in section.Content.Replace("\r\n", "\n").Split('\n'))
        {
            string line = sourceLine.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                codeBlock = !codeBlock;
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                _content.AppendText(Environment.NewLine);
                AppendText(line[4..], _headingFont, Color.FromArgb(45, 70, 95));
                _content.AppendText(Environment.NewLine);
                continue;
            }

            string display = NormalizeMarkdown(line);
            Font font = codeBlock ? _codeFont : _bodyFont;
            AppendText(display, font, SystemColors.WindowText);
            _content.AppendText(Environment.NewLine);
        }

        _content.SelectionStart = 0;
        _content.ScrollToCaret();
    }

    private void AppendText(string text, Font font, Color color)
    {
        _content.SelectionStart = _content.TextLength;
        _content.SelectionLength = 0;
        _content.SelectionFont = font;
        _content.SelectionColor = color;
        _content.AppendText(text);
    }

    private static string NormalizeMarkdown(string line)
    {
        string normalized = line.StartsWith("- ", StringComparison.Ordinal)
            ? $"• {line[2..]}"
            : line;
        normalized = normalized.Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal);
        return MarkdownLink.Replace(normalized, "$1 ($2)");
    }

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _headingFont.Dispose();
            _bodyFont.Dispose();
            _codeFont.Dispose();
            _appIconBitmap.Dispose();
        }
        base.Dispose(disposing);
    }
}
