namespace SmartTooltip.Core
{
    /// <summary>What a rule's target needs before the tip can start.</summary>
    public enum TargetRequirement
    {
        /// <summary>The target must be on screen. Good for UI.</summary>
        OnScreen,

        /// <summary>The target only has to exist. If it is off screen, the tip points toward it.</summary>
        Exists
    }
}
