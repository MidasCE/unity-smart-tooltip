namespace SmartTooltip.Core
{
    public interface ITooltipProgressStore
    {
        bool HasSeen(string ruleId);
        void MarkSeen(string ruleId);
        void ResetAll();
    }
}
