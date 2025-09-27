using System.Diagnostics.CodeAnalysis;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;
using VCollab.Networking;
using VCollab.Services;
using VCollab.Utils.Graphics;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;
    [Resolved]
    private NetworkManager NetworkManager { get; set; } = null!;
    [Resolved]
    private DiscordRpcService DiscordRpcService { get; set; } = null!;
    [Resolved]
    private LogsSenderService LogsSenderService { get; set; } = null!;

    private SpoutSenderContainer _modelsCanvas = null!;
    private SpoutTextureReceiver _userModelSpoutReceiver = null!;
    private DraggableResizableSprite _userResizableSprite = null!;
    private SpoutSprite _userSpoutSprite = null!;
    private FrameTextureReader _userModelReader = null!;
    private RoomManageDrawable _roomManageUI = null!;

    private Timer _periodicSaveTimer = null!;
    private bool _savingSettings = false;

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
                // Main model texture receiver
                _userModelSpoutReceiver = new SpoutTextureReceiver(),

                // User model reader, this will capture the source Spout2 texture to send it over network
                _userModelReader = new NetworkSendFrameTextureReader(_userModelSpoutReceiver),

                // Models canvas is a Spout sender
                _modelsCanvas = new SpoutSenderContainer("VCollab")
                {
                    RelativeSizeAxes = Axes.Both,

                    Children =
                    [
                        _userResizableSprite = new DraggableResizableSprite(
                            _userSpoutSprite = new SpoutSprite(_userModelSpoutReceiver),
                            Settings.UserModelSettings.Scale
                        )
                    ]
                },

                // Room UI
                _roomManageUI = new RoomManageDrawable
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Margin = new MarginPadding(12)
                },

                new NetworkMetricsDrawable
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Margin = new MarginPadding(10)
                },

                // Temporary buttons
                new CircularSolidButton
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    BackgroundColour = Colors.Primary,
                    Clicked = () => this.Push(new CaptureSetupScreen())
                },
                new CircularSolidButton
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    BackgroundColour = Colors.Tertiary,
                    Clicked = host.Exit
                },
                new CircularSolidButton
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    BackgroundColour = Colors.Alt1,
                    Clicked = LogsSenderService.SendLogs
                },

                new FrameCountDisplay()
            ]
        });

        // Update user model draw texture and read
        UpdateUserModel();
        UpdateUserModelTextureReader();

        // Set user model offset and scale from settings
        _userResizableSprite.Position = Settings.UserModelSettings.PositionOffset;

        // Settings save task
        _periodicSaveTimer = new Timer(OnPeriodicSave, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        NetworkManager.NewNetworkFrameConsumer += OnNewNetworkFrameConsumer;
        DiscordRpcService.RoomJoined += DiscordRoomJoined;
    }

    private void OnNewNetworkFrameConsumer(INetworkFrameConsumer networkFrameConsumer)
    {
        if (networkFrameConsumer is Sprite drawableFrameConsumer)
        {
            Scheduler.Add(() => _modelsCanvas.Add(new DraggableResizableSprite(drawableFrameConsumer)
            {
                X = DrawWidth  * .3f,
                Y = DrawHeight * .3f
            }));
        }
    }

    private void DiscordRoomJoined()
    {
        _roomManageUI.Expire();
    }

    public override void OnResuming(ScreenTransitionEvent e)
    {
        base.OnResuming(e);

        // Update spout receiver sender name since settings could have changed there
        UpdateUserModel();
        UpdateUserModelTextureReader();
    }

    private void OnPeriodicSave(object? _)
    {
        // Only save settings if a save is not already ongoing
        if (Interlocked.CompareExchange(ref _savingSettings, true, false))
        {
            return;
        }

        Settings.UserModelSettings.Scale = _userResizableSprite.ScaleFactor;
        Settings.UserModelSettings.PositionOffset = _userResizableSprite.Position;

        Settings.Save();

        _savingSettings = false;
    }

    private void UpdateUserModel()
    {
        if (!string.IsNullOrWhiteSpace(Settings.SpoutSourceSettings?.SenderName))
        {
            var sourceSettings = Settings.SpoutSourceSettings;

            _userModelSpoutReceiver.SenderName = sourceSettings.SenderName;

            var textureRectangle = new RectangleF(
                sourceSettings.OffsetX,
                sourceSettings.OffsetY,
                sourceSettings.RelativeWidth,
                sourceSettings.RelativeHeight
            );
            var textureSize = new Vector2(sourceSettings.TextureWidth, sourceSettings.TextureHeight);

            _userSpoutSprite.UpdateTextureRectangle(textureRectangle, textureSize);
        }
    }

    private void UpdateUserModelTextureReader()
    {
        if (!string.IsNullOrWhiteSpace(Settings.SpoutSourceSettings?.SenderName))
        {
            var sourceSettings = Settings.SpoutSourceSettings;

            var offsetX = (uint) Math.Round(sourceSettings.OffsetX * sourceSettings.TextureWidth, MidpointRounding.AwayFromZero);
            var offsetY = (uint) Math.Round(sourceSettings.OffsetY * sourceSettings.TextureHeight, MidpointRounding.AwayFromZero);
            var width = (uint) Math.Round(sourceSettings.RelativeWidth * sourceSettings.TextureWidth, MidpointRounding.AwayFromZero);
            var height = (uint) Math.Round(sourceSettings.RelativeHeight * sourceSettings.TextureHeight, MidpointRounding.AwayFromZero);

            _userModelReader.TextureRegion = new TextureRegion(offsetX, offsetY, width, height);
        }
    }

    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _periodicSaveTimer?.Dispose();

            OnPeriodicSave(null);

            NetworkManager.NewNetworkFrameConsumer -= OnNewNetworkFrameConsumer;
            DiscordRpcService.RoomJoined -= DiscordRoomJoined;
        }

        base.Dispose(isDisposing);
    }
}