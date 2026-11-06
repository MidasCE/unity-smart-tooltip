namespace SmartTooltip
{
    public enum TargetShape
    {
        /// <summary>UI rect if on a canvas, else renderers, else colliders, else the object position.</summary>
        Auto,
        Renderers,
        Colliders,
        CustomBox
    }
}
