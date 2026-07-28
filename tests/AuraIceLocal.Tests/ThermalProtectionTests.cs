namespace AuraIceLocal.Tests;

public sealed class ThermalProtectionTests
{
    [Fact]
    public void EntersAtEightyAndReleasesOnlyAfterFiveContinuousSecondsBelowSeventyFive()
    {
        var controller = new ThermalProtectionController();
        DateTime start = new(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc);

        ThermalProtectionResult triggered = controller.Evaluate(Snapshot(81, 80), 70, start);
        Assert.Equal(ThermalProtectionState.Active, triggered.State);
        Assert.Equal(81, triggered.DisplayTemperature);
        Assert.Equal("Core Max", triggered.ProtectionSensor);

        ThermalProtectionResult cooling = controller.Evaluate(Snapshot(74, 73), 69, start.AddSeconds(1));
        Assert.Equal(ThermalProtectionState.CoolingDown, cooling.State);
        Assert.Equal(74, cooling.DisplayTemperature);

        ThermalProtectionResult interrupted = controller.Evaluate(Snapshot(75, 74), 68, start.AddSeconds(4));
        Assert.Equal(ThermalProtectionState.Active, interrupted.State);

        ThermalProtectionResult coolingAgain = controller.Evaluate(Snapshot(74, 73), 67, start.AddSeconds(5));
        Assert.Equal(ThermalProtectionState.CoolingDown, coolingAgain.State);

        ThermalProtectionResult tooSoon = controller.Evaluate(Snapshot(73, 72), 66, start.AddSeconds(9.999));
        Assert.Equal(ThermalProtectionState.CoolingDown, tooSoon.State);

        ThermalProtectionResult released = controller.Evaluate(Snapshot(73, 72), 65, start.AddSeconds(10));
        Assert.Equal(ThermalProtectionState.Normal, released.State);
        Assert.Equal(65, released.DisplayTemperature);
    }

    private static HardwareSnapshot Snapshot(float coreMax, float package) => new(
        DateTime.Now,
        "Core Average",
        70,
        null,
        null,
        null,
        null,
        null,
        [
            new SensorReading("Core Average", "/intelcpu/0/temperature/0", 70),
            new SensorReading("CPU Package", "/intelcpu/0/temperature/1", package),
            new SensorReading("Core Max", "/intelcpu/0/temperature/2", coreMax)
        ]);
}
