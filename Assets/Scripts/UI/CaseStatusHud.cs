using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.UI
{
    public class CaseStatusHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private float messageDuration = 5f;

        private string _message = "Vaka baslatiliyor...";
        private float _messageUntil;
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _objectiveStyle;
        private CaseSessionManager _subscribedSession;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribedSession != null)
            {
                _subscribedSession.SessionMessagePublished -= HandleSessionMessage;
                _subscribedSession.EvidenceCollected -= HandleEvidenceCollected;
                _subscribedSession = null;
            }
        }

        private void Update()
        {
            TrySubscribe();
            EnsurePlayerInteraction();
        }

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay)
            {
                return;
            }

            EnsureStyles();
            EnsurePlayerInteraction();
            DrawTopLeftStatus();
            DrawTopRightObjective();
            DrawMessage();
        }

        private void HandleSessionMessage(string message)
        {
            ShowMessage(message);
        }

        private void TrySubscribe()
        {
            if (_subscribedSession != null || CaseSessionManager.Instance == null)
            {
                return;
            }

            _subscribedSession = CaseSessionManager.Instance;
            _subscribedSession.SessionMessagePublished += HandleSessionMessage;
            _subscribedSession.EvidenceCollected += HandleEvidenceCollected;

            if (_subscribedSession.ActiveCase != null)
            {
                ShowMessage(_subscribedSession.ActiveCase.OpeningBrief);
            }
        }

        private void HandleEvidenceCollected(EvidenceData evidence)
        {
            if (evidence != null && evidence.IsCritical)
            {
                ShowMessage($"Kritik delil: {evidence.Title}");
            }
        }

        private void ShowMessage(string message)
        {
            _message = message;
            _messageUntil = Time.time + messageDuration;
        }

                private void DrawTopLeftStatus()
        {
            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null)
            {
                return;
            }

            var rect = new Rect(18f, 18f, 390f, 186f);
            GUILayout.BeginArea(rect, _boxStyle);
            GUILayout.Label(session.ActiveCase.CaseTitle, _titleStyle);
            GUILayout.Label($"Operatif: {PlayerProfileSettings.LoadPlayerName()}", _messageStyle);
            GUILayout.Label($"Delil: {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}", _messageStyle);
            GUILayout.Label($"Kritik delil: {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}", _messageStyle);
            GUILayout.Label($"Sorgu kaydi: {session.InterviewedNpcCount}", _messageStyle);
            GUILayout.Label($"Sure: {FormatElapsedTime(session.ElapsedCaseTimeSeconds)}", _messageStyle);

            if (playerInteraction != null)
            {
                GUILayout.Label($"Bolge: {SchoolLocationUtility.GetZoneTitle(playerInteraction.transform.position)}", _messageStyle);
            }

            var exploration = Object.FindAnyObjectByType<SchoolExplorationTracker>();
            if (exploration != null)
            {
                GUILayout.Label($"Kesif: {exploration.VisitedZoneCount} bolge", _messageStyle);
            }

            var onlineBootstrap = Object.FindAnyObjectByType<RelayNetworkBootstrap>();
            if (onlineBootstrap != null)
            {
                var onlineText = string.IsNullOrWhiteSpace(onlineBootstrap.CurrentJoinCode)
                    ? onlineBootstrap.CurrentStatus
                    : $"Co-op: {onlineBootstrap.CurrentStatus} [{onlineBootstrap.CurrentJoinCode}]";
                GUILayout.Label(onlineText, _messageStyle);
            }

            if (playerInteraction != null && playerInteraction.CurrentInteractable != null)
            {
                GUILayout.Label($"Bakilan hedef: {playerInteraction.CurrentInteractable.PromptText}", _messageStyle);
            }

            GUILayout.EndArea();
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentColor);
        }

        private void DrawTopRightObjective()
        {
            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null)
            {
                return;
            }

            var rect = new Rect(Screen.width - 388f, 18f, 370f, 126f);
            GUILayout.BeginArea(rect, _boxStyle);
            GUILayout.Label("Anlik Hedef", _titleStyle);
            GUILayout.Label(session.GetRecommendedNextStep(), _objectiveStyle);
            GUILayout.EndArea();
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentWarmColor);
        }

        private void DrawMessage()
        {
            if (Time.time > _messageUntil || string.IsNullOrWhiteSpace(_message))
            {
                return;
            }

            var width = Mathf.Min(760f, Screen.width - 40f);
            var height = 92f;
            var rect = new Rect((Screen.width - width) * 0.5f, 28f, width, height);
            GUI.Label(rect, _message, _boxStyle);
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentColor);
        }

        private void EnsurePlayerInteraction()
        {
            if (playerInteraction != null && playerInteraction.isActiveAndEnabled)
            {
                return;
            }

            var candidates = Object.FindObjectsByType<PlayerInteractionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].isActiveAndEnabled)
                {
                    playerInteraction = candidates[i];
                    return;
                }
            }
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = ModernGuiTheme.CreatePanelStyle(new RectOffset(16, 16, 12, 12));
            _titleStyle = ModernGuiTheme.CreateLabelStyle(20, true, false);
            _messageStyle = ModernGuiTheme.CreateLabelStyle(15, false, false, ModernGuiTheme.MutedTextColor);
            _objectiveStyle = ModernGuiTheme.CreateLabelStyle(16, true, false);
        }
    }
}



