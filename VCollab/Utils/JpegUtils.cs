using TurboJpegWrapper;
using Veldrid;

namespace VCollab.Utils;

public static class JpegUtils
{
    public static TJPixelFormats VeldridToJpegPixelFormat(PixelFormat pixelFormat) => pixelFormat switch
    {
        PixelFormat.R8G8B8A8UNorm => TJPixelFormats.TJPF_RGBX,

        PixelFormat.B8G8R8A8UNorm => TJPixelFormats.TJPF_BGRX,

        _ => throw new ArgumentException($"Cannot convert pixel format '{pixelFormat}' to Jpeg one, it may be a pixel format that uses more than 4 bytes per color channel",
            nameof(pixelFormat))
    };
}