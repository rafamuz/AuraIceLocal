using LibreHardwareMonitor.Hardware;

namespace AuraIceLocal.Tests;

public sealed class PawnIoAndPollingTests
{
    [Theory]
    [InlineData("2.2.0", true)]
    [InlineData("2.2.0.0", true)]
    [InlineData("2.3.0", true)]
    [InlineData("2.1.0", false)]
    [InlineData(null, false)]
    public void RequiresPawnIo22OrNewer(string? version, bool expected)
    {
        Assert.Equal(expected, PawnIoSupport.IsSupportedVersion(version));
    }

    [Fact]
    public void AcceptsOnlyPinnedOfficialInstallerHash()
    {
        Assert.True(PawnIoSupport.IsExpectedInstallerHash(PawnIoSupport.ExpectedInstallerSha256));
        Assert.True(PawnIoSupport.IsExpectedInstallerHash(PawnIoSupport.ExpectedInstallerSha256.ToLowerInvariant()));
        Assert.False(PawnIoSupport.IsExpectedInstallerHash(new string('0', 64)));
    }

    [Theory]
    [InlineData(HardwareType.Cpu, false, true)]
    [InlineData(HardwareType.GpuNvidia, false, true)]
    [InlineData(HardwareType.Memory, false, true)]
    [InlineData(HardwareType.Motherboard, false, false)]
    [InlineData(HardwareType.Motherboard, true, true)]
    [InlineData(HardwareType.SuperIO, false, false)]
    [InlineData(HardwareType.SuperIO, true, true)]
    [InlineData(HardwareType.EmbeddedController, true, false)]
    [InlineData(HardwareType.Cooler, true, false)]
    public void AppliesSafeHardwarePollingPolicy(
        HardwareType hardwareType,
        bool slowHardwareDue,
        bool expected)
    {
        Assert.Equal(expected, HardwareUpdatePolicy.ShouldUpdate(hardwareType, slowHardwareDue));
    }
}
