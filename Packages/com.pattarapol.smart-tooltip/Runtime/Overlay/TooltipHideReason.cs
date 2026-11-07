namespace SmartTooltip
{
    public enum TooltipHideReason
    {
        /// <summary>Your code called Hide.</summary>
        Requested,

        /// <summary>The player tapped outside the hole.</summary>
        OutsideTap,

        /// <summary>The target was disabled or destroyed.</summary>
        TargetLost
    }
}
