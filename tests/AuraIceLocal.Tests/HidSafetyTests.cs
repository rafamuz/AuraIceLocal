namespace AuraIceLocal.Tests;

public sealed class HidSafetyTests
{
    private static readonly DeviceProfile Profile = new()
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
        ProductNameContains = ["温度显示HID设备"],
        ManufacturerContains = ["铭研科技"]
    };

    [Fact]
    public void KnownIdsAndExactWritableOutputAreConfirmedWithoutAuxiliaryData()
    {
        DeviceMatch match = HidDeviceClassifier.FindBest([Profile], new HidDescriptorSnapshot(
            0xAA88, 0x8666, 11, 0, 99, null, null, null, null));

        Assert.Equal(DeviceConfidence.Confirmed, match.Confidence);
        Assert.True(HidDeviceSafety.IsSafeForUsbTransport(match.Profile, match.Confidence, 11));
    }

    [Fact]
    public void RealDescriptorIsConfirmedAndAuxiliaryDataRaisesScore()
    {
        DeviceMatch minimal = HidDeviceClassifier.FindBest([Profile], new HidDescriptorSnapshot(
            0xAA88, 0x8666, 11, 0, 99, null, null, null, null));
        DeviceMatch real = HidDeviceClassifier.FindBest([Profile], new HidDescriptorSnapshot(
            0xAA88, 0x8666, 11, 11, 0, "铭研科技", "温度显示HID设备", "0x0001", "0x0000"));

        Assert.Equal(DeviceConfidence.Confirmed, real.Confidence);
        Assert.True(real.Score > minimal.Score);
    }

    [Fact]
    public void WrongOutputIsRecognizedButCannotReachTransport()
    {
        DeviceMatch match = HidDeviceClassifier.FindBest([Profile], new HidDescriptorSnapshot(
            0xAA88, 0x8666, 24, 11, 0, null, null, null, null));

        Assert.Equal(DeviceConfidence.Recognized, match.Confidence);
        Assert.False(HidDeviceSafety.IsSafeForUsbTransport(match.Profile, match.Confidence, 24));
    }

    [Fact]
    public void PossibleAndUnknownNeverReachTransport()
    {
        Assert.False(HidDeviceSafety.IsSafeForUsbTransport(Profile, DeviceConfidence.Possible, 11));
        Assert.False(HidDeviceSafety.IsSafeForUsbTransport(Profile, DeviceConfidence.Unknown, 11));
    }

    [Fact]
    public void TransportRejectsAnyDifferentPacketLength()
    {
        AuraIceHidTransport.ValidateReportLength(11, 11);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            AuraIceHidTransport.ValidateReportLength(11, 24));
        Assert.Contains("11 bytes", error.Message);
        Assert.Contains("24 bytes", error.Message);
    }
}
