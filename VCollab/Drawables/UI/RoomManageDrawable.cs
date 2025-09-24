using osu.Framework.Logging;
using TextCopy;
using VCollab.Networking;
using VCollab.Utils;

namespace VCollab.Drawables.UI;

public partial class RoomManageDrawable : FillFlowContainer
{
    [Resolved]
    protected NetworkManager NetworkManager { get; private set; } = null!;

    [Resolved]
    private VCollabSettings VCollabSettings { get; set; } = null!;

    private readonly FillFlowContainer _buttonsContainer;

    public RoomManageDrawable()
    {
        AutoSizeAxes = Axes.Y;
        Width = 200;
        Direction = FillDirection.Vertical;

        AddRangeInternal
        ([
            new SpriteText
            {
                Font = FontUsage.Default.With(size: 26),
                Margin = new MarginPadding { Left = 4f, Bottom = 6f},

                Colour = Colors.Primary,
                Text = "Room management"
            },

            _buttonsContainer = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                Height = 30,
                Direction = FillDirection.Horizontal,

                Children =
                [
                    new RectangleTextButton(Colors.Primary, "Host")
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Width = .5f,
                        Margin = new MarginPadding(6, 0),
                        Action = HostButtonClicked
                    },
                    new RectangleTextButton(Colors.Secondary, "Join")
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Width = .5f,
                        Margin = new MarginPadding(6, 0),
                        Action = JoinButtonClicked
                    }
                ]
            }
        ]);
    }

    // Create new room by generating a new random token and copy it to clipboard
    private void HostButtonClicked()
    {
        var name = VCollabSettings.UserName;

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var roomToken = RoomTokenUtils.GenerateToken();

        if (NetworkManager.StartAsHost(name, roomToken))
        {
            // Replace buttons with a "copy link" button to allow copying the token
            _buttonsContainer.Children =
            [
                new RectangleTextButton(Colors.Primary, "Copy invite")
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.None,
                    Width = 212,
                    Margin = new MarginPadding(6, 0),
                    Action = CopyInviteToClipboard
                }
            ];

            CopyInviteToClipboard();
        }
    }

    // Join a room by using the token in the clipboard
    private void JoinButtonClicked()
    {
        var name = VCollabSettings.UserName;

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var roomToken = ClipboardService.GetText();

        if (!RoomTokenUtils.IsValidToken(roomToken))
        {
            // TODO Notify user of wrong/no invite in clipboard
            Logger.Log("Could not find a valid token in clipboard", LoggingTarget.Runtime, LogLevel.Important);

            return;
        }

        if (NetworkManager.StartAsPeer(name, roomToken))
        {
            // Remove all UI after joining a room
            Expire();
        }
    }

    private void CopyInviteToClipboard()
    {
        // TODO Add "feedback notification" to notify the user the invite has been successfully copied to clipboard
        if (!string.IsNullOrWhiteSpace(NetworkManager.RoomToken))
        {
            ClipboardService.SetText(NetworkManager.RoomToken);
        }
    }
}