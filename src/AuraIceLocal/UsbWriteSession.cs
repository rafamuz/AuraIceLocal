namespace AuraIceLocal;

internal sealed class UsbWriteSession
{
    public bool WritesEnabled { get; private set; }

    public void Authorize() => WritesEnabled = true;

    public void Disable() => WritesEnabled = false;
}
