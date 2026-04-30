using UnityEngine;

namespace MobilOfl.UI
{
    public static class ModernGuiTheme
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.12f, 0.975f);
        public static readonly Color PanelSoftColor = new Color(0.11f, 0.14f, 0.18f, 0.95f);
        public static readonly Color BorderColor = new Color(0.2f, 0.29f, 0.34f, 0.96f);
        public static readonly Color AccentColor = new Color(0.89f, 0.77f, 0.45f, 1f);
        public static readonly Color AccentWarmColor = new Color(0.33f, 0.82f, 0.78f, 1f);
        public static readonly Color TextColor = new Color(0.96f, 0.97f, 0.94f, 1f);
        public static readonly Color MutedTextColor = new Color(0.7f, 0.77f, 0.8f, 1f);

        private static Texture2D _whiteTexture;
        private static Texture2D _panelTexture;
        private static Texture2D _panelSoftTexture;
        private static Texture2D _buttonTexture;
        private static Texture2D _buttonActiveTexture;
        private static Texture2D _inputTexture;

        public static Texture2D WhiteTexture => _whiteTexture != null ? _whiteTexture : (_whiteTexture = Texture2D.whiteTexture);
        public static Texture2D PanelTexture => _panelTexture != null ? _panelTexture : (_panelTexture = MakeTexture(PanelColor));
        public static Texture2D PanelSoftTexture => _panelSoftTexture != null ? _panelSoftTexture : (_panelSoftTexture = MakeTexture(PanelSoftColor));
        public static Texture2D ButtonTexture => _buttonTexture != null ? _buttonTexture : (_buttonTexture = MakeTexture(new Color(0.12f, 0.16f, 0.19f, 1f)));
        public static Texture2D ButtonActiveTexture => _buttonActiveTexture != null ? _buttonActiveTexture : (_buttonActiveTexture = MakeTexture(new Color(0.22f, 0.27f, 0.21f, 1f)));
        public static Texture2D InputTexture => _inputTexture != null ? _inputTexture : (_inputTexture = MakeTexture(new Color(0.08f, 0.11f, 0.14f, 1f)));

        public static GUIStyle CreatePanelStyle(RectOffset padding = null)
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = PanelTexture, textColor = TextColor },
                border = new RectOffset(1, 1, 1, 1),
                padding = padding ?? new RectOffset(18, 18, 16, 16),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        public static GUIStyle CreateSoftPanelStyle(RectOffset padding = null)
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = PanelSoftTexture, textColor = TextColor },
                border = new RectOffset(1, 1, 1, 1),
                padding = padding ?? new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        public static GUIStyle CreateLabelStyle(int fontSize, bool bold = false, bool centered = false, Color? color = null)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft,
                wordWrap = true,
                richText = false,
                normal = { textColor = color ?? TextColor }
            };
        }

        public static GUIStyle CreateButtonStyle(int fontSize = 15)
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f,
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(2, 2, 2, 2),
                normal = { background = ButtonTexture, textColor = TextColor },
                hover = { background = ButtonActiveTexture, textColor = TextColor },
                active = { background = ButtonActiveTexture, textColor = TextColor }
            };
        }

        public static GUIStyle CreateTextFieldStyle(int fontSize = 15)
        {
            return new GUIStyle(GUI.skin.textField)
            {
                fontSize = fontSize,
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(2, 2, 2, 2),
                normal = { background = InputTexture, textColor = TextColor },
                focused = { background = InputTexture, textColor = TextColor }
            };
        }

        public static void DrawPanelChrome(Rect rect, Color accent)
        {
            DrawRect(new Rect(rect.x + 5f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.26f));
            DrawRect(rect, new Color(0f, 0f, 0f, 0.08f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 6f), accent);
            DrawRect(new Rect(rect.x, rect.y + 6f, rect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), BorderColor);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BorderColor);
            DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), BorderColor);
            DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BorderColor);
            DrawRect(new Rect(rect.x + 18f, rect.y + 14f, 68f, 2f), new Color(accent.r, accent.g, accent.b, 0.5f));
        }

        public static void DrawMetricPill(Rect rect, string text, Color accent, GUIStyle labelStyle)
        {
            DrawRect(rect, PanelSoftColor);
            DrawRect(new Rect(rect.x, rect.y, 6f, rect.height), accent);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
            GUI.Label(rect, text, labelStyle);
        }

        public static void DrawProgress(Rect rect, float normalized, Color fillColor, string overlayText, GUIStyle overlayStyle)
        {
            DrawRect(rect, new Color(0.08f, 0.09f, 0.11f, 1f));
            DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), new Color(0.12f, 0.14f, 0.17f, 1f));
            DrawRect(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(normalized)), rect.height - 4f), fillColor);
            DrawRect(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(normalized)), 3f), new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(rect, overlayText, overlayStyle);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = previousColor;
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }
}
