using Veldrid;

namespace VCollab.Utils;

public class SpoutUtils
{
    public static PixelFormat DxgiToVeldridPixelFormat(uint dxgiFormat) => dxgiFormat switch
    {
        28 => PixelFormat.R8G8B8A8UNorm, // DXGI_FORMAT_R8G8B8A8_UNORM (This is the one used by VTubeStudio)

        87 => PixelFormat.B8G8R8A8UNorm, // DXGI_FORMAT_B8G8R8A8_UNORM (This is default Spout2 format)

        10 => PixelFormat.R16G16B16A16Float, // DXGI_FORMAT_R16G16B16A16_FLOAT
        11 => PixelFormat.R16G16B16A16UNorm, // DXGI_FORMAT_R16G16B16A16_UNORM

        2 => PixelFormat.R32G32B32A32Float, // DXGI_FORMAT_R32G32B32A32_FLOAT

        _ => throw new ArgumentException("Unknown DXGI pixel format, cannot convert it to Veldrid one", nameof(dxgiFormat))
    };

    public static uint VeldridPixelFormatToDxgi(PixelFormat pixelFormat) => pixelFormat switch
    {
        PixelFormat.R8G8B8A8UNorm => 28,

        PixelFormat.B8G8R8A8UNorm => 87,

        PixelFormat.R16G16B16A16Float => 10,
        PixelFormat.R16G16B16A16SNorm => 11,

        PixelFormat.R32G32B32A32Float => 2,

        _ => throw new ArgumentException("Unknown Veldrid pixel format, cannot convert it to DXGI one",
            nameof(pixelFormat))
    };
}