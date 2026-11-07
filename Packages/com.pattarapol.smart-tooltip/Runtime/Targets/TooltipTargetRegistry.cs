using System;
using System.Collections.Generic;

namespace SmartTooltip
{
    public sealed class TooltipTargetRegistry : ITooltipTargetResolver
    {
        public static TooltipTargetRegistry Default { get; } = new TooltipTargetRegistry();

        readonly Dictionary<string, List<ITooltipTarget>> byId = new Dictionary<string, List<ITooltipTarget>>();
        readonly Dictionary<ITooltipTarget, string> registeredIds = new Dictionary<ITooltipTarget, string>();

        /// <summary>Safe to call again after the target's id changes.</summary>
        public void Register(ITooltipTarget target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            Unregister(target);
            if (string.IsNullOrEmpty(target.Id))
                return;

            if (!byId.TryGetValue(target.Id, out var list))
                byId[target.Id] = list = new List<ITooltipTarget>();

            list.Add(target);
            registeredIds[target] = target.Id;
        }

        public void Unregister(ITooltipTarget target)
        {
            if (target == null || !registeredIds.TryGetValue(target, out var id))
                return;

            registeredIds.Remove(target);
            var list = byId[id];
            list.Remove(target);
            if (list.Count == 0)
                byId.Remove(id);
        }

        /// <summary>Many objects can share an id. The newest one that is on screen wins.</summary>
        public bool TryResolve(string targetId, out ITooltipTarget target)
        {
            target = null;
            if (string.IsNullOrEmpty(targetId) || !byId.TryGetValue(targetId, out var list))
                return false;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].TryGetScreenRect(out var rect) && ScreenProjection.IsOnScreen(rect))
                {
                    target = list[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryFind(string targetId, out ITooltipTarget target)
        {
            if (TryResolve(targetId, out target))
                return true;
            if (string.IsNullOrEmpty(targetId) || !byId.TryGetValue(targetId, out var list))
                return false;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].TryGetScreenRect(out _))
                {
                    target = list[i];
                    return true;
                }
            }

            return false;
        }
    }
}
