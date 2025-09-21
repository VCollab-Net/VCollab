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
    public int Latency { get; set; }

    private SpriteText _latencyValueText = null!;

    private SpriteText _bitrateUpValueText = null!;
    private SpriteText _bitrateDownValueText = null!;

    private SpriteText _packetsUpValueText = null!;
    private SpriteText _packetsDownValueText = null!;

    private const string PacketsValueFormat = "F1";
    private readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private readonly Stopwatch _stopwatch = new();
    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private long _lastPacketsReceived = 0;
    private long _lastPacketsSent = 0;

    public NetworkMetricsDrawable()
    {
        Instance = this;
    }

    [BackgroundDependencyLoader]
    private void Load(TextureStore textureStore)
    {
        Size = new Vector2(320, 104);

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
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 1,
                        Colour = Color4.Transparent
                    },

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
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Width = 1,
                        Colour = Color4.Transparent
                    },

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
                    arrowDown()
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

            _lastBytesSent = NetStatistics.BytesSent;
            _lastBytesReceived = NetStatistics.BytesReceived;
            _lastPacketsReceived = NetStatistics.PacketsReceived;
            _lastPacketsSent = NetStatistics.PacketsSent;

            _bitrateUpValueText.Text = $"{bitrateUp.Bytes().ToString()}/s";
            _bitrateDownValueText.Text = $"{bitrateDown.Bytes().ToString()}/s";
            _packetsUpValueText.Text = $"{packetsUp.ToString(PacketsValueFormat)} pk/s";
            _packetsDownValueText.Text = $"{packetsDown.ToString(PacketsValueFormat)} pk/s";

            _latencyValueText.Text = $"{Latency} ms";
        }
    }
}