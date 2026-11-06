using UnityEngine;

namespace SmartTooltip
{
    /// <summary>Add this to any object you want to point at. Works for UI, 2D and 3D.</summary>
    [DisallowMultipleComponent]
    public sealed class TooltipTarget : MonoBehaviour, ITooltipTarget
    {
        [SerializeField] string id;
        [SerializeField] TargetShape shape = TargetShape.Auto;

        [Tooltip("Only used when Shape is Custom Box. Local space.")]
        [SerializeField] Vector3 boxCenter;
        [SerializeField] Vector3 boxSize = Vector3.one;

        [Tooltip("Leave empty to use the camera that renders this object's layer.")]
        [SerializeField] Camera worldCamera;

        Renderer[] renderers;
        Collider[] colliders;
        Collider2D[] colliders2D;
        Canvas canvas;

        public string Id => id;

        public TargetShape Shape
        {
            get => shape;
            set => shape = value;
        }

        public Camera WorldCamera
        {
            get => worldCamera;
            set => worldCamera = value;
        }

        public void SetCustomBox(Vector3 center, Vector3 size)
        {
            shape = TargetShape.CustomBox;
            boxCenter = center;
            boxSize = size;
        }

        /// <summary>Tag an object from code, for example something spawned at runtime.</summary>
        public static TooltipTarget Attach(GameObject gameObject, string id)
        {
            if (!gameObject.TryGetComponent(out TooltipTarget target))
                target = gameObject.AddComponent<TooltipTarget>();

            target.SetId(id);
            return target;
        }

        public void SetId(string newId)
        {
            id = newId;
            if (isActiveAndEnabled)
                TooltipTargetRegistry.Default.Register(this);
        }

        /// <summary>Call this if you add renderers or colliders deeper in the hierarchy at runtime.</summary>
        public void Refresh()
        {
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            colliders2D = GetComponentsInChildren<Collider2D>();
            canvas = GetComponentInParent<Canvas>();
        }

        void OnEnable()
        {
            Refresh();
            TooltipTargetRegistry.Default.Register(this);
        }

        void OnDisable() => TooltipTargetRegistry.Default.Unregister(this);

        void OnTransformChildrenChanged() => Refresh();

        void OnTransformParentChanged() => Refresh();

        void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled)
                TooltipTargetRegistry.Default.Register(this);
        }

        public bool TryGetScreenRect(out Rect screenRect)
        {
            screenRect = default;
            if (!isActiveAndEnabled)
                return false;

            if (shape == TargetShape.Auto && canvas != null && transform is RectTransform rectTransform)
                return ScreenProjection.TryProjectUi(rectTransform, canvas, out screenRect);

            var camera = worldCamera != null ? worldCamera : ScreenProjection.FindCamera(gameObject.layer);
            if (camera == null)
                return false;

            switch (shape)
            {
                case TargetShape.CustomBox:
                    return ScreenProjection.TryProjectBox(camera, transform, boxCenter, boxSize, out screenRect);
                case TargetShape.Renderers:
                    return TryProjectRenderers(camera, out screenRect);
                case TargetShape.Colliders:
                    return TryProjectColliders(camera, out screenRect);
                default:
                    return TryProjectRenderers(camera, out screenRect)
                        || TryProjectColliders(camera, out screenRect)
                        || ScreenProjection.TryProjectPoint(camera, transform.position, out screenRect);
            }
        }

        bool TryProjectRenderers(Camera camera, out Rect screenRect)
        {
            screenRect = default;
            var any = false;
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                Rect part;
                bool found;
                // Local bounds turn with the object, so rotated meshes and sprites get a tight fit.
                if (r is MeshRenderer || r is SpriteRenderer)
                {
                    var local = r.localBounds;
                    found = ScreenProjection.TryProjectBox(camera, r.transform, local.center, local.size, out part);
                }
                else
                {
                    found = ScreenProjection.TryProjectBounds(camera, r.bounds, out part);
                }

                if (found)
                    Union(ref screenRect, ref any, part);
            }

            return any;
        }

        bool TryProjectColliders(Camera camera, out Rect screenRect)
        {
            screenRect = default;
            var any = false;
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;

                Rect part;
                var found = c is BoxCollider box
                    ? ScreenProjection.TryProjectBox(camera, box.transform, box.center, box.size, out part)
                    : ScreenProjection.TryProjectBounds(camera, c.bounds, out part);
                if (found)
                    Union(ref screenRect, ref any, part);
            }

            foreach (var c in colliders2D)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    continue;

                Rect part;
                var found = c is BoxCollider2D box
                    ? ScreenProjection.TryProjectBox(camera, box.transform, box.offset, box.size, out part)
                    : ScreenProjection.TryProjectBounds(camera, c.bounds, out part);
                if (found)
                    Union(ref screenRect, ref any, part);
            }

            return any;
        }

        static void Union(ref Rect rect, ref bool any, Rect next)
        {
            if (any)
            {
                rect = Rect.MinMaxRect(
                    Mathf.Min(rect.xMin, next.xMin), Mathf.Min(rect.yMin, next.yMin),
                    Mathf.Max(rect.xMax, next.xMax), Mathf.Max(rect.yMax, next.yMax));
            }
            else
            {
                rect = next;
                any = true;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (shape != TargetShape.CustomBox)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
    }
}
