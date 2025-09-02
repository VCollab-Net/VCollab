using osu.Framework.Screens;

namespace VCollab.Screens;

public abstract partial class FadingScreen : Screen
{
    public override void OnEntering(ScreenTransitionEvent e)
    {
        this.FadeInFromZero(400, Easing.OutQuint);

        base.OnEntering(e);
    }

    public override bool OnExiting(ScreenExitEvent e)
    {
        this.FadeOut(400, Easing.OutQuint);

        return base.OnExiting(e);
    }
}