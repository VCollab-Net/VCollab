using System.Text.Json;
using LiteNetLib;
using MemoryPack;
using osu.Framework.Logging;
using VCollab.Networking.Information;
using VCollab.Signaling.Shared;
using VCollab.Utils;

namespace VCollab.Networking;

public class PeerNetworkClient : NetworkClient
{
    protected override byte? DataChannelOffset => _channelOffset;

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

        NetworkMetricsDrawable.MembersCount++;
    }

    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // TODO Handle host disconnect, this should leave room and allow join/creating a new one
        Logger.Log($"Disconnected from host '{PeerStates[HostChannelOffset]?.Name}' ({disconnectInfo.Reason})", LoggingTarget.Network);

        // Clear peer states when host disconnects
        for (var i = 0; i < PeerStates.Length; i++)
        {
            PeerStates[i]?.Dispose();
            PeerStates[i] = null;
        }

        _hostPeer = null;
        _channelOffset = null;

        NetworkMetricsDrawable.MembersCount--;
    }

    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        base.OnNetworkLatencyUpdate(peer, latency);

        // TODO Update latency
        Logger.Log($"Latency to host ({PeerStates[HostChannelOffset]?.Name}): {latency}", LoggingTarget.Network);
    }

    protected override void SendRawData(ReadOnlySpan<byte> data, DeliveryMethod deliveryMethod)
    {
        _hostPeer?.Send(data, deliveryMethod);
    }

    protected override void HandleInformationMessage(NetPeer _, IInformationMessage message)
    {
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

                NetworkMetricsDrawable.MembersCount++;

                break;

            case StateInitializationMessage stateInitializationMessage:

                _channelOffset = stateInitializationMessage.ChannelOffset;
                foreach (var peerInfo in stateInitializationMessage.PeerInfos.Where(peerInfo => peerInfo.ChannelOffset != _channelOffset))
                {
                    PeerStates[peerInfo.ChannelOffset]?.Dispose();
                    PeerStates[peerInfo.ChannelOffset] = NewPeerState(peerInfo.ChannelOffset, peerInfo.Name);
                }

                NetworkMetricsDrawable.MembersCount = stateInitializationMessage.PeerInfos.Length + 1; // + host

                break;

            case DisconnectedPeerMessage disconnectedPeerMessage:

                PeerStates[disconnectedPeerMessage.ChannelOffset]?.Dispose();
                PeerStates[disconnectedPeerMessage.ChannelOffset] = null;

                NetworkMetricsDrawable.MembersCount--;

                break;

            default:
                Logger.Log($"Cannot handle received information message of type {message.GetType()}", LoggingTarget.Network, LogLevel.Important);

                break;
        }
    }

    public void ConnectToRoom(string name, string roomToken)
    {
        var requestData = new NatRequestData(false, name, roomToken);

        NetManager.NatPunchModule.SendNatIntroduceRequest(
            SignalingUtils.ServerUrl,
            SignalingUtils.ServerPort,
            JsonSerializer.Serialize(requestData, JsonSourceGenerationContext.Default.NatRequestData)
        );
    }
}