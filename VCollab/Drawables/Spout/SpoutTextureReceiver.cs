using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using SpoutDx.Net.Interop;
using VCollab.Utils;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Drawables.Spout;

public partial class SpoutTextureReceiver : Drawable
{
    public event Action<Texture?>? TextureUpdated;

    private string? _senderName;
    public string? SenderName
    {
        get => _senderName;
        set
        {
            if (value is null || value == _senderName)
            {
                // Do nothing if value is the same
                return;
            }

            _senderName = value;
            _needsNameUpdate = true;
        }
    }

    public Texture? Texture { get; private set; }

    private VeldridRenderer _renderer = null!;
    private Scheduler _drawThreadScheduler = null!;
    private SpoutReceiver? _spoutReceiver;

    private bool _needsNameUpdate = false;
    private bool _isInitialized = false;

    public SpoutTextureReceiver()
    {
        // Nothing to draw
        Size = Vector2.Zero;
        Alpha = 0;

        // Make sure updates are still ran
        AlwaysPresent = true;
    }

    public string[] GetSenderNames()
    {
        if (_spoutReceiver is not null)
        {
            return _spoutReceiver.GetSenderNames();
        }

        return [];
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

        _renderer = renderer;
        var d3d11Device = renderer.Device.GetD3D11Info().Device;

        // Initialize Spout2 receiver
        _drawThreadScheduler.AddOnce(() =>
        {
            _spoutReceiver = new SpoutReceiver(d3d11Device);

            // Select first available sender
            if (_senderName is not null)
            {
                _spoutReceiver.SenderName = _senderName;
            }

            _isInitialized = true;
        });
    }

    protected override void Update()
    {
        if (!_isInitialized || string.IsNullOrWhiteSpace(_senderName))
        {
            return;
        }

        if (_spoutReceiver is null)
        {
            Logger.Log("SpoutDx wrapper should not be null here!", level: LogLevel.Important);

            return;
        }

        _drawThreadScheduler.AddOnce(DrawThreadTask);
    }

    private void DrawThreadTask()
    {
        // Check if name has been updated
        if (_needsNameUpdate)
        {
            _spoutReceiver!.SenderName = SenderName;

            _needsNameUpdate = false;
        }

        // Check if texture has been updated
        if (_spoutReceiver!.IsUpdated())
        {
            Logger.Log("Spout2 texture updated");
            InitializeReceiverTexture();
        }

        // Copy texture to internal SpoutDx class texture
        if (!_spoutReceiver!.ReceiveTexture())
        {
            Logger.Log("Could not receive texture!", level: LogLevel.Important);

            return;
        }

        // TODO Do something with this?
        // if (_spoutReceiver.IsFrameNew())
        // {
        //
        // }
    }

    private void InitializeReceiverTexture()
    {
        if (_spoutReceiver is null)
        {
            Logger.Log("SpoutDx wrapper should not be null here!", level: LogLevel.Important);

            return;
        }

        if (!_spoutReceiver.ReceiveTexture())
        {
            Logger.Log("Could not receive texture!", level: LogLevel.Important);

            return;
        }

        var spoutReceivedTexture = _spoutReceiver.GetSenderTexture();

        var format = SpoutUtils.DxgiToVeldridPixelFormat(_spoutReceiver.SenderTextureFormat);
        var width = _spoutReceiver.SenderTextureWidth;
        var height = _spoutReceiver.SenderTextureHeight;

        Logger.Log($"Spout2 Texture info: {format}, {width}x{height}");

        var texture = _renderer.CreateTexture(new VeldridSharedTexture(
            _renderer,
            spoutReceivedTexture,
            width,
            height,
            format
        ), WrapMode.ClampToBorder, WrapMode.ClampToBorder);

        Texture = texture;

        TextureUpdated?.Invoke(texture);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spoutReceiver?.Dispose();
        }

        base.Dispose(disposing);
    }
}