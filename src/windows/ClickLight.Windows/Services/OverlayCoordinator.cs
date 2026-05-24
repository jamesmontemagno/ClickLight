using ClickLight.Windows.Core.Models;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ClickLight.Windows.Services;

public sealed class OverlayCoordinator : IDisposable
{
    private readonly Dictionary<string, OverlayWindow> _overlaysByScreenId = new();
    private ClickSettings _settings;

    public OverlayCoordinator(ClickSettings settings)
    {
        _settings = settings;
    }

    public void Start()
    {
        RebuildOverlays();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public void ApplySettings(ClickSettings settings)
    {
        _settings = settings;
        foreach (var overlay in _overlaysByScreenId.Values)
        {
            overlay.ApplySettings(settings);
        }
    }

    public void Show(ClickEvent clickEvent)
    {
        var screen = Screen.AllScreens.FirstOrDefault(candidate => candidate.Bounds.Contains((int)clickEvent.X, (int)clickEvent.Y))
            ?? Screen.PrimaryScreen;

        if (screen is null)
        {
            return;
        }

        if (!_overlaysByScreenId.TryGetValue(screen.DeviceName, out var overlay))
        {
            RebuildOverlays();
            if (!_overlaysByScreenId.TryGetValue(screen.DeviceName, out overlay))
            {
                return;
            }
        }

        overlay.Show(clickEvent);
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        foreach (var overlay in _overlaysByScreenId.Values)
        {
            overlay.Close();
        }

        _overlaysByScreenId.Clear();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        RebuildOverlays();
    }

    private void RebuildOverlays()
    {
        foreach (var overlay in _overlaysByScreenId.Values)
        {
            overlay.Close();
        }

        _overlaysByScreenId.Clear();
        foreach (var screen in Screen.AllScreens)
        {
            _overlaysByScreenId[screen.DeviceName] = new OverlayWindow(screen.Bounds, _settings);
        }
    }
}
