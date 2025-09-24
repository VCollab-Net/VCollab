using osu.Framework.Logging;
using osu.Framework.Platform;
using VCollab.Utils;
using VCollab.Utils.Graphics;

namespace VCollab.Networking;

public sealed class NetworkManager : IDisposable
{
    public event Action<INetworkFrameConsumer>? NewNetworkFrameConsumer;

    public GameHost GameHost { get; }
    public string RoomToken { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public NetworkState NetworkState { get; private set; } = NetworkState.Unconnected;

    public int ConnectedPeersCount => _networkClient?.ConnectedPeersCount ?? 0;

    private NetworkClient? _networkClient = null;

    public NetworkManager(GameHost gameHost)
    {
        GameHost = gameHost;
    }

    public bool StartAsHost(string name, string roomToken)
    {
        if (!RoomTokenUtils.IsValidToken(roomToken))
        {
            return false;
        }

        UserName = name;
        RoomToken = roomToken;
        NetworkState = NetworkState.Hosting;

        Logger.Log($"Starting network client as Host: {name}", LoggingTarget.Network);

        // The HostNetworkClient will automatically start sending nat requests and check for new peers
        var hostNetworkClient = new HostNetworkClient(this);

        _networkClient = hostNetworkClient;

        return true;
    }

    public bool StartAsPeer(string name, string roomToken)
    {
        if (!RoomTokenUtils.IsValidToken(roomToken))
        {
            return false;
        }

        UserName = name;
        RoomToken = roomToken;
        NetworkState = NetworkState.Connected;

        Logger.Log($"Starting network client as Peer: {name}", LoggingTarget.Network);

        // TODO Handle retries or case where there is no room with this token available
        // The PeerNetworkClient has to send a nat request and wait for a response
        var peerNetworkClient = new PeerNetworkClient(this);

        peerNetworkClient.ConnectToRoom(name, roomToken);

        _networkClient = peerNetworkClient;

        return true;
    }

    public INetworkFrameConsumer CreateFrameConsumer()
    {
        var frameConsumer = new NetworkModelSprite();

        NewNetworkFrameConsumer?.Invoke(frameConsumer);

        return frameConsumer;
    }

    public void SendModelData(
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData,
        TextureInfo textureInfo,
        long frameCount,
        int uncompressedAlphaDataSize
    )
    {
        _networkClient?.SendModelData(textureData, alphaData, textureInfo, frameCount, uncompressedAlphaDataSize);
    }

    public void Dispose()
    {
        _networkClient?.Dispose();
    }
}