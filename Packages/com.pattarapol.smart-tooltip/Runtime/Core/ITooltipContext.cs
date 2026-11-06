namespace SmartTooltip.Core
{
    /// <summary>Your game state. Conditions read from it.</summary>
    public interface ITooltipContext
    {
        bool TryGet<T>(string key, out T value);
    }
}
