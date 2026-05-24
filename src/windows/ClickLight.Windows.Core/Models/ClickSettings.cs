namespace ClickLight.Windows.Core.Models;

public sealed record ClickSettings(
    bool IsEnabled,
    bool ShowPress,
    bool ShowRelease,
    bool ShowRightClick,
    bool ShowDrag,
    bool ShowMenuBarText,
    double Size,
    double Intensity,
    double Duration)
{
    public static ClickSettings Defaults { get; } = new(
        IsEnabled: true,
        ShowPress: true,
        ShowRelease: true,
        ShowRightClick: true,
        ShowDrag: true,
        ShowMenuBarText: true,
        Size: 64,
        Intensity: 0.9,
        Duration: 0.48);
}
