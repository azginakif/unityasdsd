using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiCaseResultHud : MonoBehaviour
    {
        [SerializeField] private float wrongAccusationMessageDuration = 3f;

        private bool _hasResult;
        private bool _success;
        private string _resultMessage;
        private float _wrongMessageUntil;
        private CaseSessionManager _subscribedSession;
        private RectTransform _root;
        private CanvasGroup _successGroup;
        private CanvasGroup _warningGroup;
        private Text _successBodyText;
        private Text _successStatsText;
        private Text _warningText;
        private bool _built;

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            BuildIfNeeded();
            DisableLegacy();
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribedSession != null)
            {
                _subscribedSession.CaseResolved -= HandleCaseResolved;
                _subscribedSession = null;
            }
        }

        private void Update()
        {
            BuildIfNeeded();
            DisableLegacy();
            TrySubscribe();
            Refresh();
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            var canvasTransform = transform.Find("UguiCaseResultCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiCaseResultCanvas", transform);
            }

            var canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;

            var scaler = canvasTransform.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasTransform.GetComponent<GraphicRaycaster>() == null)
            {
                canvasTransform.gameObject.AddComponent<GraphicRaycaster>();
            }

            RuntimeUiFactory.Stretch(canvasTransform);
            RuntimeUiFactory.ClearChildren(canvasTransform);
            _root = canvasTransform;

            BuildSuccessPanel();
            BuildWarningPanel();
            _built = true;
        }

        private void BuildSuccessPanel()
        {
            var overlay = RuntimeUiFactory.CreateUiRoot("SuccessOverlay", _root);
            RuntimeUiFactory.Stretch(overlay);
            RuntimeUiFactory.AddImage(overlay.gameObject, new Color(0.02f, 0.03f, 0.05f, 0.88f));
            _successGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var panel = RuntimeUiFactory.CreateCard("SuccessPanel", overlay, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(760f, 470f);
            RuntimeUiFactory.AddVerticalLayout(panel, 12f, new RectOffset(24, 24, 24, 22));

            var title = RuntimeUiFactory.CreateText("Title", panel, "VAKA COZULDU", 38, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.alignment = TextAnchor.MiddleCenter;
            _successBodyText = RuntimeUiFactory.CreateText("Body", panel, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            _successBodyText.alignment = TextAnchor.MiddleCenter;
            _successStatsText = RuntimeUiFactory.CreateText("Stats", panel, string.Empty, 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            _successStatsText.alignment = TextAnchor.MiddleCenter;
            RuntimeUiFactory.CreateSpacer("Spacer", panel, 12f);
            var restartButton = RuntimeUiFactory.CreateButton("RestartButton", panel, "Vakayi Yeniden Baslat", new Color(0.22f, 0.18f, 0.08f, 1f), 18);
            RuntimeUiFactory.EnsureLayoutElement(restartButton.transform, preferredHeight: 52f, preferredWidth: 260f);
            restartButton.onClick.AddListener(RestartCase);
        }

        private void BuildWarningPanel()
        {
            var panel = RuntimeUiFactory.CreateCard("WarningPanel", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -180f);
            panel.sizeDelta = new Vector2(600f, 112f);
            RuntimeUiFactory.AddVerticalLayout(panel, 4f, new RectOffset(18, 18, 16, 12));
            _warningText = RuntimeUiFactory.CreateText("WarningText", panel, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _warningText.alignment = TextAnchor.MiddleCenter;
            _warningGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }

        private void TrySubscribe()
        {
            if (_subscribedSession != null || CaseSessionManager.Instance == null)
            {
                return;
            }

            _subscribedSession = CaseSessionManager.Instance;
            _subscribedSession.CaseResolved += HandleCaseResolved;
        }

        private void HandleCaseResolved(bool success, string resultMessage)
        {
            _hasResult = true;
            _success = success;
            _resultMessage = resultMessage;
            if (!success)
            {
                _wrongMessageUntil = Time.time + wrongAccusationMessageDuration;
            }
        }

        private void Refresh()
        {
            if (_successGroup == null || _warningGroup == null)
            {
                return;
            }

            _successGroup.alpha = _hasResult && _success ? 1f : 0f;
            _successGroup.blocksRaycasts = _hasResult && _success;
            _successGroup.interactable = _hasResult && _success;

            var showWarning = _hasResult && !_success && Time.time <= _wrongMessageUntil;
            _warningGroup.alpha = showWarning ? 1f : 0f;
            _warningGroup.blocksRaycasts = false;
            _warningGroup.interactable = false;

            if (_hasResult && _success)
            {
                var session = CaseSessionManager.Instance;
                _successBodyText.text = _resultMessage;
                if (session != null)
                {
                    _successStatsText.text =
                        $"Toplanan delil: {session.CollectedEvidenceIds.Count}\n" +
                        $"Kritik delil: {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}\n" +
                        $"Cikarim: {session.InferenceHistory.Count}\n" +
                        $"Gorusulen NPC: {session.InterviewedNpcCount}\n" +
                        $"Gecen sure: {FormatElapsedTime(session.ElapsedCaseTimeSeconds)}";
                }
            }

            if (showWarning)
            {
                var session = CaseSessionManager.Instance;
                _warningText.text = session == null ? _resultMessage : _resultMessage + "\n" + session.GetRecommendedNextStep();
            }
        }

        private void RestartCase()
        {
            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState != null && networkCaseState.IsOnlineSessionActive)
            {
                networkCaseState.RequestRestartSession();
                _hasResult = false;
                _success = false;
                _resultMessage = string.Empty;
                return;
            }

            var session = CaseSessionManager.Instance;
            if (session != null)
            {
                session.RestartCurrentCase();
            }

            _hasResult = false;
            _success = false;
            _resultMessage = string.Empty;
        }

        private void DisableLegacy()
        {
            var legacy = Object.FindAnyObjectByType<CaseResultHud>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
