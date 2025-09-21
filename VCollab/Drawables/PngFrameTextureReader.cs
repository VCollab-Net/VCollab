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

public partial class PngFrameTextureReader : FrameTextureReader
{
    private ReadOnlyMemory<byte> _lastTextureData;
    private ReadOnlyMemory<byte> _lastAlphaData;
    private TextureInfo _lastTextureInfo;

    public PngFrameTextureReader(ITextureProvider textureProvider) : base(textureProvider, 25)
    {
        if (!Directory.Exists("tmp"))
        {
            Directory.CreateDirectory("tmp");
        }
    }

    protected override void OnFrameAvailable(ReadOnlyMemory<byte> textureData, ReadOnlyMemory<byte> alphaData, TextureInfo textureInfo)
    {
        _lastTextureData = textureData;
        _lastAlphaData = alphaData;
        _lastTextureInfo = textureInfo;

        Task.Run(WriteTextureDataToPng);
    }

    private void WriteTextureDataToPng()
    {
        var textureData = _lastTextureData.Span;
        var textureInfo = _lastTextureInfo;
        var frameCount = FramesCount;

        // on some backends (Direct3D11, in particular), the staging resource data may contain padding at the end of each row for alignment,
        // which means that for the image width, we cannot use the framebuffer's width raw.
        using var image = Image.LoadPixelData<Rgba32>(textureData, (int)(textureInfo.RowPitch / Marshal.SizeOf<Rgba32>()), (int)textureInfo.Height);

        // if the image width doesn't match the framebuffer, it means that we still have padding at the end of each row mentioned above to get rid of.
        // snip it to get a clean image.
        if (image.Width != textureInfo.Width)
            image.Mutate(i => i.Crop((int)textureInfo.Width, (int)textureInfo.Height));

        image.SaveAsPng(Path.Combine("tmp", $"frame-{frameCount:D4}-texture.png"));
    }
}