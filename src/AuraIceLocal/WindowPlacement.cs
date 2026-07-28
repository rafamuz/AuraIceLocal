namespace AuraIceLocal;

internal static class WindowPlacement
{
    public const double InitialWidthRatio = 0.60;
    public const double InitialHeightRatio = 0.80;

    public static Rectangle CreateInitialBounds(Rectangle workingArea, Size minimumSize)
    {
        int width = FitDimension(
            (int)Math.Round(workingArea.Width * InitialWidthRatio, MidpointRounding.AwayFromZero),
            minimumSize.Width,
            workingArea.Width);
        int height = FitDimension(
            (int)Math.Round(workingArea.Height * InitialHeightRatio, MidpointRounding.AwayFromZero),
            minimumSize.Height,
            workingArea.Height);

        return new Rectangle(
            workingArea.Left + ((workingArea.Width - width) / 2),
            workingArea.Top + ((workingArea.Height - height) / 2),
            width,
            height);
    }

    public static Rectangle RestoreVisibleBounds(
        Rectangle savedBounds,
        IReadOnlyList<Rectangle> workingAreas,
        Rectangle primaryWorkingArea,
        Size minimumSize)
    {
        Rectangle target = workingAreas
            .Select(area => new { Area = area, Intersection = Rectangle.Intersect(area, savedBounds) })
            .OrderByDescending(item => (long)item.Intersection.Width * item.Intersection.Height)
            .FirstOrDefault(item => item.Intersection.Width > 0 && item.Intersection.Height > 0)
            ?.Area ?? primaryWorkingArea;

        int width = FitDimension(savedBounds.Width, minimumSize.Width, target.Width);
        int height = FitDimension(savedBounds.Height, minimumSize.Height, target.Height);
        int x = Math.Clamp(savedBounds.X, target.Left, target.Right - width);
        int y = Math.Clamp(savedBounds.Y, target.Top, target.Bottom - height);

        return new Rectangle(x, y, width, height);
    }

    private static int FitDimension(int requested, int minimum, int available)
    {
        int effectiveMinimum = Math.Min(minimum, available);
        return Math.Clamp(requested, effectiveMinimum, available);
    }
}
