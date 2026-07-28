using System.Diagnostics;

namespace AuraIceLocal;

internal static class WindowsStartupManager
{
    private const string TaskName = "AuraIceLocal";

    public static bool IsEnabled()
    {
        try
        {
            return RunTaskScheduler(["/Query", "/TN", TaskName], allowNotFound: true) == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            string executablePath = Application.ExecutablePath;
            int exitCode = RunTaskScheduler(
                [
                    "/Create",
                    "/TN", TaskName,
                    "/SC", "ONLOGON",
                    "/RL", "HIGHEST",
                    "/TR", $"\"{executablePath}\" --startup",
                    "/F"
                ],
                allowNotFound: false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    "Não foi possível registrar o AuraIceLocal no Agendador de Tarefas do Windows.");
            }

            return;
        }

        int deleteExitCode = RunTaskScheduler(["/Delete", "/TN", TaskName, "/F"], allowNotFound: true);
        if (deleteExitCode != 0 && IsEnabled())
        {
            throw new InvalidOperationException(
                "Não foi possível remover a inicialização automática do AuraIceLocal.");
        }
    }

    private static int RunTaskScheduler(IEnumerable<string> arguments, bool allowNotFound)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("O Agendador de Tarefas do Windows não pôde ser iniciado.");

        if (!process.WaitForExit(10_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("O Agendador de Tarefas do Windows não respondeu em 10 segundos.");
        }

        if (allowNotFound && process.ExitCode == 1)
        {
            return process.ExitCode;
        }

        return process.ExitCode;
    }
}
