using System;
using UnityEngine;

namespace SmartTooltip
{
    /// <summary>Turns UI and world shapes into screen rects. Useful when writing your own ITooltipTarget.</summary>
    public static class ScreenProjection
    {
        static readonly Vector3[] Corners = new Vector3[8];
        static readonly Vector3[] Projected = new Vector3[8];
        static Camera[] cameras = new Camera[4];

        public static bool IsOnScreen(Rect screenRect) =>
            screenRect.Overlaps(new Rect(0, 0, Screen.width, Screen.height));

        /// <summary>Camera.main if it renders this layer, else the enabled camera with the highest depth that does.</summary>
        public static Camera FindCamera(int layer)
        {
            var mask = 1 << layer;
            var main = Camera.main;
            if (main != null && Renders(main, mask))
                return main;

            var count = Camera.allCamerasCount;
            if (cameras.Length < count)
                cameras = new Camera[count];
            Camera.GetAllCameras(cameras);

            Camera best = null;
            for (var i = 0; i < count; i++)
            {
                var camera = cameras[i];
                if (Renders(camera, mask) && (best == null || camera.depth > best.depth))
                    best = camera;
            }

            Array.Clear(cameras, 0, count);
            return best;
        }

        public static bool TryProjectUi(RectTransform rectTransform, Canvas canvas, out Rect screenRect)
        {
            screenRect = default;
            if (canvas == null || !canvas.isActiveAndEnabled)
                return false;

            var root = canvas.rootCanvas;
            rectTransform.GetWorldCorners(Corners);

            switch (root.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    screenRect = ProjectOnScreen(null);
                    return true;
                case RenderMode.ScreenSpaceCamera:
                    screenRect = ProjectOnScreen(root.worldCamera);
                    return true;
                default:
                    var camera = root.worldCamera != null ? root.worldCamera : FindCamera(rectTransform.gameObject.layer);
                    return camera != null && ProjectCorners(camera, 4, out screenRect);
            }
        }

        public static bool TryProjectBounds(Camera camera, Bounds bounds, out Rect screenRect)
        {
            var min = bounds.min;
            var max = bounds.max;
            for (var i = 0; i < 8; i++)
            {
                Corners[i] = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
            }

            return ProjectCorners(camera, 8, out screenRect);
        }

        /// <summary>A box in the local space of <paramref name="transform"/>, so it turns and scales with the object.</summary>
        public static bool TryProjectBox(Camera camera, Transform transform, Vector3 center, Vector3 size, out Rect screenRect)
        {
            var half = size * 0.5f;
            for (var i = 0; i < 8; i++)
            {
                var local = center + new Vector3(
                    (i & 1) == 0 ? -half.x : half.x,
                    (i & 2) == 0 ? -half.y : half.y,
                    (i & 4) == 0 ? -half.z : half.z);
                Corners[i] = transform.TransformPoint(local);
            }

            return ProjectCorners(camera, 8, out screenRect);
        }

        public static bool TryProjectPoint(Camera camera, Vector3 worldPoint, out Rect screenRect)
        {
            Corners[0] = worldPoint;
            return ProjectCorners(camera, 1, out screenRect);
        }

        static bool Renders(Camera camera, int mask) =>
            camera.isActiveAndEnabled && camera.targetTexture == null && (camera.cullingMask & mask) != 0;

        static bool ProjectCorners(Camera camera, int count, out Rect screenRect)
        {
            var inFront = 0;
            for (var i = 0; i < count; i++)
            {
                var point = camera.WorldToScreenPoint(Corners[i]);
                if (point.z > 0)
                    Projected[inFront++] = point;
            }

            screenRect = inFront > 0 ? Enclose(Projected, inFront) : BehindCamera(camera, count);
            return true;
        }

        // A point behind the camera projects to the opposite side of the screen.
        // Flip it back and push it far off screen, so it points the way the player would turn.
        static Rect BehindCamera(Camera camera, int count)
        {
            var sum = Vector3.zero;
            for (var i = 0; i < count; i++)
                sum += Corners[i];

            var view = camera.pixelRect;
            var flipped = view.center - (Vector2)camera.WorldToScreenPoint(sum / count);
            var direction = flipped.sqrMagnitude > 0.01f ? flipped.normalized : Vector2.down;
            var point = view.center + direction * (view.width + view.height);
            return new Rect(point, Vector2.zero);
        }

        static Rect ProjectOnScreen(Camera camera)
        {
            for (var i = 0; i < 4; i++)
                Projected[i] = RectTransformUtility.WorldToScreenPoint(camera, Corners[i]);

            return Enclose(Projected, 4);
        }

        static Rect Enclose(Vector3[] points, int count)
        {
            var min = (Vector2)points[0];
            var max = min;
            for (var i = 1; i < count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
