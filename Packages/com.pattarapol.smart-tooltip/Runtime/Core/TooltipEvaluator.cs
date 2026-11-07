using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTooltip.Core
{
    public sealed class TooltipEvaluator
    {
        readonly IReadOnlyList<TooltipRule> rules;
        readonly ITooltipProgressStore progress;

        public TooltipEvaluator(IEnumerable<TooltipRule> rules, ITooltipProgressStore progress)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));

            var list = rules.ToList();
            var duplicate = list.GroupBy(r => r.Id).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
                throw new ArgumentException($"Duplicate tooltip rule id '{duplicate.Key}'.", nameof(rules));

            this.rules = list.OrderByDescending(r => r.Priority).ToArray();
        }

        /// <summary>
        /// Rules that can show now, best first. Seen rules and rules that are already done are left out.
        /// If a target can't be found, try the next one.
        /// </summary>
        public IEnumerable<TooltipRule> Candidates(ITooltipContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return rules.Where(r => !(r.ShowOnce && progress.HasSeen(r.Id)) && r.IsSatisfiedBy(context) && !r.IsDone(context));
        }

        public void MarkShown(TooltipRule rule)
        {
            if (rule.ShowOnce)
                progress.MarkSeen(rule.Id);
        }
    }
}
