using osu.Framework.Platform;
using osu.Framework.Screens;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
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
    }
}