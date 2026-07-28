using LibreHardwareMonitor.Hardware;

namespace AuraIceLocal;

internal sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor = new();
    private bool _disposed;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };
        _computer.Open();
    }

    public HardwareSnapshot Read(string preferredCpuSensorName)
    {
        ThrowIfDisposed();
        _computer.Accept(_updateVisitor);

        List<ISensor> allSensors = EnumerateSensors(_computer.Hardware).ToList();
        List<ISensor> cpuTemperatureSensors = allSensors
            .Where(s => s.Hardware.HardwareType == HardwareType.Cpu && s.SensorType == SensorType.Temperature)
            .ToList();

        ISensor? selectedCpuSensor = SelectCpuTemperatureSensor(cpuTemperatureSensors, preferredCpuSensorName);

        float? cpuLoad = FindSensorValue(
            allSensors,
            s => s.Hardware.HardwareType == HardwareType.Cpu &&
                 s.SensorType == SensorType.Load &&
                 NameEquals(s, "CPU Total"));

        float? memoryLoad = FindSensorValue(
            allSensors,
            s => s.Hardware.HardwareType == HardwareType.Memory && s.SensorType == SensorType.Load);

        ISensor? gpuTemperature = FindGpuSensor(allSensors, SensorType.Temperature, "GPU Core");
        ISensor? gpuLoad = FindGpuSensor(allSensors, SensorType.Load, "GPU Core");

        float? motherboardTemperature = FindSensorValue(
            allSensors,
            s => IsMotherboardRelated(s.Hardware.HardwareType) &&
                 s.SensorType == SensorType.Temperature &&
                 (NameEquals(s, "CPU") || NameContains(s, "System") || NameContains(s, "Motherboard")))
            ?? FindSensorValue(
                allSensors,
                s => IsMotherboardRelated(s.Hardware.HardwareType) && s.SensorType == SensorType.Temperature);

        IReadOnlyList<SensorReading> diagnosticSensors = cpuTemperatureSensors
            .Select(s => new SensorReading(s.Name, s.Identifier.ToString(), s.Value))
            .OrderBy(s => SensorSortOrder(s.Name))
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HardwareSnapshot(
            DateTime.Now,
            selectedCpuSensor?.Name ?? "Não encontrado",
            selectedCpuSensor?.Value,
            cpuLoad,
            memoryLoad,
            gpuTemperature?.Value,
            gpuLoad?.Value,
            motherboardTemperature,
            diagnosticSensors);
    }

    private static ISensor? SelectCpuTemperatureSensor(List<ISensor> sensors, string preferredName)
    {
        ISensor? preferred = sensors.FirstOrDefault(s => NameEquals(s, preferredName) && s.Value.HasValue);
        if (preferred is not null)
        {
            return preferred;
        }

        string[] fallbacks = ["Core Average", "CPU Package", "Core Max"];
        foreach (string fallback in fallbacks)
        {
            ISensor? sensor = sensors.FirstOrDefault(s => NameEquals(s, fallback) && s.Value.HasValue);
            if (sensor is not null)
            {
                return sensor;
            }
        }

        return sensors.FirstOrDefault(s => s.Value.HasValue);
    }

    private static ISensor? FindGpuSensor(List<ISensor> sensors, SensorType type, string preferredName)
    {
        static bool IsGpu(HardwareType type) =>
            type is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia;

        return sensors.FirstOrDefault(s =>
                   IsGpu(s.Hardware.HardwareType) &&
                   s.SensorType == type &&
                   NameEquals(s, preferredName) &&
                   s.Value.HasValue)
               ?? sensors.FirstOrDefault(s =>
                   IsGpu(s.Hardware.HardwareType) &&
                   s.SensorType == type &&
                   s.Value.HasValue);
    }

    private static float? FindSensorValue(IEnumerable<ISensor> sensors, Func<ISensor, bool> predicate) =>
        sensors.FirstOrDefault(s => predicate(s) && s.Value.HasValue)?.Value;

    private static IEnumerable<ISensor> EnumerateSensors(IEnumerable<IHardware> hardwareItems)
    {
        foreach (IHardware hardware in hardwareItems)
        {
            foreach (ISensor sensor in hardware.Sensors)
            {
                yield return sensor;
            }

            foreach (ISensor sensor in EnumerateSensors(hardware.SubHardware))
            {
                yield return sensor;
            }
        }
    }

    private static bool NameEquals(ISensor sensor, string name) =>
        string.Equals(sensor.Name, name, StringComparison.OrdinalIgnoreCase);

    private static bool NameContains(ISensor sensor, string value) =>
        sensor.Name.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool IsMotherboardRelated(HardwareType type) =>
        type is HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController;

    private static int SensorSortOrder(string name) => name switch
    {
        "Core Average" => 0,
        "CPU Package" => 1,
        "Core Max" => 2,
        _ => 10
    };

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _computer.Close();
        _disposed = true;
    }
}
