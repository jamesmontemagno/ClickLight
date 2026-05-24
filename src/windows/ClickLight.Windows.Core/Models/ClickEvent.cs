namespace ClickLight.Windows.Core.Models;

public readonly record struct ClickEvent(
    ClickKind Kind,
    double X,
    double Y,
    double Timestamp);
