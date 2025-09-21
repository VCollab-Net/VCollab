using System.Buffers;
using K4os.Compression.LZ4;
using TurboJpegWrapper;
using VCollab.Networking;
using VCollab.Utils;
using VCollab.Utils.Graphics;

namespace VCollab.Drawables;

public partial class NetworkSendFrameTextureReader : FrameTextureReader
{
    [Resolved]
    private NetworkManager NetworkManager { get; set; } = null!;

    private readonly TJCompressor _jpegCompressor = new();
    private readonly ArrayBufferWriter<byte> _alphaCompressedDataBuffer = new();

    private ReadOnlyMemory<byte> _lastTextureData;
    private ReadOnlyMemory<byte> _lastAlphaData;
    private TextureInfo _lastTextureInfo;
    private bool _sendingFrame = false;

    public NetworkSendFrameTextureReader(ITextureProvider textureProvider) : base(textureProvider, 25)
    {

    }

    protected override void OnFrameAvailable(ReadOnlyMemory<byte> textureData, ReadOnlyMemory<byte> alphaData, TextureInfo textureInfo)
    {
        _lastTextureData = textureData;
        _lastAlphaData = alphaData;
        _lastTextureInfo = textureInfo;

        Task.Run(SendDataOverNetwork);
    }

    private void SendDataOverNetwork()
    {
        // Only one frame can be sent at a time, skip this frame if a sending operation is already running
        if (Interlocked.CompareExchange(ref _sendingFrame, true, false))
        {
            return;
        }

        var frameCount = FramesCount;
        var textureInfo = _lastTextureInfo;
        var textureData = _lastTextureData.Span;
        var alphaData = _lastAlphaData.Span;

        // Encode texture data to jpeg
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

        // Compress alpha data
        _alphaCompressedDataBuffer.ResetWrittenCount();
        var compressedAlphaDataWriter = _alphaCompressedDataBuffer.GetSpan(LZ4Codec.MaximumOutputSize(alphaData.Length));
        var compressedSize = LZ4Codec.Encode(alphaData, compressedAlphaDataWriter);
        _alphaCompressedDataBuffer.Advance(compressedSize);

        var compressedAlphaData = _alphaCompressedDataBuffer.WrittenSpan;

        // Send data over network
        NetworkManager.SendModelData(jpegData, compressedAlphaData, textureInfo, frameCount, alphaData.Length);

        _sendingFrame = false;
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