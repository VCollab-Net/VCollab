using System.Reflection;
using Veldrid;

namespace VCollab.Utils.Extensions;

public static class ReflectionExtensions
{
    private static readonly PropertyInfo _d3d11TextureDeviceTextureProperty =
        typeof(Texture).Assembly.GetType("Veldrid.D3D11.D3D11Texture")!
            .GetProperty("DeviceTexture", BindingFlags.Instance | BindingFlags.Public)!;

    private static readonly PropertyInfo _id3d11Texture2DNativePointerProperty =
        typeof(Vortice.Direct3D11.AsyncGetDataFlags).Assembly.GetType("Vortice.Direct3D11.ID3D11Texture2D")!
            .GetProperty("NativePointer", BindingFlags.Instance | BindingFlags.Public)!;

    // Veldrid Textures
    public static object GetDeviceTexture(this Texture texture) => _d3d11TextureDeviceTextureProperty.GetValue(texture)!;

    public static IntPtr GetId3d11Texture2DNativePointer(object id3d11Texture2D) =>
        (IntPtr) _id3d11Texture2DNativePointerProperty.GetValue(id3d11Texture2D)!;
}