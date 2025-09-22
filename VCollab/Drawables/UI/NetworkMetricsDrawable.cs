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
        Size = new Vector2(320, 142);

        var arrowUpTexture = textureStore.Get("network-arrow-up");
        var arrowDownTexture = textureStore.Get("network-arrow-down");

        var arrowUp = () => new Sprite
        {
            Margin = new MarginPadding { Left = 1, Top = 1, Right = 6 },
            Size = new Vector2(18, 18),

            Texture = arrowUpTexture
        };
        var arrowDown = () => new Sprite
        {
            Margin = new MarginPadding { Left = 1, Top = 1 },
            Size = new Vector2(18, 18),

            Texture = arrowDownTexture
        };

        var newLine = () => new Box
        {
            RelativeSizeAxes = Axes.X,
            Width = 1,
            Colour = Color4.Transparent
        };

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
                Direction = FillDirection.Full,
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
                        Text = "Network metrics"
                    },

                    // Latency
                    new SpriteText
                    {
                        Margin = new MarginPadding { Right = 2 },
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.Primary,
                        Text = "Latency: "
                    },
                    _latencyValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },

                    // New line
                    newLine(),

                    // Bitrate
                    new SpriteText
                    {
                        Margin = new MarginPadding { Right = 15 },
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.Primary,
                        Text = "Bitrate:"
                    },
                    _bitrateUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowUp(),
                    _bitrateDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowDown(),

                    // New line
                    newLine(),

                    // Packets
                    new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.Primary,
                        Text = "Packets: "
                    },
                    _packetsUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowUp(),
                    _packetsDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowDown(),

                    // New line
                    newLine(),

                    // Frames
                    new SpriteText
                    {
                        Margin = new MarginPadding { Right = 3 },
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.Primary,
                        Text = "Frames: "
                    },
                    _framesUpValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowUp(),
                    _framesDownValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    },
                    arrowDown(),

                    newLine(),
                    new SpriteText
                    {
                        Margin = new MarginPadding { Right = 3 },
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.Primary,
                        Text = "Frameskip: "
                    },
                    _framesSkippedValueText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = Colors.TextLight,
                        Text = "N/A"
                    }
                ]
            },
        ];

        this.WithGlowEffect(Colors.Primary, 12, 8);
    }

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
            _framesSkippedValueText.Text = FramesSkipped.ToString();

            _latencyValueText.Text = $"{Latency} ms";
        }
    }
}