using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;
using VCollab.Utils.Extensions;

namespace VCollab.Screens;

public partial class CaptureSetupScreen : FadingScreen
{
    private ComboBox<string> _senderPicker = null!;
    private SpoutTextureReceiver _spoutTextureReceiver = null!;

    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    private double _lastFetchSendersTime;

    [BackgroundDependencyLoader]
    private void Load()
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
                _spoutTextureReceiver = new SpoutTextureReceiver(),

                new SpoutSprite(_spoutTextureReceiver)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit
                },

                new CircularSolidButton
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    BackgroundColour = Colors.Secondary,
                    Clicked = this.Exit
                },

                // UI Panel
                new Container
                {
                    Size = new Vector2(350, 500),
                    Position = new Vector2(30, 30),

                    Children = [
                        // UI Panel background
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colors.Primary.Opacity(.2f)
                        },

                        // UI panel controls
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding(20),

                            Children = [
                                // Source picker
                                new SpriteText
                                {
                                    Font = FontUsage.Default.With(size: 22),
                                    Margin = new MarginPadding { Left = 2f, Bottom = 8f },

                                    Colour = Colors.Primary,
                                    Text = "Spout source"
                                },
                                _senderPicker = new ComboBox<string>([])
                            ]
                        }
                    ]
                }.WithGlowEffect(Colors.Primary, 20, 12)
            ]
        });

        _senderPicker.SelectionChanged += selection => _spoutTextureReceiver.SenderName = selection;
    }

    protected override void Update()
    {
        base.Update();

        // Regularly update sender list
        if (Time.Current - _lastFetchSendersTime > 1000)
        {
            _senderPicker.SetItem(_spoutTextureReceiver.GetSenderNames());

            _lastFetchSendersTime = Time.Current;
        }
    }
}