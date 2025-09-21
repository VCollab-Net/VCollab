using System.Text.Json;
using System.Text.Json.Serialization;
using osu.Framework.Platform;
using VCollab.Utils;

namespace VCollab.Settings;

public record VCollabSettings : IDependencyInjectionCandidate
{
    public const string FileName = "vcollab.json";

    // Used to invalidate old settings
    public int SettingsVersion { get; } = 1;

    public required SpoutSourceSettings? SpoutSourceSettings { get; set; }
    public UserModelSettings UserModelSettings { get; set; } = new();

    private Storage _storage = null!;

    [JsonConstructor]
    public VCollabSettings()
    {

    }

    public static VCollabSettings Load(Storage storage)
    {
        if (storage.Exists(FileName))
        {
            var settingsPath = storage.GetFullPath(FileName, true);

            var settings = JsonSerializer.Deserialize<VCollabSettings>(
                File.ReadAllText(settingsPath),
                JsonSourceGenerationContext.Default.VCollabSettings
            );

            if (settings is not null)
            {
                settings._storage = storage;

                return settings;
            }
        }

        var newSettings = new VCollabSettings
        {
            SpoutSourceSettings = null,
            _storage = storage
        };

        newSettings.Save();

        return newSettings;
    }

    public void Save()
    {
        using var saveStream = _storage.CreateFileSafely(FileName);

        JsonSerializer.Serialize(saveStream, this, JsonSourceGenerationContext.Default.VCollabSettings);
    }
}