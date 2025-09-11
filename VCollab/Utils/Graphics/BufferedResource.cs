using Veldrid;

namespace VCollab.Utils.Graphics;

public sealed class BufferedResource<TResource> : IDisposable where TResource : IBindableResource, IDisposable
{
    public TResource? Resource;
    public Fence? WaitFence;

    public void Dispose()
    {
        Resource?.Dispose();
        WaitFence?.Dispose();
    }
}