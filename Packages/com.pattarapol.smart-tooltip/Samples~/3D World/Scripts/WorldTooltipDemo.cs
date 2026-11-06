using SmartTooltip.Core;
using UnityEngine;

namespace SmartTooltip.Samples
{
    /// <summary>Shows one tip per target. Tap outside the hole to go to the next one.</summary>
    public sealed class WorldTooltipDemo : MonoBehaviour
    {
        [SerializeField] TooltipTarget[] targets;
        [SerializeField] string[] messages;

        TooltipOverlay overlay;
        int index;

        void Start()
        {
            overlay = TooltipOverlay.Create();
            overlay.Hidden += ShowNext;
            Show();
        }

        void ShowNext()
        {
            index = (index + 1) % targets.Length;
            Show();
        }

        void Show()
        {
            var target = targets[index];
            overlay.Show(new TooltipPrompt(target.Id, messages[index]), target);
        }
    }
}
