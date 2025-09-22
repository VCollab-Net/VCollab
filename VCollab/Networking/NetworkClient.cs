using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LiteNetLib;
using MemoryPack;
using osu.Framework.Logging;
using VCollab.Networking.Information;
using VCollab.Signaling.Shared;
using VCollab.Utils;
using VCollab.Utils.Graphics;

namespace VCollab.Networking;

public abstract class NetworkClient : INetEventListener, INatPunchListener, IDisposable
{
    protected const byte InformationMessagesChannel = 0;
    protected const DeliveryMethod InformationMessagesDeliveryMethod = DeliveryMethod.ReliableOrdered;

    protected const DeliveryMethod ModelDataDeliveryMethod = DeliveryMethod.Unreliable;

    protected const int MaxPeerChannelOffset = 24;
    protected const int HostChannelOffset = 0;

    public const int ChunkSize = 1020;
    public const int ChunkHeaderSize = sizeof(byte) + sizeof(int) + sizeof(short); // Channel + FrameCount + ChunkOffset
    public const int ChunkDataSize = ChunkSize - ChunkHeaderSize;

    protected readonly NetworkManager NetworkManager;
    protected readonly NetManager NetManager;
    protected readonly PeerState?[] PeerStates = new PeerState?[MaxPeerChannelOffset];
    protected bool IsRunning = true;

    protected abstract byte? DataChannelOffset { get; }

    private int _frameChannelOffset = 0;
    private readonly ArrayBufferWriter<byte> _frameInformationDataBuffer = new();

    protected NetworkClient(NetworkManager networkManager)
    {
        NetworkManager = networkManager;

        NetManager = new NetManager(this)
        {
            #if DEBUG
            DisconnectTimeout = int.MaxValue,
            #endif

            NatPunchEnabled = true,
            EnableStatistics = true,
            AutoRecycle = true,
            UseNativeSockets = true,
            ChannelsCount = 1,
            PacketPoolSize = 10_000
        };

        if (NetworkMetricsDrawable.Instance is { } networkMetricsDrawable)
        {
            networkMetricsDrawable.NetStatistics = NetManager.Statistics;
        }

        var networkThread = new Thread(NetworkLoop)
        {
            IsBackground = true,
            Name = "VCollabNetworkThread"
        };
        networkThread.Start();
    }

    private void NetworkLoop()
    {
        try
        {
            NetManager.Start();
            NetManager.NatPunchModule.Init(this);

            // Regularly poll for new network events
            while (IsRunning)
            {
                NetManager.NatPunchModule.PollEvents();
                NetManager.PollEvents();

                // This timeout is not precise on Windows as the system tick interval is around 15.6ms
                // it should not cause issues as a "frame" is at least 30ms
                Thread.Sleep(5);
            }

            Logger.Log("Network thread ran to completion.", LoggingTarget.Network);
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Fatal error occured in the network thread: {e.Message}", LoggingTarget.Network, true);

            NetworkManager.GameHost.Exit();
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Logger.Log($"Network error: {socketError}", LoggingTarget.Network, LogLevel.Error);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Logger.Log("Received data from unconnected client.", LoggingTarget.Network);
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(NetworkManager.RoomToken);
    }

    public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
    {
        // Nothing to do here, this is for the signaling server
    }

    public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
    {
        var natIntroductionData = JsonSerializer.Deserialize<NatIntroductionData>(token, JsonSourceGenerationContext.Default.NatIntroductionData);

        if (natIntroductionData is not null)
        {
            // The client connects to the host
            if (this is PeerNetworkClient)
            {
                NetManager.Connect(targetEndPoint, NetworkManager.RoomToken);
            }

            NatIntroduction(natIntroductionData);
        }
        else
        {
            Logger.Log("Received invalid nat introduction data!", LoggingTarget.Network, LogLevel.Error);
        }
    }

    protected virtual void NatIntroduction(NatIntroductionData natIntroductionData)
    {
        // Default implementation does nothing
    }

    protected PeerState NewPeerState(int channelOffset, string name)
    {
        var frameConsumer = NetworkManager.CreateFrameConsumer();

        return new PeerState(channelOffset, name, frameConsumer);
    }

    public void OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channelNumber,
        DeliveryMethod deliveryMethod
    )
    {
        // Information data and model data use different delivery methods on the same channel
        if (deliveryMethod is InformationMessagesDeliveryMethod)
        {
            var message = MemoryPackSerializer.Deserialize<IInformationMessage>(reader.GetRemainingBytesSpan());

            if (message is null)
            {
                Logger.Log("Received invalid/corrupted information message", LoggingTarget.Network, LogLevel.Important);

                return;
            }

            Logger.Log($"Received information message: {message}", LoggingTarget.Network, LogLevel.Debug);

            HandleInformationMessage(peer, message);
        }
        // If it's not information, it is model data
        else
        {
            var modelData = reader.GetRemainingBytesSpan();

            ReceiveModelData(modelData);
            OnModelDataReceived(peer, modelData);
        }
    }

    protected virtual void OnModelDataReceived(NetPeer peer, ReadOnlySpan<byte> data)
    {
        // Nothing to do here, this method is used for additional processing of model data like host forwarding
    }

