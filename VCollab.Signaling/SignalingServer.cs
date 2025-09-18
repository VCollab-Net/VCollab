using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LiteNetLib;
using VCollab.Signaling.Shared;

namespace VCollab.Signaling;

public class SignalingServer : BackgroundService, INatPunchListener, INetEventListener
{
    private readonly TimeSpan ExpirationTime = TimeSpan.FromSeconds(30);

    private readonly ILogger<SignalingServer> _logger;

    private readonly Dictionary<string, HostPeer> _hosts = new();
    private readonly NetManager _puncher;

    public SignalingServer(ILogger<SignalingServer> logger)
    {
        _logger = logger;

        _puncher = new NetManager(this)
        {
            IPv6Enabled = false,
            NatPunchEnabled = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _puncher.Start(SignalingUtils.ServerPort);
        _puncher.NatPunchModule.Init(this);

        _logger.LogInformation("Signaling server started, waiting for peers on port {Port}...", SignalingUtils.ServerPort);

        // Events poll and host expiration loop
        while (!stoppingToken.IsCancellationRequested)
        {
            // Poll network events
            _puncher.NatPunchModule.PollEvents();
            _puncher.PollEvents();

            // Check room inactivity
            var now = DateTime.Now;
            var toRemove = _hosts
                .Where(host => now - host.Value.RefreshTime > ExpirationTime)
                .ToArray();

            foreach (var hostRoomPair in toRemove)
            {
                _logger.LogInformation("Removing inactive room from '{HostName}'", hostRoomPair.Value.RequestData.Name);

                _hosts.Remove(hostRoomPair.Key);
            }

            await Task.Delay(20, stoppingToken);
        }

        _logger.LogInformation("Stopping signaling server...");

        _puncher.Stop();
    }

    public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
    {
        var requestData = JsonSerializer.Deserialize<NatRequestData>(token, JsonSourceGenerationContext.Default.NatRequestData);

        // Invalid data format
        if (requestData is null)
        {
            _logger.LogWarning("Received invalid data: {Token}", token);

            return;
        }

        if (requestData.IsHost)
        {
            // If no room with this secret exist, create a new one, otherwise refresh time
            if (_hosts.TryGetValue(requestData.RoomSecret, out var hostPeer))
            {
                hostPeer.Refresh();

                _logger.LogInformation("Host refresh: {HostName}", requestData.Name);
            }
            else
            {
                _hosts[requestData.RoomSecret] = new HostPeer(localEndPoint, remoteEndPoint, requestData);

                _logger.LogInformation("Host new room: {HostName}", requestData.Name);
            }
        }
        else
        {
            // If no room exist for this secret it may have expired, there is nothing to do
            if (!_hosts.TryGetValue(requestData.RoomSecret, out var hostPeer))
            {
                _logger.LogInformation("Peer tried to connect to a room that does not exist: {Name}", requestData.Name);

                return;
            }

            // Otherwise it is a connection request, do the nat introduction
            _logger.LogInformation(
                "Introduction: Host ({HostName}) - Client ({ClientName})",
                hostPeer.RequestData.Name,
                requestData.Name
            );

            var introductionData = JsonSerializer.Serialize(
                new NatIntroductionData(hostPeer.RequestData.Name, requestData.Name),
                JsonSourceGenerationContext.Default.NatIntroductionData
            );

            _puncher.NatPunchModule.NatIntroduce(
                hostPeer.InternalAddress,
                hostPeer.ExternalAddress,
                localEndPoint,
                remoteEndPoint,
                introductionData
            );
        }
    }

    public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
    {
        // Nothing to do, this is the signaling server
    }

    public void OnPeerConnected(NetPeer peer)
    {
        // The signaling server does not accept direct connection
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // Nothing to do
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        // Nothing to do
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // Nothing to do
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Nothing to do
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Nothing to do
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Signaling server should not receive direct connection requests
        request.RejectForce();
    }
}