using System.Reflection;

using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using VCollab.Networking;
using VCollab.Screens;
using VCollab.Services;

namespace VCollab;

public partial class VCollabGame : Game
{
    private ScreenStack _screenStack = null!;
    private NetworkManager? _networkManager = null;
    private VCollabSettings _settings = null!;
    private DiscordRpcService _discordRpcService = null!;
    private LogsSenderService _logsSenderService = null!;

    [BackgroundDependencyLoader]
    private void Load()
    {
        // Make embedded resources available to TextureStore / Audio etc.
        Resources.AddStore(
            new NamespacedResourceStore<byte[]>(
                new DllResourceStore(Assembly.GetExecutingAssembly()),
                "Resources"
            )
        );
    }

    protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
    {
        var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        // Read settings
        _settings = VCollabSettings.Load(Host.Storage);

        dependencies.Cache(_settings);

        // Network manager
        _networkManager = new NetworkManager(Host);

        dependencies.Cache(_networkManager);

        // Discord Rich Presence service
        _discordRpcService = new DiscordRpcService(_networkManager, _settings);

        dependencies.Cache(_discordRpcService);

        // Logs sender service
        _logsSenderService = new LogsSenderService(Host.Storage, _settings);

        dependencies.Cache(_logsSenderService);

        return dependencies;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        Logger.Log($"Starting VCollab version {ThisAssembly.AssemblyInformationalVersion} built in {ThisAssembly.AssemblyConfiguration} configuration");

        // This ensures the window opens maximized right away
        if (Host.Window != null)
        {
            Host.Window.WindowState = osu.Framework.Platform.WindowState.FullscreenBorderless;
        }

        // Set sensible default values for framerate
        Host.Renderer.VerticalSync = false;
        Host.MaximumUpdateHz = 120;
        Host.UpdateThread.InactiveHz = 120;
        Host.MaximumDrawHz = 60;
        Host.DrawThread.InactiveHz = 60;

        _screenStack = new ScreenStack()
        {
            Name = "MainScreenStack"
        };
        _screenStack.Push(new MainScreen());

        // Show welcome screen if we detect it's a first-time boot
        if (string.IsNullOrWhiteSpace(_settings.UserName))
        {
            _screenStack.Push(new WelcomeSetupScreen());
        }

        AddRange([
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colors.Background
            },

            _screenStack
        ]);
    }

    public override void SetHost(GameHost host)
    {
        base.SetHost(host);

        var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType(), "App.ico");
        if (iconStream is not null)
        {
            host.Window.SetIconFromStream(iconStream);
        }

        host.Window.Title = Name;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _networkManager?.Dispose();
        }

        base.Dispose(disposing);
    }
}