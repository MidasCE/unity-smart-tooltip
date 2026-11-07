using UnityEngine;

namespace SmartTooltip.Samples
{
    /// <summary>Slides a panel in from off screen and back out, then turns it off.</summary>
    public sealed class SlidePanel : MonoBehaviour
    {
        const float Seconds = 0.4f;

        RectTransform rect;
        Vector2 shownPosition;
        Vector2 hiddenPosition;
        float progress;
        float direction;

        public void Setup(Vector2 shown, Vector2 hidden)
        {
            rect = (RectTransform)transform;
            shownPosition = shown;
            hiddenPosition = hidden;
            CloseNow();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            direction = 1f;
        }

        public void Close() => direction = -1f;

        public void CloseNow()
        {
            progress = 0f;
            direction = 0f;
            rect.anchoredPosition = hiddenPosition;
            gameObject.SetActive(false);
        }

        void Update()
        {
            progress = Mathf.Clamp01(progress + direction * Time.unscaledDeltaTime / Seconds);
            rect.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, Mathf.SmoothStep(0f, 1f, progress));
            if (direction < 0f && progress <= 0f)
                CloseNow();
        }
    }
}
