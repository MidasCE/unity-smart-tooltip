using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartTooltip.Samples
{
    /// <summary>Small helpers that build uGUI from code, so the sample scene needs no hand made UI.</summary>
    static class DemoUi
    {
        static Font font;

        static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Pins the rect to one point of its parent, for example (1, 1) for the top right.</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Fills the parent, minus these gaps in pixels.</summary>
        public static RectTransform Fill(this RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            TextAnchor alignment = TextAnchor.MiddleCenter, Color? color = null)
        {
            var label = Rect(name, parent).gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color ?? Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(string name, Transform parent, string text, Color color, Action onClick)
        {
            var image = Panel(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            Label("Label", image.transform, text, 36).rectTransform.Fill();
            return button;
        }

        public static void SetText(this Button button, string text) => button.GetComponentInChildren<Text>().text = text;
    }
}
