using System;

namespace SmartTooltip.Core
{
    public sealed class TooltipPrompt
    {
        public string TargetId { get; }
        public string Text { get; }
        public Placement Placement { get; }

        /// <summary>Popup style name. Null uses the default style.</summary>
        public string Style { get; }

        public TooltipPrompt(string targetId, string text, Placement placement = Placement.Auto, string style = null)
        {
            if (string.IsNullOrEmpty(targetId))
                throw new ArgumentException("Target id is required.", nameof(targetId));

            TargetId = targetId;
            Text = text ?? string.Empty;
            Placement = placement;
            Style = style;
        }
    }
}
