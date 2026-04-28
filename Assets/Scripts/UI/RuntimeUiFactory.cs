using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public static class RuntimeUiFactory
    {
        private static Font _defaultFont;

        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont != null)
                {
                    return _defaultFont;
                }

                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _defaultFont;
            }
        }

        public static RectTransform CreateUiRoot(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public static Image AddImage(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }

            image.color = color;
            return image;
        }

        public static Outline AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        public static Shadow AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            return shadow;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            Color color,
            FontStyle fontStyle,
            TextAnchor anchor)
        {
            var rect = CreateUiRoot(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, fontSize + 12f);
            rect.localScale = Vector3.one;

            var text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;

            var fitter = rect.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, int fontSize = 18)
        {
            var rect = CreateUiRoot(name, parent);
            var image = AddImage(rect.gameObject, backgroundColor);
            AddOutline(rect.gameObject, new Color(0f, 0f, 0f, 0.6f), new Vector2(1f, -1f));
            AddShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -3f));

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 0.9f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelText = CreateText("Label", rect, label, fontSize, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(labelText.rectTransform);
            return button;
        }

        public static InputField CreateInputField(string name, Transform parent, string placeholderText, int fontSize = 18)
        {
            var rect = CreateUiRoot(name, parent);
            AddImage(rect.gameObject, new Color(0.07f, 0.09f, 0.12f, 0.96f));
            AddOutline(rect.gameObject, ModernGuiTheme.BorderColor, new Vector2(1f, -1f));

            var textArea = CreateUiRoot("TextArea", rect);
            Stretch(textArea);
            textArea.offsetMin = new Vector2(18f, 12f);
            textArea.offsetMax = new Vector2(-18f, -12f);

            var placeholder = CreateText(
                "Placeholder",
                textArea,
                placeholderText,
                fontSize,
                new Color(ModernGuiTheme.MutedTextColor.r, ModernGuiTheme.MutedTextColor.g, ModernGuiTheme.MutedTextColor.b, 0.74f),
                FontStyle.Normal,
                TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform);

            var text = CreateText("Text", textArea, string.Empty, fontSize, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform);

            var inputField = rect.gameObject.AddComponent<InputField>();
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.caretColor = ModernGuiTheme.AccentWarmColor;
            inputField.selectionColor = new Color(ModernGuiTheme.AccentWarmColor.r, ModernGuiTheme.AccentWarmColor.g, ModernGuiTheme.AccentWarmColor.b, 0.35f);
            return inputField;
        }

        public static ScrollRect CreateScrollView(string name, Transform parent, out RectTransform content)
        {
            var root = CreateUiRoot(name, parent);
            AddImage(root.gameObject, new Color(0.05f, 0.06f, 0.08f, 0.72f));
            AddOutline(root.gameObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(1f, -1f));

            var viewport = CreateUiRoot("Viewport", root);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateUiRoot("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            content.localScale = Vector3.one;

            var scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            return scrollRect;
        }

        public static VerticalLayoutGroup AddVerticalLayout(Transform target, float spacing, RectOffset padding, bool controlHeight = true)
        {
            var layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(Transform target, float spacing, RectOffset padding, bool forceExpandHeight = false)
        {
            var layout = target.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = forceExpandHeight;
            return layout;
        }

        public static ContentSizeFitter AddContentSizeFitter(Transform target, ContentSizeFitter.FitMode verticalMode)
        {
            var fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = target.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = verticalMode;
            return fitter;
        }

        public static LayoutElement EnsureLayoutElement(Transform target, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.gameObject.AddComponent<LayoutElement>();
            }

            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static RectTransform CreateCard(string name, Transform parent, Color color, Color accentColor)
        {
            var rect = CreateUiRoot(name, parent);
            AddImage(rect.gameObject, color);
            AddOutline(rect.gameObject, ModernGuiTheme.BorderColor, new Vector2(1f, -1f));
            AddShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -4f));

            var accent = CreateUiRoot("Accent", rect);
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.sizeDelta = new Vector2(0f, 6f);
            accent.anchoredPosition = Vector2.zero;
            AddImage(accent.gameObject, accentColor);
            var accentLayout = EnsureLayoutElement(accent, preferredHeight: 6f);
            accentLayout.ignoreLayout = true;

            return rect;
        }

        public static GameObject CreateSpacer(string name, Transform parent, float preferredHeight)
        {
            var spacer = CreateUiRoot(name, parent).gameObject;
            EnsureLayoutElement(spacer.transform, preferredHeight: preferredHeight);
            return spacer;
        }

        public static void ClearChildren(Transform target)
        {
            for (var i = target.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(target.GetChild(i).gameObject);
            }
        }
    }
}
