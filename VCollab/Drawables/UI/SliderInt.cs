using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Layout;
using VCollab.Utils.Extensions;
using Vector2 = osuTK.Vector2;

namespace VCollab.Drawables.UI;

public sealed partial class SliderInt : CompositeDrawable
{
    private const float TrackHeight = 35f;
    private const float ThumbWidth = 50f;

    public int Value
    {
        get => _minValue + (int)((_maxValue - _minValue) * _value);
        set
        {
            _value = (value - _minValue) / ((float)_maxValue - _minValue);

            _valueText.Text = Value.ToString();

            UpdateThumbPosition();
        }
    }

    private readonly Container _track;
    private readonly Container _thumb;
    private readonly SpriteText _valueText;

    private readonly int _minValue;
    private readonly int _maxValue;

    private float _value; // 0..1

    public SliderInt(int minValue, int maxValue, int initialValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;

        InternalChildren =
        [
            _track = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colors.Primary.Opacity(.2f)
                }
            }.WithGlowEffect(Colors.Primary, 8f),
            _thumb = new Container
            {
                Size = new Vector2(ThumbWidth, TrackHeight),
                Padding = new MarginPadding(4f),
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 6f,
                    Children =
                    [
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colors.Primary.Opacity(.7f)
                        },
                        _valueText = new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = FontUsage.Default.With(size: 18f, weight: "Bold"),
                            Colour = Colors.TextLight
                        }
                    ]
                }
            }
        ];

        RelativeSizeAxes = Axes.X;
        Height = TrackHeight;

        Value = initialValue;
    }

    protected override bool OnDragStart(DragStartEvent e)
    {
        return true; // accept drag
    }

    protected override void OnDrag(DragEvent e)
    {
        UpdateValueFromPosition(e.ScreenSpaceMousePosition.X);
    }

    protected override bool OnClick(ClickEvent e)
    {
        UpdateValueFromPosition(e.ScreenSpaceMousePosition.X);

        return true;
    }

    protected override void OnSizingChanged()
    {
        UpdateThumbPosition();
    }

    protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
    {
        UpdateThumbPosition();

        return base.OnInvalidate(invalidation, source);
    }

    private void UpdateValueFromPosition(float screenX)
    {
        var localX = ToLocalSpace(new Vector2(screenX, 0)).X - (ThumbWidth / 2f);
        var usableWidth = DrawWidth - ThumbWidth;

        _value = Math.Clamp(usableWidth <= 0 ? 0 : localX / usableWidth, 0f, 1f);

        _valueText.Text = Value.ToString();

        UpdateThumbPosition();
    }

    private void UpdateThumbPosition()
    {
        float usableWidth = DrawWidth - ThumbWidth;

        if (usableWidth >= 0)
        {
            _thumb.X = Math.Clamp(_value * usableWidth, 0f, DrawWidth);
        }
    }
}