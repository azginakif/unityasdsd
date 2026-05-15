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
        [SerializeField] private float dialogueDuration = 6.2f;
        [SerializeField] private float dialogueCharactersPerSecond = 52f;

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
        private RectTransform _checklistCard;
        private Image _noiseFill;
        private Image _alertFill;
        private Text _stealthStateText;
        private RectTransform _tensionCard;
        private RectTransform _dialogueRoot;
        private CanvasGroup _dialogueGroup;
        private Image _dialoguePanelImage;
        private Image _dialogueSpeakerImage;
        private Text _dialogueSpeakerText;
        private Text _dialogueBodyText;
        private Text _dialogueMetaText;
        private Image _dialogueSignalFill;
        private string _dialogueSpeaker = string.Empty;
        private string _dialogueLine = string.Empty;
        private bool _dialogueRevealedLead;
        private float _dialogueStartedAt;
        private float _dialogueUntil;
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
                _subscribedSession.NpcConversationRegistered -= HandleNpcConversation;
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
                var desktopHudVisible = !MainMenuHud.IsBlockingGameplay && !CaseNotebookHud.IsAnyNotebookOpen;
                _root.gameObject.SetActive(desktopHudVisible);
            }

            UpdateMobileControlLayout();
            RefreshDialoguePanel();

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
            BuildDialoguePanel();
            BuildChecklistCard();
            BuildTensionCard();
            _built = true;
        }

        private void BuildStatusCard()
        {
            var card = RuntimeUiFactory.CreateCard("StatusCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(0f, 1f);
            card.pivot = new Vector2(0f, 1f);
            card.anchoredPosition = new Vector2(18f, -18f);
            card.sizeDelta = new Vector2(310f, 112f);
            RuntimeUiFactory.AddVerticalLayout(card, 3f, new RectOffset(14, 14, 15, 10));
            _statusTitleText = RuntimeUiFactory.CreateText("StatusTitle", card, "Vaka", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusBodyText = RuntimeUiFactory.CreateText("StatusBody", card, string.Empty, 12, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildObjectiveCard()
        {
            var card = RuntimeUiFactory.CreateCard("ObjectiveCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(1f, 1f);
            card.anchorMax = new Vector2(1f, 1f);
            card.pivot = new Vector2(1f, 1f);
            card.anchoredPosition = new Vector2(-18f, -18f);
            card.sizeDelta = new Vector2(360f, 96f);
            RuntimeUiFactory.AddVerticalLayout(card, 5f, new RectOffset(14, 14, 15, 10));
            RuntimeUiFactory.CreateText("ObjectiveLabel", card, "HEDEF", 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _objectiveText = RuntimeUiFactory.CreateText("ObjectiveBody", card, string.Empty, 13, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
        }

        private void BuildMessageBanner()
        {
            var card = RuntimeUiFactory.CreateCard("MessageBanner", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -26f);
            card.sizeDelta = new Vector2(600f, 52f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(16, 16, 11, 8));
            _messageText = RuntimeUiFactory.CreateText("MessageText", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
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
            card.anchoredPosition = new Vector2(0f, -88f);
            card.sizeDelta = new Vector2(360f, 58f);
            RuntimeUiFactory.AddVerticalLayout(card, 2f, new RectOffset(18, 18, 12, 10));
            _locationTitleText = RuntimeUiFactory.CreateText("LocationTitle", card, string.Empty, 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _locationSubtitleText = RuntimeUiFactory.CreateText("LocationSubtitle", card, string.Empty, 11, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
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
            card.sizeDelta = new Vector2(430f, 76f);
            RuntimeUiFactory.AddVerticalLayout(card, 2f, new RectOffset(16, 16, 14, 10));
            _waypointTitleText = RuntimeUiFactory.CreateText("WaypointTitle", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _waypointBodyText = RuntimeUiFactory.CreateText("WaypointBody", card, string.Empty, 12, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _waypointMetaText = RuntimeUiFactory.CreateText("WaypointMeta", card, string.Empty, 10, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildDialoguePanel()
        {
            _dialogueRoot = RuntimeUiFactory.CreateCard("DialoguePanel", _root, new Color(0.045f, 0.06f, 0.09f, 0.96f), ModernGuiTheme.AccentColor);
            _dialoguePanelImage = _dialogueRoot.GetComponent<Image>();
            _dialogueRoot.anchorMin = new Vector2(0.5f, 0f);
            _dialogueRoot.anchorMax = new Vector2(0.5f, 0f);
            _dialogueRoot.pivot = new Vector2(0.5f, 0f);
            _dialogueRoot.anchoredPosition = new Vector2(0f, 104f);
            _dialogueRoot.sizeDelta = new Vector2(900f, 126f);

            var layout = RuntimeUiFactory.AddHorizontalLayout(_dialogueRoot, 16f, new RectOffset(18, 18, 16, 14), true);
            layout.childForceExpandWidth = false;

            var portrait = RuntimeUiFactory.CreateCard("SpeakerChip", _dialogueRoot, new Color(0.08f, 0.13f, 0.17f, 1f), ModernGuiTheme.AccentWarmColor);
            _dialogueSpeakerImage = portrait.GetComponent<Image>();
            RuntimeUiFactory.EnsureLayoutElement(portrait, preferredWidth: 74f, preferredHeight: 96f);
            RuntimeUiFactory.AddVerticalLayout(portrait, 6f, new RectOffset(12, 12, 12, 10));
            RuntimeUiFactory.CreateIcon("SpeakerIcon", portrait, "users", ModernGuiTheme.AccentColor, new Vector2(34f, 34f));
            RuntimeUiFactory.CreateText("SpeakerRole", portrait, "NPC", 12, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);

            var content = RuntimeUiFactory.CreateUiRoot("DialogueContent", _dialogueRoot);
            RuntimeUiFactory.EnsureLayoutElement(content, flexibleWidth: 1f, preferredHeight: 96f);
            RuntimeUiFactory.AddVerticalLayout(content, 5f, new RectOffset(0, 0, 0, 0));

            var header = RuntimeUiFactory.CreateUiRoot("DialogueHeader", content);
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 22f);
            var headerLayout = RuntimeUiFactory.AddHorizontalLayout(header, 10f, new RectOffset(0, 0, 0, 0), true);
            headerLayout.childForceExpandWidth = false;

            _dialogueSpeakerText = RuntimeUiFactory.CreateText("DialogueSpeaker", header, string.Empty, 15, ModernGuiTheme.AccentWarmColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(_dialogueSpeakerText.transform, flexibleWidth: 1f);
            _dialogueMetaText = RuntimeUiFactory.CreateText("DialogueMeta", header, "SORGULAMA", 11, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.MiddleRight);
            RuntimeUiFactory.EnsureLayoutElement(_dialogueMetaText.transform, preferredWidth: 150f);

            _dialogueBodyText = RuntimeUiFactory.CreateText("DialogueBody", content, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _dialogueBodyText.resizeTextForBestFit = true;
            _dialogueBodyText.resizeTextMinSize = 14;
            _dialogueBodyText.resizeTextMaxSize = 18;
            RuntimeUiFactory.EnsureLayoutElement(_dialogueBodyText.transform, flexibleWidth: 1f, preferredHeight: 48f);

            var signalTrack = RuntimeUiFactory.CreateUiRoot("DialogueSignalTrack", content);
            RuntimeUiFactory.EnsureLayoutElement(signalTrack, preferredHeight: 6f);
            RuntimeUiFactory.AddImage(signalTrack.gameObject, new Color(0.08f, 0.11f, 0.15f, 1f));
            RuntimeUiFactory.ApplyOneUiRounding(signalTrack.gameObject, 3f);
            _dialogueSignalFill = RuntimeUiFactory.CreateUiRoot("DialogueSignalFill", signalTrack).gameObject.AddComponent<Image>();
            _dialogueSignalFill.color = ModernGuiTheme.AccentColor;
            var fillRect = _dialogueSignalFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _dialogueGroup = _dialogueRoot.gameObject.GetComponent<CanvasGroup>();
            if (_dialogueGroup == null)
            {
                _dialogueGroup = _dialogueRoot.gameObject.AddComponent<CanvasGroup>();
            }

            _dialogueGroup.alpha = 0f;
            _dialogueGroup.blocksRaycasts = false;
            _dialogueGroup.interactable = false;
        }

        private void BuildChecklistCard()
        {
            var card = RuntimeUiFactory.CreateCard("ChecklistCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            _checklistCard = card;
            card.anchorMin = new Vector2(0f, 0f);
            card.anchorMax = new Vector2(0f, 0f);
            card.pivot = new Vector2(0f, 0f);
            card.anchoredPosition = new Vector2(18f, 18f);
            card.sizeDelta = new Vector2(292f, 124f);
            RuntimeUiFactory.AddVerticalLayout(card, 5f, new RectOffset(14, 14, 15, 10));
            RuntimeUiFactory.CreateText("ChecklistLabel", card, "ILERLEME", 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var progressShell = RuntimeUiFactory.CreateUiRoot("ProgressShell", card);
            var progressLayout = RuntimeUiFactory.EnsureLayoutElement(progressShell, flexibleWidth: 1f, preferredHeight: 34f);
            progressLayout.minHeight = 34f;

            var progressTrack = RuntimeUiFactory.CreateUiRoot("ProgressTrack", progressShell);
            progressTrack.anchorMin = new Vector2(0f, 0.5f);
            progressTrack.anchorMax = new Vector2(1f, 0.5f);
            progressTrack.pivot = new Vector2(0.5f, 0.5f);
            progressTrack.offsetMin = new Vector2(0f, -5f);
            progressTrack.offsetMax = new Vector2(0f, 5f);
            RuntimeUiFactory.AddImage(progressTrack.gameObject, new Color(0.08f, 0.11f, 0.15f, 0.95f));
            RuntimeUiFactory.ApplyOneUiRounding(progressTrack.gameObject, 5f);
            _progressFill = RuntimeUiFactory.CreateUiRoot("Fill", progressTrack).gameObject.AddComponent<Image>();
            var fillRect = _progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            _progressFill.color = ModernGuiTheme.AccentColor;
            RuntimeUiFactory.ApplyOneUiRounding(_progressFill.gameObject, 5f);
            _progressText = RuntimeUiFactory.CreateText("ProgressText", progressShell, string.Empty, 12, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(_progressText.rectTransform);

            _tacticText = RuntimeUiFactory.CreateText("TacticText", card, string.Empty, 12, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            var scroll = RuntimeUiFactory.CreateScrollView("StepsScroll", card, out _stepsContent);
            RuntimeUiFactory.EnsureLayoutElement(scroll.transform, flexibleHeight: 1f, preferredHeight: 28f);
            RuntimeUiFactory.AddVerticalLayout(_stepsContent, 5f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.AddContentSizeFitter(_stepsContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildTensionCard()
        {
            var card = RuntimeUiFactory.CreateCard("TensionCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            _tensionCard = card;
            card.anchorMin = new Vector2(1f, 0f);
            card.anchorMax = new Vector2(1f, 0f);
            card.pivot = new Vector2(1f, 0f);
            card.anchoredPosition = new Vector2(-18f, 18f);
            card.sizeDelta = new Vector2(292f, 112f);
            RuntimeUiFactory.AddVerticalLayout(card, 5f, new RectOffset(14, 14, 15, 10));
            RuntimeUiFactory.CreateText("TensionLabel", card, "GIZLILIK", 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _stealthStateText = RuntimeUiFactory.CreateText("TensionState", card, string.Empty, 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _noiseFill = CreateMeter(card, "Gurultu");
            _alertFill = CreateMeter(card, "Dikkat");
        }

        private void UpdateMobileControlLayout()
        {
            var showDesktopBottomCards = !MobileInvestigationOverlay.IsMobileHudVisible;
            if (_checklistCard != null)
            {
                _checklistCard.gameObject.SetActive(showDesktopBottomCards);
            }

            if (_tensionCard != null)
            {
                _tensionCard.gameObject.SetActive(showDesktopBottomCards);
            }
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
            RefreshTension();
            RefreshMessageBanner();
            RefreshLocationVisuals();
        }

        private void RefreshStatus(CaseSessionManager session)
        {
            _statusTitleText.text = session.ActiveCase.CaseTitle;
            var zone = playerInteraction != null
                ? SchoolLocationUtility.GetZoneTitle(playerInteraction.transform.position)
                : "Bolge yok";
            var target = playerInteraction != null && playerInteraction.CurrentInteractable != null
                ? playerInteraction.CurrentInteractable.PromptText
                : "Serbest kesif";
            var networkLine = string.Empty;
            var bootstrap = Object.FindAnyObjectByType<RelayNetworkBootstrap>();
            if (bootstrap != null && !string.IsNullOrWhiteSpace(bootstrap.CurrentStatus))
            {
                networkLine = string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode)
                    ? $"\n{bootstrap.CurrentStatus}"
                    : $"\n{bootstrap.CurrentStatus} [{bootstrap.CurrentJoinCode}]";
            }

            _statusBodyText.text =
                $"Operatif: {PlayerProfileSettings.LoadPlayerName()}   Sure: {FormatElapsedTime(session.ElapsedCaseTimeSeconds)}\n" +
                $"Delil {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}   Kritik {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}\n" +
                $"{zone}   |   {target}" +
                networkLine;
        }

        private void RefreshObjective(CaseSessionManager session)
        {
            var stealth = Object.FindAnyObjectByType<PlayerStealthController>();
            if (stealth != null && stealth.IsHighAlert)
            {
                _objectiveText.text = "Dikkat seviyesi yuksek. En yakin sakinlesme noktasina cekilip NPC baskisini dusur.";
                return;
            }

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

            worldPosition = SchoolLocationUtility.PrototypeToWorldPosition(worldPosition, playerInteraction.transform.position);
            var directionHint = GetDirectionHint(worldPosition);
            var distance = Vector3.Distance(playerInteraction.transform.position, worldPosition);
            _waypointTitleText.text = "SONRAKI HEDEF: " + title;
            _waypointBodyText.text = subtitle;
            _waypointMetaText.text = $"Yon: {directionHint}   Uzaklik: {distance:0}m";
        }

        private void RefreshChecklist(CaseSessionManager session)
        {
            if (progressTracker == null || _progressFill == null || _progressText == null || _stepsContent == null)
            {
                return;
            }

            var ratio = progressTracker.GetCompletionRatio();
            var fillRect = _progressFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            _progressText.text = $"{Mathf.RoundToInt(ratio * 100f)}% tamamlandi";
            _tacticText.text = "Taktik: " + session.GetRecommendedNextStep();

            RuntimeUiFactory.ClearChildren(_stepsContent);
            var steps = progressTracker.Steps;
            var shown = 0;
            for (var i = 0; i < steps.Count && shown < 1; i++)
            {
                if (steps[i].IsCompleted)
                {
                    continue;
                }

                DrawChecklistStep(i, steps[i].Label, false);
                shown++;
            }

            if (shown > 0)
            {
                return;
            }

            for (var i = Mathf.Max(0, steps.Count - 1); i < steps.Count && shown < 1; i++)
            {
                DrawChecklistStep(i, steps[i].Label, steps[i].IsCompleted);
                shown++;
            }
        }

        private void DrawChecklistStep(int index, string label, bool isCompleted)
        {
            var prefix = isCompleted ? "[x]" : "[ ]";
            var color = isCompleted ? ModernGuiTheme.AccentColor : ModernGuiTheme.TextColor;
            var card = RuntimeUiFactory.CreateCard("Step" + index, _stepsContent, new Color(0.09f, 0.11f, 0.14f, 0.96f), isCompleted ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.BorderColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 30f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(10, 10, 8, 6));
            RuntimeUiFactory.CreateText("StepText", card, prefix + " " + label, 12, color, isCompleted ? FontStyle.Bold : FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void RefreshTension()
        {
            var stealth = Object.FindAnyObjectByType<PlayerStealthController>();
            if (stealth == null)
            {
                if (_stealthStateText != null)
                {
                    _stealthStateText.text = "Gizlilik verisi bekleniyor.";
                }

                SetMeterFill(_noiseFill, 0f, new Color(0.28f, 0.78f, 0.82f, 1f));
                SetMeterFill(_alertFill, 0f, new Color(0.88f, 0.44f, 0.24f, 1f));
                return;
            }

            _stealthStateText.text = $"Profil: {stealth.MovementProfile}";
            SetMeterFill(_noiseFill, stealth.NoiseLevel01, new Color(0.24f, 0.86f, 0.9f, 1f));
            SetMeterFill(_alertFill, stealth.AlertLevel01, stealth.IsHighAlert ? new Color(0.96f, 0.3f, 0.22f, 1f) : new Color(0.9f, 0.58f, 0.24f, 1f));
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

        private void RefreshDialoguePanel()
        {
            if (_dialogueGroup == null || _dialogueRoot == null)
            {
                return;
            }

            var hasDialogue = !string.IsNullOrWhiteSpace(_dialogueLine) && Time.time < _dialogueUntil;
            var mobileHud = MobileInvestigationOverlay.IsMobileHudVisible;
            var hiddenPosition = new Vector2(0f, mobileHud ? 146f : 78f);
            var visiblePosition = new Vector2(0f, mobileHud ? 190f : 112f);
            var targetSize = new Vector2(mobileHud ? 760f : 900f, mobileHud ? 118f : 126f);
            _dialogueRoot.sizeDelta = Vector2.Lerp(_dialogueRoot.sizeDelta, targetSize, Time.unscaledDeltaTime * 9f);

            if (!hasDialogue)
            {
                _dialogueGroup.alpha = Mathf.MoveTowards(_dialogueGroup.alpha, 0f, Time.unscaledDeltaTime * 7f);
                _dialogueRoot.anchoredPosition = Vector2.Lerp(_dialogueRoot.anchoredPosition, hiddenPosition, Time.unscaledDeltaTime * 8f);
                return;
            }

            var elapsed = Mathf.Max(0f, Time.time - _dialogueStartedAt);
            var timeLeft = _dialogueUntil - Time.time;
            var intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.28f));
            var outro = Mathf.Clamp01(timeLeft / 0.32f);
            var alpha = Mathf.Min(intro, outro);
            _dialogueGroup.alpha = alpha;
            _dialogueRoot.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, intro);
            _dialogueRoot.localScale = Vector3.Lerp(new Vector3(0.97f, 0.97f, 1f), Vector3.one, intro);

            var characterRate = Mathf.Max(1f, dialogueCharactersPerSecond);
            var typedCharacters = Mathf.Clamp(Mathf.FloorToInt(elapsed * characterRate), 0, _dialogueLine.Length);
            var visibleLine = _dialogueLine.Substring(0, typedCharacters);
            if (typedCharacters < _dialogueLine.Length && Mathf.FloorToInt(Time.unscaledTime * 6f) % 2 == 0)
            {
                visibleLine += "_";
            }

            _dialogueSpeakerText.text = _dialogueSpeaker;
            _dialogueBodyText.text = visibleLine;
            _dialogueMetaText.text = _dialogueRevealedLead ? "YENI IPUCU" : "SORGULAMA";

            var accent = _dialogueRevealedLead ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.AccentColor;
            _dialogueSpeakerText.color = accent;
            _dialogueMetaText.color = _dialogueRevealedLead ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.MutedTextColor;
            if (_dialoguePanelImage != null)
            {
                _dialoguePanelImage.color = new Color(0.045f, 0.06f, 0.09f, Mathf.Lerp(0.9f, 0.98f, alpha));
            }

            if (_dialogueSpeakerImage != null)
            {
                _dialogueSpeakerImage.color = _dialogueRevealedLead
                    ? new Color(0.18f, 0.12f, 0.07f, 1f)
                    : new Color(0.08f, 0.13f, 0.17f, 1f);
            }

            if (_dialogueSignalFill != null)
            {
                _dialogueSignalFill.color = accent;
                var fillRect = _dialogueSignalFill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(Mathf.Clamp01(typedCharacters / (float)Mathf.Max(1, _dialogueLine.Length)), 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }
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
            if (IsCurrentDialogueMessage(message))
            {
                return;
            }

            _currentMessage = message;
            _messageUntil = Time.time + messageDuration;
        }

        private void HandleNpcConversation(string npcId, string npcDisplayName, string line, bool revealedLead)
        {
            _dialogueSpeaker = string.IsNullOrWhiteSpace(npcDisplayName) ? "NPC" : npcDisplayName;
            _dialogueLine = CompactDialogueLine(line);
            _dialogueRevealedLead = revealedLead;
            _dialogueStartedAt = Time.time;
            var characterRate = Mathf.Max(1f, dialogueCharactersPerSecond);
            _dialogueUntil = Time.time + Mathf.Max(dialogueDuration, _dialogueLine.Length / characterRate + 1.4f);

            if (_dialogueGroup != null)
            {
                _dialogueGroup.alpha = 0f;
            }
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
                _subscribedSession.NpcConversationRegistered -= HandleNpcConversation;
            }

            _subscribedSession = CaseSessionManager.Instance;
            _subscribedSession.SessionMessagePublished += HandleSessionMessage;
            _subscribedSession.EvidenceCollected += HandleEvidenceCollected;
            _subscribedSession.NpcConversationRegistered += HandleNpcConversation;

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
                progressTracker = Object.FindAnyObjectByType<CaseProgressTracker>();
            }
        }

        private void DisableLegacyHudScripts()
        {
            var status = Object.FindAnyObjectByType<CaseStatusHud>();
            if (status != null)
            {
                status.enabled = false;
            }

            var checklist = Object.FindAnyObjectByType<CaseChecklistHud>();
            if (checklist != null)
            {
                checklist.enabled = false;
            }

            var waypoint = Object.FindAnyObjectByType<InvestigationWaypointHud>();
            if (waypoint != null)
            {
                waypoint.enabled = false;
            }

            var banner = Object.FindAnyObjectByType<LocationBannerHud>();
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

            var stealth = Object.FindAnyObjectByType<PlayerStealthController>();
            if (stealth != null && stealth.IsHighAlert && playerInteraction != null)
            {
                GetNearestRecoverySpot(playerInteraction.transform.position, out worldPosition, out title, out subtitle);
                return true;
            }

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

            if (!session.HasTool("tool.archive-pass"))
            {
                title = "Arsiv Gecis Karti";
                worldPosition = new Vector3(7.8f, 1f, 9.5f);
                subtitle = "Ogretmenler odasindaki karti al. Arsiv raflari bu olmadan acilmayacak.";
                return true;
            }

            if (!session.HasEvidence("evidence.archive-ledger"))
            {
                title = "Arsiv Kanadi";
                worldPosition = new Vector3(-15f, 1f, 9f);
                subtitle = "Gecis karti sende. Raf kutusunu arayip giris defterini ortaya cikar.";
                return true;
            }

            if (!session.HasEvidence("evidence.canteen-testimony"))
            {
                title = "Kantin Calisani";
                worldPosition = new Vector3(13.2f, 1f, 8.8f);
                subtitle = "Kutuphane notundan sonra kantin tarafinda yeni tanik aciliyor.";
                return true;
            }

            if (!session.HasTool("tool.lockpick"))
            {
                title = "Maymuncuk Seti";
                worldPosition = new Vector3(-8.1f, 1f, 9.8f);
                subtitle = "Guvenlik ekipman dolabindaki seti al. Kilitli cekmeceyi bununla acacaksin.";
                return true;
            }

            if (!session.HasEvidence("evidence.locker-key"))
            {
                title = "Ogretmenler Odasi";
                worldPosition = new Vector3(7f, 1f, 10f);
                subtitle = "Maymuncukla masadaki cekmeceyi ara; yedek anahtar burada sakli.";
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

        private bool IsCurrentDialogueMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                string.IsNullOrWhiteSpace(_dialogueSpeaker) ||
                string.IsNullOrWhiteSpace(_dialogueLine) ||
                Time.time >= _dialogueUntil)
            {
                return false;
            }

            return message.StartsWith(_dialogueSpeaker + ":", System.StringComparison.Ordinal) &&
                message.Contains(_dialogueLine);
        }

        private static string CompactDialogueLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            var compact = line.Replace('\n', ' ').Replace('\r', ' ').Trim();
            while (compact.Contains("  "))
            {
                compact = compact.Replace("  ", " ");
            }

            return compact.Length <= 180 ? compact : compact.Substring(0, 177) + "...";
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static void GetNearestRecoverySpot(Vector3 origin, out Vector3 worldPosition, out string title, out string subtitle)
        {
            var positions = new[]
            {
                new Vector3(1.8f, 1f, 6.5f),
                new Vector3(8.45f, 1f, -4.8f),
                new Vector3(-4f, 1f, 22f)
            };
            var titles = new[]
            {
                "Koridor Banki",
                "Kutuphane Rafi",
                "Avlu Banki"
            };
            var subtitles = new[]
            {
                "Bankta sakinlesip dikkat seviyeni dusur.",
                "Raf arkasinda bekleyip baskiyi azalt.",
                "Avluya cekilip nefesini toparla."
            };

            var bestIndex = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < positions.Length; i++)
            {
                var distance = Vector3.SqrMagnitude(origin - positions[i]);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = i;
            }

            worldPosition = positions[bestIndex];
            title = titles[bestIndex];
            subtitle = subtitles[bestIndex];
        }

        private Image CreateMeter(Transform parent, string label)
        {
            var root = RuntimeUiFactory.CreateUiRoot(label + "Meter", parent);
            RuntimeUiFactory.AddVerticalLayout(root, 3f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.CreateText(label + "Label", root, label.ToUpperInvariant(), 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var shell = RuntimeUiFactory.CreateUiRoot(label + "Shell", root);
            RuntimeUiFactory.EnsureLayoutElement(shell, preferredHeight: 18f);
            RuntimeUiFactory.AddImage(shell.gameObject, new Color(0.07f, 0.09f, 0.11f, 1f));
            RuntimeUiFactory.AddOutline(shell.gameObject, new Color(0f, 0f, 0f, 0.32f), new Vector2(1f, -1f));

            var fill = RuntimeUiFactory.CreateUiRoot(label + "Fill", shell).gameObject.AddComponent<Image>();
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(0f, -2f);
            fill.color = ModernGuiTheme.AccentColor;

            var valueText = RuntimeUiFactory.CreateText(label + "Value", shell, string.Empty, 11, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            RuntimeUiFactory.Stretch(valueText.rectTransform);
            valueText.gameObject.name = "Value";
            return fill;
        }

        private void SetMeterFill(Image fill, float value, Color color)
        {
            if (fill == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(value);
            fill.color = color;
            fill.rectTransform.sizeDelta = new Vector2(320f * clamped, 0f);
            var valueText = fill.transform.parent != null ? fill.transform.parent.Find("Value") : null;
            if (valueText != null)
            {
                var text = valueText.GetComponent<Text>();
                if (text != null)
                {
                    text.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
                }
            }
        }
    }
}

