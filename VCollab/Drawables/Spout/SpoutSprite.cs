using osu.Framework.Graphics.Primitives;
using osu.Framework.Layout;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace VCollab.Drawables.Spout;

public partial class SpoutSprite : Sprite
{
    private readonly SpoutTextureReceiver _spoutTextureReceiver;

    private Texture? _newTexture;
    private bool _needsTextureUpdate = false;

    public SpoutSprite(SpoutTextureReceiver spoutTextureReceiver)
    {
        _spoutTextureReceiver = spoutTextureReceiver;

        // Make sure this sprite always fill its container and keep aspect ratio
        RelativeSizeAxes = Axes.Both;
        FillMode = FillMode.Fill;

        _spoutTextureReceiver.TextureUpdated += OnTextureUpdated;
    }

    public void UpdateTextureRectangle(RectangleF rectangle, Vector2 textureSize)
    {
        var widthFactor = 1 / rectangle.Width;
        var heightFactor = 1 / rectangle.Height;

        TextureRectangle = new RectangleF(-rectangle.X * widthFactor, -rectangle.Y * heightFactor, widthFactor, heightFactor);

        // Required to properly update parent size
        if (Parent is DraggableResizableSprite)
        {
            var width = (float) Math.Round(textureSize.X * rectangle.Width, MidpointRounding.AwayFromZero);
            var height = (float) Math.Round(textureSize.Y * rectangle.Height, MidpointRounding.AwayFromZero);

            Parent.Size = new Vector2(width, height);
            Parent.Invalidate(Invalidation.DrawSize, InvalidationSource.Child);
        }
    }

    private void OnTextureUpdated(Texture? texture)
    {
        _newTexture = texture;
        _needsTextureUpdate = true;
    }

    protected override void Update()
    {
        if (_needsTextureUpdate)
        {
            Texture = _newTexture;
            Invalidate(Invalidation.DrawNode);

            _needsTextureUpdate = false;
        }

        base.Update();
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