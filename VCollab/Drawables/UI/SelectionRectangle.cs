using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Primitives;
using osuTK.Input;

namespace VCollab.Drawables.UI;

public partial class SelectionRectangle : CompositeDrawable
{
    public event Action<RectangleF>? SelectionChanged;

    public float MinWidth { get; set; } = 110f;
    public float MinHeight { get; set; } = 110f;

    public bool ClampToParentBounds { get; set; } = true;

    public float HandleThickness
    {
        get => _handleThickness;
        set
        {
            _handleThickness = value;
            LayoutHandles();
        }
    }

    public float HandleLength
    {
        get => _handleLength;
        set
        {
            _handleLength = value;
            LayoutHandles();
        }
    }

    public RectangleF Selection
    {
        get => new(Position.X, Position.Y, Size.X, Size.Y);
        set
        {
            Position = value.Location;
            Size = value.Size;
            SelectionChanged?.Invoke(Selection);
        }
    }

    private readonly Container _visuals;
    private readonly Box _backgroundBox;

    private readonly EdgeHandle _leftHandle;
    private readonly EdgeHandle _rightHandle;
    private readonly EdgeHandle _topHandle;
    private readonly EdgeHandle _bottomHandle;
    private readonly MoveGrip _moveGrip;

    private float _handleThickness = 16f;
    private float _handleLength = 60f;

    private readonly Color4 _rectangleColor;

    private Edge _activeEdge = Edge.None;
    private Vector2 _dragStartPosParent;
    private Vector2 _dragStartSize;
    private Vector2 _dragStartMouseParent;

    public SelectionRectangle(Vector2 initialSize, Color4 rectangleColor, Color4 handlesColor)
    {
        _rectangleColor = rectangleColor;

        Size = initialSize;

        Anchor = Anchor.TopLeft;
        Origin = Anchor.TopLeft;

        // Visual rectangle
        InternalChildren =
        [
            _visuals = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                BorderThickness = 4f,
                BorderColour = rectangleColor.Opacity(.8f),
                CornerRadius = 0,
                Children =
                [
                    _backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = rectangleColor.Opacity(.2f)
                    }
                ]
            },

            // Move grip covers the whole rect; edge handles sit on top.
            _moveGrip = new MoveGrip(this),

