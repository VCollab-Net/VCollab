using osu.Framework.Platform;
using osu.Framework.Threading;

namespace VCollab.Drawables;

public partial class FrameCountDisplay : CompositeDrawable
{
    private SpriteText _frameIndexSprite = null!;
    private DrawThread _drawThread = null!;

    [BackgroundDependencyLoader]
    private void Load(GameHost host)
    {
        _drawThread = host.DrawThread;

        Anchor = Anchor.TopCentre;
        Origin = Anchor.TopCentre;

        AddInternal(
            _frameIndexSprite = new SpriteText
            {
                Colour = Colors.Primary
            }
        );
    }

    protected override void Update()
    {
        base.Update();

        _frameIndexSprite.Text = _drawThread.FrameIndex.ToString();
    }
}