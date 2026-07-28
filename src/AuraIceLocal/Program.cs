using System.Threading;
using Velopack;

namespace AuraIceLocal;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();

        const string mutexName = @"Local\AuraIceLocal_5C12B3A1";
        _singleInstanceMutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "O RM Aura Ice Display já está em execução.",
                "RM Aura Ice Display",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        bool startedWithWindows = args.Any(argument =>
            string.Equals(argument, "--startup", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(startedWithWindows));

        _singleInstanceMutex.ReleaseMutex();
        _singleInstanceMutex.Dispose();
    }
}
