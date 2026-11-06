using System.Collections.Generic;

namespace SmartTooltip.Core
{
    public sealed class InMemoryProgressStore : ITooltipProgressStore
    {
        readonly HashSet<string> seen = new HashSet<string>();

        public bool HasSeen(string ruleId) => seen.Contains(ruleId);
        public void MarkSeen(string ruleId) => seen.Add(ruleId);
        public void ResetAll() => seen.Clear();
    }
}
