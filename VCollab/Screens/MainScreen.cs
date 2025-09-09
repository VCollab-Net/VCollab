using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;
using VCollab.Utils.Graphics;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    private SpoutSenderContainer _modelsCanvas = null!;
    private SpoutTextureReceiver _userModelSpoutReceiver = null!;
    private DraggableResizableSprite _userResizableSprite = null!;
    private SpoutSprite _userSpoutSprite = null!;
    private JpegFrameTextureReader _userModelReader = null!;

    private Timer _periodicSaveTimer = null!;

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
                _userModelReader = new JpegFrameTextureReader(_userModelSpoutReceiver),

                // Models canvas is a Spout sender
                _modelsCanvas = new SpoutSenderContainer("VCollab")
                {
                    RelativeSizeAxes = Axes.Both,

                    Child = _userResizableSprite = new DraggableResizableSprite(
                        _userSpoutSprite = new SpoutSprite(_userModelSpoutReceiver),
                        Settings.UserModelSettings.Scale
                    )
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
    }

    public override void OnResuming(ScreenTransitionEvent e)
    {
        base.OnResuming(e);

        // Update spout receiver sender name since settings could have changed there
        UpdateUserModel();
        UpdateUserModelTextureReader();
    }

    private void OnPeriodicSave(object? state)
    {
        Settings.UserModelSettings.Scale = _userResizableSprite.ScaleFactor;
        Settings.UserModelSettings.PositionOffset = _userResizableSprite.Position;

        Settings.Save();
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

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _periodicSaveTimer.Dispose();
        }

        base.Dispose(isDisposing);
    }
}