using System.Buffers;
using System.Diagnostics;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using VCollab.Utils.Extensions;
using VCollab.Utils.Graphics;
using VCollab.Utils.Graphics.Compute;
using Veldrid;

namespace VCollab.Drawables;

public abstract partial class FrameTextureReader : Drawable
{
    public long MinimumReadIntervalMilliseconds { get; }

    public long FramesCount { get; private set; } = -1;
    /// <summary>
    /// Frames skipped because CPU did not receive data in time. Starts at -1 because CPU is always lagging behind
    /// by one frame since we're using double buffering.
    /// </summary>
    public long FramesSkipCount { get; private set; } = -1;

    public TextureRegion? TextureRegion { get; set; }

    private DoubleBufferedTextureReader _textureReader = null!;
    private DoubleBufferedAlphaMaskPacker _alphaPacker = null!;
    private Scheduler _drawThreadScheduler = null!;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly ITextureProvider _textureProvider;

    private readonly ArrayBufferWriter<byte> _textureBufferWriter = new();
    private readonly ArrayBufferWriter<byte> _alphaBufferWriter = new();

    private long _lastReadTime = 0;
    private bool _isInitialized = false;

    protected FrameTextureReader(ITextureProvider textureProvider, long millisecondsInterval)
    {
        _textureProvider = textureProvider;
        MinimumReadIntervalMilliseconds = millisecondsInterval;

        // Nothing to draw
        Size = Vector2.Zero;
        Alpha = 0;

        // Make sure updates are still ran
        AlwaysPresent = true;
    }

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        // Veldrid interop
        if (host.Renderer is not VeldridRenderer renderer || renderer.Device.BackendType != GraphicsBackend.Direct3D11)
        {
            var exception = new NotSupportedException("The spout receiver only works for Veldrid with D3D11 surface");

            Logger.Error(exception, "Unsupported platform and renderer");

            throw exception;
        }

        _drawThreadScheduler = host.DrawThread.Scheduler;

        _textureReader = new DoubleBufferedTextureReader(renderer.Device);
        _alphaPacker = new DoubleBufferedAlphaMaskPacker(renderer.Device);

        _isInitialized = true;
    }

    protected override void Update()
    {
        if (!_isInitialized || TextureRegion is null)
        {
            return;
        }

        _drawThreadScheduler.AddOnce(ReadTextureAndAlpha);
    }

    protected abstract void OnFrameAvailable(ReadOnlyMemory<byte> textureData, ReadOnlyMemory<byte> alphaData, TextureInfo textureInfo);

    private void ReadTextureAndAlpha()
    {
        // Skip texture and alpha read if last read time is not old enough
        if (_stopwatch.ElapsedMilliseconds - _lastReadTime < MinimumReadIntervalMilliseconds)
        {
            return;
        }

        _lastReadTime = _stopwatch.ElapsedMilliseconds;

        var osuTexture = _textureProvider.Texture;

        if (osuTexture?.NativeTexture is not IVeldridTexture veldridTexture || TextureRegion is not { } textureRegion)
        {
            return;
        }

        var toRead = veldridTexture.GetResourceList()[0].Texture;

        // Read and copy texture and alpha data
        var textureData = _textureReader.ProcessFrame(toRead, textureRegion, out var textureInfo);
        var alphaData = _alphaPacker.ProcessFrame(toRead, textureRegion);

        // Skip frame if returned data is empty, cpu most likely did not receive data in time, or it's the first frame
        if (textureData.IsEmpty || alphaData.IsEmpty)
        {
            FramesSkipCount++;

            return;
        }

        // Present the new frame to consumer
        FramesCount++;

        var textureDataMemory = textureData.WriteToBufferMemory(_textureBufferWriter);
        var alphaDataMemory = alphaData.WriteToBufferMemory(_alphaBufferWriter);

        OnFrameAvailable(textureDataMemory, alphaDataMemory, textureInfo);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textureReader.Dispose();
            _alphaPacker.Dispose();
        }

        base.Dispose(disposing);
    }
}