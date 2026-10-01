using System.Text.Json;
using System.Text.Json.Serialization;
using VideoDownloader.Models;

namespace VideoDownloader.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public SettingsService(string folder)
    {
        _path = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options);
                if (settings is not null && settings.OutputFolder.Length > 0)
                    return settings;
            }
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }

        return AppSettings.CreateDefault();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
