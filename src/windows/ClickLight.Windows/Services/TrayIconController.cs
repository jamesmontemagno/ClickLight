using ClickLight.Windows.Core.Models;
using System.Drawing;
using System.Windows.Forms;

namespace ClickLight.Windows.Services;

public sealed class TrayIconController : IDisposable
{
    private readonly UpdateService _updateService;
    private readonly Action<Func<ClickSettings, ClickSettings>> _updateSettings;
    private NotifyIcon? _notifyIcon;

    public TrayIconController(UpdateService updateService, Action<Func<ClickSettings, ClickSettings>> updateSettings)
    {
        _updateService = updateService;
        _updateSettings = updateSettings;
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? HideRequested;
    public event EventHandler? TestPulseRequested;
    public event EventHandler? QuitRequested;

    public void Start(ClickSettings settings, string captureStatus)
    {
        if (_notifyIcon is not null)
        {
            Refresh(settings, captureStatus);
            return;
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = BuildTooltip(settings),
            ContextMenuStrip = BuildMenu(settings, captureStatus)
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Refresh(ClickSettings settings, string captureStatus)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Text = BuildTooltip(settings);
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.ContextMenuStrip = BuildMenu(settings, captureStatus);
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }

    private ContextMenuStrip BuildMenu(ClickSettings settings, string captureStatus)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(CreateToggleItem("Enabled", settings.IsEnabled, value => value with { IsEnabled = !value.IsEnabled }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateToggleItem("Show Press", settings.ShowPress, value => value with { ShowPress = !value.ShowPress }));
        menu.Items.Add(CreateToggleItem("Show Release", settings.ShowRelease, value => value with { ShowRelease = !value.ShowRelease }));
        menu.Items.Add(CreateToggleItem("Show Right Click", settings.ShowRightClick, value => value with { ShowRightClick = !value.ShowRightClick }));
        menu.Items.Add(CreateToggleItem("Show Drag", settings.ShowDrag, value => value with { ShowDrag = !value.ShowDrag }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateToggleItem("Verbose Tray Tooltip", settings.ShowMenuBarText, value => value with { ShowMenuBarText = !value.ShowMenuBarText }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateOptionsMenu("Size", settings.Size, [
            ("Small", 44d),
            ("Medium", 64d),
            ("Large", 88d),
            ("Huge", 116d)
        ]));
        ReplaceOptionsHandler((ToolStripMenuItem)menu.Items[^1], option => _updateSettings(value => value with { Size = option }));

        menu.Items.Add(CreateOptionsMenu("Intensity", settings.Intensity, [
            ("Subtle", 0.28d),
            ("Normal", 0.7d),
            ("Bright", 1.0d),
            ("Beacon", 1.35d)
        ]));
        ReplaceOptionsHandler((ToolStripMenuItem)menu.Items[^1], option => _updateSettings(value => value with { Intensity = option }));

        menu.Items.Add(CreateOptionsMenu("Duration", settings.Duration, [
            ("Snappy", 0.28d),
            ("Normal", 0.48d),
            ("Slow", 0.72d),
            ("Very Slow", 1.0d)
        ]));
        ReplaceOptionsHandler((ToolStripMenuItem)menu.Items[^1], option => _updateSettings(value => value with { Duration = option }));

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem($"Click Capture: {captureStatus}") { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem("Test Pulse at Pointer", null, (_, _) => TestPulseRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Permissions: Not Required", null, (_, _) => { }) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem(_updateService.IsConfigured ? "Check for Updates..." : "Updates: Not Configured", null, (_, _) => _updateService.CheckForUpdates())
        {
            Enabled = _updateService.IsConfigured
        });
        menu.Items.Add(new ToolStripMenuItem("Open ClickLight", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new ToolStripMenuItem("Hide Window", null, (_, _) => HideRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new ToolStripMenuItem("Quit ClickLight", null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty)));
        return menu;
    }

    private ToolStripMenuItem CreateToggleItem(string title, bool isChecked, Func<ClickSettings, ClickSettings> updater)
    {
        var item = new ToolStripMenuItem(title) { Checked = isChecked, CheckOnClick = false };
        item.Click += (_, _) => _updateSettings(updater);
        return item;
    }

    private static ToolStripMenuItem CreateOptionsMenu(string title, double selectedValue, (string Label, double Value)[] options)
    {
        var menu = new ToolStripMenuItem(title);
        foreach (var option in options)
        {
            var child = new ToolStripMenuItem(option.Label)
            {
                Checked = Math.Abs(selectedValue - option.Value) < 0.01
            };
            child.Tag = option.Value;
            menu.DropDownItems.Add(child);
        }

        return menu;
    }

    private void ReplaceOptionsHandler(ToolStripMenuItem menu, Action<double> onSelect)
    {
        foreach (ToolStripMenuItem item in menu.DropDownItems)
        {
            item.Click += (_, _) => onSelect((double)item.Tag!);
        }
    }

    private string BuildTooltip(ClickSettings settings)
    {
        return settings.ShowMenuBarText
            ? $"ClickLight • {(settings.IsEnabled ? "Enabled" : "Paused")}"
            : "ClickLight";
    }
}
