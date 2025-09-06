using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    private SpoutSenderContainer _modelsCanvas = null!;
    private SpoutTextureReceiver _userModelSpoutReceiver = null!;
    private DraggableResizableSprite _userResizableSprite = null!;
    private SpoutSprite _userSpoutSprite = null!;

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
                }
            ]
        });

        // Update user model draw texture
        UpdateUserModel();

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

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _periodicSaveTimer.Dispose();
        }

        base.Dispose(isDisposing);
    }
}