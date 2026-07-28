using HidSharp;

namespace AuraIceLocal;

internal enum DeviceConfidence
{
    Unknown,
    Possible,
    Recognized,
    Confirmed
}

internal sealed class HidDeviceCandidate
{
    public required HidDevice Device { get; init; }
    public required string RuntimePath { get; init; }
    public required int VendorId { get; init; }
    public required int ProductId { get; init; }
    public required int OutputReportLength { get; init; }
    public int InputReportLength { get; init; }
    public int FeatureReportLength { get; init; }
    public string? Manufacturer { get; init; }
    public string? ProductName { get; init; }
    public string? SerialNumber { get; init; }
    public string? UsagePage { get; init; }
    public string? Usage { get; init; }
    public DeviceProfile? Profile { get; init; }
    public int Score { get; init; }
    public DeviceConfidence Confidence { get; init; }
    public string MatchDetails { get; init; } = string.Empty;

    public bool HasOutputReport => OutputReportLength > 0;
    public bool IsSafeForAutomaticUse => HidDeviceSafety.IsSafeForUsbTransport(Profile, Confidence, OutputReportLength);

    public string PersistentIdentity => string.Join("|", new[]
    {
        Profile?.Id ?? string.Empty,
        VendorId.ToString("X4"),
        ProductId.ToString("X4"),
        SerialNumber ?? string.Empty,
        ProductName ?? string.Empty
    });

    public string DisplayName
    {
        get
        {
            string profile = Profile is null
                ? "HID não reconhecido"
                : $"{ConfidenceLabel}: {Profile.Name}";
            string product = string.IsNullOrWhiteSpace(ProductName) ? string.Empty : $" — {ProductName}";
            return $"{profile}{product} [{VendorId:X4}:{ProductId:X4}, saída {OutputReportLength} bytes]";
        }
    }

    public string ConfidenceLabel => Confidence switch
    {
        DeviceConfidence.Confirmed => "Confirmado",
        DeviceConfidence.Recognized => "Reconhecido",
        DeviceConfidence.Possible => "Possível",
        _ => "Desconhecido"
    };

    public override string ToString() => DisplayName;
}

internal sealed record HidScanResult(
    DateTime Timestamp,
    IReadOnlyList<HidDeviceCandidate> AllDevices,
    IReadOnlyList<HidDeviceCandidate> Candidates,
    string ProfileSource)
{
    public IReadOnlyList<HidDeviceCandidate> SafeCandidates => Candidates.Where(candidate => candidate.IsSafeForAutomaticUse).ToArray();
}
