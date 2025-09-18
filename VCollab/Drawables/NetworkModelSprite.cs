using System.Buffers;
using System.Collections.Concurrent;
using K4os.Compression.LZ4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using VCollab.Networking;
using Image = SixLabors.ImageSharp.Image;

namespace VCollab.Drawables;

public partial class NetworkModelSprite : Drawable, INetworkFrameConsumer
{
    public string? UserName { get; set; } = null;
    public ConcurrentBag<FullFrameData>? FramesBag { get; set; } = null;

    protected override void Update()
    {
        // Check for available frames and save them to files for debug
        if (UserName is not null && FramesBag is not null)
        {
            while (!FramesBag.IsEmpty)
            {
                if (FramesBag.TryTake(out var frameData))
                {
                    Task.Run(() => SaveFrameData(frameData));
                }
            }
        }
    }

    private async Task SaveFrameData(FullFrameData frameData)
    {
        var frameCount = frameData.FrameCount;
        var textureInfo = frameData.TextureInfo;
        var textureData = frameData.TextureDataMemory;

        var baseFilePath = Path.Combine("tmp", $"{UserName}-frame-{frameCount:D5}");

        // Decompress alpha data
        var uncompressedAlphaData = ArrayPool<byte>.Shared.Rent(frameData.UncompressedAlphaDataSize);
        var uncompressedAlphaDataSize = LZ4Codec.Decode(frameData.AlphaDataSpan, uncompressedAlphaData);

        // Save alpha later as viewable image
        var image = Image.LoadPixelData<L8>(uncompressedAlphaData.AsSpan()[..uncompressedAlphaDataSize], (int)textureInfo.Width / 8, (int)textureInfo.Height);
        image.Mutate(i => i.Resize((int)textureInfo.Width, (int)textureInfo.Height, new NearestNeighborResampler()));

        var writeTextureTask = File.WriteAllBytesAsync($"{baseFilePath}.jpg", textureData);
        var alphaWriteTask = image.SaveAsPngAsync($"{baseFilePath}.png");

        await Task.WhenAll(writeTextureTask, alphaWriteTask);

        ArrayPool<byte>.Shared.Return(uncompressedAlphaData);
        frameData.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // This frame consumer is disposed when the peer state becomes invalid, most often due to disconnect
            Expire();
        }

        base.Dispose(disposing);
    }
}