            _leftHandle = new EdgeHandle(this, Edge.Left, handlesColor),
            _rightHandle = new EdgeHandle(this, Edge.Right, handlesColor),
            _topHandle = new EdgeHandle(this, Edge.Top, handlesColor),
            _bottomHandle = new EdgeHandle(this, Edge.Bottom, handlesColor)
        ];

        // Initial layout of handles
        LayoutHandles();
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        // Ensure we start within parent bounds if requested
        var position = Position;
        var size = Size;

        if (ClampToParentBounds)
        {
            ClampToParent(ref position, ref size);
        }

        Position = position;
        Size = size;
    }

    private void LayoutHandles()
    {
        // Move grip covers full area; ensure it sits under handles in input stack.
        _moveGrip.RelativeSizeAxes = Axes.Both;
        _moveGrip.Size = Vector2.One;
        ChangeInternalChildDepth(_moveGrip, 1); // lower depth => behind handles visually and in input

        // Left
        _leftHandle.Anchor = Anchor.CentreLeft;
        _leftHandle.Origin = Anchor.CentreLeft;
        _leftHandle.Width = _handleThickness;
        _leftHandle.Height = _handleLength;
        _leftHandle.X = +(_handleThickness / 2);
        _leftHandle.Y = 0;
        ChangeInternalChildDepth(_leftHandle, 0);

        // Right
        _rightHandle.Anchor = Anchor.CentreRight;
        _rightHandle.Origin = Anchor.CentreRight;
        _rightHandle.Width = _handleThickness;
        _rightHandle.Height = _handleLength;
        _rightHandle.X = -(_handleThickness / 2);
        _rightHandle.Y = 0;
        ChangeInternalChildDepth(_rightHandle, 0);

        // Top
        _topHandle.Anchor = Anchor.TopCentre;
        _topHandle.Origin = Anchor.TopCentre;
        _topHandle.Height = _handleThickness;
        _topHandle.Width = _handleLength;
        _topHandle.X = 0;
        _topHandle.Y = +(_handleThickness / 2);
        ChangeInternalChildDepth(_topHandle, 0);

        // Bottom
        _bottomHandle.Anchor = Anchor.BottomCentre;
        _bottomHandle.Origin = Anchor.BottomCentre;
        _bottomHandle.Height = _handleThickness;
        _bottomHandle.Width = _handleLength;
        _bottomHandle.X = 0;
        _bottomHandle.Y = -(_handleThickness / 2);
        ChangeInternalChildDepth(_bottomHandle, 0);
    }

    internal void BeginInteraction(Edge edge, Vector2 screenSpaceMouse)
    {
        if (Parent == null)
        {
            return;
        }

        _activeEdge = edge;
        _dragStartPosParent = Position;
        _dragStartSize = Size;
        _dragStartMouseParent = Parent.ToLocalSpace(screenSpaceMouse);
    }

    internal void UpdateInteraction(Vector2 screenSpaceMouse)
    {
        if (Parent == null || _activeEdge == Edge.None)
        {
            return;
        }

        var currentMouseParent = Parent.ToLocalSpace(screenSpaceMouse);
        var delta = currentMouseParent - _dragStartMouseParent;

        var newPos = _dragStartPosParent;
        var newSize = _dragStartSize;

        switch (_activeEdge)
        {
            case Edge.Move:
                newPos = _dragStartPosParent + delta;
                break;

            case Edge.Left:
                newPos.X = _dragStartPosParent.X + delta.X;
                newSize.X = _dragStartSize.X - delta.X;
                break;

            case Edge.Right:
                newSize.X = _dragStartSize.X + delta.X;
                break;

            case Edge.Top:
                newPos.Y = _dragStartPosParent.Y + delta.Y;
                newSize.Y = _dragStartSize.Y - delta.Y;
                break;

            case Edge.Bottom:
                newSize.Y = _dragStartSize.Y + delta.Y;
                break;
        }

        EnforceMinSize(ref newPos, ref newSize, _activeEdge);

        if (ClampToParentBounds)
        {
            ClampToParent(ref newPos, ref newSize);
        }

        Position = newPos;
        Size = newSize;

        SelectionChanged?.Invoke(Selection);
    }

    internal void EndInteraction()
    {
        _activeEdge = Edge.None;
    }

    private void EnforceMinSize(ref Vector2 pos, ref Vector2 size, Edge edge)
    {
        // Width
        if (size.X < MinWidth)
        {
            float deficit = MinWidth - size.X;

            if (edge == Edge.Left)
            {
                pos.X -= deficit;
            }

            size.X = MinWidth;
        }

        // Height
        if (size.Y < MinHeight)
        {
            float deficit = MinHeight - size.Y;

            if (edge == Edge.Top)
            {
                pos.Y -= deficit;
            }

            size.Y = MinHeight;
        }
    }

    private void ClampToParent(ref Vector2 pos, ref Vector2 size)
    {
        if (Parent == null)
        {
            return;
        }

        var parentSize = Parent.DrawSize;

        // Clamp left/top side
        if (pos.X < 0)
        {
            size.X += pos.X; // pos.X is negative
            pos.X = 0;
        }

        if (pos.Y < 0)
        {
            size.Y += pos.Y;
            pos.Y = 0;
        }

        // Clamp right/bottom side
        float right = pos.X + size.X;
        float bottom = pos.Y + size.Y;

        if (right > parentSize.X)
        {
            size.X = MathF.Max(MinWidth, parentSize.X - pos.X);
        }

        if (bottom > parentSize.Y)
        {
            size.Y = MathF.Max(MinHeight, parentSize.Y - pos.Y);
        }

        // If size collapsed due to parent size being too small, ensure non-negative.
        size.X = MathF.Max(0, size.X);
        size.Y = MathF.Max(0, size.Y);

        // Final clamp to keep the whole rect inside, considering minimums
        pos.X = Clamp(pos.X, 0, MathF.Max(0, parentSize.X - size.X));
        pos.Y = Clamp(pos.Y, 0, MathF.Max(0, parentSize.Y - size.Y));
    }

    protected override bool OnHover(HoverEvent e)
    {
        _backgroundBox.FadeColour(_rectangleColor.Opacity(.25f), 300);

        return base.OnHover(e);
    }

    protected override void OnHoverLost(HoverLostEvent e)
    {
        _backgroundBox.FadeColour(_rectangleColor.Opacity(.2f), 300);

        base.OnHoverLost(e);
    }

    private static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);

    internal enum Edge
    {
        None,
        Move,
        Left,
        Right,
        Top,
        Bottom
    }

    private sealed partial class MoveGrip : CompositeDrawable
    {
        private readonly SelectionRectangle _owner;

        public MoveGrip(SelectionRectangle owner)
        {
            _owner = owner;
            // Transparent but interactive
            InternalChild = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0
            };
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
            {
                return false;
            }

            _owner.BeginInteraction(Edge.Move, e.ScreenSpaceMousePosition);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            _owner.UpdateInteraction(e.ScreenSpaceMousePosition);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            _owner.EndInteraction();
        }
    }

    private sealed partial class EdgeHandle : CompositeDrawable
    {
        private readonly SelectionRectangle _owner;
        private readonly Edge _edge;
        private readonly Color4 _color;
        private readonly Box _background;

        public EdgeHandle(SelectionRectangle owner, Edge edge, Color4 color)
        {
            _owner = owner;
            _edge = edge;
            _color = color;

            Masking = true;
            BorderThickness = 3f;
            BorderColour = color.Opacity(.8f);

            InternalChild = _background = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = color.Opacity(.5f)
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            // Subtle visual feedback
            _background.FadeColour(_color.Opacity(.7f), 50);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            _background.FadeColour(_color.Opacity(.5f), 50);

            base.OnHoverLost(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
            {
                return false;
            }

            _owner.BeginInteraction(_edge, e.ScreenSpaceMousePosition);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            _owner.UpdateInteraction(e.ScreenSpaceMousePosition);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            _owner.EndInteraction();
        }
    }
}