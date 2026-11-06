using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTooltip.Core
{
    public sealed class TooltipRule
    {
        public string Id { get; }
        public IReadOnlyList<ICondition> Conditions { get; }
        public TooltipPrompt Prompt { get; }

        /// <summary>Higher shows first. Same priority keeps list order.</summary>
        public int Priority { get; }

        public bool ShowOnce { get; }

        public TooltipRule(string id, TooltipPrompt prompt, IEnumerable<ICondition> conditions = null,
            int priority = 0, bool showOnce = true)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Rule id is required.", nameof(id));

            Id = id;
            Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            Conditions = (conditions ?? Enumerable.Empty<ICondition>()).ToArray();
            Priority = priority;
            ShowOnce = showOnce;
        }

        public bool IsSatisfiedBy(ITooltipContext context) => Conditions.All(c => c.Evaluate(context));
    }
}
