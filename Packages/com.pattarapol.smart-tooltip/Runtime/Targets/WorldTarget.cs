using System;
using UnityEngine;

namespace SmartTooltip
{
    /// <summary>
    /// A target with no component, such as a tile, a spot on the ground or a point in the sky.
    /// Register it with TooltipTargetRegistry and unregister it when you are done.
    /// </summary>
    public sealed class WorldTarget : ITooltipTarget
    {
        readonly Func<Bounds> getBounds;

        public string Id { get; }

        /// <summary>Leave null to use Camera.main.</summary>
        public Camera Camera { get; set; }

        /// <param name="getBounds">Called every time the rect is needed, so the target can move.</param>
        public WorldTarget(string id, Func<Bounds> getBounds, Camera camera = null)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Target id is required.", nameof(id));

            Id = id;
            this.getBounds = getBounds ?? throw new ArgumentNullException(nameof(getBounds));
            Camera = camera;
        }

        public static WorldTarget AtPoint(string id, Vector3 worldPoint, Camera camera = null) =>
            new WorldTarget(id, () => new Bounds(worldPoint, Vector3.zero), camera);

        public bool TryGetScreenRect(out Rect screenRect)
        {
            screenRect = default;
            var camera = Camera != null ? Camera : Camera.main;
            return camera != null && ScreenProjection.TryProjectBounds(camera, getBounds(), out screenRect);
        }
    }
}
