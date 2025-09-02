using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Effects;
using osu.Framework.Screens;
using VCollab.Utils.Extensions;

namespace VCollab.Screens;

public partial class CaptureSetupScreen : FadingScreen
{
    private ComboBox<string> _senderPicker = null!;

    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void Load()
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
                new CircularSolidButton
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
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
                                _senderPicker = new ComboBox<string>(["First item", "Second item", "Third item"])
                            ]
                        }
                    ]
                }.WithGlowEffect(Colors.Primary, 20, 12)
            ]
        });
    }
}