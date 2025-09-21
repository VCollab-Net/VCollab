using System.Text.Json;
using LiteNetLib;
using MemoryPack;
using osu.Framework.Logging;
using VCollab.Networking.Information;
using VCollab.Signaling.Shared;

namespace VCollab.Networking;

public class PeerNetworkClient : NetworkClient
{
    private NetPeer? _hostPeer = null;
    private byte? _channelOffset = null;

    public PeerNetworkClient(NetworkManager networkManager) : base(networkManager)
    {

    }

    protected override void NatIntroduction(NatIntroductionData natIntroductionData)
    {
        PeerStates[HostChannelOffset]?.Dispose();
        PeerStates[HostChannelOffset] = NewPeerState(HostChannelOffset, natIntroductionData.HostName);
    }

    public override void OnPeerConnected(NetPeer peer)
    {
        Logger.Log("OnPeerConnected", LoggingTarget.Network);

        // Set host and send first message which should contain our name
        // The host will answer with a state initialization message so we can start sending data
        _hostPeer = peer;

        var message = MemoryPackSerializer.Serialize<IInformationMessage>(new PeerConnectionMessage(NetworkManager.UserName));

        peer.Send(message, InformationMessagesChannel, InformationMessagesDeliveryMethod);

        Logger.Log("Sent peer connection message to host", LoggingTarget.Network);
    }

    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // TODO Handle host disconnect, this should leave room and allow join/creating a new one
        Logger.Log($"Disconnected from host '{PeerStates[HostChannelOffset]?.Name}' ({disconnectInfo.Reason})", LoggingTarget.Network);

        PeerStates[HostChannelOffset]?.Dispose();
        PeerStates[HostChannelOffset] = null;
        _hostPeer = null;
        _channelOffset = null;
    }

    public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (channelNumber <= ReservedChannelsEnd)
        {
            if (channelNumber != InformationMessagesChannel)
            {
                Logger.Log($"Received message on invalid channel for client: {channelNumber}");

                return;
            }

            HandleInformationMessage(reader);

            return;
        }

        // This is model data
        ReceiveModelData(reader.GetRemainingBytesSpan(), channelNumber);
    }

    private void HandleInformationMessage(NetPacketReader reader)
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
            case ErrorMessage errorMessage:

                // TODO Handle case where we cannot join a room. This is similar to host disconnecting
                if (errorMessage.ErrorCode == ErrorCode.RoomFull)
                {
                    Logger.Log("Room is full, cannot join", LoggingTarget.Network, LogLevel.Error);

                    NetworkManager.GameHost.Exit();
                }

                break;

            case NewPeerMessage newPeerMessage:

                PeerStates[newPeerMessage.ChannelOffset]?.Dispose();
                PeerStates[newPeerMessage.ChannelOffset] = NewPeerState(newPeerMessage.ChannelOffset, newPeerMessage.Name);

                break;

            case StateInitializationMessage stateInitializationMessage:

                _channelOffset = stateInitializationMessage.ChannelOffset;
                foreach (var peerInfo in stateInitializationMessage.PeerInfos.Where(peerInfo => peerInfo.ChannelOffset != _channelOffset))
                {
                    PeerStates[peerInfo.ChannelOffset]?.Dispose();
                    PeerStates[peerInfo.ChannelOffset] = NewPeerState(peerInfo.ChannelOffset, peerInfo.Name);
                }

                break;

            case DisconnectedPeerMessage disconnectedPeerMessage:

                PeerStates[disconnectedPeerMessage.ChannelOffset]?.Dispose();
                PeerStates[disconnectedPeerMessage.ChannelOffset] = null;

                break;

            default:
                Logger.Log($"Cannot handle received information message of type {message.GetType()}", LoggingTarget.Network, LogLevel.Important);

                break;
        }
    }

    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // TODO Update latency
        Logger.Log($"Latency to host: {latency}", LoggingTarget.Network);
    }

    public void ConnectToRoom(string name, string roomToken)
    {
        var requestData = new NatRequestData(false, name, roomToken);

        NetManager.NatPunchModule.SendNatIntroduceRequest(
            SignalingUtils.ServerUrl,
            SignalingUtils.ServerPort,
            JsonSerializer.Serialize(requestData)
        );
    }

    protected override void SendModelDataCore(
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData,
        ReadOnlySpan<byte> frameInformationData,
        int frameCount
    )
    {
        if (_hostPeer is not null && _channelOffset is { } channelOffset)
        {
            SendModelDataToPeer(
                textureData,
                alphaData,
                frameInformationData,
                frameCount,
                _hostPeer,
                channelOffset
            );
        }
    }
}