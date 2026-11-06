using System.Collections.Generic;

namespace SmartTooltip.Core
{
    public sealed class TooltipContext : ITooltipContext
    {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public TooltipContext Set<T>(string key, T value)
        {
            values[key] = value;
            return this;
        }

        public bool Remove(string key) => values.Remove(key);

        public bool TryGet<T>(string key, out T value)
        {
            if (values.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}
