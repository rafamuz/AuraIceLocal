namespace AuraIceLocal;

internal sealed record AuraIcePacket(
    byte CpuTemperature,
    byte GpuTemperature,
    byte CpuLoad,
    byte MemoryLoad,
    byte MotherboardTemperature,
    byte GpuLoad)
{
    public const int ReportLength = 11;
    public const int ReportIdIndex = 0;

    public byte[] BuildReport()
    {
        byte[] report = new byte[ReportLength];
        report[ReportIdIndex] = 0;
        report[1] = CpuTemperature;
        report[2] = GpuTemperature;
        report[3] = CpuLoad;
        report[4] = MemoryLoad;
        report[5] = MotherboardTemperature;
        report[6] = GpuLoad;
        return report;
    }

    public static AuraIcePacket FromSnapshot(HardwareSnapshot snapshot, double displayedCpuTemperature) => new(
        ToTemperature(displayedCpuTemperature),
        ToTemperature(snapshot.GpuTemperature),
        ToPercentage(snapshot.CpuLoad),
        ToPercentage(snapshot.MemoryLoad),
        ToTemperature(snapshot.MotherboardTemperature),
        ToPercentage(snapshot.GpuLoad));

    public string ToReadableString() =>
        $"CPU {CpuTemperature} °C | GPU {GpuTemperature} °C | CPU {CpuLoad}% | RAM {MemoryLoad}% | MB {MotherboardTemperature} °C | GPU {GpuLoad}%";

    public string ToHex() => string.Join(" ", BuildReport().Select(value => value.ToString("X2")));

    internal static byte ToTemperature(float? value) => ToTemperature(value.HasValue ? value.Value : 0.0);

    internal static byte ToTemperature(double value) => ToBoundedByte(value, 125);

    internal static byte ToPercentage(float? value) => ToPercentage(value.HasValue ? value.Value : 0.0);

    internal static byte ToPercentage(double value) => ToBoundedByte(value, 100);

    private static byte ToBoundedByte(double value, byte maximum)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        return (byte)Math.Clamp(rounded, 0, maximum);
    }
}
