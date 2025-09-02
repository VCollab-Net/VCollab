using osu.Framework.Screens;

namespace VCollab.Screens;

public abstract partial class FadingScreen : Screen
{
    public override void OnEntering(ScreenTransitionEvent e)
    {
        this.FadeInFromZero(600, Easing.None);

        base.OnEntering(e);
    }

    public override bool OnExiting(ScreenExitEvent e)
    {
        this.FadeOut(600, Easing.None);

        return base.OnExiting(e);
    }
}