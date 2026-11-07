using SmartTooltip.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SmartTooltip.Samples
{
    /// <summary>
    /// One tip per target, run by the director. Tips start even when the target is off screen or behind the camera.
    /// Tap the target to finish a tip, or tap anywhere else to dismiss it.
    /// </summary>
    public sealed class WorldTooltipDemo : MonoBehaviour
    {
        [SerializeField] TooltipTarget[] targets;
        [SerializeField] string[] messages;

        readonly InMemoryProgressStore progress = new InMemoryProgressStore();
        TooltipDirector director;
        Text status;

        void Start()
        {
            status = CreateStatus();

            var rules = new TooltipRule[targets.Length];
            for (var i = 0; i < targets.Length; i++)
            {
                // No done condition, so tapping the target finishes the tip.
                rules[i] = new TooltipRule(targets[i].Id, new TooltipPrompt(targets[i].Id, messages[i]),
                    requires: TargetRequirement.Exists);
            }

            director = TooltipDirector.Create(rules, new TooltipContext(), progress);
            director.Started += rule => status.text = $"Showing \"{rule.Id}\"";
            director.Ended += OnEnded;
        }

        void OnEnded(TooltipRule rule, TooltipEndReason reason)
        {
            status.text = $"\"{rule.Id}\" ended as {reason}";

            // After the last tip, start again from the first one.
            if (rule.Id == targets[targets.Length - 1].Id && progress.HasSeen(rule.Id))
            {
                progress.ResetAll();
                director.Check();
            }
        }

        static Text CreateStatus()
        {
            var canvas = new GameObject("Status Canvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var text = new GameObject("Status", typeof(RectTransform)).AddComponent<Text>();
            text.rectTransform.SetParent(canvas.transform, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0f, 1f);
            text.rectTransform.anchoredPosition = new Vector2(32f, -32f);
            text.rectTransform.sizeDelta = new Vector2(900f, 60f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.raycastTarget = false;
            return text;
        }
    }
}
