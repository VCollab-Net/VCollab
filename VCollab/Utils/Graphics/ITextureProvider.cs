using osu.Framework.Graphics.Textures;

namespace VCollab.Utils.Graphics;

public interface ITextureProvider
{
    public Texture? Texture { get; }
}