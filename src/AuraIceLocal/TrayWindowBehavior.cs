namespace AuraIceLocal;

internal static class TrayWindowBehavior
{
    public static bool ShouldHideInsteadOfExit(bool exitRequested, CloseReason closeReason) =>
        !exitRequested && closeReason == CloseReason.UserClosing;
}
