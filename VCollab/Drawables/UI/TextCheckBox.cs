using osu.Framework.Extensions.Color4Extensions;
using VCollab.Utils.Extensions;

namespace VCollab.Drawables.UI;

public sealed partial class TextCheckBox : CompositeDrawable
{
    public string Text
    {
        get => _spriteText.Text.ToString();
        set => _spriteText.Text = value;
    }

    public bool Checked { get; private set; } = false;

    private readonly Box _backgroundBox;
    private readonly Box _checkBox;
    private readonly SpriteText _spriteText;
    private readonly Color4 _color;

    public TextCheckBox(Color4 color, bool initialValue)
    {
        _color = color;
        Checked = initialValue;

        AutoSizeAxes = Axes.Both;

        InternalChild = new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(10, 0),

            // Checkbox container
            Children =
            [
                new Container
                {
                    Size = new Vector2(24f),

                    Children =
                    [
                        _backgroundBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = color.Opacity(.2f)
                        },
                        new Container
                        {
                            Masking = true,
                            CornerRadius = 3f,
                            Size = new Vector2(18f),
                            Margin = new MarginPadding(3f),

                            Child = _checkBox = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = color.Opacity(.9f).Lighten(.2f)
                            }
                        }
                    ]
                }.WithGlowEffect(color, 5f),

                // Associated text
                _spriteText = new SpriteText
                {
                    Margin = new MarginPadding(0, 2),
                    Font = FontUsage.Default.With(size: 20f),
                    Colour = Colors.TextLight,
                    Text = ""
                }
            ],
        };

        if (!Checked)
        {
            _checkBox.Hide();
        }
    }

    protected override bool OnClick(ClickEvent e)
    {
        Toggle();

        return true;
    }

    protected override bool OnHover(HoverEvent e)
    {
        _backgroundBox.FadeColour(_color.Opacity(.45f), 300);
        _checkBox.FadeColour(_color.Opacity(1f).Lighten(.25f), 300);

        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        _backgroundBox.FadeColour(_color.Opacity(.2f), 300);
        _checkBox.FadeColour(_color.Opacity(.9f).Lighten(.2f), 300);

        base.OnHoverLost(e);
    }

    private void Toggle()
    {
        Checked = !Checked;

        if (Checked)
        {
            _checkBox.Show();
        }
        else
        {
            _checkBox.Hide();
        }
    }
}