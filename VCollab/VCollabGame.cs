using System.Reflection;

using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Screens;
using VCollab.Networking;
using VCollab.Screens;

namespace VCollab;

public partial class VCollabGame : Game
{
    private ScreenStack _screenStack = null!;
    private NetworkManager? _networkManager = null;
    private VCollabSettings _settings = null!;

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

        return dependencies;
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

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