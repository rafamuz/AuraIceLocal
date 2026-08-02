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

    [Fact]
    public void HelpWindowUsesMostOfTheAvailableScreen()
    {
        Rectangle bounds = HelpWindowLayout.CreateInitialBounds(
            new Rectangle(0, 0, 1920, 1000),
            new Size(900, 620));

        Assert.Equal(new Rectangle(173, 70, 1574, 860), bounds);
    }

    [Theory]
    [InlineData(1200, 340)]
    [InlineData(900, 340)]
    [InlineData(700, 272)]
    public void HelpNavigationStartsAtSafeResizableWidth(int clientWidth, int expected)
    {
        int? distance = HelpWindowLayout.GetInitialSplitterDistance(
            clientWidth,
            desiredDistance: 340,
            panel1Minimum: 220,
            panel2Minimum: 420,
            splitterWidth: 8);

        Assert.Equal(expected, distance);
    }

    [Fact]
    public void HelpNavigationWaitsUntilBothPanelsFit()
    {
        Assert.Null(HelpWindowLayout.GetInitialSplitterDistance(
            clientWidth: 600,
            desiredDistance: 340,
            panel1Minimum: 220,
            panel2Minimum: 420,
            splitterWidth: 8));
    }
}
