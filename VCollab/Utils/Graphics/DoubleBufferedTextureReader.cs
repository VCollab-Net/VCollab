using Veldrid;

namespace VCollab.Utils.Graphics;

/// <summary>
/// Read from a texture using alternating staging buffers. This class does not check for cross-frame concurrency.
/// </summary>
public sealed class DoubleBufferedTextureReader : IDisposable
{
    // Frame count starts at -2 because the first frame will be available on frame 2
    public long FrameCount { get; private set; } = -2;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ResourceFactory _resourceFactory;

    private Texture? _firstStagingTexture;
    private Texture? _secondStagingTexture;

    private Fence? _currentWaitFence;
    private Fence? _previousWaitFence;

    private bool _uploadingToFirstBuffer = true;

    private readonly Lock _swappingBuffersLock = new();

    public DoubleBufferedTextureReader(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _resourceFactory = graphicsDevice.ResourceFactory;
    }

    public bool IsNewFrameAvailable(ref long lastFrameCount)
    {
        // First frame is available on frame 2
        if (FrameCount < 0)
        {
            return false;
        }

        // New frame is only available if frame count changed
        if (lastFrameCount >= FrameCount)
        {
            return false;
        }

        // If it is a new frame, it is available if the fence has been signaled (this should always be true if frames are long enough)
        if (_previousWaitFence?.Signaled is true)
        {
            lastFrameCount = FrameCount;

            return true;
        }

        return false;
    }

    public unsafe ReadOnlySpan<byte> ReadTextureData(out TextureInfo textureInfo)
    {
        Texture? readFromTexture;

        using (_swappingBuffersLock.EnterScope())
        {
            readFromTexture = _uploadingToFirstBuffer ? _secondStagingTexture : _firstStagingTexture;

            if (readFromTexture is null || _previousWaitFence?.Signaled is not true)
            {
                throw new InvalidOperationException($"No frame could be read, make sure to call {nameof(IsNewFrameAvailable)} first!");
            }
        }

        var resource = _graphicsDevice.Map(readFromTexture, MapMode.Read);
        var span = new ReadOnlySpan<byte>(resource.Data.ToPointer(), (int)resource.SizeInBytes);

        _graphicsDevice.Unmap(readFromTexture);

        textureInfo = new TextureInfo(readFromTexture.Width, readFromTexture.Height, readFromTexture.Format, resource.RowPitch);

        return span;
    }

    public void UploadTexture(Texture sourceTexture, TextureRegion textureRegion)
    {
        // Present texture from last frame to CPU
        using (_swappingBuffersLock.EnterScope())
        {
            SwapBuffers();
        }

        ref var targetTexture = ref _uploadingToFirstBuffer ? ref _firstStagingTexture : ref _secondStagingTexture;

        EnsureTextureFormat(sourceTexture, ref targetTexture, textureRegion);

        // Send the gpu commands to copy source texture to target staging texture
        using var commands = _resourceFactory.CreateCommandList();
        _currentWaitFence = _resourceFactory.CreateFence(false);

        commands.Begin();
        commands.CopyTexture(
            sourceTexture,
            textureRegion.OffsetX,
            textureRegion.OffsetY,0, 0, 0,
            targetTexture,
            0, 0, 0, 0, 0,
            textureRegion.Width,
            textureRegion.Height,
            1, 1
        );
        commands.End();

        _graphicsDevice.SubmitCommands(commands, _currentWaitFence);
    }

    private void EnsureTextureFormat(Texture sourceTexture, ref Texture? targetTexture, TextureRegion textureRegion)
    {
        if (targetTexture is null
            || targetTexture.Width != textureRegion.Width
            || targetTexture.Height != textureRegion.Height
            || targetTexture.Format != sourceTexture.Format)
        {
            targetTexture = _resourceFactory.CreateTexture(TextureDescription.Texture2D(
                textureRegion.Width,
                textureRegion.Height,
                1,
                1,
                sourceTexture.Format,
                TextureUsage.Staging
            ));
        }
    }

    private void SwapBuffers()
    {
        _previousWaitFence?.Dispose();
        _previousWaitFence = _currentWaitFence;

        _uploadingToFirstBuffer = !_uploadingToFirstBuffer;

        FrameCount++;
    }

    public void Dispose()
    {
        _firstStagingTexture?.Dispose();
        _secondStagingTexture?.Dispose();

        _currentWaitFence?.Dispose();
        _previousWaitFence?.Dispose();
    }
}