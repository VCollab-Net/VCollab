using System.Diagnostics;
using Humanizer;
using LiteNetLib;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Textures;
using VCollab.Utils.Extensions;

namespace VCollab.Drawables.UI;

public partial class NetworkMetricsDrawable : Container
{
    public static NetworkMetricsDrawable? Instance { get; private set; }

    public NetStatistics? NetStatistics { get; set; } = null;
    public static int Latency { get; set; }

    public static long FramesReceived { get; set; } = 0;
    public static long FramesSent { get; set; } = 0;
    public static long FramesSkipped { get; set; } = 0;
    public static int MembersCount { get; set; } = 1;

    private SpriteText _membersCountValueText;

    private SpriteText _latencyValueText = null!;

    private SpriteText _bitrateUpValueText = null!;
    private SpriteText _bitrateDownValueText = null!;

    private SpriteText _packetsUpValueText = null!;
    private SpriteText _packetsDownValueText = null!;

    private SpriteText _framesUpValueText = null!;
    private SpriteText _framesDownValueText = null!;

    private SpriteText _framesSkippedValueText = null!;

    private const string PacketsValueFormat = "F1";
    private readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private readonly Stopwatch _stopwatch = new();
    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private long _lastPacketsReceived = 0;
    private long _lastPacketsSent = 0;
    private long _lastFramesReceived = 0;
    private long _lastFramesSent = 0;

    public NetworkMetricsDrawable()
    {
        Instance = this;
    }

    [BackgroundDependencyLoader]
    private void Load(TextureStore textureStore)
    {
        Size = new Vector2(350, 158);

        var arrowUpTexture = textureStore.Get("network-arrow-up");
        var arrowDownTexture = textureStore.Get("network-arrow-down");

        var arrowUp = () => new Sprite
        {
            Margin = new MarginPadding { Left = 0, Top = 1, Right = 3 },
            Size = new Vector2(18, 18),

            Texture = arrowUpTexture
        };
        var arrowDown = () => new Sprite
        {
            Margin = new MarginPadding { Left = 8, Top = 1, Right = 3 },
            Size = new Vector2(18, 18),

            Texture = arrowDownTexture
        };

        Drawable?[][] gridContent =
        [
            // Participants
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Members:"
                },
                _membersCountValueText = new SpriteText
                {
                    Margin = new MarginPadding { Left = 4 },
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.TextLight,
                    Text = "1"
                },
                null
            ],
            // Latency
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Latency:"
                },
                _latencyValueText = new SpriteText
                {
                    Margin = new MarginPadding { Left = 4 },
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.TextLight,
                    Text = "N/A"
                },
                null
            ],
            // Frameskip
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Skipped:"
                },
                _framesSkippedValueText = new SpriteText
                {
                    Margin = new MarginPadding { Left = 4 },
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.TextLight,
                    Text = "N/A"
                },
                null
            ],
            // Bitrate
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Bitrate:"
                },
                Horizontal
                (
                    arrowUp(),
                    _bitrateUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                ),
                Horizontal
                (
                    arrowDown(),
                    _bitrateDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                )
            ],
            // Packets
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Packets:"
                },
                Horizontal
                (
                    arrowUp(),
                    _packetsUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                ),
                Horizontal
                (
                    arrowDown(),
                    _packetsDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                )
            ],
            // Frames
            [
                new SpriteText
                {
                    Font = FontUsage.Default.With(size: 20),
                    Colour = Colors.Primary,
                    Text = "Frames:"
                },
                Horizontal
                (
                    arrowUp(),
                    _framesUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                ),
                Horizontal
                (
                    arrowDown(),
                    _framesDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                )
            ]
        ];

        Children =
        [
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colors.Primary.Opacity(.2f)
            },

            new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(12, 8),

                Children =
                [
                    // Title
                    new SpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 1f,

                        Font = FontUsage.Default.With(size: 22),
                        Colour = Colors.Primary,
                        Text = "Network Metrics"
                    },

                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions =
                        [
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.AutoSize)
                        ],
                        ColumnDimensions =
                        [
                            new Dimension(GridSizeMode.Relative, .26f),
                            new Dimension(GridSizeMode.Relative, .37f),
                            new Dimension(GridSizeMode.Relative, .37f)
                        ],
                        Content = gridContent
                    }
                ]
            },
        ];

        this.WithGlowEffect(Colors.Primary, 12, 8);
    }

    private static FillFlowContainer Horizontal(params ReadOnlySpan<Drawable> drawables) => new FillFlowContainer
    {
        Direction = FillDirection.Horizontal,
        Children = drawables.ToArray()
    };

    protected override void Update()
    {
        if (NetStatistics is null)
        {
            return;
        }

        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();

            return;
        }

        var elapsed = _stopwatch.Elapsed;
        if (elapsed >= UpdateInterval)
        {
            _stopwatch.Restart();

            var bitrateUp = (NetStatistics.BytesSent - _lastBytesSent) / elapsed.TotalSeconds;
            var bitrateDown = (NetStatistics.BytesReceived - _lastBytesReceived) / elapsed.TotalSeconds;

            var packetsUp = (NetStatistics.PacketsSent - _lastPacketsSent) / elapsed.TotalSeconds;
            var packetsDown = (NetStatistics.PacketsReceived - _lastPacketsReceived) / elapsed.TotalSeconds;

            var framesUp = (FramesSent - _lastFramesSent) / elapsed.TotalSeconds;
            var framesDown = (FramesReceived - _lastFramesReceived) / elapsed.TotalSeconds;

            _lastBytesSent = NetStatistics.BytesSent;
            _lastBytesReceived = NetStatistics.BytesReceived;
            _lastPacketsReceived = NetStatistics.PacketsReceived;
            _lastPacketsSent = NetStatistics.PacketsSent;
            _lastFramesSent = FramesSent;
            _lastFramesReceived = FramesReceived;

            _bitrateUpValueText.Text = $"{bitrateUp.Bytes().ToString()}/s";
            _bitrateDownValueText.Text = $"{bitrateDown.Bytes().ToString()}/s";
            _packetsUpValueText.Text = $"{packetsUp.ToString(PacketsValueFormat)} pk/s";
            _packetsDownValueText.Text = $"{packetsDown.ToString(PacketsValueFormat)} pk/s";
            _framesUpValueText.Text = $"{framesUp.ToString(PacketsValueFormat)} fps";
            _framesDownValueText.Text = $"{framesDown.ToString(PacketsValueFormat)} fps";
            _framesSkippedValueText.Text = $"{FramesSkipped} frames";
            _membersCountValueText.Text = MembersCount.ToString();

            _latencyValueText.Text = $"{Latency} ms";
        }
    }
}