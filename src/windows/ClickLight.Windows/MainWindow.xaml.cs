using ClickLight.Windows.Core.Models;
using ClickLight.Windows.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClickLight.Windows;

public sealed class MainWindow : Window
{
    private readonly TextBlock _statusTextBlock;
    private readonly TextBlock _settingsTextBlock;

    public MainWindow()
    {
        Title = "ClickLight for Windows";

        _statusTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        _settingsTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };

        var stack = new StackPanel
        {
            Padding = new Thickness(24),
            Spacing = 12
        };

        stack.Children.Add(new TextBlock
        {
            Text = "ClickLight",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        stack.Children.Add(new TextBlock
        {
            Text = "The app starts in the system tray and uses transparent overlays to highlight clicks.",
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(_statusTextBlock);
        stack.Children.Add(_settingsTextBlock);

        var button = new Microsoft.UI.Xaml.Controls.Button
        {
            Content = "Hide to tray",
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left
        };
        button.Click += HideToTray_Click;
        stack.Children.Add(button);

        Content = new ScrollViewer
        {
            Content = stack
        };
    }

    public void HideToTray()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
    }

    public void ShowFromTray()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Activate();
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNORMAL);
        NativeMethods.SetForegroundWindow(hwnd);
    }

    public void UpdateSnapshot(string captureStatus, ClickSettings settings)
    {
        _statusTextBlock.Text = $"Capture: {captureStatus}";
        _settingsTextBlock.Text = $"Enabled: {settings.IsEnabled} | Press: {settings.ShowPress} | Release: {settings.ShowRelease} | Right Click: {settings.ShowRightClick} | Drag: {settings.ShowDrag} | Size: {settings.Size:0} | Intensity: {settings.Intensity:0.00} | Duration: {settings.Duration:0.00}s";
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }
}
