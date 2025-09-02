using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;

namespace VCollab.Drawables.UI;

public partial class CircularSolidButton : CompositeDrawable
{
    public Action? Clicked { get; set; }

    public Color4? BackgroundColour
    {
        get => _background?.Colour;
        set
        {
            if (value is { } color)
            {
                _background!.Colour = color;
                _circleContainer!.EdgeEffect = GenerateEdgeEffect();
                _circleContainer.BorderColour = color.Darken(.2f);
                _circleContainer.BorderThickness = 3;
            }
        }
    }

    private readonly Box? _background;
    private readonly CircularContainer? _circleContainer;

    public CircularSolidButton()
    {
        Size = new Vector2(64); // Default diameter

        InternalChild = new ClickableContainer
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            RelativeSizeAxes = Axes.Both,
            Action = () => Clicked?.Invoke(),

            Child = _circleContainer = new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Masking = true, // Enforces the circular clipping
                EdgeEffect = GenerateEdgeEffect(),

                Children =
                [
                    _background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White
                    }
                ]
            }
        };
    }

    private EdgeEffectParameters GenerateEdgeEffect() => new()
    {
        Type = EdgeEffectType.Glow,
        Colour = BackgroundColour ?? Color4.White,
        Radius = 8,
        Roundness = 1
    };

    protected override bool OnHover(HoverEvent e)
    {
        _circleContainer.ScaleTo(1.05f, 150, Easing.OutQuint);
        return true;
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        _circleContainer.ScaleTo(1f, 150, Easing.OutQuint);

        base.OnHoverLost(e);
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        _circleContainer.ScaleTo(0.95f, 80, Easing.OutQuint);

        return base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseUpEvent e)
    {
        _circleContainer.ScaleTo(IsHovered ? 1.05f : 1f, 100, Easing.OutQuint);

        base.OnMouseUp(e);
    }
}