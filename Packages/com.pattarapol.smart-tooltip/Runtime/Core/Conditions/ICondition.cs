namespace SmartTooltip.Core
{
    public interface ICondition
    {
        bool Evaluate(ITooltipContext context);
    }
}
