using System.Buffers;
using System.Runtime.InteropServices;
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
    private readonly ArrayBufferWriter<byte> _textureBufferWriter;
    private readonly ArrayBufferWriter<byte> _alphaBufferWriter;

    private TextureInfo _lastTextureInfo;
    private ReadOnlyMemory<byte> _lastTextureData;
    private ReadOnlyMemory<byte> _lastAlphaData;

    public JpegFrameTextureReader(ITextureProvider textureProvider) : base(textureProvider, 20)
    {
        _jpegCompressor = new TJCompressor();
        _textureBufferWriter = new ArrayBufferWriter<byte>();
        _alphaBufferWriter = new ArrayBufferWriter<byte>();
    }

    protected override void OnFrameAvailable()
    {
        // Copy frame data and run jpeg encode task on another thread to free up Update() thread
        var textureData = TextureReader.ReadTextureData(out var textureInfo);

        _textureBufferWriter.ResetWrittenCount();

        var textureCopySpan = _textureBufferWriter.GetSpan(textureData.Length);
        textureData.CopyTo(textureCopySpan);

        _textureBufferWriter.Advance(textureData.Length);

        _lastTextureData = _textureBufferWriter.WrittenMemory;
        _lastTextureInfo = textureInfo;

        Task.Run(WriteTextureDataToJpeg);

        // Also copy alpha data
        var alphaData = AlphaPacker.ReadAlphaData();

        _alphaBufferWriter.ResetWrittenCount();

        var alphaCopySpan = _alphaBufferWriter.GetSpan(alphaData.Length);
        alphaData.CopyTo(alphaCopySpan);

        _alphaBufferWriter.Advance(alphaData.Length);

        _lastAlphaData = _alphaBufferWriter.WrittenMemory;

        Task.Run(WriteAlphaDataToImage);
    }

    private async Task WriteAlphaDataToImage()
    {
        var alphaData = _lastAlphaData.Span;
        var frameCount = AlphaLastFrameCount;

        // var image = Image.LoadPixelData<L8>(alphaData, (int)_lastTextureInfo.Width / 8, (int)_lastTextureInfo.Height);
        // image.Mutate(i => i.Resize((int)_lastTextureInfo.Width, (int)_lastTextureInfo.Height, new NearestNeighborResampler()));

        // await image.SaveAsPngAsync(Path.Combine("tmp", $"frame-{frameCount:D4}-alpha.png"));
    }

    private void WriteTextureDataToJpeg()
    {
        var textureData = _lastTextureData.Span;
        var textureInfo = _lastTextureInfo;
        var frameCount = TextureLastFrameCount;

        var jpegData = _jpegCompressor.CompressShared(
            textureData,
            (int) (textureInfo.Width * Marshal.SizeOf<Rgba32>()),
            (int) textureInfo.Width,
            (int) textureInfo.Height,
            JpegUtils.VeldridToJpegPixelFormat(textureInfo.PixelFormat),
            TJSubsamplingOptions.TJSAMP_420,
            75,
            TJFlags.NONE
        );

        // File.WriteAllBytes(Path.Combine("tmp", $"frame-{frameCount:D4}-texture.jpg"), jpegData);
    }
}