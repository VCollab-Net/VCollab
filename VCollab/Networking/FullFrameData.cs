using System.Buffers;
using VCollab.Utils.Graphics;

namespace VCollab.Networking;

public sealed class FullFrameData : IDisposable
{
    public ReadOnlySpan<byte> TextureDataSpan => _backingBuffer.WrittenSpan[.._textureSize];
    public ReadOnlyMemory<byte> TextureDataMemory => _backingBuffer.WrittenMemory[.._textureSize];

    public ReadOnlySpan<byte> AlphaDataSpan => _backingBuffer.WrittenSpan[_textureSize..];
    public ReadOnlyMemory<byte> AlphaDataMemory => _backingBuffer.WrittenMemory[_textureSize..];

    public int FrameCount { get; }
    public TextureInfo TextureInfo { get; }

    private readonly PeerState _peerState;
    private readonly ArrayBufferWriter<byte> _backingBuffer;
    private readonly int _textureSize;

    public FullFrameData(
        PeerState peerState,
        ArrayBufferWriter<byte> backingBuffer,
        int textureSize,
        int frameCount,
        TextureInfo textureInfo
    )
    {
        _peerState = peerState;
        _backingBuffer = backingBuffer;
        _textureSize = textureSize;

        FrameCount = frameCount;
        TextureInfo = textureInfo;
    }

    public ArrayBufferWriter<byte> ReturnBackingBuffer()
    {
        return _backingBuffer;
    }

    public void Dispose()
    {
        _peerState.ReturnFrameBuffer(_backingBuffer);
    }
}