using System.Threading;

namespace AuraIceLocal;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    private static void Main(string[] args)
    {
        const string mutexName = @"Local\AuraIceLocal_5C12B3A1";
        _singleInstanceMutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "O AuraIceLocal já está em execução.",
                "AuraIceLocal",
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
