using UnityEngine;
using UnityEngine.UI;

namespace SmartTooltip
{
    /// <summary>
    /// The tooltip box. Make your own prefab with this component to change the look.
    /// The arrow must point up and have its pivot in the center. The overlay turns it.
    /// </summary>
    public class TooltipBubble : MonoBehaviour
    {
        [SerializeField] RectTransform arrow;
        [SerializeField] Text text;
        [SerializeField] float maxWidth = 560f;
        [SerializeField] Vector2 padding = new Vector2(28f, 20f);

        public RectTransform Rect => (RectTransform)transform;
        public RectTransform Arrow => arrow;
        public float ArrowLength => arrow != null ? arrow.rect.height : 0f;

        /// <summary>Override this if your bubble uses TextMeshPro or a layout group.</summary>
        public virtual void SetText(string value)
        {
            if (text == null)
                return;

            text.text = value;
            var width = Mathf.Min(text.preferredWidth, maxWidth);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            var height = text.preferredHeight;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + padding.x * 2);
            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + padding.y * 2);
        }

        internal static TooltipBubble CreateDefault(Transform parent)
        {
            var background = NewImage("Bubble", parent, DefaultSprites.RoundedRect);
            background.type = Image.Type.Sliced;

            var arrowImage = NewImage("Arrow", background.transform, DefaultSprites.Arrow);
            arrowImage.rectTransform.sizeDelta = new Vector2(36f, 18f);

            var label = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(background.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.color = new Color32(30, 30, 30, 255);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            var bubble = background.gameObject.AddComponent<TooltipBubble>();
            bubble.arrow = arrowImage.rectTransform;
            bubble.text = label;
            return bubble;
        }

        static Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
