using MobilOfl.Gameplay;
using UnityEngine;

namespace MobilOfl.UI
{
    public class FocusReticleHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private float size = 18f;

        private Texture2D _whiteTexture;
        private GUIStyle _promptStyle;

        private void Update()
        {
            EnsurePlayerInteraction();
        }

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            if (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved)
            {
                return;
            }

            EnsureResources();
            EnsurePlayerInteraction();

            var hasTarget = playerInteraction != null && playerInteraction.CurrentInteractable != null;
            var color = hasTarget ? new Color(0.2f, 0.95f, 0.65f, 0.95f) : new Color(1f, 1f, 1f, 0.55f);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            DrawLine(new Rect(center.x - size - 12f, center.y - 1f, size, 2f), color);
            DrawLine(new Rect(center.x + 12f, center.y - 1f, size, 2f), color);
            DrawLine(new Rect(center.x - 1f, center.y - size - 12f, 2f, size), color);
            DrawLine(new Rect(center.x - 1f, center.y + 12f, 2f, size), color);

            if (!hasTarget)
            {
                return;
            }

            var promptRect = new Rect(center.x - 170f, center.y + 42f, 340f, 42f);
            GUI.Label(promptRect, playerInteraction.CurrentInteractable.PromptText, _promptStyle);
            ModernGuiTheme.DrawPanelChrome(promptRect, ModernGuiTheme.AccentColor);
        }

        private void DrawLine(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsurePlayerInteraction()
        {
            if (playerInteraction != null && playerInteraction.isActiveAndEnabled)
            {
                return;
            }

            var candidates = Object.FindObjectsByType<PlayerInteractionController>(FindObjectsInactive.Exclude);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].isActiveAndEnabled)
                {
                    playerInteraction = candidates[i];
                    return;
                }
            }
        }

        private void EnsureResources()
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = Texture2D.whiteTexture;
            }

            if (_promptStyle != null)
            {
                return;
            }

            _promptStyle = ModernGuiTheme.CreateSoftPanelStyle(new RectOffset(10, 10, 8, 8));
            _promptStyle.alignment = TextAnchor.MiddleCenter;
            _promptStyle.fontSize = 17;
            _promptStyle.fontStyle = FontStyle.Bold;
        }
    }
}