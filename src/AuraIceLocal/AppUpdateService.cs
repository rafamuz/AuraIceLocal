using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace AuraIceLocal;

internal sealed class AppUpdateService
{
    public const string RepositoryUrl = "https://github.com/rafamuz/AuraIceLocal";

    public string CurrentVersion =>
        VelopackLocator.Current.CurrentlyInstalledVersion?.ToString()
        ?? Application.ProductVersion;

    public bool IsInstalled =>
        VelopackLocator.Current.CurrentlyInstalledVersion is not null &&
        !VelopackLocator.Current.IsPortable;

    public VelopackAsset? PendingUpdate => CreateManager().UpdatePendingRestart;

    public Task<UpdateInfo?> CheckForUpdatesAsync() => CreateManager().CheckForUpdatesAsync();

    public Task DownloadUpdatesAsync(
        UpdateInfo update,
        Action<int> progress,
        CancellationToken cancellationToken = default) =>
        CreateManager().DownloadUpdatesAsync(update, progress, cancellationToken);

    public void ApplyUpdatesAndRestart(UpdateInfo update) =>
        CreateManager().ApplyUpdatesAndRestart(update.TargetFullRelease);

    public void ApplyUpdatesAndRestart(VelopackAsset update) =>
        CreateManager().ApplyUpdatesAndRestart(update);

    private static UpdateManager CreateManager() => new(
        new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
}
