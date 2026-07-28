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

    public HelpForm()
    {
        _sections = HelpContent.LoadSections();
        Text = "Ajuda — RM Aura Ice Display";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(1080, 760);
        Font = new Font("Segoe UI", 10);
        ShowInTaskbar = false;

        BuildLayout();
        _sectionList.DataSource = _sections.ToArray();
        _sectionList.DisplayMember = nameof(HelpSection.Title);
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
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Manual do usuário",
            AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 55, 78),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 260,
            Panel1MinSize = 200,
            Panel2MinSize = 380
        };

        _sectionList.Dock = DockStyle.Fill;
        _sectionList.BorderStyle = BorderStyle.FixedSingle;
        _sectionList.IntegralHeight = false;
        _sectionList.Font = new Font("Segoe UI", 10);
        split.Panel1.Padding = new Padding(0, 0, 10, 0);
        split.Panel1.Controls.Add(_sectionList);

        _content.Dock = DockStyle.Fill;
        _content.ReadOnly = true;
        _content.BorderStyle = BorderStyle.FixedSingle;
        _content.BackColor = SystemColors.Window;
        _content.DetectUrls = true;
        _content.HideSelection = false;
        _content.ScrollBars = RichTextBoxScrollBars.Vertical;
        split.Panel2.Controls.Add(_content);
        root.Controls.Add(split, 0, 1);

        var closeButton = new Button
        {
            Text = "Fechar",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 12, 0, 0)
        };
        closeButton.Click += (_, _) => Close();
        root.Controls.Add(closeButton, 0, 2);

        Controls.Add(root);
        AcceptButton = closeButton;
        CancelButton = closeButton;
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
        }
        base.Dispose(disposing);
    }
}
