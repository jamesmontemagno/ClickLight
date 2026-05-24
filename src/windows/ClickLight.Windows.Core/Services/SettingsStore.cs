using System.Text.Json;
using System.Text.Json.Serialization;
using ClickLight.Windows.Core.Models;

namespace ClickLight.Windows.Core.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _settingsPath;

    public SettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClickLight",
            "windows-settings.json");

        Settings = Load();
    }

    public event EventHandler<ClickSettings>? SettingsChanged;

    public ClickSettings Settings { get; private set; }

    public void Update(Func<ClickSettings, ClickSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        var updated = mutate(Settings);
        if (updated == Settings)
        {
            return;
        }

        Settings = updated;
        Save();
        SettingsChanged?.Invoke(this, Settings);
    }

    private ClickSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return ClickSettings.Defaults;
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<ClickSettings>(json, SerializerOptions) ?? ClickSettings.Defaults;
        }
        catch
        {
            return ClickSettings.Defaults;
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(Settings, SerializerOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
