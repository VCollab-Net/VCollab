using System.IO.Compression;
using Discord;
using Discord.Webhook;
using NUnit.Framework.Internal;
using osu.Framework.Logging;
using osu.Framework.Platform;
using Logger = osu.Framework.Logging.Logger;

namespace VCollab.Services;

public class LogsSenderService
{
    // This is not a production secret as it can only be used to send messages in a private channel
    private const string WebhookUrl = "https://discord.com/api/webhooks/1421469528176726056/JuTiOEXLoYQCZ_7mCm29Xhktm0jpwFEnTjXPGFmHUcVRdHC9OK4jbuOsd_YzRxkYV8Qy";
    private const string LogsDirectoryName = "logs";
    private const string LogFilesPattern = "*.log";
    private const int UnixTimestampLength = 10;

    private readonly TimeSpan LogsMaximumAge = TimeSpan.FromDays(2);

    private readonly DiscordWebhookClient _discordWebhookClient = new (WebhookUrl);
    private readonly Storage _storage;
    private readonly VCollabSettings _settings;

    private bool _sendingLogs = false;

    public LogsSenderService(Storage storage, VCollabSettings settings)
    {
        _storage = storage;
        _settings = settings;
    }

    public void SendLogs()
    {
        // Only send logs if a send is not already ongoing
        if (Interlocked.CompareExchange(ref _sendingLogs, true, false))
        {
            return;
        }

        // This method always run in the background
        Task.Run(async () =>
        {
            try
            {
                // Fetch latest logs from game storage
                var logsStorage = _storage.GetStorageForDirectory(LogsDirectoryName);

                var now = DateTimeOffset.Now;

                // Make an archive that will be sent with the message
                using var inMemoryFile = new MemoryStream();
                using (var zipArchive = new ZipArchive(inMemoryFile, ZipArchiveMode.Create, leaveOpen: true))
                {
                    // Only keep files that are less than 2 days old
                    foreach (var logFileName in logsStorage.GetFiles(".", LogFilesPattern))
                    {
                        var fileName = Path.GetFileName(logFileName);

                        // Log filename stores the unix timestamp of its creation in the first part of the name (e.g. 1758755751.runtime.log)
                        if (!long.TryParse(fileName.AsSpan()[..UnixTimestampLength], out var timestamp))
                        {
                            continue;
                        }

                        var creationDate = DateTimeOffset.FromUnixTimeSeconds(timestamp);

                        if (now - creationDate > LogsMaximumAge)
                        {
                            continue;
                        }

                        zipArchive.CreateEntryFromFile(logsStorage.GetFullPath(logFileName), logFileName);
                    }
                }

                // Send message with archive to Discord
                inMemoryFile.Seek(0, SeekOrigin.Begin);
                await _discordWebhookClient.SendFileAsync(
                    text: $"Logs received from `{_settings.UserName}`",
                    attachment: new FileAttachment(inMemoryFile, $"logs-{now.ToUnixTimeSeconds()}-{_settings.UserName}.zip")
                );
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error occured while sending logs", LoggingTarget.Network, true);
            }

            _sendingLogs = false;
        });
    }
}