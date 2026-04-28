using MobilOfl.Gameplay;
using UnityEngine;

namespace MobilOfl.UI
{
    public class CaseChecklistHud : MonoBehaviour
    {
        [SerializeField] private CaseProgressTracker progressTracker;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _doneItemStyle;
        private GUIStyle _progressStyle;

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay || progressTracker == null || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            EnsureStyles();

            var rect = new Rect(18f, Screen.height - 224f, 370f, 206f);
            GUILayout.BeginArea(rect, _panelStyle);
            GUILayout.Label("VAKA LISTESI", _titleStyle);
            GUILayout.Space(6f);

            var progressRect = GUILayoutUtility.GetRect(10f, 20f, GUILayout.ExpandWidth(true));
            ModernGuiTheme.DrawProgress(
                progressRect,
                progressTracker.GetCompletionRatio(),
                ModernGuiTheme.AccentColor,
                $"{Mathf.RoundToInt(progressTracker.GetCompletionRatio() * 100f)}% tamamlandi",
                _progressStyle);

            GUILayout.Space(8f);

            var session = CaseSessionManager.Instance;
            if (session != null)
            {
                GUILayout.Label("Taktik: " + session.GetRecommendedNextStep(), _itemStyle);
                GUILayout.Space(6f);
            }

            var steps = progressTracker.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var prefix = steps[i].IsCompleted ? "[x]" : "[ ]";
                GUILayout.Label($"{prefix} {steps[i].Label}", steps[i].IsCompleted ? _doneItemStyle : _itemStyle);
            }

            GUILayout.EndArea();
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentWarmColor);
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = ModernGuiTheme.CreatePanelStyle(new RectOffset(16, 16, 14, 14));
            _titleStyle = ModernGuiTheme.CreateLabelStyle(16, true, false);
            _itemStyle = ModernGuiTheme.CreateLabelStyle(13, false, false, ModernGuiTheme.TextColor);
            _doneItemStyle = ModernGuiTheme.CreateLabelStyle(13, true, false, ModernGuiTheme.AccentColor);
            _progressStyle = ModernGuiTheme.CreateLabelStyle(12, true, true, ModernGuiTheme.TextColor);
        }
    }
}


