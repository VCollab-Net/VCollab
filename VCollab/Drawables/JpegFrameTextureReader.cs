using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using TurboJpegWrapper;
using VCollab.Utils;
using VCollab.Utils.Graphics;
using Image = SixLabors.ImageSharp.Image;

namespace VCollab.Drawables;

public partial class JpegFrameTextureReader : FrameTextureReader
{
    private readonly TJCompressor _jpegCompressor;

    private ReadOnlyMemory<byte> _lastTextureData;
    private ReadOnlyMemory<byte> _lastAlphaData;
    private TextureInfo _lastTextureInfo;

    public JpegFrameTextureReader(ITextureProvider textureProvider) : base(textureProvider, 25)
    {
        _jpegCompressor = new TJCompressor();
    }

    protected override void OnFrameAvailable(ReadOnlyMemory<byte> textureData, ReadOnlyMemory<byte> alphaData, TextureInfo textureInfo)
    {
        _lastTextureData = textureData;
        _lastAlphaData = alphaData;
        _lastTextureInfo = textureInfo;

        Task.Run(WriteTextureDataToJpeg);
        Task.Run(WriteAlphaDataToImage);
    }

    private async Task WriteAlphaDataToImage()
    {
        var alphaData = _lastAlphaData.Span;
        var frameCount = FramesCount;

        var image = Image.LoadPixelData<L8>(alphaData, (int)_lastTextureInfo.Width / 8, (int)_lastTextureInfo.Height);
        image.Mutate(i => i.Resize((int)_lastTextureInfo.Width, (int)_lastTextureInfo.Height, new NearestNeighborResampler()));

        await image.SaveAsPngAsync(Path.Combine("tmp", $"frame-{frameCount:D4}-alpha.png"));
    }

    private void WriteTextureDataToJpeg()
    {
        var textureData = _lastTextureData.Span;
        var textureInfo = _lastTextureInfo;
        var frameCount = FramesCount;

        var jpegData = _jpegCompressor.CompressShared(
            textureData,
            (int) textureInfo.RowPitch,
            (int) textureInfo.Width,
            (int) textureInfo.Height,
            JpegUtils.VeldridToJpegPixelFormat(textureInfo.PixelFormat),
            TJSubsamplingOptions.TJSAMP_420,
            75,
            TJFlags.NONE
        );

        File.WriteAllBytes(Path.Combine("tmp", $"frame-{frameCount:D4}-texture.jpg"), jpegData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _jpegCompressor.Dispose();
        }

        base.Dispose(disposing);
    }
}