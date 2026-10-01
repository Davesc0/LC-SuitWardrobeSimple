using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SuitWardrobeSimple
{
    internal static class Ui
    {
        internal static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.6f);
        internal static readonly Color Panel = new Color(0.06f, 0.055f, 0.05f, 0.97f);
        internal static readonly Color Inset = new Color(0.1f, 0.09f, 0.08f, 1f);
        internal static readonly Color Raised = new Color(0.16f, 0.14f, 0.12f, 1f);
        internal static readonly Color Accent = new Color(1f, 0.46f, 0.13f, 1f);
        internal static readonly Color AccentDark = new Color(0.45f, 0.19f, 0.05f, 1f);
        internal static readonly Color Text = new Color(1f, 0.88f, 0.78f, 1f);
        internal static readonly Color Muted = new Color(0.62f, 0.55f, 0.5f, 1f);

        internal static TMP_FontAsset Font => HUDManager.Instance != null && HUDManager.Instance.chatText != null
            ? HUDManager.Instance.chatText.font
            : null;

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Fill(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        internal static void Box(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        internal static void Column(RectTransform rect, float left, float top, float width, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(left + width, -top);
        }

        internal static Image Image(Transform parent, string name, Color color)
        {
            Image image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static TextMeshProUGUI Label(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null)
                label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = Text;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        // Tints can only darken, so buttons rest dimmed and the multiplier restores full colour on hover.
        internal static Button Button(Transform parent, string name, string text, float size, Color color, UnityAction onClick)
        {
            Image background = Image(parent, name, color);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.66f, 0.66f, 0.66f, 1f),
                highlightedColor = Color.white,
                pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f),
                selectedColor = new Color(0.66f, 0.66f, 0.66f, 1f),
                disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f),
                colorMultiplier = 1.5f,
                fadeDuration = 0.06f
            };
            if (onClick != null)
                button.onClick.AddListener(onClick);

            if (text != null)
            {
                TextMeshProUGUI label = Label(background.transform, "Label", text, size, TextAlignmentOptions.Center);
                Fill(label.rectTransform);
            }
            return button;
        }

        internal static TMP_InputField InputField(Transform parent, string name, string placeholder, float size)
        {
            RectTransform root = Rect(name, parent);

            // Kept inactive until it's set up, the field looks for its parts on enable.
            root.gameObject.SetActive(false);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = Inset;

            RectTransform viewport = Rect("Text Area", root);
            Fill(viewport, 8f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI hint = Label(viewport, "Placeholder", placeholder, size);
            hint.color = Muted;
            hint.fontStyle = FontStyles.Italic;
            Fill(hint.rectTransform);

            TextMeshProUGUI text = Label(viewport, "Text", string.Empty, size);
            Fill(text.rectTransform);

            TMP_InputField field = root.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.textViewport = viewport;
            field.textComponent = text;
            field.placeholder = hint;
            if (Font != null)
                field.fontAsset = Font;
            field.pointSize = size;
            field.customCaretColor = true;
            field.caretColor = Accent;
            field.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            field.lineType = TMP_InputField.LineType.SingleLine;

            root.gameObject.SetActive(true);
            return field;
        }

        internal static RectTransform Scroll(Transform parent, string name, bool vertical, out ScrollRect scroll)
        {
            RectTransform root = Rect(name, parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();

            // Invisible background, so the mouse wheel works over the empty parts too.
            Image catcher = Image(root, "Viewport", new Color(0f, 0f, 0f, 0f));
            RectTransform viewport = catcher.rectTransform;
            Fill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Rect("Content", viewport);
            if (vertical)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
            }
            content.sizeDelta = Vector2.zero;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fitter.horizontalFit = vertical ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.vertical = vertical;
            scroll.horizontal = !vertical;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 45f;
            return content;
        }
    }
}
