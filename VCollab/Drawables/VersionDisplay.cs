using osu.Framework.Platform;
using osu.Framework.Threading;

namespace VCollab.Drawables;

public partial class VersionDisplay : CompositeDrawable
{
    [BackgroundDependencyLoader]
    private void Load()
    {
        AutoSizeAxes = Axes.Both;
        Anchor = Anchor.TopCentre;
        Origin = Anchor.TopCentre;

        AddInternal(
            new SpriteText
            {
                Colour = Colors.Primary,
                Text = $"VCollab v{ThisAssembly.AssemblyInformationalVersion}"
            }
        );
    }
}