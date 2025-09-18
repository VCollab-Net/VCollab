using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using VCollab.Utils.Extensions;

namespace VCollab.Drawables.UI;

public partial class TextInput : TextBox
{
    public new const float FontSize = 22f;

    private readonly Box _background;

    public TextInput(Color4 color)
    {
        base.FontSize = FontSize;
        Height = FontSize + 4;
        RelativeSizeAxes = Axes.X;

        AddInternal(_background = new Box()
        {
            RelativeSizeAxes = Axes.Both,
            Colour = color.Opacity(.2f)
        });

        this.WithGlowEffect<Container<Drawable>, Drawable>(color, 4);
    }

    protected override void NotifyInputError()
    {
        // Ignore invalid characters
    }

    protected override SpriteText CreatePlaceholder() => new SpriteText
    {
        Font = FontUsage.Default.With(size: FontSize),
        Colour = Colors.TextLight.Opacity(.6f)
    };

    protected override Drawable GetDrawableCharacter(char c) => new SpriteText
    {
        Text = c.ToString(),
        Font = FontUsage.Default.With(size: FontSize),
        Colour = Colors.TextLight
    };

    protected override Caret CreateCaret()
    {
        var caret = new BasicTextBox.BasicCaret
        {
            CaretWidth = 3f,
            Height = FontSize,
            SelectionColour = Colors.Tertiary.Opacity(.7f)
        };

        if (caret.InternalChild is Container { Child: Box box })
        {
            box.Colour = Colors.Tertiary;
        }

        return caret;
    }
}