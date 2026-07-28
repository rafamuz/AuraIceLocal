namespace AuraIceLocal.Tests;

public sealed class HelpContentTests
{
    [Fact]
    public void EmbeddedManualDocumentsAllMainPanelActions()
    {
        IReadOnlyList<HelpSection> sections = HelpContent.LoadSections();
        string manual = string.Join("\n", sections.Select(section => $"{section.Title}\n{section.Content}"));

        Assert.True(sections.Count >= 15);
        Assert.Contains("Procurar visores", manual, StringComparison.Ordinal);
        Assert.Contains("Iniciar monitoramento", manual, StringComparison.Ordinal);
        Assert.Contains("Parar monitoramento", manual, StringComparison.Ordinal);
        Assert.Contains("Enviar um pacote de teste", manual, StringComparison.Ordinal);
        Assert.Contains("Iniciar com o Windows", manual, StringComparison.Ordinal);
        Assert.Contains("Monitorar e enviar ao abrir", manual, StringComparison.Ordinal);
        Assert.Contains("Verificar atualizações", manual, StringComparison.Ordinal);
        Assert.Contains("Painel", manual, StringComparison.Ordinal);
        Assert.Contains("Sair", manual, StringComparison.Ordinal);
        Assert.Contains("Confirmado", manual, StringComparison.Ordinal);
        Assert.Contains("11 bytes", manual, StringComparison.Ordinal);
    }

    [Fact]
    public void ParserBuildsNavigableSectionsFromMarkdownHeadings()
    {
        IReadOnlyList<HelpSection> sections = HelpContent.ParseSections(
            "# Manual\nIntrodução.\n\n## Visor\nConteúdo do visor.\n## Sensores\nConteúdo dos sensores.");

        Assert.Equal(["Apresentação", "Visor", "Sensores"], sections.Select(section => section.Title));
    }
}
