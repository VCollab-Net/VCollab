using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Effects;

namespace VCollab.Utils.Extensions;

public static class ContainerExtensions
{
    public static TContainer WithGlowEffect<TContainer>(this TContainer container, Color4 color, float cornerRadius, float glowRadius = 8)
        where TContainer : Container
    {
        container.CornerRadius = cornerRadius;
        container.Masking = true;

        container.BorderThickness = 2;
        container.BorderColour = color.Darken(.5f).Opacity(.8f);

        container.EdgeEffect = new EdgeEffectParameters()
        {
            Type = EdgeEffectType.Glow,
            Colour = color.Opacity(.8f),
            Radius = glowRadius,
            Hollow = true
        };

        return container;
    }
}