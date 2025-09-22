using System.Buffers;
using System.Collections.Concurrent;
using osu.Framework.Logging;
using VCollab.Utils.Graphics;

namespace VCollab.Networking;

public sealed class PeerState : IDisposable
{
    public int ChannelOffset { get; }
    public string Name { get; }

    public int Latency { get; set; }

    public ConcurrentBag<FullFrameData> AvailableFrames { get; } = new();

    private const int FrameStatesLength = 9;

    private readonly FrameState[] _frameStates = new FrameState[FrameStatesLength];
    private readonly Stack<ArrayBufferWriter<byte>> _buffersPool = new(15);
    private readonly INetworkFrameConsumer _frameConsumer;

    public PeerState(int channelOffset, string name, INetworkFrameConsumer frameConsumer)
    {
        ChannelOffset = channelOffset;
        Name = name;
        _frameConsumer = frameConsumer;

        for (var i = 0; i < _frameStates.Length; i++)
        {
            _frameStates[i] = new FrameState(this);
        }

        for (int i = 0; i < _buffersPool.Capacity; i++)
        {
            _buffersPool.Push(new ArrayBufferWriter<byte>());
        }

        _frameConsumer.UserName = name;
        _frameConsumer.FramesBag = AvailableFrames;
    }

    public void UpdateFrameInformation(NetworkFrameInformation frameInformation)
    {
        var frameOffset = frameInformation.FrameChannelOffset;

        _frameStates[frameOffset].UpdateFrameInformation(frameInformation);
    }

    public void ReadFrameData(ReadOnlySpan<byte> data, int frameOffset)
    {
        _frameStates[frameOffset].ReadFrameData(data);
    }

    public void FrameCompleted(
        int frameCount,
        int uncompressedAlphaDataSize,
        TextureInfo textureInfo,
        ReadOnlySpan<byte> textureData,
        ReadOnlySpan<byte> alphaData
    )
    {
        // Update network metrics
        NetworkMetricsDrawable.FramesReceived++;

        // Copy frame data for later use by the graphics pipeline
        if (!_buffersPool.TryPop(out var bufferWriter))
        {
            Logger.Log("Buffers pool is empty, the graphics pipeline is lagging behind!", LoggingTarget.Network, LogLevel.Important);

            return;
        }

        bufferWriter.ResetWrittenCount();

        var copySpan = bufferWriter.GetSpan(textureData.Length + alphaData.Length);

        textureData.CopyTo(copySpan);
        alphaData.CopyTo(copySpan[textureData.Length..]);

        bufferWriter.Advance(textureData.Length + alphaData.Length);

        AvailableFrames.Add(new FullFrameData(
            this,
            bufferWriter,
            textureData.Length,
            frameCount,
            uncompressedAlphaDataSize,
            textureInfo
        ));
    }

    public void ReturnFrameBuffer(ArrayBufferWriter<byte> backingBuffer)
    {
        _buffersPool.Push(backingBuffer);
    }

    public void Dispose()
    {
        _frameConsumer.Dispose();
    }
}