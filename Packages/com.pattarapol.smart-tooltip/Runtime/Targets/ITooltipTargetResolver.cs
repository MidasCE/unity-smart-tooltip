namespace SmartTooltip
{
    public interface ITooltipTargetResolver
    {
        bool TryResolve(string targetId, out ITooltipTarget target);
    }
}
