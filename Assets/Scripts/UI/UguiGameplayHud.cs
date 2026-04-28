using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiGameplayHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private CaseProgressTracker progressTracker;
        [SerializeField] private float messageDuration = 5f;
        [SerializeField] private float locationDuration = 2.4f;

        private Canvas _canvas;
        private RectTransform _root;
        private Text _statusTitleText;
        private Text _statusBodyText;
        private Text _objectiveText;
        private Text _messageText;
        private CanvasGroup _messageGroup;
        private Text _locationTitleText;
        private Text _locationSubtitleText;
        private CanvasGroup _locationGroup;
        private Text _waypointTitleText;
        private Text _waypointBodyText;
        private Text _waypointMetaText;
        private Image _progressFill;
        private Text _progressText;
        private Text _tacticText;
        private RectTransform _stepsContent;
        private float _messageUntil;
        private float _locationUntil;
        private string _currentMessage = string.Empty;
        private string _currentLocationTitle = string.Empty;
        private string _currentLocationSubtitle = string.Empty;
        private float _nextRefreshAt;
        private CaseSessionManager _subscribedSession;
        private bool _built;

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            BuildIfNeeded();
            DisableLegacyHudScripts();
            TrySubscribe();
            RefreshImmediate();
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
            BuildIfNeeded();
            DisableLegacyHudScripts();
            ResolveReferences();
            TrySubscribe();
            UpdateLocationBanner();

            if (_root != null)
            {
                _root.gameObject.SetActive(!MainMenuHud.IsBlockingGameplay && !CaseNotebookHud.IsAnyNotebookOpen);
            }

            if (Time.unscaledTime >= _nextRefreshAt)
            {
                RefreshImmediate();
                _nextRefreshAt = Time.unscaledTime + 0.2f;
            }
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            var canvasTransform = transform.Find("UguiGameplayCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiGameplayCanvas", transform);
            }

            _canvas = canvasTransform.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 70;

            var scaler = canvasTransform.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = canvasTransform.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            RuntimeUiFactory.Stretch(canvasTransform);
            RuntimeUiFactory.ClearChildren(canvasTransform);

            _root = canvasTransform;
            BuildStatusCard();
            BuildObjectiveCard();
            BuildMessageBanner();
            BuildLocationBanner();
            BuildWaypointCard();
            BuildChecklistCard();
            _built = true;
        }

        private void BuildStatusCard()
        {
            var card = RuntimeUiFactory.CreateCard("StatusCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(0f, 1f);
            card.pivot = new Vector2(0f, 1f);
            card.anchoredPosition = new Vector2(18f, -18f);
            card.sizeDelta = new Vector2(420f, 218f);
            RuntimeUiFactory.AddVerticalLayout(card, 6f, new RectOffset(16, 16, 18, 14), false);
            _statusTitleText = RuntimeUiFactory.CreateText("StatusTitle", card, "Vaka", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusBodyText = RuntimeUiFactory.CreateText("StatusBody", card, string.Empty, 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildObjectiveCard()
        {
            var card = RuntimeUiFactory.CreateCard("ObjectiveCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(1f, 1f);
            card.anchorMax = new Vector2(1f, 1f);
            card.pivot = new Vector2(1f, 1f);
            card.anchoredPosition = new Vector2(-18f, -18f);
            card.sizeDelta = new Vector2(430f, 138f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 18, 14), false);
            RuntimeUiFactory.CreateText("ObjectiveLabel", card, "ANLIK HEDEF", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _objectiveText = RuntimeUiFactory.CreateText("ObjectiveBody", card, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
        }

        private void BuildMessageBanner()
        {
            var card = RuntimeUiFactory.CreateCard("MessageBanner", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -26f);
            card.sizeDelta = new Vector2(780f, 92f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(20, 20, 18, 12), false);
            _messageText = RuntimeUiFactory.CreateText("MessageText", card, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _messageGroup = card.gameObject.GetComponent<CanvasGroup>();
            if (_messageGroup == null)
            {
                _messageGroup = card.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void BuildLocationBanner()
        {
            var card = RuntimeUiFactory.CreateCard("LocationBanner", _root, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -132f);
            card.sizeDelta = new Vector2(540f, 92f);
            RuntimeUiFactory.AddVerticalLayout(card, 4f, new RectOffset(18, 18, 16, 12), false);
            _locationTitleText = RuntimeUiFactory.CreateText("LocationTitle", card, string.Empty, 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _locationSubtitleText = RuntimeUiFactory.CreateText("LocationSubtitle", card, string.Empty, 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
            _locationGroup = card.gameObject.GetComponent<CanvasGroup>();
            if (_locationGroup == null)
            {
                _locationGroup = card.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void BuildWaypointCard()
        {
            var card = RuntimeUiFactory.CreateCard("WaypointCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(0.5f, 0f);
            card.anchorMax = new Vector2(0.5f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(0f, 20f);
            card.sizeDelta = new Vector2(560f, 108f);
            RuntimeUiFactory.AddVerticalLayout(card, 4f, new RectOffset(16, 16, 16, 12), false);
            _waypointTitleText = RuntimeUiFactory.CreateText("WaypointTitle", card, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _waypointBodyText = RuntimeUiFactory.CreateText("WaypointBody", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _waypointMetaText = RuntimeUiFactory.CreateText("WaypointMeta", card, string.Empty, 12, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildChecklistCard()
        {
            var card = RuntimeUiFactory.CreateCard("ChecklistCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(0f, 0f);
            card.anchorMax = new Vector2(0f, 0f);
            card.pivot = new Vector2(0f, 0f);
            card.anchoredPosition = new Vector2(18f, 18f);
            card.sizeDelta = new Vector2(430f, 246f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 18, 14), false);
            RuntimeUiFactory.CreateText("ChecklistLabel", card, "VAKA LISTESI", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var progressShell = RuntimeUiFactory.CreateUiRoot("ProgressShell", card);
            RuntimeUiFactory.EnsureLayoutElement(progressShell, preferredHeight: 28f);
            RuntimeUiFactory.AddImage(progressShell.gameObject, new Color(0.07f, 0.09f, 0.11f, 1f));
            RuntimeUiFactory.AddOutline(progressShell.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(1f, -1f));
            _progressFill = RuntimeUiFactory.CreateUiRoot("Fill", progressShell).gameObject.AddComponent<Image>();
            var fillRect = _progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(0f, -2f);
            _progressFill.color = ModernGuiTheme.AccentColor;
            _progressText = RuntimeUiFactory.CreateText("ProgressText", progressShell, string.Empty, 13, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);

            _tacticText = RuntimeUiFactory.CreateText("TacticText", card, string.Empty, 13, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            var scroll = RuntimeUiFactory.CreateScrollView("StepsScroll", card, out _stepsContent);
            RuntimeUiFactory.EnsureLayoutElement(scroll.transform, flexibleHeight: 1f, preferredHeight: 124f);
            RuntimeUiFactory.AddVerticalLayout(_stepsContent, 6f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_stepsContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void RefreshImmediate()
        {
            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null)
            {
                return;
            }

            RefreshStatus(session);
            RefreshObjective(session);
            RefreshWaypoint(session);
            RefreshChecklist(session);
            RefreshMessageBanner();
            RefreshLocationVisuals();
        }

        private void RefreshStatus(CaseSessionManager session)
        {
            _statusTitleText.text = session.ActiveCase.CaseTitle;
            var status =
                $"Operatif: {PlayerProfileSettings.LoadPlayerName()}\n" +
                $"Delil: {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}\n" +
                $"Kritik: {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}\n" +
                $"Sorgu kaydi: {session.InterviewedNpcCount}\n" +
                $"Sure: {FormatElapsedTime(session.ElapsedCaseTimeSeconds)}";

            if (playerInteraction != null)
            {
                status += "\nBolge: " + SchoolLocationUtility.GetZoneTitle(playerInteraction.transform.position);
                if (playerInteraction.CurrentInteractable != null)
                {
                    status += "\nBakilan hedef: " + playerInteraction.CurrentInteractable.PromptText;
                }
            }

            var exploration = Object.FindFirstObjectByType<SchoolExplorationTracker>();
            if (exploration != null)
            {
                status += $"\nKesif: {exploration.VisitedZoneCount} bolge";
            }

            var bootstrap = Object.FindFirstObjectByType<RelayNetworkBootstrap>();
            if (bootstrap != null)
            {
                status += string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode)
                    ? "\nCo-op: " + bootstrap.CurrentStatus
                    : $"\nCo-op: {bootstrap.CurrentStatus} [{bootstrap.CurrentJoinCode}]";
            }

            _statusBodyText.text = status;
        }

        private void RefreshObjective(CaseSessionManager session)
        {
            _objectiveText.text = session.GetRecommendedNextStep();
        }

        private void RefreshWaypoint(CaseSessionManager session)
        {
            if (playerInteraction == null || session.IsCaseResolved)
            {
                _waypointTitleText.text = "HEDEF YOK";
                _waypointBodyText.text = "Vaka tamamlandi ya da oyuncu referansi eksik.";
                _waypointMetaText.text = string.Empty;
                return;
            }

            if (!TryGetNextTarget(session, out var title, out var worldPosition, out var subtitle))
            {
                _waypointTitleText.text = "HEDEF YOK";
                _waypointBodyText.text = string.Empty;
                _waypointMetaText.text = string.Empty;
                return;
            }

            var directionHint = GetDirectionHint(worldPosition);
            var distance = Vector3.Distance(playerInteraction.transform.position, worldPosition);
            _waypointTitleText.text = "SONRAKI HEDEF: " + title;
            _waypointBodyText.text = subtitle;
            _waypointMetaText.text = $"Yon: {directionHint}   Uzaklik: {distance:0}m";
        }

        private void RefreshChecklist(CaseSessionManager session)
        {
            if (progressTracker == null)
            {
                return;
            }

            var ratio = progressTracker.GetCompletionRatio();
            _progressFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, 400f * ratio), 0f);
            _progressText.text = $"{Mathf.RoundToInt(ratio * 100f)}% tamamlandi";
            _tacticText.text = "Taktik: " + session.GetRecommendedNextStep();

            RuntimeUiFactory.ClearChildren(_stepsContent);
            var steps = progressTracker.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var prefix = steps[i].IsCompleted ? "[x]" : "[ ]";
                var color = steps[i].IsCompleted ? ModernGuiTheme.AccentColor : ModernGuiTheme.TextColor;
                var card = RuntimeUiFactory.CreateCard("Step" + i, _stepsContent, new Color(0.09f, 0.11f, 0.14f, 0.96f), steps[i].IsCompleted ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.BorderColor);
                RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(12, 12, 12, 10), false);
                RuntimeUiFactory.CreateText("StepText", card, prefix + " " + steps[i].Label, 13, color, steps[i].IsCompleted ? FontStyle.Bold : FontStyle.Normal, TextAnchor.UpperLeft);
            }
        }

        private void RefreshMessageBanner()
        {
            if (_messageGroup == null)
            {
                return;
            }

            var timeLeft = _messageUntil - Time.time;
            if (timeLeft <= 0f || string.IsNullOrWhiteSpace(_currentMessage))
            {
                _messageGroup.alpha = 0f;
                return;
            }

            _messageGroup.alpha = Mathf.Clamp01(Mathf.Min(1f, timeLeft / 0.35f));
            _messageText.text = _currentMessage;
        }

        private void RefreshLocationVisuals()
        {
            if (_locationGroup == null)
            {
                return;
            }

            var timeLeft = _locationUntil - Time.time;
            if (timeLeft <= 0f || string.IsNullOrWhiteSpace(_currentLocationTitle))
            {
                _locationGroup.alpha = 0f;
                return;
            }

            _locationGroup.alpha = Mathf.Clamp01(Mathf.Min(1f, timeLeft / 0.35f));
            _locationTitleText.text = _currentLocationTitle;
            _locationSubtitleText.text = _currentLocationSubtitle;
        }

        private void UpdateLocationBanner()
        {
            if (playerInteraction == null)
            {
                return;
            }

            var title = SchoolLocationUtility.GetZoneTitle(playerInteraction.transform.position);
            if (title == _currentLocationTitle)
            {
                return;
            }

            _currentLocationTitle = title;
            _currentLocationSubtitle = SchoolLocationUtility.GetZoneSubtitle(playerInteraction.transform.position);
            _locationUntil = Time.time + locationDuration;
        }

        private void HandleSessionMessage(string message)
        {
            _currentMessage = message;
            _messageUntil = Time.time + messageDuration;
        }

        private void HandleEvidenceCollected(EvidenceData evidence)
        {
            if (evidence != null && evidence.IsCritical)
            {
                HandleSessionMessage("Kritik delil: " + evidence.Title);
            }
        }

        private void TrySubscribe()
        {
            if (CaseSessionManager.Instance == null || _subscribedSession == CaseSessionManager.Instance)
            {
                return;
            }

            if (_subscribedSession != null)
            {
                _subscribedSession.SessionMessagePublished -= HandleSessionMessage;
                _subscribedSession.EvidenceCollected -= HandleEvidenceCollected;
            }

            _subscribedSession = CaseSessionManager.Instance;
            _subscribedSession.SessionMessagePublished += HandleSessionMessage;
            _subscribedSession.EvidenceCollected += HandleEvidenceCollected;

            if (_subscribedSession.ActiveCase != null)
            {
                HandleSessionMessage(_subscribedSession.ActiveCase.OpeningBrief);
            }
        }

        private void ResolveReferences()
        {
            if (playerInteraction == null || !playerInteraction.isActiveAndEnabled)
            {
                var candidates = Object.FindObjectsByType<PlayerInteractionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] != null && candidates[i].isActiveAndEnabled)
                    {
                        playerInteraction = candidates[i];
                        break;
                    }
                }
            }

            if (progressTracker == null)
            {
                progressTracker = Object.FindFirstObjectByType<CaseProgressTracker>();
            }
        }

        private void DisableLegacyHudScripts()
        {
            var status = Object.FindFirstObjectByType<CaseStatusHud>();
            if (status != null)
            {
                status.enabled = false;
            }

            var checklist = Object.FindFirstObjectByType<CaseChecklistHud>();
            if (checklist != null)
            {
                checklist.enabled = false;
            }

            var waypoint = Object.FindFirstObjectByType<InvestigationWaypointHud>();
            if (waypoint != null)
            {
                waypoint.enabled = false;
            }

            var banner = Object.FindFirstObjectByType<LocationBannerHud>();
            if (banner != null)
            {
                banner.enabled = false;
            }
        }

        private bool TryGetNextTarget(CaseSessionManager session, out string title, out Vector3 worldPosition, out string subtitle)
        {
            title = string.Empty;
            worldPosition = Vector3.zero;
            subtitle = string.Empty;

            if (!session.HasEvidence("evidence.security-log"))
            {
                title = "Guvenlik Odasi";
                worldPosition = new Vector3(-7f, 1f, 10f);
                subtitle = "Gece hareketlerini teyit eden ilk dijital kayit burada.";
                return true;
            }

            if (!session.HasEvidence("evidence.answer-key-note"))
            {
                title = "Kutuphane Masasi";
                worldPosition = new Vector3(7f, 1f, -2f);
                subtitle = "Yazili not ve fiziksel kagit izi kutuphane tarafinda.";
                return true;
            }

            if (!session.HasEvidence("evidence.guard-testimony"))
            {
                title = "Guvenlik Gorevlisi";
                worldPosition = new Vector3(-2f, 1f, 9f);
                subtitle = "Kamera kaydini bulduysan tanigin ifadesini acabilirsin.";
                return true;
            }

            if (!session.HasEvidence("evidence.student-testimony"))
            {
                title = "Kutuphane Ogrencisi";
                worldPosition = new Vector3(5f, 1f, -2f);
                subtitle = "Notu gordukten sonra ogrenci yeni bir ifade verebilir.";
                return true;
            }

            if (!session.HasEvidence("evidence.archive-ledger"))
            {
                title = "Arsiv Kanadi";
                worldPosition = new Vector3(-15f, 1f, 9f);
                subtitle = "Giris defteri suphelinin onceki erisim izini burada sakliyor.";
                return true;
            }

            if (!session.HasEvidence("evidence.canteen-testimony"))
            {
                title = "Kantin Calisani";
                worldPosition = new Vector3(13.2f, 1f, 8.8f);
                subtitle = "Kutuphane notundan sonra kantin tarafinda yeni tanik aciliyor.";
                return true;
            }

            if (!session.HasEvidence("evidence.locker-key"))
            {
                title = "Ogretmenler Odasi";
                worldPosition = new Vector3(7f, 1f, 10f);
                subtitle = "Yedek anahtar supheli erisim zincirini tamamlar.";
                return true;
            }

            if (session.HasAnyAccusableSuspect())
            {
                title = "Vaka Masasi";
                worldPosition = new Vector3(0f, 1f, -5.6f);
                subtitle = "Dosyayi acip supheliyi secmek icin artik yeterli delil var.";
                return true;
            }

            title = "Koridor Tarama";
            worldPosition = new Vector3(0f, 1f, 4f);
            subtitle = "Takim notlarini kontrol et ve eksik ipucunu yeniden tara.";
            return true;
        }

        private string GetDirectionHint(Vector3 worldPosition)
        {
            var localTarget = playerInteraction.transform.InverseTransformPoint(worldPosition);
            if (localTarget.z < -1f)
            {
                return localTarget.x < 0f ? "ARKA SOL" : "ARKA SAG";
            }

            if (localTarget.x < -1.25f)
            {
                return "SOLA DON";
            }

            if (localTarget.x > 1.25f)
            {
                return "SAGA DON";
            }

            return "DUZ ILERI";
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}

