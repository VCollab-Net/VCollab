using System.Reflection;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.IO.Stores;

namespace VCollab;

public partial class VCollabGame : Game
{
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

        AddRange([
            new Box() { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black },
        ]);
    }
}