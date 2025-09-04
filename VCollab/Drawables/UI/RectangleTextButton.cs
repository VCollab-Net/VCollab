using osu.Framework.Extensions.Color4Extensions;
using VCollab.Utils.Extensions;

namespace VCollab.Drawables.UI;

public partial class RectangleTextButton : Container
{
    private readonly Box _backgroundBox;
    private readonly Color4 _color;
    public Action? Action { get; set; }

    public RectangleTextButton(Color4 color, string text)
    {
        _color = color;

        AutoSizeAxes = Axes.Both;

        Children =
        [
            // Background
            _backgroundBox = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = color.Opacity(.3f)
            },

            // Text
            new SpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Margin = new MarginPadding(8, 4),
                Colour = Colors.TextLight,
                Text = text
            }
        ];

        // Add glow effect
        this.WithGlowEffect(color, 4);
    }

    protected override bool OnClick(ClickEvent e)
    {
        Action?.Invoke();

        return true;
    }

    protected override bool OnHover(HoverEvent e)
    {
        _backgroundBox.FadeColour(_color.Opacity(.5f), 300);

        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        _backgroundBox.FadeColour(_color.Opacity(.3f), 300);

        base.OnHoverLost(e);
    }
}