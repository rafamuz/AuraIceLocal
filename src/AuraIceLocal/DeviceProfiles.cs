using System.Globalization;
using System.Text.Json;

namespace AuraIceLocal;

internal sealed class DeviceProfilesFile
{
    public int SchemaVersion { get; set; } = 2;
    public List<DeviceProfile> Profiles { get; set; } = [];
}

internal sealed class DeviceProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = "AuraIceV1";
    public string VendorId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int OutputReportLength { get; set; }
    public int InputReportLength { get; set; }
    public int FeatureReportLength { get; set; }
    public string UsagePage { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string[] ProductNameContains { get; set; } = [];
    public string[] ManufacturerContains { get; set; } = [];

    public int VendorIdValue => ParseHexId(VendorId);
    public int ProductIdValue => ParseHexId(ProductId);

    public bool SupportsAuraIceV1 => string.Equals(Protocol, "AuraIceV1", StringComparison.OrdinalIgnoreCase);

    public int PreferredReportLength => OutputReportLength > 0 ? OutputReportLength : AuraIcePacket.ReportLength;

    private static int ParseHexId(string value)
    {
        string normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        return int.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : -1;
    }
}

internal static class DeviceProfileRepository
{
    private const string FileName = "device-profiles.json";

    public static IReadOnlyList<DeviceProfile> Load(out string sourceDescription)
    {
        string localOverride = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AuraIceLocal",
            FileName);

        string bundled = Path.Combine(AppContext.BaseDirectory, FileName);

        foreach ((string Path, string Description) candidate in new[]
                 {
                     (localOverride, "perfil personalizado em %LOCALAPPDATA%"),
                     (bundled, "perfis incluídos no aplicativo")
                 })
        {
            IReadOnlyList<DeviceProfile>? loaded = TryLoad(candidate.Path);
            if (loaded is { Count: > 0 })
            {
                sourceDescription = candidate.Description;
                return loaded;
            }
        }

        sourceDescription = "perfil interno de emergência";
        return [CreateBuiltInAuraIceProfile()];
    }

    private static IReadOnlyList<DeviceProfile>? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            DeviceProfilesFile? file = JsonSerializer.Deserialize<DeviceProfilesFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (file is null || file.SchemaVersion < 2)
            {
                return null;
            }

            DeviceProfile[] valid = file.Profiles
                .Where(IsValid)
                .GroupBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            return valid.Length > 0 ? valid : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValid(DeviceProfile profile) =>
        !string.IsNullOrWhiteSpace(profile.Id) &&
        !string.IsNullOrWhiteSpace(profile.Name) &&
        profile.VendorIdValue >= 0 &&
        profile.ProductIdValue >= 0 &&
        profile.OutputReportLength == AuraIcePacket.ReportLength;

    private static DeviceProfile CreateBuiltInAuraIceProfile() => new()
    {
        Id = "rise-mode-aura-ice-v1",
        Name = "Rise Mode Aura Ice",
        Protocol = "AuraIceV1",
        VendorId = "AA88",
        ProductId = "8666",
        OutputReportLength = 11,
        InputReportLength = 11,
        FeatureReportLength = 0,
        UsagePage = "0x0001",
        Usage = "0x0000",
        ProductNameContains = ["Rise", "Aura", "Ice", "温度显示HID设备"],
        ManufacturerContains = ["Rise Mode", "Rise", "铭研科技"]
    };
}
