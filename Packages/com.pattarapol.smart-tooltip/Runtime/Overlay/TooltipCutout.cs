using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartTooltip
{
    /// <summary>The dark layer. Blocks taps everywhere except inside the hole.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TooltipCutout : Graphic, ICanvasRaycastFilter, IPointerClickHandler
    {
        static readonly int HoleId = Shader.PropertyToID("_Hole");
        static readonly int ScreenSizeId = Shader.PropertyToID("_ScreenSize");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        static readonly Vector4 NoHole = new Vector4(-1e5f, -1e5f, -1e5f, -1e5f);

        Material instance;
        Rect hole;

        public event Action Clicked;

        /// <summary>In screen pixels.</summary>
        public Rect Hole => hole;

        internal static TooltipCutout Create(Transform parent)
        {
            var go = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var cutout = go.AddComponent<TooltipCutout>();
            var shader = Resources.Load<Shader>("TooltipCutout");
            if (shader == null)
            {
                Debug.LogError("Smart Tooltip: TooltipCutout shader is missing from Resources.");
                return cutout;
            }

            cutout.instance = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            cutout.material = cutout.instance;
            return cutout;
        }

        public void SetHole(Rect screenHole, float radius, float softness, Color dim, float canvasScale)
        {
            hole = screenHole;
            color = dim;
            if (instance == null)
                return;

            var hasHole = screenHole.width >= 0.5f && screenHole.height >= 0.5f;
            var size = rectTransform.rect.size * canvasScale;
            instance.SetVector(HoleId, hasHole
                ? new Vector4(screenHole.xMin, screenHole.yMin, screenHole.xMax, screenHole.yMax)
                : NoHole);
            instance.SetVector(ScreenSizeId, new Vector4(size.x, size.y, 0, 0));
            instance.SetFloat(RadiusId, radius);
            instance.SetFloat(SoftnessId, softness);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => !hole.Contains(screenPoint);

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (instance != null)
                Destroy(instance);
        }
    }
}
