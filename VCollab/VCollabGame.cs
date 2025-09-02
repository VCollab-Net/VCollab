using System.Reflection;

using osu.Framework.IO.Stores;
using osu.Framework.Screens;
using VCollab.Screens;

namespace VCollab;

public partial class VCollabGame : Game
{
    private ScreenStack _screenStack = null!;

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
        var settings = VCollabSettings.Load();

        dependencies.Cache(settings);

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
        Host.MaximumUpdateHz = 120;
        Host.MaximumDrawHz = 60;
        Host.MaximumInactiveHz = 60;

        _screenStack = new ScreenStack();
        _screenStack.Push(new MainScreen());

        AddRange([
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colors.Background
            },

            _screenStack
        ]);
    }
}