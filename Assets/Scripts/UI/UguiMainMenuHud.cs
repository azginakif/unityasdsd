using System.Collections.Generic;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiMainMenuHud : MonoBehaviour
    {
        [SerializeField] private MainMenuHud logic;

        private Canvas _canvas;
        private RectTransform _overlayRoot;
        private RectTransform _collapsedRoot;
        private RectTransform _windowRoot;
        private CanvasGroup _overlayGroup;
        private float _openBlend;
        private InputField _playerNameField;
        private InputField _joinCodeField;
        private Text _statusText;
        private Text _backendText;
        private Text _heroStatusText;
        private Text _heroMetaText;
        private Text _collapsedHintText;
        private Text _missionBriefText;
        private Text _missionNextStepText;
        private Text _missionStatsText;
        private Text _controlsText;
        private Text _hintText;
        private Text _modeMetricText;
        private Text _caseMetricText;
        private Text _playerMetricText;
        private Text _readyMetricText;
        private Button _hostButton;
        private Button _joinButton;
        private Button _readyButton;
        private Text _readyButtonText;
        private RectTransform _rosterContent;
        private float _nextRefreshAt;
        private bool _built;
        private bool _syncingFields;

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            BuildIfNeeded();
            RefreshImmediate();
        }

        private void Update()
        {
            if (logic == null)
            {
                logic = GetComponent<MainMenuHud>();
                if (logic == null)
                {
                    logic = MainMenuHud.Instance;
                }

                if (logic != null)
                {
                    logic.RenderWithOnGui = false;
                }
            }

            if (!_built)
            {
                BuildIfNeeded();
            }

            SyncVisibility();
            AnimateMenu(logic != null && logic.IsOpen);

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

            if (logic == null)
            {
                logic = GetComponent<MainMenuHud>();
            }

            if (logic != null)
            {
                logic.RenderWithOnGui = false;
            }

            var canvasTransform = transform.Find("UguiMainMenuCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiMainMenuCanvas", transform);
            }

            _canvas = canvasTransform.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 85;

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

            BuildCollapsedButton(canvasTransform);
            BuildOverlay(canvasTransform);

            _built = true;
            _openBlend = logic != null && logic.IsOpen ? 1f : 0f;
            SyncVisibility();
            AnimateMenu(logic != null && logic.IsOpen);
        }

        private void BuildCollapsedButton(RectTransform canvasTransform)
        {
            _collapsedRoot = RuntimeUiFactory.CreateUiRoot("CollapsedMenuRoot", canvasTransform);
            _collapsedRoot.anchorMin = new Vector2(0f, 0f);
            _collapsedRoot.anchorMax = new Vector2(0f, 0f);
            _collapsedRoot.pivot = new Vector2(0f, 0f);
            _collapsedRoot.anchoredPosition = new Vector2(24f, 24f);
            _collapsedRoot.sizeDelta = new Vector2(168f, 58f);

            var openButton = RuntimeUiFactory.CreateButton("OpenMenuButton", _collapsedRoot, "MENU", new Color(0.12f, 0.15f, 0.18f, 0.96f), 18);
            RuntimeUiFactory.Stretch(openButton.GetComponent<RectTransform>());
            openButton.onClick.AddListener(() => logic?.OpenMenu());

            _collapsedHintText = RuntimeUiFactory.CreateText("CollapsedHint", _collapsedRoot, "Dosya merkezi", 11, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperCenter);
            _collapsedHintText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _collapsedHintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _collapsedHintText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _collapsedHintText.rectTransform.anchoredPosition = new Vector2(0f, 66f);
        }

        private void BuildOverlay(RectTransform canvasTransform)
        {
            _overlayRoot = RuntimeUiFactory.CreateUiRoot("OverlayRoot", canvasTransform);
            RuntimeUiFactory.Stretch(_overlayRoot);
            RuntimeUiFactory.AddImage(_overlayRoot.gameObject, new Color(0.02f, 0.03f, 0.04f, 0.92f));
            _overlayGroup = _overlayRoot.gameObject.GetComponent<CanvasGroup>();
            if (_overlayGroup == null)
            {
                _overlayGroup = _overlayRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var washTop = RuntimeUiFactory.CreateUiRoot("WarmWash", _overlayRoot);
            washTop.anchorMin = new Vector2(0f, 1f);
            washTop.anchorMax = new Vector2(1f, 1f);
            washTop.pivot = new Vector2(0.5f, 1f);
            washTop.sizeDelta = new Vector2(0f, 150f);
            washTop.anchoredPosition = Vector2.zero;
            RuntimeUiFactory.AddImage(washTop.gameObject, new Color(0.26f, 0.18f, 0.08f, 0.22f));

            var washBottom = RuntimeUiFactory.CreateUiRoot("ColdWash", _overlayRoot);
            washBottom.anchorMin = new Vector2(0f, 0f);
            washBottom.anchorMax = new Vector2(1f, 0f);
            washBottom.pivot = new Vector2(0.5f, 0f);
            washBottom.sizeDelta = new Vector2(0f, 220f);
            washBottom.anchoredPosition = Vector2.zero;
            RuntimeUiFactory.AddImage(washBottom.gameObject, new Color(0.08f, 0.22f, 0.24f, 0.16f));

            var sideGlow = RuntimeUiFactory.CreateUiRoot("SideGlow", _overlayRoot);
            sideGlow.anchorMin = new Vector2(0f, 0f);
            sideGlow.anchorMax = new Vector2(0f, 1f);
            sideGlow.pivot = new Vector2(0f, 0.5f);
            sideGlow.sizeDelta = new Vector2(220f, 0f);
            sideGlow.anchoredPosition = Vector2.zero;
            RuntimeUiFactory.AddImage(sideGlow.gameObject, new Color(0.06f, 0.28f, 0.3f, 0.18f));

            var topFrame = RuntimeUiFactory.CreateUiRoot("TopFrame", _overlayRoot);
            topFrame.anchorMin = new Vector2(0.08f, 1f);
            topFrame.anchorMax = new Vector2(0.92f, 1f);
            topFrame.pivot = new Vector2(0.5f, 1f);
            topFrame.sizeDelta = new Vector2(0f, 3f);
            topFrame.anchoredPosition = new Vector2(0f, -26f);
            RuntimeUiFactory.AddImage(topFrame.gameObject, new Color(0.76f, 0.64f, 0.36f, 0.3f));

            _windowRoot = RuntimeUiFactory.CreateCard("WindowRoot", _overlayRoot, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            _windowRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _windowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _windowRoot.pivot = new Vector2(0.5f, 0.5f);
            _windowRoot.sizeDelta = new Vector2(1380f, 820f);
            _windowRoot.anchoredPosition = Vector2.zero;

            var layout = RuntimeUiFactory.AddVerticalLayout(_windowRoot, 16f, new RectOffset(26, 26, 26, 26));
            layout.childForceExpandHeight = false;

            BuildHeader(_windowRoot);
            BuildMetrics(_windowRoot);
            BuildBody(_windowRoot);
            BuildFooter(_windowRoot);
        }

        private void BuildHeader(RectTransform parent)
        {
            var header = RuntimeUiFactory.CreateUiRoot("Header", parent);
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 112f);
            RuntimeUiFactory.AddHorizontalLayout(header, 18f, new RectOffset(0, 0, 0, 0), true);

            var titleBlock = RuntimeUiFactory.CreateUiRoot("TitleBlock", header);
            RuntimeUiFactory.EnsureLayoutElement(titleBlock, flexibleWidth: 1f);
            RuntimeUiFactory.AddVerticalLayout(titleBlock, 4f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.CreateText("Title", titleBlock, "MOBIL OFL", 56, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("Subtitle", titleBlock, "Okul ici suc dosyasi, co-op arastirma ve mobil odakli prototip.", 18, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var heroBadge = RuntimeUiFactory.CreateCard("HeroBadge", titleBlock, new Color(0.1f, 0.13f, 0.16f, 0.95f), ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(heroBadge, preferredHeight: 54f);
            RuntimeUiFactory.AddVerticalLayout(heroBadge, 2f, new RectOffset(16, 16, 10, 8), false);
            _heroStatusText = RuntimeUiFactory.CreateText("HeroStatus", heroBadge, "DOSYA HAZIR", 14, ModernGuiTheme.AccentWarmColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _heroMetaText = RuntimeUiFactory.CreateText("HeroMeta", heroBadge, "Tek oyuncu ya da co-op oturumunu buradan baslat.", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var profileCard = RuntimeUiFactory.CreateCard("ProfileCard", header, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(profileCard, preferredWidth: 340f, preferredHeight: 112f);
            RuntimeUiFactory.AddVerticalLayout(profileCard, 10f, new RectOffset(16, 16, 18, 16), false);
            RuntimeUiFactory.CreateText("ProfileLabel", profileCard, "OYUNCU PROFILI", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _playerNameField = RuntimeUiFactory.CreateInputField("PlayerNameField", profileCard, "Oyuncu adi", 20);
            RuntimeUiFactory.EnsureLayoutElement(_playerNameField.transform, preferredHeight: 56f);
            _playerNameField.onValueChanged.AddListener(OnPlayerNameChanged);
        }

        private void BuildMetrics(RectTransform parent)
        {
            var metricsRow = RuntimeUiFactory.CreateUiRoot("MetricsRow", parent);
            RuntimeUiFactory.EnsureLayoutElement(metricsRow, preferredHeight: 88f);
            var row = RuntimeUiFactory.AddHorizontalLayout(metricsRow, 12f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            _caseMetricText = CreateMetricCard(metricsRow, "VAKA");
            _playerMetricText = CreateMetricCard(metricsRow, "OYUNCU");
            _modeMetricText = CreateMetricCard(metricsRow, "MOD");
            _readyMetricText = CreateMetricCard(metricsRow, "HAZIR");
        }

        private Text CreateMetricCard(Transform parent, string label)
        {
            var card = RuntimeUiFactory.CreateCard(label + "Card", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, flexibleWidth: 1f, preferredHeight: 88f);
            RuntimeUiFactory.AddVerticalLayout(card, 6f, new RectOffset(16, 16, 16, 14), false);
            RuntimeUiFactory.CreateText(label + "Label", card, label, 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            return RuntimeUiFactory.CreateText(label + "Value", card, "-", 26, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
        }

        private void BuildBody(RectTransform parent)
        {
            var body = RuntimeUiFactory.CreateUiRoot("Body", parent);
            RuntimeUiFactory.EnsureLayoutElement(body, preferredHeight: 500f, flexibleHeight: 1f);
            var row = RuntimeUiFactory.AddHorizontalLayout(body, 16f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var leftColumn = RuntimeUiFactory.CreateUiRoot("LeftColumn", body);
            RuntimeUiFactory.EnsureLayoutElement(leftColumn, preferredWidth: 430f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(leftColumn, 14f, new RectOffset(0, 0, 0, 0));
            BuildSessionCard(leftColumn);
            BuildControlsCard(leftColumn);

            var rightColumn = RuntimeUiFactory.CreateUiRoot("RightColumn", body);
            RuntimeUiFactory.EnsureLayoutElement(rightColumn, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(rightColumn, 14f, new RectOffset(0, 0, 0, 0));
            BuildMissionCard(rightColumn);
            BuildRosterCard(rightColumn);
        }

        private void BuildSessionCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("SessionCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 286f);
            RuntimeUiFactory.AddVerticalLayout(card, 10f, new RectOffset(18, 18, 18, 18), false);
            RuntimeUiFactory.CreateText("SessionTitle", card, "OTURUM", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusText = RuntimeUiFactory.CreateText("StatusText", card, "Hazir.", 15, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _backendText = RuntimeUiFactory.CreateText("BackendText", card, "-", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var primaryActions = RuntimeUiFactory.CreateUiRoot("PrimaryActions", card);
            RuntimeUiFactory.EnsureLayoutElement(primaryActions, preferredHeight: 48f);
            var actionLayout = RuntimeUiFactory.AddHorizontalLayout(primaryActions, 10f, new RectOffset(0, 0, 0, 0), true);
            actionLayout.childForceExpandWidth = true;

            var soloButton = RuntimeUiFactory.CreateButton("SoloButton", primaryActions, "Tek Basina Basla", new Color(0.13f, 0.17f, 0.2f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(soloButton.transform, flexibleWidth: 1f, preferredHeight: 48f);
            soloButton.onClick.AddListener(() => logic?.StartSoloFromUi());

            _hostButton = RuntimeUiFactory.CreateButton("HostButton", primaryActions, "Co-op Host", new Color(0.26f, 0.2f, 0.1f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_hostButton.transform, flexibleWidth: 1f, preferredHeight: 48f);
            _hostButton.onClick.AddListener(() => logic?.StartHostFromUi());

            RuntimeUiFactory.CreateText("JoinLabel", card, "JOIN CODE", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _joinCodeField = RuntimeUiFactory.CreateInputField("JoinCodeField", card, "ABC123", 18);
            RuntimeUiFactory.EnsureLayoutElement(_joinCodeField.transform, preferredHeight: 52f);
            _joinCodeField.onValueChanged.AddListener(OnJoinCodeChanged);

            var secondaryActions = RuntimeUiFactory.CreateUiRoot("SecondaryActions", card);
            RuntimeUiFactory.EnsureLayoutElement(secondaryActions, preferredHeight: 46f);
            var secondaryLayout = RuntimeUiFactory.AddHorizontalLayout(secondaryActions, 10f, new RectOffset(0, 0, 0, 0), true);
            secondaryLayout.childForceExpandWidth = true;

            _joinButton = RuntimeUiFactory.CreateButton("JoinButton", secondaryActions, "Oturuma Katil", new Color(0.12f, 0.2f, 0.17f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_joinButton.transform, flexibleWidth: 1f, preferredHeight: 46f);
            _joinButton.onClick.AddListener(() => logic?.JoinCurrentCodeFromUi());

            var copyButton = RuntimeUiFactory.CreateButton("CopyButton", secondaryActions, "Kodu Kopyala", new Color(0.11f, 0.12f, 0.15f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(copyButton.transform, flexibleWidth: 1f, preferredHeight: 46f);
            copyButton.onClick.AddListener(CopyJoinCode);
        }

        private void BuildControlsCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("ControlsCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(18, 18, 18, 18), false);
            RuntimeUiFactory.CreateText("ControlsTitle", card, "KONTROLLER", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _controlsText = RuntimeUiFactory.CreateText("ControlsText", card, string.Empty, 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _hintText = RuntimeUiFactory.CreateText("HintText", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildMissionCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("MissionCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 286f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 10f, new RectOffset(18, 18, 18, 18), false);
            RuntimeUiFactory.CreateText("MissionTitle", card, "GOREV DOSYASI", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            var briefingStrip = RuntimeUiFactory.CreateCard("BriefingStrip", card, new Color(0.12f, 0.15f, 0.18f, 0.96f), ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(briefingStrip, preferredHeight: 68f);
            RuntimeUiFactory.AddVerticalLayout(briefingStrip, 3f, new RectOffset(16, 16, 12, 10), false);
            RuntimeUiFactory.CreateText("BriefingLabel", briefingStrip, "CANLI BRIEFING", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionBriefText = RuntimeUiFactory.CreateText("MissionBrief", briefingStrip, string.Empty, 16, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("NextLabel", card, "SIRADAKI HAMLE", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionNextStepText = RuntimeUiFactory.CreateText("MissionNextStep", card, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionStatsText = RuntimeUiFactory.CreateText("MissionStats", card, string.Empty, 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildRosterCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("RosterCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 200f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 10f, new RectOffset(18, 18, 18, 18), false);

            var header = RuntimeUiFactory.CreateUiRoot("RosterHeader", card);
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 42f);
            var headerLayout = RuntimeUiFactory.AddHorizontalLayout(header, 10f, new RectOffset(0, 0, 0, 0), true);
            headerLayout.childForceExpandWidth = false;

            RuntimeUiFactory.CreateText("RosterTitle", header, "TAKIM DURUMU", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(header.GetChild(0), flexibleWidth: 1f);

            _readyButton = RuntimeUiFactory.CreateButton("ReadyButton", header, "Hazirim", new Color(0.14f, 0.22f, 0.18f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(_readyButton.transform, preferredWidth: 168f, preferredHeight: 40f);
            _readyButtonText = _readyButton.GetComponentInChildren<Text>();
            _readyButton.onClick.AddListener(ToggleReady);

            var scrollView = RuntimeUiFactory.CreateScrollView("RosterScroll", card, out _rosterContent);
            RuntimeUiFactory.EnsureLayoutElement(scrollView.transform, flexibleHeight: 1f, preferredHeight: 118f);
            RuntimeUiFactory.AddVerticalLayout(_rosterContent, 8f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_rosterContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildFooter(RectTransform parent)
        {
            var footer = RuntimeUiFactory.CreateUiRoot("Footer", parent);
            RuntimeUiFactory.EnsureLayoutElement(footer, preferredHeight: 46f);
            var layout = RuntimeUiFactory.AddHorizontalLayout(footer, 12f, new RectOffset(0, 0, 0, 0), true);
            layout.childForceExpandWidth = false;

            var note = RuntimeUiFactory.CreateText("FooterNote", footer, "Vaka masasina donup dosya uzerinden suphelileri karsilastir. Arayuz artik uGUI ile calisiyor.", 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(note.transform, flexibleWidth: 1f);

            var closeButton = RuntimeUiFactory.CreateButton("CloseButton", footer, "Kapat", new Color(0.12f, 0.12f, 0.15f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(closeButton.transform, preferredWidth: 136f, preferredHeight: 42f);
            closeButton.onClick.AddListener(() => logic?.CloseMenu());
        }

        private void RefreshImmediate()
        {
            if (!_built || logic == null)
            {
                return;
            }

            var bootstrap = logic.Bootstrap;
            var session = CaseSessionManager.Instance;
            var networkCaseState = NetworkCaseState.Instance;

            _syncingFields = true;
            if (_playerNameField != null && !_playerNameField.isFocused)
            {
                _playerNameField.text = logic.PlayerName;
            }

            if (_joinCodeField != null && !_joinCodeField.isFocused)
            {
                _joinCodeField.text = logic.JoinCodeInput;
            }
            _syncingFields = false;

            _statusText.text = logic.CurrentStatus;
            _backendText.text = bootstrap == null
                ? "Ag katmani: offline"
                : $"Ag katmani: {bootstrap.BackendLabel}\n{bootstrap.BackendUpgradeHint}";
            _heroStatusText.text = bootstrap != null && bootstrap.IsOnlineSessionActive ? "CO-OP OTURUM CANLI" : "DOSYA HAZIR";
            _heroMetaText.text = session == null ? "Sahne kuruldugunda ilk delili topla." : session.GetRecommendedNextStep();

            _controlsText.text =
                "PC: WASD hareket, Mouse bakis, E etkilesim, Tab dosya, Esc menu.\n" +
                "Mobil: Joystick hareket, sag alan bakis, AL etkilesim, DOSYA vaka dosyasi.";
            _hintText.text = session == null ? "Aktif oturum bekleniyor." : session.GetRecommendedNextStep();

            _caseMetricText.text = session != null && session.ActiveCase != null ? session.ActiveCase.CaseTitle : "Hazir";
            _playerMetricText.text = string.IsNullOrWhiteSpace(logic.PlayerName) ? "Dedektif" : logic.PlayerName;
            _modeMetricText.text = bootstrap != null ? bootstrap.CurrentMode : "Offline";
            _readyMetricText.text = networkCaseState == null ? "-" : $"{networkCaseState.ReadyPlayerCount}/{networkCaseState.RegisteredPlayerCount}";

            if (session != null && session.ActiveCase != null)
            {
                _missionBriefText.text = session.ActiveCase.OpeningBrief;
                _missionNextStepText.text = session.GetRecommendedNextStep();
                _missionStatsText.text =
                    $"Toplanan delil: {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}\n" +
                    $"NPC sorgusu: {session.InterviewedNpcCount}\n" +
                    $"Takim notu: {session.TeamNotes.Count}";
            }
            else
            {
                _missionBriefText.text = "Aktif vaka bilgisi bekleniyor.";
                _missionNextStepText.text = "Kurulum tamamlandiginda ilk delili toplamaya basla.";
                _missionStatsText.text = "Delil ve sorgu verileri daha sonra burada dolacak.";
            }

            _hostButton.interactable = bootstrap == null || !bootstrap.IsBusy;
            _joinButton.interactable = bootstrap == null || !bootstrap.IsBusy;

            RebuildRoster(networkCaseState, bootstrap);
        }

        private void RebuildRoster(NetworkCaseState networkCaseState, RelayNetworkBootstrap bootstrap)
        {
            RuntimeUiFactory.ClearChildren(_rosterContent);

            if (bootstrap == null || !bootstrap.IsOnlineSessionActive || networkCaseState == null)
            {
                CreateRosterLine("Co-op oturumu acildiginda oyuncular burada listelenecek.", ModernGuiTheme.MutedTextColor, false);
                _readyButton.interactable = false;
                _readyButtonText.text = "Hazirim";
                return;
            }

            var roster = networkCaseState.GetReadyRoster();
            if (roster.Count == 0)
            {
                CreateRosterLine("Oyuncu kaydi bekleniyor.", ModernGuiTheme.MutedTextColor, false);
            }
            else
            {
                foreach (var entry in roster)
                {
                    CreateRosterLine(entry.DisplayName + (entry.IsReady ? "  |  Hazir" : "  |  Beklemede"), entry.IsReady ? ModernGuiTheme.TextColor : ModernGuiTheme.MutedTextColor, entry.IsReady);
                }
            }

            _readyButton.interactable = true;
            _readyButtonText.text = IsLocalPlayerReady(networkCaseState) ? "Beklemeye Al" : "Hazirim";
        }

        private void CreateRosterLine(string text, Color color, bool ready)
        {
            var card = RuntimeUiFactory.CreateCard("RosterLine", _rosterContent, new Color(0.1f, 0.12f, 0.15f, 0.94f), ready ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.BorderColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 46f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(14, 14, 12, 10), false);
            RuntimeUiFactory.CreateText("LineText", card, text, 14, color, FontStyle.Normal, TextAnchor.MiddleLeft);
        }

        private bool IsLocalPlayerReady(NetworkCaseState networkCaseState)
        {
            if (networkCaseState == null || NetworkManager.Singleton == null)
            {
                return false;
            }

            var localClientId = NetworkManager.Singleton.LocalClientId;
            var roster = networkCaseState.GetReadyRoster();
            for (var i = 0; i < roster.Count; i++)
            {
                if (roster[i].ClientId == localClientId)
                {
                    return roster[i].IsReady;
                }
            }

            return false;
        }

        private void ToggleReady()
        {
            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState == null)
            {
                return;
            }

            networkCaseState.RequestSetReady(!IsLocalPlayerReady(networkCaseState));
            RefreshImmediate();
        }

        private void CopyJoinCode()
        {
            if (logic == null || string.IsNullOrWhiteSpace(logic.JoinCodeInput))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = logic.JoinCodeInput;
        }

        private void OnPlayerNameChanged(string value)
        {
            if (_syncingFields || logic == null)
            {
                return;
            }

            logic.PlayerName = value;
        }

        private void OnJoinCodeChanged(string value)
        {
            if (_syncingFields || logic == null)
            {
                return;
            }

            logic.JoinCodeInput = value;
        }

        private void SyncVisibility()
        {
            if (!_built || logic == null)
            {
                return;
            }

            _overlayRoot.gameObject.SetActive(true);
            _collapsedRoot.gameObject.SetActive(!logic.IsOpen && _openBlend <= 0.02f);
            if (_collapsedHintText != null)
            {
                _collapsedHintText.text = string.IsNullOrWhiteSpace(logic.PlayerName) ? "Dosya merkezi" : logic.PlayerName;
            }
        }

        private void AnimateMenu(bool visible)
        {
            if (_overlayGroup == null || _windowRoot == null)
            {
                return;
            }

            var target = visible ? 1f : 0f;
            _openBlend = Mathf.MoveTowards(_openBlend, target, Time.unscaledDeltaTime * 5.5f);
            _overlayGroup.alpha = _openBlend;
            _overlayGroup.interactable = _openBlend > 0.98f;
            _overlayGroup.blocksRaycasts = _openBlend > 0.02f;
            _windowRoot.localScale = Vector3.Lerp(new Vector3(0.965f, 0.985f, 1f), Vector3.one, _openBlend);
            _windowRoot.anchoredPosition = Vector2.Lerp(new Vector2(0f, 28f), Vector2.zero, _openBlend);
        }
    }
}







