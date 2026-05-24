using ClickLight.Windows.Core.Models;
using ClickLight.Windows.Interop;
using SystemRectangle = System.Drawing.Rectangle;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;
using WinColor = global::Windows.UI.Color;

namespace ClickLight.Windows.Services;

public sealed class OverlayWindow : Window
{
    private readonly Canvas _canvas;
    private readonly RectInt32 _bounds;
    private ClickSettings _settings;

    public OverlayWindow(SystemRectangle bounds, ClickSettings settings)
    {
        _bounds = new RectInt32(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        _settings = settings;
        _canvas = new Canvas
        {
            Width = bounds.Width,
            Height = bounds.Height,
            Background = new SolidColorBrush(Colors.Transparent),
            IsHitTestVisible = false
        };

        Content = _canvas;
        Activate();
        ConfigureWindow();
    }

    public void ApplySettings(ClickSettings settings)
    {
        _settings = settings;
    }

    public void Show(ClickEvent clickEvent)
    {
        var localX = clickEvent.X - _bounds.X;
        var localY = clickEvent.Y - _bounds.Y;
        ShowWindow();

        switch (clickEvent.Kind)
        {
            case ClickKind.LeftDown:
                AddGlow(localX, localY, _settings.Size * 0.55, _settings.Size * 1.15, TimeSpan.FromSeconds(_settings.Duration), CreateColor(0, 189, 255, 55));
                AddRing(localX, localY, _settings.Size * 0.36, _settings.Size * 1.62, TimeSpan.FromSeconds(_settings.Duration), CreateColor(0, 189, 255), _settings.Intensity);
                AddDot(localX, localY, _settings.Size * 0.17, TimeSpan.FromSeconds(_settings.Duration * 0.8), CreateColor(0, 189, 255), 0.6);
                break;
            case ClickKind.LeftUp:
                AddGlow(localX, localY, _settings.Size * 0.72, _settings.Size * 1.05, TimeSpan.FromSeconds(_settings.Duration * 0.78), CreateColor(102, 224, 255, 35));
                AddRing(localX, localY, _settings.Size * 0.85, _settings.Size * 0.52, TimeSpan.FromSeconds(_settings.Duration * 0.78), CreateColor(102, 224, 255), _settings.Intensity * 0.6);
                AddDot(localX, localY, _settings.Size * 0.10, TimeSpan.FromSeconds(_settings.Duration * 0.68), CreateColor(102, 224, 255), 0.35);
                break;
            case ClickKind.RightDown:
                AddGlow(localX, localY, _settings.Size * 0.55, _settings.Size * 1.05, TimeSpan.FromSeconds(_settings.Duration), CreateColor(255, 117, 48, 50));
                AddRing(localX, localY, _settings.Size * 0.36, _settings.Size * 1.45, TimeSpan.FromSeconds(_settings.Duration), CreateColor(255, 117, 48), _settings.Intensity);
                AddCrosshair(localX, localY, _settings.Size * 0.36, TimeSpan.FromSeconds(_settings.Duration * 0.9), CreateColor(255, 117, 48));
                break;
            case ClickKind.RightUp:
                AddGlow(localX, localY, _settings.Size * 0.62, _settings.Size * 0.92, TimeSpan.FromSeconds(_settings.Duration * 0.78), CreateColor(255, 117, 48, 32));
                AddRing(localX, localY, _settings.Size * 0.68, _settings.Size * 0.46, TimeSpan.FromSeconds(_settings.Duration * 0.78), CreateColor(255, 117, 48), _settings.Intensity * 0.55);
                AddCrosshair(localX, localY, _settings.Size * 0.24, TimeSpan.FromSeconds(_settings.Duration * 0.68), CreateColor(255, 117, 48));
                break;
            case ClickKind.Drag:
                AddDot(localX, localY, _settings.Size * 0.12, TimeSpan.FromSeconds(Math.Min(0.38, _settings.Duration * 0.82)), CreateColor(235, 214, 56), 0.55);
                break;
        }
    }

    private void ConfigureWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.MoveAndResize(_bounds);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
        style &= ~NativeMethods.WS_VISIBLE;
        style |= NativeMethods.WS_POPUP;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);

        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
        NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);
    }

    private void ShowWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);
    }

    private void AddRing(double centerX, double centerY, double startDiameter, double endDiameter, TimeSpan duration, WinColor color, double intensity)
    {
        var ring = new Ellipse
        {
            Width = startDiameter,
            Height = startDiameter,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = Math.Max(2, _settings.Size * (0.035 + intensity * 0.045)),
            Opacity = Math.Clamp(0.18 + intensity * 0.52, 0.15, 1)
        };

        AddAnimatedEllipse(ring, centerX, centerY, startDiameter, endDiameter, duration, 1, 0);
    }

    private void AddGlow(double centerX, double centerY, double startDiameter, double endDiameter, TimeSpan duration, WinColor color)
    {
        if (_settings.Intensity < 0.7)
        {
            return;
        }

        var glow = new Ellipse
        {
            Width = startDiameter,
            Height = startDiameter,
            Fill = new SolidColorBrush(color),
            Opacity = Math.Clamp(_settings.Intensity * 0.2, 0.08, 0.25)
        };

        AddAnimatedEllipse(glow, centerX, centerY, startDiameter, endDiameter, duration, glow.Opacity, 0);
    }

    private void AddDot(double centerX, double centerY, double diameter, TimeSpan duration, WinColor color, double opacity)
    {
        var dot = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = new SolidColorBrush(color),
            Opacity = opacity
        };

        AddAnimatedEllipse(dot, centerX, centerY, diameter, diameter * 1.1, duration, opacity, 0);
    }

    private void AddCrosshair(double centerX, double centerY, double size, TimeSpan duration, WinColor color)
    {
        AddLine(centerX - size, centerY, centerX + size, centerY, duration, color);
        AddLine(centerX, centerY - size, centerX, centerY + size, duration, color);
    }

    private void AddLine(double x1, double y1, double x2, double y2, TimeSpan duration, WinColor color)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = Math.Max(2, _settings.Size * 0.09),
            Opacity = 0.85
        };

        _canvas.Children.Add(line);

        var storyboard = new Storyboard();
        var opacityAnimation = CreateAnimation(0.85, 0, duration, line, "Opacity");
        storyboard.Children.Add(opacityAnimation);
        storyboard.Completed += (_, _) => _canvas.Children.Remove(line);
        storyboard.Begin();
    }

    private void AddAnimatedEllipse(Ellipse ellipse, double centerX, double centerY, double startDiameter, double endDiameter, TimeSpan duration, double fromOpacity, double toOpacity)
    {
        Canvas.SetLeft(ellipse, centerX - startDiameter / 2);
        Canvas.SetTop(ellipse, centerY - startDiameter / 2);
        _canvas.Children.Add(ellipse);

        var storyboard = new Storyboard();
        storyboard.Children.Add(CreateAnimation(startDiameter, endDiameter, duration, ellipse, "Width"));
        storyboard.Children.Add(CreateAnimation(startDiameter, endDiameter, duration, ellipse, "Height"));
        storyboard.Children.Add(CreateAnimation(centerX - startDiameter / 2, centerX - endDiameter / 2, duration, ellipse, "(Canvas.Left)"));
        storyboard.Children.Add(CreateAnimation(centerY - startDiameter / 2, centerY - endDiameter / 2, duration, ellipse, "(Canvas.Top)"));
        storyboard.Children.Add(CreateAnimation(fromOpacity, toOpacity, duration, ellipse, "Opacity"));
        storyboard.Completed += (_, _) => _canvas.Children.Remove(ellipse);
        storyboard.Begin();
    }

    private static DoubleAnimation CreateAnimation(double from, double to, TimeSpan duration, DependencyObject target, string property)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration,
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        return animation;
    }

    private static WinColor CreateColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        return WinColor.FromArgb(alpha, red, green, blue);
    }
}
