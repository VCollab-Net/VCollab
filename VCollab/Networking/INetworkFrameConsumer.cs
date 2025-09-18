using System.Collections.Concurrent;

namespace VCollab.Networking;

public interface INetworkFrameConsumer : IDisposable
{
    public string? UserName { get; set; }
    public ConcurrentBag<FullFrameData>? FramesBag { get; set; }
}