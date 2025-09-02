using System.Text.Json;

namespace VCollab.Settings;

public record VCollabSettings(
    SpoutSourceSettings SpoutSourceSettings
) : IDependencyInjectionCandidate
{
    public const string FileName = "vcollab.json";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public static VCollabSettings Load()
    {
        if (File.Exists(FileName))
        {
            var settings = JsonSerializer.Deserialize<VCollabSettings>(
                File.ReadAllText(FileName)
            );

            if (settings is not null)
            {
                return settings;
            }
        }

        var newSettings = new VCollabSettings(new SpoutSourceSettings(string.Empty, 0d, 0d, 1d, 1d));

        newSettings.Save();

        return newSettings;
    }

    public void Save()
    {
        File.WriteAllText(FileName, JsonSerializer.Serialize(this, _serializerOptions));
    }
}