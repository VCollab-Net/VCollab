using System.Timers;
using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using Humanizer;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using VCollab.Networking;
using Timer = System.Timers.Timer;
using LogLevel = osu.Framework.Logging.LogLevel;

namespace VCollab.Services;

public sealed class DiscordRpcService : IDisposable
{
    public event Action? RoomJoined;

    private const double UpdateInterval = 2000;
    private const string ApplicationId = "1420441963899129886";

    private readonly NetworkManager _networkManager;
    private readonly VCollabSettings _vcollabSettings;

    private Timer? _timer;
    private DiscordRpcClient? _client;

    public DiscordRpcService(NetworkManager networkManager, VCollabSettings vcollabSettings)
    {
        _networkManager = networkManager;
        _vcollabSettings = vcollabSettings;

        Task.Run(() =>
        {
            // Setup discord rpc client
            _client = new DiscordRpcClient(ApplicationId);

            //This will alert us if discord wants to join a game
            _client.RegisterUriScheme();
            _client.SetSubscription(EventType.Join);

            // Set the logger
            _client.Logger = new OsuFrameworkLogger { Level = DiscordRPC.Logging.LogLevel.Info };

            // Subscribe to events
            _client.OnReady += OnReady;
            _client.OnJoin += OnJoin;

            // We do not handle join requests, let discord UI do it
            // _client.OnJoinRequested += OnJoinRequested;

            // Connect to the RPC
            _client.Initialize();

            // Set presence update on a timer
            _timer = new Timer(UpdateInterval);
            _timer.Elapsed += UpdateRichPresence;

            _timer.Start();
        });
    }

    private void OnJoin(object sender, JoinMessage message)
    {
        Logger.Log($"Joining room using Discord rich presence: {message.Secret}", LoggingTarget.Network, LogLevel.Important);

        var roomToken = message.Secret;

        if (_networkManager.NetworkState is NetworkState.Unconnected && _networkManager.StartAsPeer(_vcollabSettings.UserName, roomToken))
        {
            RoomJoined?.Invoke();
        }
    }

    private static void OnReady(object sender, ReadyMessage message)
    {
        Logger.Log($"Discord RPC ready from user {message.User.Username}", LoggingTarget.Network);
    }

    private void UpdateRichPresence(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        // No room has been created yet
        var peersCount = _networkManager.ConnectedPeersCount;
        var roomToken = _networkManager.RoomToken;

        var presence = _networkManager.NetworkState switch
        {
            NetworkState.Hosting => new RichPresence
            {
                Details = "Hosting a collaboration",
                Party = new Party
                {
                    ID = roomToken.ComputeSHA2Hash(),
                    Max = 25,
                    Privacy = Party.PrivacySetting.Private,
                    Size = 1 + peersCount
                },
                Secrets = new Secrets
                {
                    Join = roomToken
                }
            },

            NetworkState.Connected => new RichPresence
            {
                Details = "Participating in a collaboration",
                Party = new Party
                {
                    ID = roomToken.ComputeSHA2Hash(),
                    Max = 25,
                    Privacy = Party.PrivacySetting.Private,
                    Size = 1 + peersCount
                },
                Secrets = new Secrets
                {
                    Join = roomToken
                }
            },

            _ => new RichPresence
            {
                Details = "Idle",
                Party = null,
                Secrets = null
            }
        };

        _client?.SetPresence(presence);
    }

    public void Dispose()
    {
        _timer?.Dispose();

        _client?.ClearPresence();
        _client?.Dispose();
    }

    private class OsuFrameworkLogger : ILogger
    {
        public void Trace(string message, params object[] args)
        {
            if (Level <= DiscordRPC.Logging.LogLevel.Trace)
            {
                Logger.Log(message.FormatWith(args), LoggingTarget.Network, LogLevel.Debug);
            }
        }

        public void Info(string message, params object[] args)
        {
            if (Level <= DiscordRPC.Logging.LogLevel.Info)
            {
                Logger.Log(message.FormatWith(args), LoggingTarget.Network);
            }
        }

        public void Warning(string message, params object[] args)
        {
            if (Level <= DiscordRPC.Logging.LogLevel.Warning)
            {
                Logger.Log(message.FormatWith(args), LoggingTarget.Network, LogLevel.Important);
            }
        }

        public void Error(string message, params object[] args)
        {
            if (Level <= DiscordRPC.Logging.LogLevel.Error)
            {
                Logger.Log(message.FormatWith(args), LoggingTarget.Network, LogLevel.Error);
            }
        }

        public DiscordRPC.Logging.LogLevel Level { get; set; }
    }
}