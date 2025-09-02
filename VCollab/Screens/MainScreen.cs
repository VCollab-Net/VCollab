using osu.Framework.Screens;

namespace VCollab.Screens;

public partial class MainScreen : FadingScreen
{
    [BackgroundDependencyLoader]
    private void Load()
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                new CircularSolidButton
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    BackgroundColour = Color4.DarkSlateGray,
                    Clicked = () => this.Push(new CaptureSetupScreen())
                }
            ]
        });
    }
}