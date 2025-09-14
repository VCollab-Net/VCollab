using System.Buffers.Text;

namespace VCollab.Networking;

public class NetworkManager
{
    public const int RoomTokenSizeInBytes = 6;
    public int RoomTokenLength { get; } = Base64Url.GetEncodedLength(RoomTokenSizeInBytes);

    public string RoomToken { get; private set; } = string.Empty;

    private INetworkClient? _networkClient = null;

    public bool StartAsHost(string name, string roomToken)
    {
        if (roomToken.Length != RoomTokenLength)
        {
            return false;
        }

        RoomToken = roomToken;

        var hostNetworkClient = new HostNetworkClient();



        _networkClient = hostNetworkClient;

        return true;
    }

    public bool StartAsPeer(string name, string roomToken)
    {
        if (roomToken.Length != RoomTokenLength)
        {
            return false;
        }

        RoomToken = roomToken;

        return true;
    }
}