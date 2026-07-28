using System.Drawing;
using System.Windows.Forms;

namespace AuraIceLocal.Tests;

public sealed class TrayBehaviorTests
{
    [Fact]
    public void UserCloseHidesPanelButExplicitExitClosesApplication()
    {
        Assert.True(TrayWindowBehavior.ShouldHideInsteadOfExit(false, CloseReason.UserClosing));
        Assert.False(TrayWindowBehavior.ShouldHideInsteadOfExit(true, CloseReason.UserClosing));
        Assert.False(TrayWindowBehavior.ShouldHideInsteadOfExit(false, CloseReason.WindowsShutDown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(42)]
    [InlineData(79)]
    [InlineData(80)]
    [InlineData(125)]
    public void DynamicTemperatureIconIsAValidThirtyTwoPixelIcon(int? temperature)
    {
        using Icon icon = TrayTemperatureIcon.CreateIcon(temperature);

        Assert.Equal(32, icon.Width);
        Assert.Equal(32, icon.Height);
        Assert.NotEqual(IntPtr.Zero, icon.Handle);
    }

    [Fact]
    public void FirstWindowUsesSixtyByEightyPercentAndIsCentered()
    {
        Rectangle bounds = WindowPlacement.CreateInitialBounds(
            new Rectangle(0, 0, 1920, 1000),
            new Size(900, 650));

        Assert.Equal(new Rectangle(384, 100, 1152, 800), bounds);
    }

    [Fact]
    public void SavedWindowIsBroughtBackInsideAnAvailableScreen()
    {
        Rectangle bounds = WindowPlacement.RestoreVisibleBounds(
            new Rectangle(3000, 2000, 1200, 800),
            [new Rectangle(0, 0, 1920, 1040)],
            new Rectangle(0, 0, 1920, 1040),
            new Size(900, 650));

        Assert.Equal(new Rectangle(720, 240, 1200, 800), bounds);
    }
}