    protected abstract void HandleInformationMessage(NetPeer peer, IInformationMessage message);

    public void SendModelData(
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData,
        TextureInfo textureInfo,
        long frameCount,
        int uncompressedAlphaDataSize
    )
    {
        // Frame channel offset cycles between 0 and 9 since offset 0 is used for frame info data
        _frameChannelOffset = (_frameChannelOffset + 1) % 9;

        // Pack frame information
        var frameInformation = new NetworkFrameInformation(
            (byte) _frameChannelOffset,
            (int) frameCount,
            textureData.Length,
            alphaData.Length,
            uncompressedAlphaDataSize,
            (ushort) textureInfo.Width,
            (ushort) textureInfo.Height,
            textureInfo.PixelFormat,
            textureInfo.RowPitch
        );

        _frameInformationDataBuffer.ResetWrittenCount();
        MemoryPackSerializer.Serialize(_frameInformationDataBuffer, frameInformation);

        var frameInformationData = _frameInformationDataBuffer.WrittenSpan;

        if (DataChannelOffset is { } dataChannelOffset)
        {
            SendModelDataChunks(textureData, alphaData, frameInformationData, frameInformation.FrameCount, dataChannelOffset);
        }

        // Update metrics
        NetworkMetricsDrawable.FramesSent++;
    }

    private void SendModelDataChunks(
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData,
        ReadOnlySpan<byte> frameInformationData,
        int frameCount,
        byte dataChannelOffset
    )
    {
        Span<byte> buffer = stackalloc byte[ChunkSize];

        var infoChannel = (byte)(dataChannelOffset * 10);
        var frameDataChannel = (byte)(infoChannel + 1 + _frameChannelOffset);

        // Always inject data channel at the start of the network packet
        buffer[0] = infoChannel;
        frameInformationData.CopyTo(buffer[1..]);

        SendRawData(buffer[..(sizeof(byte) + frameInformationData.Length)], ModelDataDeliveryMethod);

        // Set frame data channel once for buffer, this is reused for all chunks
        buffer[0] = frameDataChannel;

        // Texture and alpha data is too big to be sent in one packet, chunk it into smaller messages
        var textureDataSize = textureData.Length;
        var alphaDataSize = alphaData.Length;
        var dataSize = textureDataSize + alphaDataSize;
        var position = 0;
        short chunkOffset = 0;

        while (position < dataSize)
        {
            var toSend = Math.Min(ChunkDataSize, dataSize - position);

            BitConverter.TryWriteBytes(buffer[sizeof(byte)..], frameCount);
            BitConverter.TryWriteBytes(buffer[(sizeof(byte) + sizeof(int))..], chunkOffset);

            // Write texture data first, then alpha data
            if (position < textureDataSize)
            {
                var textureDataToWrite = Math.Min(toSend, textureDataSize - position);

                textureData[position..(position + textureDataToWrite)].CopyTo(buffer[ChunkHeaderSize..]);

                // Check if we can fit some alpha data too in the rest of the buffer
                if (textureDataToWrite < toSend)
                {
                    alphaData[..(toSend - textureDataToWrite)].CopyTo(buffer[(ChunkHeaderSize + textureDataToWrite)..]);
                }
            }
            else
            {
                alphaData[(position - textureDataSize)..(position - textureDataSize + toSend)].CopyTo(buffer[ChunkHeaderSize..]);
            }

            SendRawData(buffer[..(ChunkHeaderSize + toSend)], ModelDataDeliveryMethod);

            position += toSend;
            chunkOffset++;
        }
    }

    protected abstract void SendRawData(ReadOnlySpan<byte> data, DeliveryMethod deliveryMethod);

    private void ReceiveModelData(ReadOnlySpan<byte> data)
    {
        // Channel number is always the first byte of model data packets
        var channelNumber = data[0];

        var (channelOffset, frameOffset) = Math.DivRem(channelNumber, (byte)10);

        if (PeerStates[channelOffset] is not { } peerState)
        {
            Logger.Log("Received data from not registered peer???", LoggingTarget.Network, LogLevel.Error);

            return;
        }

        // Frame information channel
        if (frameOffset is 0)
        {
            var frameInformation = MemoryPackSerializer.Deserialize<NetworkFrameInformation>(data[sizeof(byte)..]);

            if (frameInformation.TotalDataSize <= 0)
            {
                Logger.Log($"Received invalid/corrupt frame information on channel {channelNumber}", LoggingTarget.Network, LogLevel.Error);

                return;
            }

            peerState.UpdateFrameInformation(frameInformation);
        }
        // Actual frame data
        else
        {
            peerState.ReadFrameData(data, frameOffset - 1);
        }
    }

    public abstract void OnPeerConnected(NetPeer peer);

    public abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);

    public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        NetworkMetricsDrawable.Latency = latency;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsRunning = false;

            foreach (var peerState in PeerStates)
            {
                peerState?.Dispose();
            }

            NetManager.DisconnectAll();
            NetManager.PollEvents();
            NetManager.Stop();
        }
    }
}