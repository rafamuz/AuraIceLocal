namespace AuraIceLocal;

internal enum ThermalProtectionState
{
    Normal,
    Active,
    CoolingDown
}

internal sealed record ThermalProtectionResult(
    double? DisplayTemperature,
    double? ProtectionTemperature,
    string ProtectionSensor,
    ThermalProtectionState State);

internal sealed class ThermalProtectionController
{
    internal const double TriggerTemperatureC = 80.0;
    internal const double ReleaseTemperatureC = 75.0;
    internal static readonly TimeSpan ReleaseDelay = TimeSpan.FromSeconds(5);

    private ThermalProtectionState _state = ThermalProtectionState.Normal;
    private DateTime? _belowReleaseSince;

    public ThermalProtectionResult Evaluate(HardwareSnapshot snapshot, double? smoothedTemperature, DateTime now)
    {
        SensorReading? hottest = snapshot.CpuTemperatureSensors
            .Where(sensor => IsProtectionSensor(sensor.Name) && sensor.Value.HasValue)
            .OrderByDescending(sensor => sensor.Value)
            .FirstOrDefault();

        double? protectionTemperature = hottest?.Value;
        string protectionSensor = hottest?.Name ?? "Core Max / CPU Package indisponível";

        if (protectionTemperature >= TriggerTemperatureC)
        {
            _state = ThermalProtectionState.Active;
            _belowReleaseSince = null;
        }
        else if (_state is ThermalProtectionState.Active or ThermalProtectionState.CoolingDown)
        {
            if (protectionTemperature < ReleaseTemperatureC)
            {
                _belowReleaseSince ??= now;
                if (now - _belowReleaseSince.Value >= ReleaseDelay)
                {
                    _state = ThermalProtectionState.Normal;
                    _belowReleaseSince = null;
                }
                else
                {
                    _state = ThermalProtectionState.CoolingDown;
                }
            }
            else
            {
                _state = ThermalProtectionState.Active;
                _belowReleaseSince = null;
            }
        }

        double? displayed = _state == ThermalProtectionState.Normal
            ? smoothedTemperature
            : protectionTemperature ?? smoothedTemperature;

        return new ThermalProtectionResult(displayed, protectionTemperature, protectionSensor, _state);
    }

    public void Reset()
    {
        _state = ThermalProtectionState.Normal;
        _belowReleaseSince = null;
    }

    private static bool IsProtectionSensor(string name) =>
        string.Equals(name, "Core Max", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "CPU Package", StringComparison.OrdinalIgnoreCase);
}
