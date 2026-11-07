namespace SmartTooltip
{
    public enum TooltipEndReason
    {
        /// <summary>The rule's done condition became true, or the player tapped the target of a rule without one. Counts as seen.</summary>
        Completed,

        /// <summary>The player tapped outside the hole. Counts as seen.</summary>
        Dismissed,

        /// <summary>The target was disabled or destroyed. Not seen, so it can show again.</summary>
        TargetLost,

        /// <summary>
        /// The rule's show conditions stopped matching, for example the player left the screen.
        /// Or a hold started, Stop was called, or other code hid the overlay. Not seen, so it can show again.
        /// </summary>
        Interrupted
    }
}
