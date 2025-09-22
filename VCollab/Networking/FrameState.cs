using System.Buffers;
using VCollab.Utils.Graphics;

namespace VCollab.Networking;

public sealed class FrameState
{
    private readonly PeerState _peerState;
    private readonly ArrayBufferWriter<byte> _dataBuffer = new ();

    private int _frameCount = 0;
    private int _receivedDataSize = 0;
    private NetworkFrameInformation? _networkFrameInformation = null;
    private bool _frameCompleted = false;

    public FrameState(PeerState peerState)
    {
        _peerState = peerState;
    }

    public void UpdateFrameInformation(NetworkFrameInformation frameInformation)
    {
        CheckFrameInvalidation(frameInformation.FrameCount);

        _networkFrameInformation = frameInformation;

        CheckFrameComplete();
    }

    public void ReadFrameData(ReadOnlySpan<byte> data)
    {
        var frameCount = BitConverter.ToInt32(data);
        var chunkOffset = BitConverter.ToInt16(data[sizeof(int)..]);

        CheckFrameInvalidation(frameCount);

        var modelData = data[NetworkClient.ChunkHeaderSize..];
        var position = chunkOffset * (NetworkClient.ChunkSize - NetworkClient.ChunkHeaderSize);

        var destinationSpan = _dataBuffer.GetSpan(position + modelData.Length);

        modelData.CopyTo(destinationSpan[position..]);

        _receivedDataSize += modelData.Length;

        CheckFrameComplete();
    }

    private void CheckFrameInvalidation(int newFrameCount)
    {
        if (newFrameCount > _frameCount)
        {
            // Update metrics if we skipped this frame
            if (!_frameCompleted && _frameCount > 0)
            {
                NetworkMetricsDrawable.FramesSkipped++;
            }

            // We're receiving data for a new frame, clear up state
            _frameCount = newFrameCount;

            _receivedDataSize = 0;
            _networkFrameInformation = null;
            _frameCompleted = false;
            _dataBuffer.ResetWrittenCount();
        }
    }

    private void CheckFrameComplete()
    {
        // A frame is complete when frame information and frame data has been received
        if (_networkFrameInformation is { } frameInformation && _receivedDataSize == frameInformation.TotalDataSize)
        {
            _frameCompleted = true;

            _dataBuffer.Advance(_receivedDataSize);

            var dataSpan = _dataBuffer.WrittenSpan;
            var textureData = dataSpan[..frameInformation.TextureDataSize];
            var alphaData = dataSpan[frameInformation.TextureDataSize..];

            var textureInfo = new TextureInfo(
                frameInformation.Width,
                frameInformation.Height,
                frameInformation.PixelFormat,
                frameInformation.RowPitch
            );

            _peerState.FrameCompleted(_frameCount, frameInformation.UncompressedAlphaDataSize, textureInfo, textureData, alphaData);
        }
    }
}