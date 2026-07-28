using System.Text.Json;

namespace AuraIceLocal;

internal sealed class AppSettings
{
    public string CpuSensorName { get; set; } = "Core Average";
    public double SmoothingSeconds { get; set; } = 3.0;
    public int PollIntervalMs { get; set; } = 250;
    public int LcdUpdateIntervalMs { get; set; } = 1000;
    public double CriticalTemperatureC { get; set; } = 80.0;
    public bool StartWithWindows { get; set; }
    public bool AutoStartMonitoring { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    // Identidade persistente: perfil + VID/PID + série/nome. O DevicePath não é salvo.
    public string? SelectedDeviceIdentity { get; set; }

    public bool HasWindowPlacement =>
        WindowX.HasValue &&
        WindowY.HasValue &&
        WindowWidth > 0 &&
        WindowHeight > 0;

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AuraIceLocal");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load() => LoadFromPath(SettingsPath);

    internal static AppSettings LoadFromPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save() => SaveToPath(SettingsPath);

    internal void SaveToPath(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
