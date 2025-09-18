using System.Text.Json;
using LiteNetLib;
using MemoryPack;
using osu.Framework.Logging;
using VCollab.Networking.Information;
using VCollab.Signaling.Shared;

namespace VCollab.Networking;

public class HostNetworkClient : NetworkClient
{
    private readonly CancellationTokenSource _natLoopCancellationTokenSource;
    private readonly List<NetPeer> _peers = [];
    private readonly Dictionary<int, int> _peerIdToChannelOffset = [];

    public HostNetworkClient(NetworkManager networkManager) : base(networkManager)
    {
        _natLoopCancellationTokenSource = new CancellationTokenSource();

        Task.Run(NatHostLoop);
    }

    private async Task NatHostLoop()
    {
        // Periodically send requests to the signaling server to keep the room alive
        while (!_natLoopCancellationTokenSource.IsCancellationRequested)
        {
            var requestData = new NatRequestData(true, NetworkManager.UserName, NetworkManager.RoomToken);

            NetManager.NatPunchModule.SendNatIntroduceRequest(
                SignalingUtils.ServerUrl,
                SignalingUtils.ServerPort,
                JsonSerializer.Serialize(requestData)
            );

            Logger.Log($"Sent nat introduce request from host with name '{requestData.Name}'", LoggingTarget.Network);

            await Task.Delay(10_000);
        }
    }

    public override void OnPeerConnected(NetPeer peer)
    {
        Logger.Log($"OnPeerConnected ({peer.Id}), previously connected peers: {_peers.Count}", LoggingTarget.Network);
    }

    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_peerIdToChannelOffset.TryGetValue(peer.Id, out var channelOffset))
        {
            SendInformationMessageToAllExcept(peer, new DisconnectedPeerMessage(channelOffset));

            PeerStates[channelOffset]?.Dispose();
            PeerStates[channelOffset] = null;
            _peers.Remove(peer);
        }

        Logger.Log($"OnPeerDisconnected ({peer.Id} - {disconnectInfo.Reason}), remaining connected peers: {_peers.Count}", LoggingTarget.Network);
    }

    public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (channelNumber <= ReservedChannelsEnd)
        {
            if (channelNumber != InformationMessagesChannel)
            {
                Logger.Log($"Received message on invalid channel for host: {channelNumber}", LoggingTarget.Network);

                return;
            }

            HandleInformationMessage(peer, reader);

            return;
        }

        // This is model data
        var data = reader.GetRemainingBytesSpan();

        ReceiveModelData(data, channelNumber);

        // Forward to other peers except to the one that sent this data
        for (var i = 0; i < _peers.Count; i++)
        {
            var otherPeer = _peers[i];

            if (otherPeer.Id != peer.Id)
            {
                otherPeer.Send(data, channelNumber, deliveryMethod);
            }
        }
    }

    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // TODO Update latency
        // Logger.Log($"Latency to peer ({peer.Id}): {latency}", LoggingTarget.Network, LogLevel.Debug);
    }

    private void HandleInformationMessage(NetPeer peer, NetPacketReader reader)
    {
        var message = MemoryPackSerializer.Deserialize<IInformationMessage>(reader.GetRemainingBytesSpan());

        if (message is null)
        {
            Logger.Log("Received invalid/corrupted information message", LoggingTarget.Network, LogLevel.Important);

            return;
        }

        Logger.Log($"Received information message: {message}", LoggingTarget.Network, LogLevel.Debug);

        switch (message)
        {
            case PeerConnectionMessage peerConnectionMessage:

                HandlePeerConnection(peer, peerConnectionMessage.Name);

                break;

            default:

                Logger.Log($"Cannot handle received information message of type {message.GetType()}", LoggingTarget.Network, LogLevel.Important);

                break;
        }
    }

    private void HandlePeerConnection(NetPeer peer, string peerName)
    {
        // Determine new peer channel offset
        var peerChannelOffset = FindFirstAvailableChannelOffset();

        // The room is full, warn the new peer of this and then disconnect
        if (peerChannelOffset == -1)
        {
            SendInformationMessage(peer, new ErrorMessage(ErrorCode.RoomFull));

            Logger.Log($"Peer couldn't connect because room is full '{peerName}'", LoggingTarget.Network, LogLevel.Important);

            return;
        }

        Logger.Log($"New Peer connected on channel {peerChannelOffset} with name '{peerName}'");

        PeerStates[peerChannelOffset]?.Dispose();
        PeerStates[peerChannelOffset] = NewPeerState(peerChannelOffset, peerName);
        _peers.Add(peer);
        _peerIdToChannelOffset[peer.Id] = peerChannelOffset;

        // Initialize state for the new peer
        SendInformationMessage(peer, new StateInitializationMessage(peerChannelOffset, PeerStates
            .OfType<PeerState>()
            .Select(state => new PeerInfo(state.ChannelOffset, state.Name))
            .ToArray()
        ));

        // Notify other peers that a new peer connected to maintain state
        SendInformationMessageToAllExcept(peer, new NewPeerMessage(peerChannelOffset, peerName));

        Logger.Log($"Peer connected ({peer.Id} -> {peerChannelOffset}): '{peerName}'", LoggingTarget.Network, LogLevel.Important);

        return;

        int FindFirstAvailableChannelOffset()
        {
            // Channel number 0 is reserved by the host
            for (var channel = 1; channel < PeerStates.Length; channel++)
            {
                if (PeerStates[channel] is null)
                {
                    return channel;
                }
            }

            return -1;
        }
    }

    private void SendInformationMessage(NetPeer peer, IInformationMessage message)
    {
        var data = MemoryPackSerializer.Serialize(message);

        peer.Send(data, InformationMessagesChannel, InformationMessagesDeliveryMethod);
    }

    private void SendInformationMessageToAllExcept(NetPeer peerToExclude, IInformationMessage message)
    {
        var data = MemoryPackSerializer.Serialize(message);

        for (var i = 0; i < _peers.Count; i++)
        {
            var peer = _peers[i];

            if (peer.Id != peerToExclude.Id)
            {
                peer.Send(data, InformationMessagesChannel, InformationMessagesDeliveryMethod);
            }
        }
    }

    protected override void SendModelDataCore(
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData,
        ReadOnlySpan<byte> frameInformationData,
        int frameCount
    )
    {
        SendModelDataToPeers(
            textureData,
            alphaData,
            frameInformationData,
            frameCount,
            _peers,
            HostChannelOffset
        );
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _natLoopCancellationTokenSource.Cancel();
        }

        base.Dispose(disposing);
    }
}