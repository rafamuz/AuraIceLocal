using HidSharp;

namespace AuraIceLocal;

internal sealed class AuraIceHidTransport : IDisposable
{
    private readonly object _sync = new();
    private HidStream? _stream;
    private HidDevice? _device;
    private string? _runtimePath;

    public bool IsConnected
    {
        get
        {
            lock (_sync)
            {
                return _stream is not null;
            }
        }
    }

    public bool IsConnectedTo(HidDeviceCandidate candidate)
    {
        lock (_sync)
        {
            return _stream is not null &&
                string.Equals(_runtimePath, candidate.RuntimePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Connect(HidDeviceCandidate candidate)
    {
        lock (_sync)
        {
            EnsureSafeCandidate(candidate);
            ConnectCore(candidate);
        }
    }

    public byte[] Send(AuraIcePacket packet, HidDeviceCandidate candidate)
    {
        lock (_sync)
        {
            EnsureSafeCandidate(candidate);
            ConnectCore(candidate);
            int reportLength = _device!.GetMaxOutputReportLength();
            byte[] report = packet.BuildReport();
            ValidateReportLength(report.Length, reportLength);
            _stream!.Write(report);
            return report;
        }
    }

    internal static void ValidateReportLength(int packetLength, int deviceReportLength)
    {
        if (packetLength != deviceReportLength)
        {
            throw new InvalidOperationException(
                $"Envio bloqueado: o pacote AuraIceV1 possui {packetLength} bytes, mas o dispositivo exige {deviceReportLength} bytes.");
        }
    }

    private static void EnsureSafeCandidate(HidDeviceCandidate candidate)
    {
        if (!candidate.IsSafeForAutomaticUse)
        {
            throw new InvalidOperationException(
                "O dispositivo selecionado não foi reconhecido com segurança por um perfil AuraIceV1. Nenhum dado foi enviado.");
        }
    }

    private void ConnectCore(HidDeviceCandidate candidate)
    {
        if (_stream is not null && string.Equals(_runtimePath, candidate.RuntimePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        DisconnectCore();

        HidDevice? currentDevice = DeviceList.Local.GetHidDevices(candidate.VendorId, candidate.ProductId)
            .FirstOrDefault(device => string.Equals(device.DevicePath, candidate.RuntimePath, StringComparison.OrdinalIgnoreCase));

        if (currentDevice is null)
        {
            throw new IOException("O visor selecionado foi desconectado ou mudou de caminho. Atualize a lista de visores.");
        }

        if (!currentDevice.TryOpen(out HidStream? stream) || stream is null)
        {
            throw new IOException("Não foi possível abrir o visor. Feche o software oficial da Rise Mode e tente novamente.");
        }

        stream.WriteTimeout = 1000;
        _device = currentDevice;
        _runtimePath = currentDevice.DevicePath;
        _stream = stream;
    }

    public void Disconnect()
    {
        lock (_sync)
        {
            DisconnectCore();
        }
    }

    private void DisconnectCore()
    {
        _stream?.Dispose();
        _stream = null;
        _device = null;
        _runtimePath = null;
    }

    public void Dispose() => Disconnect();
}
