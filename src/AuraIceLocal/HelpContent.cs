using System.Reflection;

namespace AuraIceLocal;

internal sealed record HelpSection(string Title, string Content);

internal static class HelpContent
{
    private const string ResourceName = "AuraIceLocal.ManualDoUsuario.md";

    public static IReadOnlyList<HelpSection> LoadSections()
    {
        Assembly assembly = typeof(HelpContent).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("O manual incorporado não foi encontrado.");
        using var reader = new StreamReader(stream);
        return ParseSections(reader.ReadToEnd());
    }

    internal static IReadOnlyList<HelpSection> ParseSections(string markdown)
    {
        var sections = new List<HelpSection>();
        var content = new List<string>();
        string title = "Apresentação";

        void AddCurrentSection()
        {
            string body = string.Join(Environment.NewLine, content).Trim();
            if (!string.IsNullOrWhiteSpace(body))
            {
                sections.Add(new HelpSection(title, body));
            }
            content.Clear();
        }

        foreach (string sourceLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = sourceLine.TrimEnd();
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AddCurrentSection();
                title = line[3..].Trim();
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                continue;
            }

            content.Add(line);
        }

        AddCurrentSection();
        return sections;
    }
}
