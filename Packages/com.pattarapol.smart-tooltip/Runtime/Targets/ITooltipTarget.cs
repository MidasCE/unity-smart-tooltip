using UnityEngine;

namespace SmartTooltip
{
    /// <summary>Any target becomes a rect on screen, in pixels. Bottom left is (0, 0).</summary>
    public interface ITooltipTarget
    {
        string Id { get; }

        /// <summary>
        /// The rect can be off screen, so a tooltip can keep pointing at it.
        /// Return false only when the target can't be found right now, for example when it is disabled.
        /// </summary>
        bool TryGetScreenRect(out Rect screenRect);
    }
}
