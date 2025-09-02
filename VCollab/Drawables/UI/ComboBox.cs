

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Textures;
using VCollab.Utils.Extensions;

namespace VCollab.Drawables.UI;

public partial class ComboBox<TItem> : CompositeDrawable where TItem : notnull
{
    public event Action<TItem?>? SelectionChanged;

    private FillFlowContainer _optionList = null!;
    private Container _dropdownContainer = null!;
    private SpriteText _selectedLabel = null!;

    private bool _isOpen;

    public IReadOnlyList<TItem> Items { get; }

    private TItem? _selectedItem;
    private Box _backgroundBox = null!;

    public TItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value?.Equals(_selectedItem) == true)
            {
                // Do nothing if value did not change
                return;
            }

            _selectedItem = value;
            _selectedLabel.Text = value?.ToString() ?? string.Empty;

            SelectionChanged?.Invoke(value);
        }
    }

    public ComboBox(IEnumerable<TItem> items)
    {
        Items = new List<TItem>(items);
    }

    [BackgroundDependencyLoader]
    private void Load(TextureStore textureStore)
    {
        var arrowDownTexture = textureStore.Get("chevron-down-48");

        AutoSizeAxes = Axes.Y;
        RelativeSizeAxes = Axes.X;

        InternalChildren =
        [
            // Header
            new ClickableContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 30,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,

                    Children =
                    [
                        _backgroundBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colors.Secondary.Opacity(.2f)
                        },
                        _selectedLabel = new SpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Margin = new MarginPadding { Left = 8 },
                            Colour = Colors.TextLight
                        },
                        new Sprite
                        {
                            Size = new Vector2(24, 24),

                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Margin = new MarginPadding { Right = 6, Bottom = 2},

                            Colour = Colors.TextLight,
                            Texture = arrowDownTexture
                        }
                    ]
                }.WithGlowEffect(Colors.Secondary, 6),
                Action = ToggleDropdown
            },

            // Dropdown
            _dropdownContainer = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Y = 35,
                Masking = false,
                CornerRadius = 4,

                Alpha = 0, // Initially hidden

                Child = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = false,
                    Children =
                    [
                        _optionList = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Padding = new MarginPadding(4)
                        }
                    ]
                }
            }
        ];

        // Populate options
        foreach (var item in Items)
        {
            var option = new ClickableContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Child = new ComboBoxItem(item),
                Action = () =>
                {
                    SelectedItem = item;
                    CloseDropdown();
                }
            };

            _optionList.Add(option);
        }

        if (Items.Count > 0)
        {
            SelectedItem = Items[0];
        }
    }

    private void ToggleDropdown()
    {
        if (_isOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    private void OpenDropdown()
    {
        _isOpen = true;
        _dropdownContainer.FadeIn(200, Easing.OutQuint);
    }

    private void CloseDropdown()
    {
        _isOpen = false;
        _dropdownContainer.FadeOut(200, Easing.OutQuint);
    }

    protected override bool OnHover(HoverEvent e)
    {
        _backgroundBox.FadeColour(Colors.Secondary.Opacity(.35f), 300);

        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        _backgroundBox.FadeColour(Colors.Secondary.Opacity(.2f), 300);

        CloseDropdown();

        base.OnHoverLost(e);
    }

    protected override bool OnClick(ClickEvent e)
    {
        // Close if clicking outside
        if (_isOpen && !_dropdownContainer.ReceivePositionalInputAt(e.ScreenSpaceMousePosition))
        {
            CloseDropdown();
        }

        return base.OnClick(e);
    }

    private partial class ComboBoxItem : Container
    {
        private readonly Box _backgroundBox;

        public ComboBoxItem(TItem item)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Margin = new MarginPadding { Bottom = 7 };

            AddRange(
            [
                _backgroundBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colors.Tertiary.Opacity(.2f)
                },
                new SpriteText
                {
                    Margin = new MarginPadding(4) { Left = 8 },
                    Colour = Colors.TextLight,

                    Text = item.ToString() ?? "Please override ToString()",
                }
            ]);

            this.WithGlowEffect(Colors.Tertiary, 6, 6);
        }

        protected override bool OnHover(HoverEvent e)
        {
            _backgroundBox.FadeColour(Colors.Tertiary.Opacity(.35f), 300);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            _backgroundBox.FadeColour(Colors.Tertiary.Opacity(.2f), 300);

            base.OnHoverLost(e);
        }
    }
}