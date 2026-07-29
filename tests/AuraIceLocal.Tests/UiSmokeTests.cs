namespace AuraIceLocal.Tests;

public sealed class UiSmokeTests
{
    [Fact]
    public void HelpWindowCanBeConstructedAtMinimumSize()
    {
        Exception? captured = null;
        using var completed = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new HelpForm();
                form.ClientSize = form.MinimumSize;
                form.CreateControl();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                completed.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(10)), "A janela de Ajuda não terminou de inicializar.");
        thread.Join();
        Assert.Null(captured);
    }

    [Fact]
    public void EmbeddedApplicationIconsAreReadable()
    {
        using Bitmap bitmap = AppVisualAssets.CreateApplicationBitmap();
        using Icon icon = AppVisualAssets.CreateApplicationIcon();

        Assert.True(bitmap.Width >= 256);
        Assert.True(bitmap.Height >= 256);
        Assert.True(icon.Width >= 16);
        Assert.True(icon.Height >= 16);
    }
}
