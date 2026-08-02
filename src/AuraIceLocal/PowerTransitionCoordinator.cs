namespace AuraIceLocal;

internal readonly record struct PowerResumeRequest(long Generation);

internal sealed class PowerTransitionCoordinator
{
    private readonly object _sync = new();
    private long _generation;
    private bool _resumePending;

    public void Suspend(bool monitoringWasRunning)
    {
        lock (_sync)
        {
            _generation++;
            _resumePending = monitoringWasRunning;
        }
    }

    public PowerResumeRequest? Resume()
    {
        lock (_sync)
        {
            return _resumePending
                ? new PowerResumeRequest(_generation)
                : null;
        }
    }

    public bool IsPending(PowerResumeRequest request)
    {
        lock (_sync)
        {
            return _resumePending && request.Generation == _generation;
        }
    }

    public void Complete(PowerResumeRequest request)
    {
        lock (_sync)
        {
            if (request.Generation == _generation)
            {
                _resumePending = false;
            }
        }
    }

    public void Cancel()
    {
        lock (_sync)
        {
            _generation++;
            _resumePending = false;
        }
    }
}
