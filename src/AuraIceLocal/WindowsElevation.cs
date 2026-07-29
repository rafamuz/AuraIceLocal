using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace AuraIceLocal;

internal static class WindowsElevation
{
    public static bool EnsureAdministrator(string[] arguments)
    {
        if (IsAdministrator())
        {
            return true;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = BuildArgumentString(arguments),
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            MessageBox.Show(
                "O acesso administrativo foi cancelado. Sem ele, o Windows enumera os sensores da CPU, mas não libera as temperaturas. O aplicativo não foi iniciado e nenhum dado foi enviado ao visor.",
                "RM Aura Ice Display",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível iniciar o RM Aura Ice Display como administrador: {ex.Message}\n\nNenhum dado foi enviado ao visor.",
                "RM Aura Ice Display",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        return false;
    }

    internal static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    internal static string BuildArgumentString(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(QuoteArgument));

    private static string QuoteArgument(string argument)
    {
        if (argument.Length > 0 && !argument.Any(character => char.IsWhiteSpace(character) || character == '"'))
        {
            return argument;
        }

        return $"\"{argument.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}
