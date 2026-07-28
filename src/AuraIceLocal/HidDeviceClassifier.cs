namespace AuraIceLocal;

internal sealed record HidDescriptorSnapshot(
    int VendorId,
    int ProductId,
    int OutputReportLength,
    int InputReportLength,
    int FeatureReportLength,
    string? Manufacturer,
    string? ProductName,
    string? UsagePage,
    string? Usage);

internal sealed record DeviceMatch(
    DeviceProfile? Profile,
    int Score,
    DeviceConfidence Confidence,
    string Details);

internal static class HidDeviceClassifier
{
    public static DeviceMatch FindBest(IReadOnlyList<DeviceProfile> profiles, HidDescriptorSnapshot descriptor)
    {
        DeviceMatch? best = null;

        foreach (DeviceProfile profile in profiles)
        {
            bool idsMatch = descriptor.VendorId == profile.VendorIdValue && descriptor.ProductId == profile.ProductIdValue;
            bool exactOutput = descriptor.OutputReportLength == profile.OutputReportLength && descriptor.OutputReportLength > 0;
            bool inputMatches = descriptor.InputReportLength == profile.InputReportLength;
            bool featureMatches = descriptor.FeatureReportLength == profile.FeatureReportLength;
            int productMatches = CountTextMatches(descriptor.ProductName, profile.ProductNameContains);
            int manufacturerMatches = CountTextMatches(descriptor.Manufacturer, profile.ManufacturerContains);
            bool usagePageMatches = MatchesExact(descriptor.UsagePage, profile.UsagePage);
            bool usageMatches = MatchesExact(descriptor.Usage, profile.Usage);

            int score = 0;
            List<string> details = [];

            if (idsMatch)
            {
                score += 100;
                details.Add("VID/PID conhecidos");
            }

            if (exactOutput)
            {
                score += 40;
                details.Add($"relatório de saída exato ({descriptor.OutputReportLength} bytes)");
            }
            else if (descriptor.OutputReportLength > 0)
            {
                score += 5;
                details.Add($"saída incompatível ({descriptor.OutputReportLength} bytes; esperado {profile.OutputReportLength})");
            }

            AddAuxiliaryMatch(inputMatches, 10, $"entrada {descriptor.InputReportLength} bytes", ref score, details);
            AddAuxiliaryMatch(featureMatches, 5, $"feature {descriptor.FeatureReportLength} bytes", ref score, details);

            if (productMatches > 0)
            {
                score += Math.Min(productMatches * 10, 20);
                details.Add("produto compatível");
            }

            if (manufacturerMatches > 0)
            {
                score += 10;
                details.Add("fabricante compatível");
            }

            AddAuxiliaryMatch(usagePageMatches, 15, "Usage Page compatível", ref score, details);
            AddAuxiliaryMatch(usageMatches, 15, "Usage compatível", ref score, details);

            bool hasEvidence = idsMatch || exactOutput || productMatches > 0 || manufacturerMatches > 0 || usagePageMatches || usageMatches;
            if (!hasEvidence)
            {
                continue;
            }

            // A capacidade de escrita é representada por um relatório de saída positivo.
            DeviceConfidence confidence = idsMatch && exactOutput
                ? DeviceConfidence.Confirmed
                : idsMatch
                    ? DeviceConfidence.Recognized
                    : score >= 40
                        ? DeviceConfidence.Possible
                        : DeviceConfidence.Unknown;

            DeviceMatch current = new(profile, score, confidence, string.Join(", ", details));
            if (best is null || current.Score > best.Score)
            {
                best = current;
            }
        }

        return best ?? new DeviceMatch(null, 0, DeviceConfidence.Unknown, "sem correspondência com os perfis conhecidos");
    }

    private static void AddAuxiliaryMatch(bool matches, int points, string detail, ref int score, List<string> details)
    {
        if (!matches)
        {
            return;
        }

        score += points;
        details.Add(detail);
    }

    private static bool MatchesExact(string? value, string expected) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.IsNullOrWhiteSpace(expected) &&
        string.Equals(value.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int CountTextMatches(string? value, IEnumerable<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return tokens.Count(token =>
            !string.IsNullOrWhiteSpace(token) &&
            value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class HidDeviceSafety
{
    public static bool IsSafeForUsbTransport(DeviceProfile? profile, DeviceConfidence confidence, int outputReportLength) =>
        profile is { SupportsAuraIceV1: true } &&
        confidence == DeviceConfidence.Confirmed &&
        outputReportLength == AuraIcePacket.ReportLength;
}
