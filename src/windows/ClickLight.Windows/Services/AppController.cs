using System.Diagnostics;
using ClickLight.Windows.Core.Models;
using ClickLight.Windows.Core.Services;
using ClickLight.Windows.Interop;
using Microsoft.UI.Dispatching;

namespace ClickLight.Windows.Services;

public sealed class AppController : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly SettingsStore _settingsStore;
    private readonly RecentEventFilter _recentEventFilter;
    private readonly OverlayCoordinator _overlayCoordinator;
    private readonly ClickCaptureService _captureService;
    private readonly TrayIconController _trayIconController;
    private readonly UpdateService _updateService;
    private bool _disposed;

    public AppController(MainWindow mainWindow, DispatcherQueue dispatcherQueue)
    {
        _mainWindow = mainWindow;
        _settingsStore = new SettingsStore();
        _recentEventFilter = new RecentEventFilter();
        _overlayCoordinator = new OverlayCoordinator(_settingsStore.Settings);
        _captureService = new ClickCaptureService(dispatcherQueue);
        _updateService = new UpdateService();
        _trayIconController = new TrayIconController(_updateService, update => _settingsStore.Update(update));
    }

    public void Start()
    {
        _settingsStore.SettingsChanged += OnSettingsChanged;
        _captureService.ClickReceived += OnClickReceived;
        _trayIconController.OpenRequested += (_, _) => _mainWindow.ShowFromTray();
        _trayIconController.HideRequested += (_, _) => _mainWindow.HideToTray();
        _trayIconController.TestPulseRequested += (_, _) => ShowTestPulse();
        _trayIconController.QuitRequested += (_, _) => Quit();

        _overlayCoordinator.Start();
        if (_settingsStore.Settings.IsEnabled)
        {
            _captureService.Start();
        }

        _trayIconController.Start(_settingsStore.Settings, _captureService.StatusLabel);
        ApplySnapshot();
        _mainWindow.HideToTray();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _settingsStore.SettingsChanged -= OnSettingsChanged;
        _captureService.ClickReceived -= OnClickReceived;
        _captureService.Dispose();
        _trayIconController.Dispose();
        _overlayCoordinator.Dispose();
    }

    private void OnSettingsChanged(object? sender, ClickSettings settings)
    {
        if (settings.IsEnabled)
        {
            _captureService.Start();
        }
        else
        {
            _captureService.Stop();
        }

        _overlayCoordinator.ApplySettings(settings);
        _trayIconController.Refresh(settings, _captureService.StatusLabel);
        ApplySnapshot();
    }

    private void OnClickReceived(object? sender, ClickEvent clickEvent)
    {
        var settings = _settingsStore.Settings;
        if (!settings.IsEnabled || !ShouldShow(settings, clickEvent.Kind))
        {
            return;
        }

        if (!_recentEventFilter.ShouldAccept(clickEvent))
        {
            return;
        }

        _overlayCoordinator.Show(clickEvent);
    }

    private void ShowTestPulse()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return;
        }

        _overlayCoordinator.Show(new ClickEvent(
            ClickKind.LeftDown,
            point.X,
            point.Y,
            Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency));
    }

    private void ApplySnapshot()
    {
        _mainWindow.UpdateSnapshot(_captureService.StatusLabel, _settingsStore.Settings);
    }

    private void Quit()
    {
        Dispose();
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private static bool ShouldShow(ClickSettings settings, ClickKind kind)
    {
        return kind switch
        {
            ClickKind.LeftDown => settings.ShowPress,
            ClickKind.LeftUp => settings.ShowRelease,
            ClickKind.RightDown or ClickKind.RightUp => settings.ShowRightClick,
            ClickKind.Drag => settings.ShowDrag,
            _ => true
        };
    }
}
