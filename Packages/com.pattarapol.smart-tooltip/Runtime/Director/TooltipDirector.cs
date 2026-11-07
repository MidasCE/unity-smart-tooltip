using System;
using System.Collections.Generic;
using SmartTooltip.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmartTooltip
{
    /// <summary>
    /// Picks and shows tips for you. Call Check whenever your game state changes.
    /// A tip counts as seen when the player finishes or dismisses it, not when it shows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TooltipDirector : MonoBehaviour
    {
        TooltipEvaluator evaluator;
        ITooltipContext context;
        TooltipOverlay overlay;
        bool ownsOverlay;

        readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
        TooltipRule active;
        bool pending;
        int requestFrame;
        float settleEnd;
        float nextPoll;
        int holds;

        /// <summary>The rule showing now, or null.</summary>
        public TooltipRule Active => active;

        public TooltipOverlay Overlay => overlay;

        /// <summary>How long to wait for the best tip's target to appear, for example while a panel slides in, before showing a lower one.</summary>
        public float SettleTime { get; set; } = 0.5f;

        /// <summary>Seconds between automatic checks, for state that has no events. 0 turns it off.</summary>
        public float PollInterval { get; set; }

        public ITooltipTargetResolver Targets { get; set; } = TooltipTargetRegistry.Default;

        public bool IsHeld => holds > 0;

        public event Action<TooltipRule> Started;
        public event Action<TooltipRule, TooltipEndReason> Ended;

        /// <summary>Pass an overlay to use your own look. Otherwise one is made and destroyed with the director.</summary>
        public static TooltipDirector Create(IEnumerable<TooltipRule> rules, ITooltipContext context,
            ITooltipProgressStore progress, TooltipOverlay overlay = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var evaluator = new TooltipEvaluator(rules, progress);

            var director = new GameObject("Tooltip Director").AddComponent<TooltipDirector>();
            director.evaluator = evaluator;
            director.context = context;
            director.ownsOverlay = overlay == null;
            director.overlay = overlay != null ? overlay : TooltipOverlay.Create();
            if (director.ownsOverlay)
                director.overlay.transform.SetParent(director.transform, false);

            director.overlay.Hidden += director.OnOverlayHidden;
            director.overlay.TargetTapped += director.OnTargetTapped;
            director.Check();
            return director;
        }

        /// <summary>Safe to call often. Calls in the same frame become one check, run next frame after the UI has laid out.</summary>
        public void Check()
        {
            if (!pending)
                settleEnd = Time.unscaledTime + SettleTime;
            pending = true;
            requestFrame = Time.frameCount;
        }

        /// <summary>
        /// Pauses tips while the game is busy, for example in a cutscene or a popup. Dispose to release.
        /// The current tip hides and shows again after the last hold is released.
        /// </summary>
        public IDisposable Hold()
        {
            holds++;
            if (holds == 1)
                Stop();
            return new HoldHandle(this);
        }

        /// <summary>
        /// Ends the current tip. If its rule is already done it ends as Completed, otherwise as Interrupted
        /// and it shows again on a later check.
        /// </summary>
        public void Stop()
        {
            if (active == null)
                return;
            // The same tap that finished a tip often opens a popup that holds the director.
            End(active.IsDone(context) ? TooltipEndReason.Completed : TooltipEndReason.Interrupted);
            overlay.Hide();
        }

        void Release()
        {
            holds--;
            if (holds == 0)
                Check();
        }

        void Awake() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (overlay == null)
                return;
            overlay.Hidden -= OnOverlayHidden;
            overlay.TargetTapped -= OnTargetTapped;
            if (ownsOverlay)
                Destroy(overlay.gameObject);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Check();

        void Update()
        {
            if (evaluator == null)
                return;

            var now = Time.unscaledTime;
            if (PollInterval > 0f && now >= nextPoll)
            {
                nextPoll = now + PollInterval;
                Check();
            }

            if (!pending || holds > 0 || Time.frameCount <= requestFrame)
                return;

            // The overlay is closing on its own. OnOverlayHidden ends the tip and checks again.
            if (active != null && !overlay.IsShowing)
                return;

            Evaluate(now);
        }

        void Evaluate(float now)
        {
            if (active != null)
            {
                if (active.IsDone(context))
                    End(TooltipEndReason.Completed);
                else if (!active.IsSatisfiedBy(context))
                    End(TooltipEndReason.Interrupted);
                else
                {
                    pending = false;
                    return;
                }
            }

            foreach (var rule in evaluator.Candidates(context))
            {
                if (readyAt.TryGetValue(rule.Id, out var ready) && now < ready)
                    continue;

                if (TryGetTarget(rule, out var target))
                {
                    pending = false;
                    Begin(rule, target);
                    return;
                }

                // Give the best tip's target time to appear before falling back to a lower one.
                if (now < settleEnd)
                {
                    overlay.Hide();
                    return;
                }
            }

            pending = false;
            overlay.Hide();
        }

        bool TryGetTarget(TooltipRule rule, out ITooltipTarget target) =>
            rule.Requires == TargetRequirement.Exists
                ? Targets.TryFind(rule.Prompt.TargetId, out target)
                : Targets.TryResolve(rule.Prompt.TargetId, out target);

        void Begin(TooltipRule rule, ITooltipTarget target)
        {
            active = rule;
            overlay.Show(rule.Prompt, target);
            Started?.Invoke(rule);
        }

        void End(TooltipEndReason reason)
        {
            var rule = active;
            active = null;

            if (reason == TooltipEndReason.Completed || reason == TooltipEndReason.Dismissed)
            {
                evaluator.MarkShown(rule);
                if (rule.Cooldown > 0f)
                    readyAt[rule.Id] = Time.unscaledTime + rule.Cooldown;
            }

            Ended?.Invoke(rule, reason);
        }

        void OnTargetTapped()
        {
            if (active == null)
                return;

            // A rule with its own done condition decides for itself. The tap often changes that state, so check.
            if (active.DoneWhen == null)
                End(TooltipEndReason.Completed);
            Check();
        }

        void OnOverlayHidden(TooltipHideReason reason)
        {
            // Hides the director asked for already ended the tip.
            if (active == null)
                return;

            switch (reason)
            {
                case TooltipHideReason.OutsideTap:
                    End(TooltipEndReason.Dismissed);
                    Check();
                    break;
                case TooltipHideReason.TargetLost:
                    End(TooltipEndReason.TargetLost);
                    Check();
                    break;
                default:
                    // Other code hid the overlay. Checking now would bring the tip straight back.
                    End(TooltipEndReason.Interrupted);
                    break;
            }
        }

        sealed class HoldHandle : IDisposable
        {
            TooltipDirector director;

            public HoldHandle(TooltipDirector director) => this.director = director;

            public void Dispose()
            {
                if (director != null)
                    director.Release();
                director = null;
            }
        }
    }
}
