using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;
using VCollab.Utils.Extensions;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Screens;

public partial class CaptureSetupScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    private ComboBox<string> _senderPicker = null!;
    private SpoutTextureReceiver _spoutTextureReceiver = null!;
    private SelectionRectangle _selectionRectangle = null!;
    private SpriteText _selectionTextInfo = null!;
    private SpriteText _spoutTextureInfo = null!;

    private double _lastFetchSendersTime;

    [BackgroundDependencyLoader]
    private void Load()
    {
        AddInternal(new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children =
            [
                _spoutTextureReceiver = new SpoutTextureReceiver(),

                new SpoutSprite(_spoutTextureReceiver)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit
                },

                _selectionRectangle = new SelectionRectangle(
                    new Vector2(350, 600),
                    Colors.Primary,
                    Colors.Secondary
                ),

                // UI Panel
                new Container
                {
                    Size = new Vector2(350, 300),
                    Position = new Vector2(30, 30),

                    Children =
                    [
                        // UI Panel background
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colors.Primary.Opacity(.2f)
                        },

                        // UI panel controls
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children =
                            [
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Padding = new MarginPadding(20),

                                    Children =
                                    [
                                        // Source picker
                                        new SpriteText
                                        {
                                            Font = FontUsage.Default.With(size: 22),
                                            Margin = new MarginPadding { Left = 2f, Bottom = 8f },

                                            Colour = Colors.Primary,
                                            Text = "Spout source"
                                        },
                                        _senderPicker = new ComboBox<string>([]),

                                        // Source info text
                                        new SpriteText
                                        {
                                            Font = FontUsage.Default.With(size: 22),
                                            Margin = new MarginPadding { Left = 2f, Top = 30f },

                                            Colour = Colors.Primary,
                                            Text = "Source resolution"
                                        },
                                        _spoutTextureInfo = new SpriteText
                                        {
                                            Font = FontUsage.Default.With(size: 20),
                                            Margin = new MarginPadding { Left = 2f },

                                            Colour = Colors.TextLight,
                                            Text = "N/A"
                                        },

                                        // Selection info text
                                        new SpriteText
                                        {
                                            Font = FontUsage.Default.With(size: 22),
                                            Margin = new MarginPadding { Left = 2f, Top = 6f },

                                            Colour = Colors.Primary,
                                            Text = "Selection resolution"
                                        },
                                        _selectionTextInfo = new SpriteText
                                        {
                                            Font = FontUsage.Default.With(size: 20),
                                            Margin = new MarginPadding { Left = 2f },

                                            Colour = Colors.TextLight,
                                            Text = "N/A"
                                        }
                                    ]
                                },
                                // Cancel and Confirm buttons
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Anchor = Anchor.BottomRight,
                                    Origin = Anchor.BottomRight,
                                    Padding = new MarginPadding(16),

                                    Children =
                                    [
                                        new RectangleTextButton(Colors.Secondary, "Confirm")
                                        {
                                            Anchor = Anchor.BottomRight,
                                            Origin = Anchor.BottomRight,
                                            Action = ConfirmButtonClicked
                                        },
                                        new RectangleTextButton(Colors.Tertiary, "Cancel")
                                        {
                                            Anchor = Anchor.BottomRight,
                                            Origin = Anchor.BottomRight,
                                            Margin = new MarginPadding { Right = 12 },
                                            Action = CancelButtonClicked
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }.WithGlowEffect(Colors.Primary, 20, 12)
            ]
        });

        _senderPicker.SelectionChanged += selection => _spoutTextureReceiver.SenderName = selection;
        _spoutTextureReceiver.TextureUpdated += SpoutTextureUpdated;
        _selectionRectangle.SelectionChanged += SelectionChanged;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        // Load settings
        var sourceSettings = Settings.SpoutSourceSettings;

        if (sourceSettings is null)
        {
            // Always move selection rectangle to the center of the setup screen on first setup
            _selectionRectangle.Position = new Vector2(
                DrawWidth / 2 - _selectionRectangle.Width / 2,
                DrawHeight / 2 - _selectionRectangle.Height / 2
            );
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(sourceSettings.SenderName))
            {
                _senderPicker.SelectedItem = sourceSettings.SenderName;
            }

            _selectionRectangle.Position = new Vector2(
                (float) Math.Round(sourceSettings.OffsetX * DrawWidth, MidpointRounding.AwayFromZero),
                (float) Math.Round(sourceSettings.OffsetY * DrawHeight, MidpointRounding.AwayFromZero)
            );

            _selectionRectangle.Size = new Vector2(
                (float) Math.Round(sourceSettings.RelativeWidth * DrawWidth, MidpointRounding.AwayFromZero),
                (float) Math.Round(sourceSettings.RelativeHeight * DrawHeight, MidpointRounding.AwayFromZero)
            );
        }

        _selectionTextInfo.Text = $"{_selectionRectangle.Selection.Width}x{_selectionRectangle.Selection.Height}";
    }

    protected override void Update()
    {
        base.Update();

        // Regularly update sender list
        if (Time.Current - _lastFetchSendersTime > 1000)
        {
            _senderPicker.SetItem(_spoutTextureReceiver.GetSenderNames());

            _lastFetchSendersTime = Time.Current;
        }
    }

    private void SpoutTextureUpdated(Texture? texture)
    {
        _spoutTextureInfo.Text = texture is not null
            ? $"{texture.Width}x{texture.Height}"
            : "N/A";
    }

    private void SelectionChanged(RectangleF selection)
    {
        _selectionTextInfo.Text = $"{selection.Width}x{selection.Height}";
    }

    private void ConfirmButtonClicked()
    {
        if (_senderPicker.SelectedItem is null)
        {
            return;
        }

        var selection = _selectionRectangle.Selection;

        // Save changes
        Settings.SpoutSourceSettings = new SpoutSourceSettings(
            _senderPicker.SelectedItem,
            selection.X / (double)DrawWidth,
            selection.Y / (double)DrawHeight,
            selection.Width / (double)DrawWidth,
            selection.Height / (double)DrawHeight
        );

        Settings.Save();

        this.Exit();
    }

    private void CancelButtonClicked()
    {
        this.Exit();
    }
}