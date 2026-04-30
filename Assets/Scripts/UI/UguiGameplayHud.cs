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
        private Image _noiseFill;
        private Image _alertFill;
        private Text _stealthStateText;
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
            card.sizeDelta = new Vector2(332f, 134f);
            RuntimeUiFactory.AddVerticalLayout(card, 4f, new RectOffset(16, 16, 18, 12), false);
            _statusTitleText = RuntimeUiFactory.CreateText("StatusTitle", card, "Vaka", 20, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusBodyText = RuntimeUiFactory.CreateText("StatusBody", card, string.Empty, 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildObjectiveCard()
        {
            var card = RuntimeUiFactory.CreateCard("ObjectiveCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(1f, 1f);
            card.anchorMax = new Vector2(1f, 1f);
            card.pivot = new Vector2(1f, 1f);
            card.anchoredPosition = new Vector2(-18f, -18f);
            card.sizeDelta = new Vector2(372f, 116f);
            RuntimeUiFactory.AddVerticalLayout(card, 6f, new RectOffset(16, 16, 18, 12), false);
            RuntimeUiFactory.CreateText("ObjectiveLabel", card, "HEDEF", 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _objectiveText = RuntimeUiFactory.CreateText("ObjectiveBody", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
        }

        private void BuildMessageBanner()
        {
            var card = RuntimeUiFactory.CreateCard("MessageBanner", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(0.5f, 1f);
            card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -26f);
            card.sizeDelta = new Vector2(660f, 72f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(18, 18, 14, 10), false);
            _messageText = RuntimeUiFactory.CreateText("MessageText", card, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
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
            card.anchoredPosition = new Vector2(0f, -108f);
            card.sizeDelta = new Vector2(420f, 74f);
            RuntimeUiFactory.AddVerticalLayout(card, 2f, new RectOffset(18, 18, 12, 10), false);
            _locationTitleText = RuntimeUiFactory.CreateText("LocationTitle", card, string.Empty, 20, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _locationSubtitleText = RuntimeUiFactory.CreateText("LocationSubtitle", card, string.Empty, 12, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleCenter);
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
            card.sizeDelta = new Vector2(468f, 90f);
            RuntimeUiFactory.AddVerticalLayout(card, 2f, new RectOffset(16, 16, 14, 10), false);
            _waypointTitleText = RuntimeUiFactory.CreateText("WaypointTitle", card, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _waypointBodyText = RuntimeUiFactory.CreateText("WaypointBody", card, string.Empty, 13, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _waypointMetaText = RuntimeUiFactory.CreateText("WaypointMeta", card, string.Empty, 11, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildChecklistCard()
        {
            var card = RuntimeUiFactory.CreateCard("ChecklistCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            card.anchorMin = new Vector2(0f, 0f);
            card.anchorMax = new Vector2(0f, 0f);
            card.pivot = new Vector2(0f, 0f);
            card.anchoredPosition = new Vector2(18f, 18f);
            card.sizeDelta = new Vector2(332f, 182f);
            RuntimeUiFactory.AddVerticalLayout(card, 6f, new RectOffset(16, 16, 18, 12), false);
            RuntimeUiFactory.CreateText("ChecklistLabel", card, "ILERLEME", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var progressShell = RuntimeUiFactory.CreateUiRoot("ProgressShell", card);
            RuntimeUiFactory.EnsureLayoutElement(progressShell, preferredHeight: 22f);
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
            _progressText = RuntimeUiFactory.CreateText("ProgressText", progressShell, string.Empty, 12, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);

            _tacticText = RuntimeUiFactory.CreateText("TacticText", card, string.Empty, 12, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            var scroll = RuntimeUiFactory.CreateScrollView("StepsScroll", card, out _stepsContent);
            RuntimeUiFactory.EnsureLayoutElement(scroll.transform, flexibleHeight: 1f, preferredHeight: 86f);
            RuntimeUiFactory.AddVerticalLayout(_stepsContent, 5f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_stepsContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildTensionCard()
        {
            var card = RuntimeUiFactory.CreateCard("TensionCard", _root, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            card.anchorMin = new Vector2(1f, 0f);
            card.anchorMax = new Vector2(1f, 0f);
            card.pivot = new Vector2(1f, 0f);
            card.anchoredPosition = new Vector2(-18f, 18f);
            card.sizeDelta = new Vector2(332f, 130f);
            RuntimeUiFactory.AddVerticalLayout(card, 6f, new RectOffset(16, 16, 18, 12), false);
            RuntimeUiFactory.CreateText("TensionLabel", card, "GIZLILIK", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _stealthStateText = RuntimeUiFactory.CreateText("TensionState", card, string.Empty, 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _noiseFill = CreateMeter(card, "Gurultu");
            _alertFill = CreateMeter(card, "Dikkat");
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
            var bootstrap = Object.FindFirstObjectByType<RelayNetworkBootstrap>();
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
            var stealth = Object.FindFirstObjectByType<PlayerStealthController>();
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
            var shown = 0;
            for (var i = 0; i < steps.Count && shown < 3; i++)
            {
                var prefix = steps[i].IsCompleted ? "[x]" : "[ ]";
                var color = steps[i].IsCompleted ? ModernGuiTheme.AccentColor : ModernGuiTheme.TextColor;
                var card = RuntimeUiFactory.CreateCard("Step" + i, _stepsContent, new Color(0.09f, 0.11f, 0.14f, 0.96f), steps[i].IsCompleted ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.BorderColor);
                RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 30f);
                RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(10, 10, 8, 6), false);
                RuntimeUiFactory.CreateText("StepText", card, prefix + " " + steps[i].Label, 12, color, steps[i].IsCompleted ? FontStyle.Bold : FontStyle.Normal, TextAnchor.UpperLeft);
                shown++;
            }
        }

        private void RefreshTension()
        {
            var stealth = Object.FindFirstObjectByType<PlayerStealthController>();
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

            var stealth = Object.FindFirstObjectByType<PlayerStealthController>();
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
            RuntimeUiFactory.AddVerticalLayout(root, 3f, new RectOffset(0, 0, 0, 0), false);
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

