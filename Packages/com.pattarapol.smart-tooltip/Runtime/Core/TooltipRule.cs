using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTooltip.Core
{
    public sealed class TooltipRule
    {
        public string Id { get; }

        /// <summary>When the tip may show. All must be true.</summary>
        public IReadOnlyList<ICondition> Conditions { get; }

        /// <summary>
        /// When the player has done what the tip asks. Null means the tip is done when the player taps the target.
        /// </summary>
        public ICondition DoneWhen { get; }

        public TargetRequirement Requires { get; }

        public TooltipPrompt Prompt { get; }

        /// <summary>Higher shows first. Same priority keeps list order.</summary>
        public int Priority { get; }

        public bool ShowOnce { get; }

        /// <summary>Seconds before a repeatable rule can show again after it was seen. Used by TooltipDirector.</summary>
        public float Cooldown { get; }

        public TooltipRule(string id, TooltipPrompt prompt, IEnumerable<ICondition> conditions = null,
            int priority = 0, bool showOnce = true, float cooldown = 0f, ICondition doneWhen = null,
            TargetRequirement requires = TargetRequirement.OnScreen)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Rule id is required.", nameof(id));
            if (cooldown < 0f)
                throw new ArgumentOutOfRangeException(nameof(cooldown), "Cooldown can't be negative.");

            Id = id;
            Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            Conditions = (conditions ?? Enumerable.Empty<ICondition>()).ToArray();
            Priority = priority;
            ShowOnce = showOnce;
            Cooldown = cooldown;
            DoneWhen = doneWhen;
            Requires = requires;
        }

        public bool IsSatisfiedBy(ITooltipContext context)
        {
            // A plain loop, because this runs for every rule on every check.
            for (var i = 0; i < Conditions.Count; i++)
            {
                if (!Conditions[i].Evaluate(context))
                    return false;
            }
            return true;
        }

        public bool IsDone(ITooltipContext context) => DoneWhen != null && DoneWhen.Evaluate(context);
    }
}
