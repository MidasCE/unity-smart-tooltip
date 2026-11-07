using System;
using SmartTooltip.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartTooltip
{
    /// <summary>
    /// Dims the screen, cuts a hole around the target and shows a tooltip next to it.
    /// Follows the target every frame. When the target leaves the screen, the tooltip sticks to the edge and points at it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TooltipOverlay : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] Color dimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] float holePadding = 12f;
        [SerializeField] float holeRadius = 16f;
        [SerializeField] float holeSoftness = 2f;
        [Tooltip("Leave empty to use the built in bubble.")]
        [SerializeField] TooltipBubble bubblePrefab;

        [Header("Layout")]
        [SerializeField] float gap = 6f;
        [SerializeField] float screenMargin = 16f;
        [SerializeField] Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] int sortingOrder = 32000;

        [Header("Behaviour")]
        [SerializeField] bool closeOnOutsideTap = true;
        [SerializeField] float transitionTime = 0.25f;

        Canvas canvas;
        CanvasGroup group;
        TooltipCutout cutout;
        TooltipBubble bubble;

        ITooltipTarget target;
        TooltipPrompt prompt;
        OverlayLayout from;
        OverlayLayout shown;
        bool hasLayout;
        bool hiding;
        TooltipHideReason hideReason;
        bool pressStartedInHole;
        float blend = 1f;
        float alpha;

        /// <summary>A tap outside the hole. Taps inside the hole go to your game.</summary>
        public event Action OutsideTapped;

        /// <summary>A tap inside the hole, on the target. The tap still reaches your game first.</summary>
        public event Action TargetTapped;

        /// <summary>Fires once the fade out ends. Not fired when Show replaces the current tip.</summary>
        public event Action<TooltipHideReason> Hidden;

        public bool IsShowing => target != null && !hiding;
        public ITooltipTarget Target => target;
        public TooltipPrompt Prompt => prompt;

        public static TooltipOverlay Create(string name = "Tooltip Overlay") =>
            new GameObject(name).AddComponent<TooltipOverlay>();

        /// <summary>Use this if your game reads input directly instead of through the EventSystem.</summary>
        public bool IsBlocked(Vector2 screenPoint) => IsShowing && !shown.Hole.Contains(screenPoint);

        public void Show(TooltipPrompt prompt, ITooltipTarget target)
        {
            this.prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            hiding = false;
            canvas.enabled = true;
            bubble.SetText(prompt.Text);
            if (hasLayout)
            {
                from = shown;
                blend = 0f;
            }

            if (EventSystem.current == null)
                Debug.LogWarning("Smart Tooltip: no EventSystem in the scene, so taps will not be blocked or detected.");
        }

        public void Hide() => BeginHide(TooltipHideReason.Requested);

        void BeginHide(TooltipHideReason reason)
        {
            if (target == null || hiding)
                return;
            hiding = true;
            hideReason = reason;
        }

        void Awake()
        {
            canvas = GetOrAdd<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = GetOrAdd<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>();
            group = GetOrAdd<CanvasGroup>();

            cutout = TooltipCutout.Create(transform);
            cutout.Clicked += OnOutsideTap;

            bubble = bubblePrefab != null ? Instantiate(bubblePrefab, transform) : TooltipBubble.CreateDefault(transform);
            var bubbleRect = bubble.Rect;
            bubbleRect.anchorMin = Vector2.zero;
            bubbleRect.anchorMax = Vector2.zero;
            bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            if (bubble.Arrow != null)
            {
                bubble.Arrow.anchorMin = new Vector2(0.5f, 0.5f);
                bubble.Arrow.anchorMax = new Vector2(0.5f, 0.5f);
            }

            group.alpha = 0f;
            canvas.enabled = false;
        }

        void OnValidate()
        {
            if (canvas != null)
                canvas.sortingOrder = sortingOrder;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            var step = Time.unscaledDeltaTime / Mathf.Max(transitionTime, 0.01f);

            if (hiding)
            {
                alpha = Mathf.MoveTowards(alpha, 0f, step);
                Apply();
                if (alpha <= 0f)
                    FinishHide();
                return;
            }

            if (!TryGetTargetRect(out var targetRect))
            {
                BeginHide(TooltipHideReason.TargetLost);
                return;
            }

            var goal = OverlayLayout.Compute(BuildInput(targetRect));
            if (!hasLayout)
            {
                // Open the hole from the middle of the target.
                from = goal.WithClosedHole();
                shown = from;
                blend = 0f;
                hasLayout = true;
            }
            else if (goal.OnScreen != shown.OnScreen || goal.Side != shown.Side)
            {
                from = shown;
                blend = 0f;
            }

            // Follow the target exactly, and only blend while switching side or going on and off screen.
            blend = Mathf.MoveTowards(blend, 1f, step);
            var t = blend * blend * (3f - 2f * blend);
            shown = OverlayLayout.Lerp(from, goal, t);
            alpha = Mathf.MoveTowards(alpha, 1f, step);
            Apply();

            if (PointerInput.WasPressed(out var pressed))
                OnPointerPressed(pressed);
            if (PointerInput.WasReleased(out var released))
                OnPointerReleased(released);
        }

        // A tap must start and end in the hole, so a drag that ends there does not count.
        internal void OnPointerPressed(Vector2 screenPoint) => pressStartedInHole = IsInHole(screenPoint);

        internal void OnPointerReleased(Vector2 screenPoint)
        {
            var tapped = pressStartedInHole && IsInHole(screenPoint);
            pressStartedInHole = false;
            if (tapped)
                TargetTapped?.Invoke();
        }

        bool IsInHole(Vector2 screenPoint) => IsShowing && hasLayout && shown.OnScreen && shown.Hole.Contains(screenPoint);

        bool TryGetTargetRect(out Rect rect)
        {
            rect = default;
            if (target is UnityEngine.Object unityObject && unityObject == null)
                return false;
            return target.TryGetScreenRect(out rect);
        }

        OverlayLayout.Input BuildInput(Rect targetRect)
        {
            var scale = canvas.scaleFactor;
            var margin = screenMargin * scale;
            var safe = Screen.safeArea;

            return new OverlayLayout.Input
            {
                Target = targetRect,
                Wanted = prompt.Placement,
                Area = Rect.MinMaxRect(safe.xMin + margin, safe.yMin + margin, safe.xMax - margin, safe.yMax - margin),
                BubbleSize = bubble.Rect.rect.size * scale,
                ArrowLength = bubble.ArrowLength * scale,
                Gap = gap * scale,
                HolePadding = holePadding * scale
            };
        }

        void Apply()
        {
            var scale = canvas.scaleFactor;
            group.alpha = alpha;
            group.blocksRaycasts = !hiding;

            cutout.SetHole(shown.Hole, holeRadius * scale, holeSoftness * scale, dimColor, scale);
            bubble.Rect.anchoredPosition = shown.BubbleCenter / scale;
            if (bubble.Arrow != null)
            {
                bubble.Arrow.anchoredPosition = shown.ArrowOffset / scale;
                bubble.Arrow.localEulerAngles = new Vector3(0f, 0f, shown.ArrowAngle);
            }
        }

        void FinishHide()
        {
            target = null;
            prompt = null;
            hiding = false;
            hasLayout = false;
            pressStartedInHole = false;
            canvas.enabled = false;
            Hidden?.Invoke(hideReason);
        }

        void OnOutsideTap()
        {
            OutsideTapped?.Invoke();
            if (closeOnOutsideTap)
                BeginHide(TooltipHideReason.OutsideTap);
        }

        T GetOrAdd<T>() where T : Component =>
            TryGetComponent(out T existing) ? existing : gameObject.AddComponent<T>();
    }
}
