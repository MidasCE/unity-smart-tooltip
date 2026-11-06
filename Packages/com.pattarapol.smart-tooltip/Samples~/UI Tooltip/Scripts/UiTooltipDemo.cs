using System.Collections;
using SmartTooltip.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SmartTooltip.Samples
{
    /// <summary>Rules pick the next tip from the game state. Tap the highlighted button to move on.</summary>
    public sealed class UiTooltipDemo : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] Button shopButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button resetButton;
        [SerializeField] Text status;

        readonly TooltipContext context = new TooltipContext();
        TooltipEvaluator evaluator;
        TooltipOverlay overlay;
        int plays;
        bool visitedShop;

        IEnumerator Start()
        {
            overlay = TooltipOverlay.Create();
            CreateEvaluator();

            playButton.onClick.AddListener(() => { plays++; ShowNextTip(); });
            shopButton.onClick.AddListener(() => { visitedShop = true; ShowNextTip(); });
            settingsButton.onClick.AddListener(ShowNextTip);
            resetButton.onClick.AddListener(ResetDemo);

            // Wait one frame so the canvas has its final size.
            yield return null;
            ShowNextTip();
        }

        void CreateEvaluator()
        {
            evaluator = new TooltipEvaluator(new[]
            {
                new TooltipRule("first-play",
                    new TooltipPrompt("play", "Tap Play to start your first game.", Placement.Above),
                    new[] { Condition.Compare("plays", ComparisonOperator.Equal, 0) }),

                new TooltipRule("visit-shop",
                    new TooltipPrompt("shop", "Nice, you earned coins! Spend them in the Shop."),
                    new[]
                    {
                        Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1),
                        Condition.Not(Condition.Is("visitedShop", true))
                    }),

                new TooltipRule("settings",
                    new TooltipPrompt("settings", "Change sound and language here.", Placement.Below),
                    new[] { Condition.Is("visitedShop", true) }),
            }, new InMemoryProgressStore());
        }

        void ShowNextTip()
        {
            context.Set("plays", plays).Set("visitedShop", visitedShop);
            status.text = $"Plays: {plays}    Shop visited: {(visitedShop ? "yes" : "no")}";

            foreach (var rule in evaluator.Candidates(context))
            {
                if (!TooltipTargetRegistry.Default.TryResolve(rule.Prompt.TargetId, out var target))
                    continue;

                overlay.Show(rule.Prompt, target);
                evaluator.MarkShown(rule);
                return;
            }

            overlay.Hide();
        }

        void ResetDemo()
        {
            plays = 0;
            visitedShop = false;
            CreateEvaluator();
            ShowNextTip();
        }
    }
}
