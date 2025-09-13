using System.Net;
using VCollab.Signaling.Shared;

namespace VCollab.Signaling;

public record HostPeer(IPEndPoint InternalAddress, IPEndPoint ExternalAddress, NatRequestData RequestData)
{
    public DateTime RefreshTime { get; private set; } = DateTime.Now;

    public void Refresh()
    {
        RefreshTime = DateTime.Now;
    }
}