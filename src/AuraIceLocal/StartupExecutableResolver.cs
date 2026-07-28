namespace AuraIceLocal;

internal static class StartupExecutableResolver
{
    public static string Resolve(
        string currentExecutable,
        string? installedRoot,
        bool isVelopackInstalled,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        if (!isVelopackInstalled || string.IsNullOrWhiteSpace(installedRoot))
        {
            return currentExecutable;
        }

        string stableLauncher = Path.Combine(installedRoot, Path.GetFileName(currentExecutable));
        return fileExists(stableLauncher) ? stableLauncher : currentExecutable;
    }
}
