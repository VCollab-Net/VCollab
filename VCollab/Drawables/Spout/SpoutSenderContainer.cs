using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using SpoutDx.Net.Interop;
using VCollab.Utils.Extensions;
using Veldrid;

namespace VCollab.Drawables.Spout;

public partial class SpoutSenderContainer : BufferedContainer
{
    private readonly string? _spoutSenderName;

    private VeldridRenderer _renderer = null!;
    private Scheduler _drawThreadScheduler = null!;
    private SpoutSender? _spoutSender;

    private bool _isInitialized = false;
    private Texture? _textureCopy;

    public SpoutSenderContainer(string? spoutSenderName = null)
    {
        _spoutSenderName = spoutSenderName;

        AlwaysPresent = true;
    }

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        // Veldrid interop
        if (host.Renderer is not VeldridRenderer renderer || renderer.Device.BackendType != GraphicsBackend.Direct3D11)
        {
            var exception = new NotSupportedException("The spout sender only works for Veldrid with D3D11 surface");

            Logger.Error(exception, "Unsupported platform and renderer");

            throw exception;
        }

        _drawThreadScheduler = host.DrawThread.Scheduler;

        _renderer = renderer;
        var d3d11Device = renderer.Device.GetD3D11Info().Device;

        // Initialize Spout2 sender
        _drawThreadScheduler.AddOnce(() =>
        {
            _spoutSender = new SpoutSender(d3d11Device);

            // Select first available sender
            if (_spoutSenderName is not null)
            {
                _spoutSender.Name = _spoutSenderName;
            }

            _isInitialized = true;
        });
    }

    protected override void Update()
    {
        base.Update();

        if (!_isInitialized || _spoutSender is null)
        {
            return;
        }

        _drawThreadScheduler.AddOnce(() =>
        {
            var sharedData = SharedData;

            // Check if main framebuffer has been initialized
            if (sharedData.MainBuffer is not VeldridFrameBuffer veldridFrameBuffer)
            {
                return;
            }

            var nativeTexture = (VeldridTexture) veldridFrameBuffer.Texture.NativeTexture;
            var textureToSend = nativeTexture.Resources!.Texture;

            // Check if texture need initialization
            if (_textureCopy is null ||
                _textureCopy.Width != textureToSend.Width ||
                _textureCopy.Height != textureToSend.Height)
            {
                _textureCopy?.Dispose();
                _textureCopy = _renderer.Factory.CreateTexture(TextureDescription.Texture2D(
                    textureToSend.Width,
                    textureToSend.Height,
                    1,
                    1,
                    textureToSend.Format,
                    TextureUsage.RenderTarget | TextureUsage.Sampled
                ));
            }

            using var commands = _renderer.Factory.CreateCommandList();

            commands.Begin();
            commands.CopyTexture(textureToSend, _textureCopy, 0, 0);
            commands.End();

            // Do not wait for the copy to be done, the framework will do that for us
            _renderer.Device.SubmitCommands(commands);

            var nativePointer = _textureCopy.NativePointer;

            if (!_spoutSender.SendTexture(nativePointer))
            {
                Logger.Log("Could not send texture!", level: LogLevel.Important);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spoutSender?.Dispose();
            _textureCopy?.Dispose();
        }

        base.Dispose(disposing);
    }
}