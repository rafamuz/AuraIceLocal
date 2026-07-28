using System.Text.Json;

namespace AuraIceLocal.Tests;

public sealed class SafetyStateTests
{
    [Fact]
    public void SettingsLoadAndSavePreserveAutomationChoices()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"AuraIceLocal.Tests.{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "settings.json");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonSerializer.Serialize(new AppSettings
            {
                StartWithWindows = true,
                AutoStartMonitoring = true,
                WindowX = 120,
                WindowY = 80,
                WindowWidth = 1100,
                WindowHeight = 760,
                WindowMaximized = true
            }));

            AppSettings loaded = AppSettings.LoadFromPath(path);
            Assert.True(loaded.StartWithWindows);
            Assert.True(loaded.AutoStartMonitoring);
            Assert.True(loaded.HasWindowPlacement);
            Assert.True(loaded.WindowMaximized);

            loaded.SaveToPath(path);
            AppSettings saved = AppSettings.LoadFromPath(path);
            Assert.True(saved.StartWithWindows);
            Assert.True(saved.AutoStartMonitoring);
            Assert.Equal(1100, saved.WindowWidth);
            Assert.Equal(760, saved.WindowHeight);
            Assert.True(saved.WindowMaximized);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void NewSettingsDoNotAutoStartMonitoringUntilSelected()
    {
        Assert.False(new AppSettings().AutoStartMonitoring);
    }

    [Fact]
    public void SavedSettingsDoNotContainDevelopmentMode()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"AuraIceLocal.Tests.{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "settings.json");
        try
        {
            new AppSettings { AutoStartMonitoring = true }.SaveToPath(path);

            string json = File.ReadAllText(path);
            Assert.DoesNotContain("DevMode", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AutoStartMonitoring", json, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void UsbWriteSessionCanBeAuthorizedAndDisabled()
    {
        var session = new UsbWriteSession();
        Assert.False(session.WritesEnabled);

        session.Authorize();
        Assert.True(session.WritesEnabled);

        session.Disable();
        Assert.False(session.WritesEnabled);
    }

    [Fact]
    public void InstalledStartupUsesStableVelopackLauncher()
    {
        string resolved = StartupExecutableResolver.Resolve(
            @"C:\Apps\AuraIceLocal\current\AuraIceLocal.exe",
            @"C:\Apps\AuraIceLocal",
            isVelopackInstalled: true,
            fileExists: path => path == @"C:\Apps\AuraIceLocal\AuraIceLocal.exe");

        Assert.Equal(@"C:\Apps\AuraIceLocal\AuraIceLocal.exe", resolved);
    }

    [Fact]
    public void PortableStartupKeepsCurrentExecutable()
    {
        string current = @"D:\Portable\AuraIceLocal.exe";
        string resolved = StartupExecutableResolver.Resolve(
            current,
            @"C:\Apps\AuraIceLocal",
            isVelopackInstalled: false,
            fileExists: _ => true);

        Assert.Equal(current, resolved);
    }
}
