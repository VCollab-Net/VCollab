using Texture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Drawables.Spout;

public partial class SpoutSprite : Sprite
{
    private readonly SpoutTextureReceiver _spoutTextureReceiver;

    public SpoutSprite(SpoutTextureReceiver spoutTextureReceiver)
    {
        _spoutTextureReceiver = spoutTextureReceiver;

        _spoutTextureReceiver.TextureUpdated += OnTextureUpdated;
    }

    private void OnTextureUpdated(Texture? texture)
    {
        Texture = texture;

        Invalidate(Invalidation.DrawNode);
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _spoutTextureReceiver.TextureUpdated -= OnTextureUpdated;
        }

        base.Dispose(isDisposing);
    }
}