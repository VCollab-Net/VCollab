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

        Add(_screenStack);
    }
}