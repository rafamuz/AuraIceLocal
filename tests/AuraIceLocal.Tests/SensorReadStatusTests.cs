namespace AuraIceLocal.Tests;

public sealed class SensorReadStatusTests
{
    [Fact]
    public void ReportsUnavailableWhenSensorsExistWithoutValues()
    {
        HardwareSnapshot snapshot = Snapshot(
            new SensorReading("Core Average", "/intelcpu/0/temperature/1", null),
            new SensorReading("CPU Package", "/intelcpu/0/temperature/22", null),
            new SensorReading("Core Max", "/intelcpu/0/temperature/0", null));

        Assert.Equal(CpuTemperatureReadState.ValuesUnavailable, snapshot.CpuTemperatureReadState);
        Assert.Contains("acesso de baixo nível indisponível", SensorReadStatus.MonitoringText(snapshot));
    }

    [Fact]
    public void ReportsAvailableWhenAnyTemperatureHasAValue()
    {
        HardwareSnapshot snapshot = Snapshot(
            new SensorReading("Core Average", "/intelcpu/0/temperature/1", 41.5f));

        Assert.Equal(CpuTemperatureReadState.Available, snapshot.CpuTemperatureReadState);
        Assert.Equal("Monitorando — Core Average", SensorReadStatus.MonitoringText(snapshot));
    }

    [Fact]
    public void ReportsNotEnumeratedWhenTemperatureListIsEmpty()
    {
        HardwareSnapshot snapshot = Snapshot();

        Assert.Equal(CpuTemperatureReadState.NotEnumerated, snapshot.CpuTemperatureReadState);
        Assert.Contains("não foram enumerados", SensorReadStatus.MonitoringText(snapshot));
    }

    [Theory]
    [InlineData(new string[0], "")]
    [InlineData(new[] { "--startup" }, "--startup")]
    [InlineData(new[] { "value with spaces", "quoted\"value" }, "\"value with spaces\" \"quoted\\\"value\"")]
    public void ElevationPreservesArguments(string[] arguments, string expected)
    {
        Assert.Equal(expected, WindowsElevation.BuildArgumentString(arguments));
    }

    private static HardwareSnapshot Snapshot(params SensorReading[] sensors) => new(
        DateTime.UtcNow,
        "Core Average",
        sensors.FirstOrDefault(sensor => sensor.Value.HasValue)?.Value,
        null,
        null,
        null,
        null,
        null,
        sensors);
}
