using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Screens;
using VCollab.Drawables.Spout;
using VCollab.Utils.Extensions;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Screens;

public partial class WelcomeSetupScreen : FadingScreen
{
    [Resolved]
    private VCollabSettings Settings { get; set; } = null!;

    private TextInput _nameTextInput = null!;

    [BackgroundDependencyLoader]
    private void Load()
    {
        AddInternal(new FillFlowContainer()
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Vertical,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Spacing = new Vector2(0f, 5f),
            Children =
            [
                new SpriteText
                {
                    Margin = new MarginPadding { Left = -2 },
                    Font = FontUsage.Default.With(size: 28f),
                    Colour = Colors.Primary,
                    Text = "Welcome to VCollab"
                },
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20f),
                    Colour = Colors.TextLight,
                    Text = "Please start by entering a username. You will then be able to configure your model."
                },
                _nameTextInput = new TextInput(Colors.Primary)
                {
                    Margin = new MarginPadding(0, 8),
                    LengthLimit = 30,
                    PlaceholderText = "Name"
                },
                new RectangleTextButton(Colors.Primary, "Confirm")
                {
                    Margin = new MarginPadding { Top = 2 },
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Action = ConfirmButtonClicked
                }
            ]
        });
    }

    private void ConfirmButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(_nameTextInput.Text))
        {
            return;
        }

        Settings.UserName = _nameTextInput.Text;
        Settings.Save();

        // Replace current welcome screen with setup screen
        var parentScreen = this.GetParentScreen();
        this.Exit();

        parentScreen.Push(new CaptureSetupScreen());
    }
}