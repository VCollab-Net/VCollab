using osu.Framework.Layout;
using osuTK.Input;

namespace VCollab.Drawables;

public partial class DraggableResizableSprite : CompositeDrawable
{
    private const float MinimumSize = 64f;

    public float ScaleFactor => _scale;

    private Vector2 _sizeInPixels;
    private float _scale;

    private Vector2 _dragOffset;

    public DraggableResizableSprite(Sprite sprite, float initialScale = 1f)
    {
        // Initial size, should not be used as size should always be set by the child sprite
        _sizeInPixels = new Vector2(400, 400);
        _scale = initialScale;

        UpdateSize();

        Masking = true;

        InternalChild = sprite;
    }

    private void UpdateSize()
    {
        var newSize = _sizeInPixels * _scale;

        // Make sure new size is not too small without breaking aspect ratio (it would be impossible to scale it back up)
        if (newSize is { X: < MinimumSize } or { Y: < MinimumSize })
        {
            return;
        }

        this.ResizeTo(newSize, 400, Easing.OutQuint);
    }

    protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
    {
        // This is an invalidation from the sprite we're drawing, make sure we adapt to the new size
        if (invalidation is Invalidation.DrawSize && source is InvalidationSource.Child)
        {
            _sizeInPixels = Size;

            UpdateSize();
        }

        return base.OnInvalidate(invalidation, source);
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != MouseButton.Left)
        {
            return false;
        }

        var mouseInParent = Parent!.ToLocalSpace(e.ScreenSpaceMousePosition);

        _dragOffset = Position - mouseInParent;

        return true;
    }

    protected override bool OnDragStart(DragStartEvent e) => true;

    protected override void OnDrag(DragEvent e)
    {
        var mouseInParent = Parent!.ToLocalSpace(e.ScreenSpaceMousePosition);

        Position = mouseInParent + _dragOffset;

        base.OnDrag(e);
    }

    protected override bool OnScroll(ScrollEvent e)
    {
        const float step = 0.10f;

        var newScale = _scale + e.ScrollDelta.Y * step;

        if (newScale <= 0f)
        {
            return true;
        }

        _scale = newScale;

        UpdateSize();

        return true;
    }
}