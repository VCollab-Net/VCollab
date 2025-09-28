using Veldrid;

namespace VCollab.Utils.Graphics;

/// <summary>
/// Read from a texture using alternating staging buffers. Always call ProcessFrame on the Draw thread!
/// </summary>
public sealed class DoubleBufferedTextureReader : IDisposable
{
    // Frame count starts at -2 because the first frame will be available on frame 2
    public long FrameCount { get; private set; } = -2;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ResourceFactory _resourceFactory;

    private readonly BufferedResource<Texture>[] _stagingTextures = [new(), new()];
    private int _gpuStagingIndex = 0;

    public DoubleBufferedTextureReader(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _resourceFactory = graphicsDevice.ResourceFactory;
    }

    public unsafe ReadOnlySpan<byte> ProcessFrame(Texture sourceTexture, TextureRegion textureRegion, out TextureInfo textureInfo)
    {
        // Queue GPU read
        var targetBufferedResource = _stagingTextures[_gpuStagingIndex];
        ref var targetTexture = ref targetBufferedResource.Resource;

        EnsureTextureFormat(sourceTexture, ref targetTexture, textureRegion);

        // Check if we're not reading out of bounds of the texture. This can happen when changing source as the
        // Spout receiver has a 1 frame delay after changing source name
        if (textureRegion.OffsetX + textureRegion.Width > sourceTexture.Width
            || textureRegion.OffsetY + textureRegion.Height > sourceTexture.Height)
        {
            // In this case, skip reading
            textureInfo = default;
            return ReadOnlySpan<byte>.Empty;
        }

        // Send the gpu commands to copy source texture to target staging texture
        using var commands = _resourceFactory.CreateCommandList();
        targetBufferedResource.WaitFence?.Dispose();
        targetBufferedResource.WaitFence = _resourceFactory.CreateFence(false);

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

        _graphicsDevice.SubmitCommands(commands, targetBufferedResource.WaitFence);

        // Read previous frame staging texture on CPU if available (should most often be the case)
        var readbackResource = _stagingTextures[(_gpuStagingIndex + 1) % _stagingTextures.Length];

        ReadOnlySpan<byte> data;
        if (readbackResource.Resource is not null && readbackResource.WaitFence?.Signaled is true)
        {
            var readFromTexture = readbackResource.Resource;

            var resource = _graphicsDevice.Map(readFromTexture, MapMode.Read);
            var span = new ReadOnlySpan<byte>(resource.Data.ToPointer(), (int)resource.SizeInBytes);

            _graphicsDevice.Unmap(readFromTexture);

            data = span;
            textureInfo = new TextureInfo(readFromTexture.Width, readFromTexture.Height, readFromTexture.Format, resource.RowPitch);
        }
        else
        {
            data = ReadOnlySpan<byte>.Empty;
            textureInfo = default;
        }

        // Swap buffers for next frame
        SwapBuffers();

        return data;
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
        _gpuStagingIndex = (_gpuStagingIndex + 1) % _stagingTextures.Length;

        FrameCount++;
    }

    public void Dispose()
    {
        foreach (var bufferedResource in _stagingTextures)
        {
            bufferedResource.Dispose();
        }
    }
}