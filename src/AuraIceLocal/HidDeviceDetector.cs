using HidSharp;
using HidSharp.Reports;

namespace AuraIceLocal;

internal sealed class HidDeviceDetector
{
    private readonly IReadOnlyList<DeviceProfile> _profiles;

    public string ProfileSource { get; }

    public HidDeviceDetector()
    {
        _profiles = DeviceProfileRepository.Load(out string source);
        ProfileSource = source;
    }

    public HidScanResult Scan()
    {
        List<HidDeviceCandidate> devices = [];

        foreach (HidDevice device in DeviceList.Local.GetHidDevices())
        {
            int outputLength = SafeInt(device.GetMaxOutputReportLength);
            int inputLength = SafeInt(device.GetMaxInputReportLength);
            int featureLength = SafeInt(device.GetMaxFeatureReportLength);
            string? manufacturer = SafeString(device.GetManufacturer);
            string? productName = SafeString(device.GetProductName);
            string? serialNumber = SafeString(device.GetSerialNumber);
            (string? usagePage, string? usage) = TryReadUsage(device);

            DeviceMatch match = HidDeviceClassifier.FindBest(_profiles, new HidDescriptorSnapshot(
                device.VendorID,
                device.ProductID,
                outputLength,
                inputLength,
                featureLength,
                manufacturer,
                productName,
                usagePage,
                usage));

            devices.Add(new HidDeviceCandidate
            {
                Device = device,
                RuntimePath = device.DevicePath,
                VendorId = device.VendorID,
                ProductId = device.ProductID,
                OutputReportLength = outputLength,
                InputReportLength = inputLength,
                FeatureReportLength = featureLength,
                Manufacturer = manufacturer,
                ProductName = productName,
                SerialNumber = serialNumber,
                UsagePage = usagePage,
                Usage = usage,
                Profile = match.Profile,
                Score = match.Score,
                Confidence = match.Confidence,
                MatchDetails = match.Details
            });
        }

        HidDeviceCandidate[] ordered = devices
            .OrderByDescending(device => device.Confidence)
            .ThenByDescending(device => device.Score)
            .ThenBy(device => device.VendorId)
            .ThenBy(device => device.ProductId)
            .ThenBy(device => device.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        HidDeviceCandidate[] candidates = ordered
            .Where(device => device.Score > 0 || device.HasOutputReport)
            .ToArray();

        return new HidScanResult(DateTime.Now, ordered, candidates, ProfileSource);
    }

    private static int SafeInt(Func<int> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return 0;
        }
    }

    private static string? SafeString(Func<string> getter)
    {
        try
        {
            string value = getter();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static (string? UsagePage, string? Usage) TryReadUsage(HidDevice device)
    {
        try
        {
            ReportDescriptor descriptor = device.GetReportDescriptor();
            foreach (DeviceItem item in descriptor.DeviceItems)
            {
                uint[] values = item.Usages.GetAllValues().Take(1).ToArray();
                if (values.Length == 0)
                {
                    continue;
                }

                uint encodedUsage = values[0];
                uint usagePage = encodedUsage >> 16;
                uint usage = encodedUsage & 0xFFFF;
                return ($"0x{usagePage:X4}", $"0x{usage:X4}");
            }
        }
        catch
        {
            // Alguns dispositivos ou drivers não expõem o descritor completo.
        }

        return (null, null);
    }

}
