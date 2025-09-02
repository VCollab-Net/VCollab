using osu.Framework.Screens;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
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
                    BackgroundColour = Colors.Primary,
                    Clicked = () => this.Push(new CaptureSetupScreen())
                }
            ]
        });
    }
}