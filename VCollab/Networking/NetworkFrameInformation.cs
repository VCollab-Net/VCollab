using MemoryPack;
using Veldrid;

namespace VCollab.Networking;

[MemoryPackable]
public partial record struct NetworkFrameInformation(
    byte FrameChannelOffset,
    int FrameCount,
    int TextureDataSize,
    int AlphaDataSize,
    int UncompressedAlphaDataSize,
    ushort Width,
    ushort Height,
    PixelFormat PixelFormat,
    uint RowPitch
)
{
    [MemoryPackIgnore]
    public int TotalDataSize => TextureDataSize + AlphaDataSize;
}