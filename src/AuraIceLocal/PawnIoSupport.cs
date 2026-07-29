using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace AuraIceLocal;

internal static class PawnIoSupport
{
    public const string RequiredVersion = "2.2.0";
    public const string InstallerUrl =
        "https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe";
    public const string ExpectedInstallerSha256 =
        "1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032";

    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";

    public static bool IsInstalled() => IsSupportedVersion(GetInstalledVersion());

    public static string? GetInstalledVersion()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(UninstallKey);
            return key?.GetValue("DisplayVersion") as string;
        }
        catch
        {
            return null;
        }
    }

    internal static bool IsSupportedVersion(string? versionText) =>
        Version.TryParse(versionText, out Version? version) &&
        version >= new Version(2, 2, 0);

    internal static bool IsExpectedInstallerHash(string hash) =>
        string.Equals(hash, ExpectedInstallerSha256, StringComparison.OrdinalIgnoreCase);
}

internal sealed record PawnIoInstallationResult(bool RebootRequired);

internal sealed class PawnIoInstaller
{
    public async Task<PawnIoInstallationResult> InstallAsync(CancellationToken cancellationToken = default)
    {
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "RMAuraIceDisplay-PawnIO",
            Guid.NewGuid().ToString("N"));
        string installerPath = Path.Combine(temporaryDirectory, "PawnIO_setup.exe");

        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RM-Aura-Ice-Display/0.3.3");
            using HttpResponseMessage response = await client.GetAsync(
                PawnIoSupport.InstallerUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (Stream source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var destination = new FileStream(
                installerPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            await using FileStream installer = File.OpenRead(installerPath);
            byte[] hashBytes = await SHA256.HashDataAsync(installer, cancellationToken);
            string hash = Convert.ToHexString(hashBytes);
            if (!PawnIoSupport.IsExpectedInstallerHash(hash))
            {
                throw new InvalidOperationException(
                    "O instalador do PawnIO não corresponde ao arquivo oficial esperado. A instalação foi bloqueada.");
            }

            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("O instalador oficial do PawnIO não pôde ser iniciado.");

            await process.WaitForExitAsync(cancellationToken);
            const int errorSuccessRebootRequired = 3010;
            if (process.ExitCode is not 0 and not errorSuccessRebootRequired)
            {
                throw new InvalidOperationException(
                    $"A instalação do PawnIO terminou com o código {process.ExitCode}.");
            }

            return new PawnIoInstallationResult(process.ExitCode == errorSuccessRebootRequired);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }
}
