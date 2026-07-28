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
    IReadOnlyList<SensorReading> CpuTemperatureSensors);
