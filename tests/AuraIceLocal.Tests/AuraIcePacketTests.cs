namespace AuraIceLocal.Tests;

public sealed class AuraIcePacketTests
{
    [Fact]
    public void BuildReport_HasExactlyElevenBytesAndCorrectPositions()
    {
        var packet = new AuraIcePacket(66, 51, 43, 61, 35, 78);

        byte[] report = packet.BuildReport();

        Assert.Equal(11, report.Length);
        Assert.Equal(0, report[0]);
        Assert.Equal(new byte[] { 66, 51, 43, 61, 35, 78 }, report[1..7]);
        Assert.All(report[7..11], value => Assert.Equal(0, value));
        Assert.Equal("00 42 33 2B 3D 23 4E 00 00 00 00", packet.ToHex());
    }

    [Theory]
    [InlineData(-1.0, 0)]
    [InlineData(0.49, 0)]
    [InlineData(0.50, 1)]
    [InlineData(74.50, 75)]
    [InlineData(124.50, 125)]
    [InlineData(126.0, 125)]
    public void Temperature_IsRoundedAndClampedIndependently(double value, byte expected) =>
        Assert.Equal(expected, AuraIcePacket.ToTemperature(value));

    [Theory]
    [InlineData(-1.0, 0)]
    [InlineData(49.49, 49)]
    [InlineData(49.50, 50)]
    [InlineData(100.49, 100)]
    [InlineData(125.0, 100)]
    public void Percentage_IsRoundedAndClampedIndependently(double value, byte expected) =>
        Assert.Equal(expected, AuraIcePacket.ToPercentage(value));

    [Fact]
    public void UnavailableAndNonFiniteValuesBecomeZero()
    {
        Assert.Equal(0, AuraIcePacket.ToTemperature((float?)null));
        Assert.Equal(0, AuraIcePacket.ToPercentage((float?)null));
        Assert.Equal(0, AuraIcePacket.ToTemperature(double.NaN));
        Assert.Equal(0, AuraIcePacket.ToPercentage(double.PositiveInfinity));
    }
}
