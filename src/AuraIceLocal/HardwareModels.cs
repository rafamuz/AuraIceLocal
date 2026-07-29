namespace AuraIceLocal;

internal sealed record SensorReading(string Name, string Identifier, float? Value);

internal sealed record HardwareSnapshot(
    DateTime Timestamp,
    string SelectedCpuSensor,
    float? CpuTemperatureRaw,
    float? CpuLoad,
    float? MemoryLoad,
    float? GpuTemperature,
    float? GpuLoad,
    float? MotherboardTemperature,
    IReadOnlyList<SensorReading> CpuTemperatureSensors)
{
    public CpuTemperatureReadState CpuTemperatureReadState => CpuTemperatureSensors.Count == 0
        ? CpuTemperatureReadState.NotEnumerated
        : CpuTemperatureSensors.Any(sensor => sensor.Value.HasValue)
            ? CpuTemperatureReadState.Available
            : CpuTemperatureReadState.ValuesUnavailable;
}

internal enum CpuTemperatureReadState
{
    Available,
    NotEnumerated,
    ValuesUnavailable
}

internal static class SensorReadStatus
{
    public static string MonitoringText(HardwareSnapshot snapshot) => snapshot.CpuTemperatureReadState switch
    {
        CpuTemperatureReadState.Available => $"Monitorando — {snapshot.SelectedCpuSensor}",
        CpuTemperatureReadState.ValuesUnavailable =>
            "Sensores detectados, mas sem leitura — acesso de baixo nível indisponível",
        _ => "Sensores de temperatura da CPU não foram enumerados"
    };
}
