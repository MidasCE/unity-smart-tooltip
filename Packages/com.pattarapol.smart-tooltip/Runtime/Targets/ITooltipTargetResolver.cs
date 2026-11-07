namespace SmartTooltip
{
    public interface ITooltipTargetResolver
    {
        /// <summary>Finds a target with this id that is on screen right now.</summary>
        bool TryResolve(string targetId, out ITooltipTarget target);

        /// <summary>Finds a target with this id even if it is off screen or behind the camera. On screen ones win.</summary>
        bool TryFind(string targetId, out ITooltipTarget target);
    }
}
