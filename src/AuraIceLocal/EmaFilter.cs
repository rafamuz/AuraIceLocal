namespace AuraIceLocal;

internal sealed class EmaFilter
{
    private double? _value;

    public void Reset() => _value = null;

    public double Update(double sample, double elapsedSeconds, double smoothingSeconds)
    {
        if (_value is null || smoothingSeconds <= 0 || elapsedSeconds <= 0)
        {
            _value = sample;
            return sample;
        }

        double alpha = 1.0 - Math.Exp(-elapsedSeconds / smoothingSeconds);
        _value += alpha * (sample - _value.Value);
        return _value.Value;
    }
}
