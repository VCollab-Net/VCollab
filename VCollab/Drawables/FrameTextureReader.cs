using System.Diagnostics;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using TurboJpegWrapper;
using VCollab.Utils.Graphics;
using VCollab.Utils.Graphics.Compute;
using Veldrid;

namespace VCollab.Drawables;

public abstract partial class FrameTextureReader : Drawable
{
    public long ReadIntervalMilliseconds { get; }

    protected DoubleBufferedTextureReader TextureReader = null!;
    protected DoubleBufferedAlphaMaskPacker AlphaPacker = null!;
    private Scheduler _drawThreadScheduler = null!;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly ITextureProvider _textureProvider;

    protected long TextureLastFrameCount = 0;
    protected long AlphaLastFrameCount = 0;
    private long _lastReadTime = 0;
    private bool _isInitialized = false;

    protected FrameTextureReader(ITextureProvider textureProvider, long millisecondsInterval)
    {
        _textureProvider = textureProvider;
        ReadIntervalMilliseconds = millisecondsInterval;

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

        TextureReader = new DoubleBufferedTextureReader(renderer.Device);
        AlphaPacker = new DoubleBufferedAlphaMaskPacker(renderer.Device);

        _isInitialized = true;
    }

    protected override void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        // Schedule texture and alpha read if last read time is old enough
        if (_stopwatch.ElapsedMilliseconds - _lastReadTime >= ReadIntervalMilliseconds)
        {
            _drawThreadScheduler.AddOnce(ReadTextureAndAlpha);
        }

        // Execute read task if new frame is available
        if (TextureReader.IsNewFrameAvailable(ref TextureLastFrameCount) &&
            AlphaPacker.IsNewFrameAvailable(ref AlphaLastFrameCount))
        {
            OnFrameAvailable();
        }
    }

    protected abstract void OnFrameAvailable();

    private void ReadTextureAndAlpha()
    {
        _lastReadTime = _stopwatch.ElapsedMilliseconds;

        var osuTexture = _textureProvider.Texture;

        if (osuTexture?.NativeTexture is not IVeldridTexture veldridTexture)
        {
            return;
        }

        var toRead = veldridTexture.GetResourceList()[0].Texture;

        TextureReader.UploadTexture(toRead);
        AlphaPacker.UploadTexture(toRead);
    }
}