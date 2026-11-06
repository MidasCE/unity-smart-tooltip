using SmartTooltip.Core;
using UnityEngine;

namespace SmartTooltip
{
    /// <summary>Where everything goes, in screen pixels.</summary>
    struct OverlayLayout
    {
        public Rect Hole;
        public Vector2 BubbleCenter;
        public Vector2 ArrowOffset;
        public float ArrowAngle;
        public Placement Side;
        public bool OnScreen;

        public struct Input
        {
            public Rect Target;
            public Placement Wanted;
            public Rect Area;
            public Vector2 BubbleSize;
            public float ArrowLength;
            public float Gap;
            public float HolePadding;
        }

        static readonly Placement[] AutoOrder = { Placement.Above, Placement.Below, Placement.Right, Placement.Left };

        public static OverlayLayout Compute(in Input input)
        {
            var layout = new OverlayLayout { OnScreen = ScreenProjection.IsOnScreen(input.Target) };
            Rect anchor;

            if (layout.OnScreen)
            {
                layout.Hole = Expand(input.Target, input.HolePadding);
                anchor = layout.Hole;
                layout.Side = ChooseSide(input.Wanted, anchor, input);
            }
            else
            {
                // Stick to the screen edge closest to the target and point out at it.
                var edge = Clamp(input.Target.center, input.Area);
                anchor = new Rect(edge, Vector2.zero);
                layout.Hole = anchor;
                layout.Side = InwardSide(input.Target.center, input.Area);
            }

            var offset = input.Gap + input.ArrowLength;
            var half = input.BubbleSize * 0.5f;
            layout.BubbleCenter = ClampInside(Place(layout.Side, anchor, half, offset), half, input.Area);
            PlaceArrow(ref layout, anchor.center, half, input.ArrowLength);
            return layout;
        }

        public static OverlayLayout Lerp(in OverlayLayout a, in OverlayLayout b, float t) => new OverlayLayout
        {
            Hole = Rect.MinMaxRect(
                Mathf.Lerp(a.Hole.xMin, b.Hole.xMin, t), Mathf.Lerp(a.Hole.yMin, b.Hole.yMin, t),
                Mathf.Lerp(a.Hole.xMax, b.Hole.xMax, t), Mathf.Lerp(a.Hole.yMax, b.Hole.yMax, t)),
            BubbleCenter = Vector2.Lerp(a.BubbleCenter, b.BubbleCenter, t),
            ArrowOffset = Vector2.Lerp(a.ArrowOffset, b.ArrowOffset, t),
            ArrowAngle = Mathf.LerpAngle(a.ArrowAngle, b.ArrowAngle, t),
            Side = b.Side,
            OnScreen = b.OnScreen
        };

        public OverlayLayout WithClosedHole()
        {
            var copy = this;
            copy.Hole = new Rect(Hole.center, Vector2.zero);
            return copy;
        }

        static Placement ChooseSide(Placement wanted, Rect anchor, in Input input)
        {
            if (wanted != Placement.Auto)
            {
                if (Fits(wanted, anchor, input)) return wanted;
                if (Fits(Opposite(wanted), anchor, input)) return Opposite(wanted);
            }

            foreach (var side in AutoOrder)
            {
                if (Fits(side, anchor, input))
                    return side;
            }

            if (wanted != Placement.Auto)
                return wanted;

            var best = Placement.Above;
            var bestSpare = float.MinValue;
            foreach (var side in AutoOrder)
            {
                var spare = Room(side, anchor, input.Area) - Need(side, input);
                if (spare > bestSpare)
                {
                    best = side;
                    bestSpare = spare;
                }
            }

            return best;
        }

        static bool Fits(Placement side, Rect anchor, in Input input) => Room(side, anchor, input.Area) >= Need(side, input);

        static float Room(Placement side, Rect anchor, Rect area)
        {
            switch (side)
            {
                case Placement.Above: return area.yMax - anchor.yMax;
                case Placement.Below: return anchor.yMin - area.yMin;
                case Placement.Right: return area.xMax - anchor.xMax;
                default: return anchor.xMin - area.xMin;
            }
        }

        static float Need(Placement side, in Input input)
        {
            var size = side == Placement.Above || side == Placement.Below ? input.BubbleSize.y : input.BubbleSize.x;
            return size + input.Gap + input.ArrowLength;
        }

        static Placement Opposite(Placement side)
        {
            switch (side)
            {
                case Placement.Above: return Placement.Below;
                case Placement.Below: return Placement.Above;
                case Placement.Left: return Placement.Right;
                default: return Placement.Left;
            }
        }

        static Placement InwardSide(Vector2 point, Rect area)
        {
            var outX = point.x < area.xMin ? area.xMin - point.x : Mathf.Max(point.x - area.xMax, 0);
            var outY = point.y < area.yMin ? area.yMin - point.y : Mathf.Max(point.y - area.yMax, 0);
            if (outX >= outY)
                return point.x < area.xMin ? Placement.Right : Placement.Left;
            return point.y < area.yMin ? Placement.Above : Placement.Below;
        }

        static Vector2 Place(Placement side, Rect anchor, Vector2 half, float offset)
        {
            switch (side)
            {
                case Placement.Above: return new Vector2(anchor.center.x, anchor.yMax + offset + half.y);
                case Placement.Below: return new Vector2(anchor.center.x, anchor.yMin - offset - half.y);
                case Placement.Right: return new Vector2(anchor.xMax + offset + half.x, anchor.center.y);
                default: return new Vector2(anchor.xMin - offset - half.x, anchor.center.y);
            }
        }

        // The arrow sits on the bubble edge that faces the target and slides along it toward the target.
        static void PlaceArrow(ref OverlayLayout layout, Vector2 aim, Vector2 half, float arrowLength)
        {
            var toAim = aim - layout.BubbleCenter;
            var slideX = Mathf.Clamp(toAim.x, -half.x + arrowLength, half.x - arrowLength);
            var slideY = Mathf.Clamp(toAim.y, -half.y + arrowLength, half.y - arrowLength);
            var outward = arrowLength * 0.5f;

            switch (layout.Side)
            {
                case Placement.Above:
                    layout.ArrowOffset = new Vector2(slideX, -half.y - outward);
                    layout.ArrowAngle = 180f;
                    break;
                case Placement.Below:
                    layout.ArrowOffset = new Vector2(slideX, half.y + outward);
                    layout.ArrowAngle = 0f;
                    break;
                case Placement.Right:
                    layout.ArrowOffset = new Vector2(-half.x - outward, slideY);
                    layout.ArrowAngle = 90f;
                    break;
                default:
                    layout.ArrowOffset = new Vector2(half.x + outward, slideY);
                    layout.ArrowAngle = -90f;
                    break;
            }
        }

        static Rect Expand(Rect rect, float amount) =>
            Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);

        static Vector2 Clamp(Vector2 point, Rect area) =>
            new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));

        static Vector2 ClampInside(Vector2 center, Vector2 half, Rect area) => new Vector2(
            half.x * 2 >= area.width ? area.center.x : Mathf.Clamp(center.x, area.xMin + half.x, area.xMax - half.x),
            half.y * 2 >= area.height ? area.center.y : Mathf.Clamp(center.y, area.yMin + half.y, area.yMax - half.y));
    }
}
