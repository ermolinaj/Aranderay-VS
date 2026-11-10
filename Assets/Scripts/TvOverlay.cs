using UnityEngine;

namespace Aranderay
{
    // Shared styling for the controls drawn inside the TV frame.
    internal static class TvOverlay
    {
        internal static readonly Color Ink = new Color32(223, 241, 225, 255);
        internal static readonly Color Green = new Color32(151, 234, 177, 255);
        internal static readonly Color Muted = new Color32(158, 183, 167, 255);

        internal static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            var position = rect.anchoredPosition3D;
            position.z = 0;
            rect.anchoredPosition3D = position;
        }

        internal static RectTransform CreateRect(RectTransform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static RectTransform AddPanel(RectTransform parent, string name, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        internal static UnityEngine.UI.Text AddText(RectTransform parent, string name, Font font, int size, Color color,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = CreateRect(parent, name);
            var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            Format(text, font, size, color, alignment);
            return text;
        }

        internal static void Format(UnityEngine.UI.Text text, Font font, int size, Color color, TextAnchor alignment)
        {
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.alignment = alignment;
            text.alignByGeometry = false;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
        }

        internal static UnityEngine.UI.Text StyleButton(UnityEngine.UI.Button button, Font font, bool primary)
        {
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            if (button.targetGraphic != null)
                button.targetGraphic.color = Color.white;
            var colors = button.colors;
            colors.normalColor = primary ? new Color32(39, 77, 58, 255) : new Color32(29, 47, 40, 255);
            colors.highlightedColor = new Color32(55, 100, 73, 255);
            colors.selectedColor = new Color32(55, 100, 73, 255);
            colors.pressedColor = new Color32(23, 53, 39, 255);
            colors.disabledColor = new Color32(30, 37, 32, 255);
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            button.colors = colors;

            var text = button.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null)
            {
                Format(text, font, 23, Ink, TextAnchor.MiddleCenter);
                text.fontStyle = FontStyle.Bold;
                Place(text.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);
            }
            return text;
        }
    }
}
