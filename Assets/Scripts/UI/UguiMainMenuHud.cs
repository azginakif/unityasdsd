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
        private RectTransform _headerRoot;
        private RectTransform _metricsRoot;
        private RectTransform _bodyRoot;
        private RectTransform _leftColumnRoot;
        private RectTransform _rightColumnRoot;
        private RectTransform _footerRoot;
        private RectTransform _secondaryActionsRoot;
        private CanvasGroup _overlayGroup;
        private float _openBlend;
        private InputField _playerNameField;
        private InputField _joinCodeField;
        private Text _statusText;
        private Text _backendText;
        private Text _heroStatusText;
        private Text _heroMetaText;
        private Text _collapsedHintText;
        private Text _sessionTitleText;
        private Text _missionBriefText;
        private Text _missionNextStepText;
        private Text _missionStatsText;
        private Text _settingsSummaryText;
        private Text _controlsText;
        private Text _hintText;
        private Text _intelPrimaryText;
        private Text _intelSecondaryText;
        private Text _commandDeckStatusText;
        private Text _commandDeckCodeText;
        private Text _commandDeckHintText;
        private Text _footerNoteText;
        private Text _modeMetricText;
        private Text _caseMetricText;
        private Text _playerMetricText;
        private Text _readyMetricText;
        private Button _soloButton;
        private Button _hostButton;
        private Button _joinButton;
        private Button _reconnectButton;
        private Button _resetSaveButton;
        private Button _readyButton;
        private Button _closeButton;
        private Text _closeButtonText;
        private Text _readyButtonText;
        private RectTransform _rosterContent;
        private RectTransform _settingsRoot;
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
            ApplyResponsiveLayout();
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
            _collapsedRoot.anchoredPosition = new Vector2(24f, 218f);
            _collapsedRoot.sizeDelta = new Vector2(122f, 44f);

            var openButton = RuntimeUiFactory.CreateButton("OpenMenuButton", _collapsedRoot, "MENU", new Color(0.12f, 0.15f, 0.18f, 0.78f), 16);
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
            _windowRoot.sizeDelta = new Vector2(1040f, 650f);
            _windowRoot.anchoredPosition = Vector2.zero;

            var layout = RuntimeUiFactory.AddVerticalLayout(_windowRoot, 10f, new RectOffset(18, 18, 18, 18));
            layout.childForceExpandHeight = false;

            BuildHeader(_windowRoot);
            BuildMetrics(_windowRoot);
            BuildBody(_windowRoot);
            BuildFooter(_windowRoot);
        }

        private void BuildAtmosphereDecor()
        {
            var topWash = RuntimeUiFactory.CreateUiRoot("TopWash", _overlayRoot);
            topWash.anchorMin = new Vector2(0f, 1f);
            topWash.anchorMax = new Vector2(1f, 1f);
            topWash.pivot = new Vector2(0.5f, 1f);
            topWash.sizeDelta = new Vector2(0f, 140f);
            RuntimeUiFactory.AddImage(topWash.gameObject, new Color(0.12f, 0.18f, 0.2f, 0.18f));
        }

        private void BuildIntelStrip()
        {
            var strip = RuntimeUiFactory.CreateCard("IntelStrip", _overlayRoot, new Color(0.07f, 0.09f, 0.12f, 0.94f), ModernGuiTheme.AccentColor);
            strip.anchorMin = new Vector2(0.5f, 0f);
            strip.anchorMax = new Vector2(0.5f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.anchoredPosition = new Vector2(0f, 18f);
            strip.sizeDelta = new Vector2(700f, 60f);
            var layout = RuntimeUiFactory.AddHorizontalLayout(strip, 10f, new RectOffset(18, 18, 16, 14), true);
            layout.childForceExpandWidth = false;

            var left = RuntimeUiFactory.CreateUiRoot("Primary", strip);
            RuntimeUiFactory.EnsureLayoutElement(left, flexibleWidth: 1f);
            RuntimeUiFactory.AddVerticalLayout(left, 2f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.CreateText("PrimaryLabel", left, "CANLI INTEL", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _intelPrimaryText = RuntimeUiFactory.CreateText("PrimaryText", left, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var right = RuntimeUiFactory.CreateUiRoot("Secondary", strip);
            RuntimeUiFactory.EnsureLayoutElement(right, preferredWidth: 296f);
            RuntimeUiFactory.AddVerticalLayout(right, 2f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.CreateText("SecondaryLabel", right, "OTURUM OZETI", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _intelSecondaryText = RuntimeUiFactory.CreateText("SecondaryText", right, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildHeader(RectTransform parent)
        {
            var header = RuntimeUiFactory.CreateUiRoot("Header", parent);
            _headerRoot = header;
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 86f);
            RuntimeUiFactory.AddHorizontalLayout(header, 14f, new RectOffset(0, 0, 0, 0), true);

            var titleBlock = RuntimeUiFactory.CreateUiRoot("TitleBlock", header);
            RuntimeUiFactory.EnsureLayoutElement(titleBlock, flexibleWidth: 1f);
            RuntimeUiFactory.AddVerticalLayout(titleBlock, 4f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.CreateText("Title", titleBlock, "MOBIL OFL", 34, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("Subtitle", titleBlock, "Okul ici suc dosyasi ve co-op arastirma prototipi.", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var heroBadge = RuntimeUiFactory.CreateCard("HeroBadge", titleBlock, new Color(0.1f, 0.13f, 0.16f, 0.95f), ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(heroBadge, preferredHeight: 38f);
            RuntimeUiFactory.AddVerticalLayout(heroBadge, 2f, new RectOffset(14, 14, 8, 7));
            _heroStatusText = RuntimeUiFactory.CreateText("HeroStatus", heroBadge, "DOSYA HAZIR", 14, ModernGuiTheme.AccentWarmColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _heroMetaText = RuntimeUiFactory.CreateText("HeroMeta", heroBadge, "Tek oyuncu ya da co-op oturumunu buradan baslat.", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var profileCard = RuntimeUiFactory.CreateCard("ProfileCard", header, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(profileCard, preferredWidth: 300f, preferredHeight: 82f);
            RuntimeUiFactory.AddVerticalLayout(profileCard, 8f, new RectOffset(16, 16, 14, 14));
            RuntimeUiFactory.CreateText("ProfileLabel", profileCard, "OYUNCU PROFILI", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _playerNameField = RuntimeUiFactory.CreateInputField("PlayerNameField", profileCard, "Oyuncu adi", 20);
            RuntimeUiFactory.EnsureLayoutElement(_playerNameField.transform, preferredHeight: 42f);
            _playerNameField.onValueChanged.AddListener(OnPlayerNameChanged);
        }

        private void BuildMetrics(RectTransform parent)
        {
            var metricsRow = RuntimeUiFactory.CreateUiRoot("MetricsRow", parent);
            _metricsRoot = metricsRow;
            RuntimeUiFactory.EnsureLayoutElement(metricsRow, preferredHeight: 58f);
            var row = RuntimeUiFactory.AddHorizontalLayout(metricsRow, 10f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            _caseMetricText = CreateMetricCard(metricsRow, "VAKA");
            _playerMetricText = CreateMetricCard(metricsRow, "OYUNCU");
            _modeMetricText = CreateMetricCard(metricsRow, "MOD");
            _readyMetricText = CreateMetricCard(metricsRow, "HAZIR");
        }

        private Text CreateMetricCard(Transform parent, string label)
        {
            var card = RuntimeUiFactory.CreateCard(label + "Card", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, flexibleWidth: 1f, preferredHeight: 56f);
            RuntimeUiFactory.AddVerticalLayout(card, 2f, new RectOffset(13, 13, 9, 7));
            RuntimeUiFactory.CreateText(label + "Label", card, label, 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            return RuntimeUiFactory.CreateText(label + "Value", card, "-", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
        }

        private void BuildBody(RectTransform parent)
        {
            var body = RuntimeUiFactory.CreateUiRoot("Body", parent);
            _bodyRoot = body;
            RuntimeUiFactory.EnsureLayoutElement(body, preferredHeight: 416f, flexibleHeight: 1f);
            var row = RuntimeUiFactory.AddHorizontalLayout(body, 12f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var leftColumn = RuntimeUiFactory.CreateUiRoot("LeftColumn", body);
            _leftColumnRoot = leftColumn;
            RuntimeUiFactory.EnsureLayoutElement(leftColumn, preferredWidth: 360f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(leftColumn, 10f, new RectOffset(0, 0, 0, 0));
            BuildSessionCard(leftColumn);
            BuildSettingsCard(leftColumn);

            var rightColumn = RuntimeUiFactory.CreateUiRoot("RightColumn", body);
            _rightColumnRoot = rightColumn;
            RuntimeUiFactory.EnsureLayoutElement(rightColumn, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(rightColumn, 10f, new RectOffset(0, 0, 0, 0));
            BuildMissionCard(rightColumn);
            BuildRosterCard(rightColumn);
        }

        private void BuildSessionCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("SessionCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 252f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 7f, new RectOffset(15, 15, 13, 13));
            _sessionTitleText = RuntimeUiFactory.CreateText("SessionTitle", card, "ANA MENU", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusText = RuntimeUiFactory.CreateText("StatusText", card, "Hazir.", 16, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_statusText.transform, preferredHeight: 24f);
            _backendText = RuntimeUiFactory.CreateText("BackendText", card, "-", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_backendText.transform, preferredHeight: 22f);

            var primaryActions = RuntimeUiFactory.CreateUiRoot("PrimaryActions", card);
            RuntimeUiFactory.EnsureLayoutElement(primaryActions, preferredHeight: 48f);
            var actionLayout = RuntimeUiFactory.AddHorizontalLayout(primaryActions, 10f, new RectOffset(0, 0, 0, 0), true);
            actionLayout.childForceExpandWidth = true;

            _soloButton = RuntimeUiFactory.CreateButton("SoloButton", primaryActions, "Solo Basla", new Color(0.13f, 0.17f, 0.2f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_soloButton.transform, flexibleWidth: 1f, preferredHeight: 48f);
            _soloButton.onClick.AddListener(() => logic?.StartSoloFromUi());

            _hostButton = RuntimeUiFactory.CreateButton("HostButton", primaryActions, "Co-op Host", new Color(0.26f, 0.2f, 0.1f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_hostButton.transform, flexibleWidth: 1f, preferredHeight: 48f);
            _hostButton.onClick.AddListener(() => logic?.StartHostFromUi());

            RuntimeUiFactory.CreateText("JoinLabel", card, "JOIN CODE", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _joinCodeField = RuntimeUiFactory.CreateInputField("JoinCodeField", card, "ABC123", 18);
            RuntimeUiFactory.EnsureLayoutElement(_joinCodeField.transform, preferredHeight: 42f);
            _joinCodeField.onValueChanged.AddListener(OnJoinCodeChanged);

            var secondaryActions = RuntimeUiFactory.CreateUiRoot("SecondaryActions", card);
            _secondaryActionsRoot = secondaryActions;
            RuntimeUiFactory.EnsureLayoutElement(secondaryActions, preferredHeight: 40f);
            var secondaryLayout = RuntimeUiFactory.AddHorizontalLayout(secondaryActions, 10f, new RectOffset(0, 0, 0, 0), true);
            secondaryLayout.childForceExpandWidth = true;

            _joinButton = RuntimeUiFactory.CreateButton("JoinButton", secondaryActions, "Oturuma Katil", new Color(0.12f, 0.2f, 0.17f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_joinButton.transform, flexibleWidth: 1f, preferredHeight: 40f);
            _joinButton.onClick.AddListener(() => logic?.JoinCurrentCodeFromUi());

            _reconnectButton = RuntimeUiFactory.CreateButton("ReconnectButton", secondaryActions, "Yeniden Baglan", new Color(0.19f, 0.15f, 0.08f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(_reconnectButton.transform, flexibleWidth: 1f, preferredHeight: 40f);
            _reconnectButton.onClick.AddListener(() => logic?.ReconnectFromUi());

            var copyButton = RuntimeUiFactory.CreateButton("CopyButton", secondaryActions, "Kodu Kopyala", new Color(0.11f, 0.12f, 0.15f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(copyButton.transform, flexibleWidth: 1f, preferredHeight: 40f);
            copyButton.onClick.AddListener(CopyJoinCode);
        }

        private void BuildSettingsCard(Transform parent)
        {
            _settingsRoot = RuntimeUiFactory.CreateCard("MiniSettingsCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(_settingsRoot, preferredHeight: 116f);
            RuntimeUiFactory.AddVerticalLayout(_settingsRoot, 7f, new RectOffset(15, 15, 13, 13));
            RuntimeUiFactory.CreateText("SettingsTitle", _settingsRoot, "MINI AYARLAR", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _settingsSummaryText = RuntimeUiFactory.CreateText("SettingsSummary", _settingsRoot, string.Empty, 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_settingsSummaryText.transform, preferredHeight: 32f);

            var actions = RuntimeUiFactory.CreateUiRoot("SettingsActions", _settingsRoot);
            RuntimeUiFactory.EnsureLayoutElement(actions, preferredHeight: 34f);
            var row = RuntimeUiFactory.AddHorizontalLayout(actions, 8f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            var volumeDown = RuntimeUiFactory.CreateButton("VolumeDown", actions, "Ses -", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(volumeDown.transform, flexibleWidth: 1f, preferredHeight: 34f);
            volumeDown.onClick.AddListener(() => AdjustVolume(-0.1f));

            var volumeUp = RuntimeUiFactory.CreateButton("VolumeUp", actions, "Ses +", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(volumeUp.transform, flexibleWidth: 1f, preferredHeight: 34f);
            volumeUp.onClick.AddListener(() => AdjustVolume(0.1f));

            var lookDown = RuntimeUiFactory.CreateButton("LookDown", actions, "Bakis -", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(lookDown.transform, flexibleWidth: 1f, preferredHeight: 34f);
            lookDown.onClick.AddListener(() => AdjustLookSensitivity(-0.2f));

            var lookUp = RuntimeUiFactory.CreateButton("LookUp", actions, "Bakis +", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(lookUp.transform, flexibleWidth: 1f, preferredHeight: 34f);
            lookUp.onClick.AddListener(() => AdjustLookSensitivity(0.2f));
        }

        private void BuildCommandDeckCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("CommandDeckCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 136f);
            RuntimeUiFactory.AddVerticalLayout(card, 7f, new RectOffset(16, 16, 14, 14));

            RuntimeUiFactory.CreateText("DeckTitle", card, "KOMUTA GUVERTESI", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _commandDeckStatusText = RuntimeUiFactory.CreateText("DeckStatus", card, "Baglanti hazir.", 14, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var codeShell = RuntimeUiFactory.CreateCard("CodeShell", card, new Color(0.08f, 0.11f, 0.14f, 0.95f), ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(codeShell, preferredHeight: 40f);
            var shellLayout = RuntimeUiFactory.AddHorizontalLayout(codeShell, 10f, new RectOffset(14, 14, 10, 10), true);
            shellLayout.childForceExpandWidth = false;

            RuntimeUiFactory.CreateText("CodeLabel", codeShell, "JOIN", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            _commandDeckCodeText = RuntimeUiFactory.CreateText("CodeValue", codeShell, "YOK", 20, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(_commandDeckCodeText.transform, flexibleWidth: 1f);

            var pulse = RuntimeUiFactory.CreateUiRoot("SignalPulse", codeShell);
            RuntimeUiFactory.EnsureLayoutElement(pulse, preferredWidth: 18f, preferredHeight: 18f);
            var pulseImage = RuntimeUiFactory.AddImage(pulse.gameObject, ModernGuiTheme.AccentWarmColor);
            AddFloatMotion(pulse, pulseImage, new Vector2(0f, 1.5f), 1.6f, 0.12f, 0.08f, 0.2f);

            _commandDeckHintText = RuntimeUiFactory.CreateText("DeckHint", card, "Host acildiginda kod ve oyuncu akisi burada odaklanir.", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildControlsCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("ControlsCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("ControlsTitle", card, "KONTROLLER", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _controlsText = RuntimeUiFactory.CreateText("ControlsText", card, string.Empty, 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _hintText = RuntimeUiFactory.CreateText("HintText", card, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildMissionCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("MissionCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 206f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 14, 14));
            RuntimeUiFactory.CreateText("MissionTitle", card, "GOREV DOSYASI", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            var briefingStrip = RuntimeUiFactory.CreateCard("BriefingStrip", card, new Color(0.12f, 0.15f, 0.18f, 0.96f), ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(briefingStrip, preferredHeight: 58f);
            RuntimeUiFactory.AddVerticalLayout(briefingStrip, 3f, new RectOffset(16, 16, 12, 10));
            RuntimeUiFactory.CreateText("BriefingLabel", briefingStrip, "CANLI BRIEFING", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionBriefText = RuntimeUiFactory.CreateText("MissionBrief", briefingStrip, string.Empty, 16, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("NextLabel", card, "SIRADAKI HAMLE", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionNextStepText = RuntimeUiFactory.CreateText("MissionNextStep", card, string.Empty, 15, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _missionStatsText = RuntimeUiFactory.CreateText("MissionStats", card, string.Empty, 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildRosterCard(Transform parent)
        {
            var card = RuntimeUiFactory.CreateCard("RosterCard", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 116f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 14, 14));

            var header = RuntimeUiFactory.CreateUiRoot("RosterHeader", card);
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 36f);
            var headerLayout = RuntimeUiFactory.AddHorizontalLayout(header, 10f, new RectOffset(0, 0, 0, 0), true);
            headerLayout.childForceExpandWidth = false;

            RuntimeUiFactory.CreateText("RosterTitle", header, "TAKIM DURUMU", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(header.GetChild(0), flexibleWidth: 1f);

            _readyButton = RuntimeUiFactory.CreateButton("ReadyButton", header, "Hazirim", new Color(0.14f, 0.22f, 0.18f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(_readyButton.transform, preferredWidth: 150f, preferredHeight: 34f);
            _readyButtonText = _readyButton.GetComponentInChildren<Text>();
            _readyButton.onClick.AddListener(ToggleReady);

            var scrollView = RuntimeUiFactory.CreateScrollView("RosterScroll", card, out _rosterContent);
            RuntimeUiFactory.EnsureLayoutElement(scrollView.transform, flexibleHeight: 1f, preferredHeight: 52f);
            RuntimeUiFactory.AddVerticalLayout(_rosterContent, 8f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.AddContentSizeFitter(_rosterContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildFooter(RectTransform parent)
        {
            var footer = RuntimeUiFactory.CreateUiRoot("Footer", parent);
            _footerRoot = footer;
            RuntimeUiFactory.EnsureLayoutElement(footer, preferredHeight: 36f);
            var layout = RuntimeUiFactory.AddHorizontalLayout(footer, 12f, new RectOffset(0, 0, 0, 0), true);
            layout.childForceExpandWidth = false;

            _footerNoteText = RuntimeUiFactory.CreateText("FooterNote", footer, "Vaka masasina donup dosya uzerinden suphelileri karsilastir. Arayuz artik uGUI ile calisiyor.", 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(_footerNoteText.transform, flexibleWidth: 1f);

            _resetSaveButton = RuntimeUiFactory.CreateButton("ResetSaveButton", footer, "Kaydi Sifirla", new Color(0.18f, 0.12f, 0.1f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_resetSaveButton.transform, preferredWidth: 148f, preferredHeight: 36f);
            _resetSaveButton.onClick.AddListener(ClearSoloSave);

            _closeButton = RuntimeUiFactory.CreateButton("CloseButton", footer, "Oyuna Don", new Color(0.12f, 0.12f, 0.15f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_closeButton.transform, preferredWidth: 128f, preferredHeight: 36f);
            _closeButtonText = _closeButton.GetComponentInChildren<Text>();
            _closeButton.onClick.AddListener(() => logic?.CloseMenu());
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
            var menuMode = logic.CurrentMenuMode;

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

            _sessionTitleText.text = menuMode == MainMenuHud.RuntimeMenuMode.Opening
                ? "ANA MENU"
                : (menuMode == MainMenuHud.RuntimeMenuMode.Lobby ? "ONLINE LOBI" : "OYUN MENUSU");
            _statusText.text = logic.CurrentStatus;
            _backendText.text = bootstrap == null
                ? "Ag: offline"
                : $"Ag: {bootstrap.BackendLabel}";
            _heroStatusText.text = menuMode == MainMenuHud.RuntimeMenuMode.Opening
                ? "OPERASYON MERKEZI"
                : (bootstrap != null && bootstrap.IsOnlineSessionActive ? "CO-OP OTURUM CANLI" : "DOSYA HAZIR");
            _heroMetaText.text = BuildHeroMeta(menuMode, session, bootstrap, networkCaseState);

            if (_soloButton != null)
            {
                _soloButton.GetComponentInChildren<Text>().text = menuMode == MainMenuHud.RuntimeMenuMode.Pause ? "Yeni Solo Vaka" : "Solo Basla";
            }

            if (_controlsText != null)
            {
                _controlsText.text =
                    "PC: WASD hareket, Mouse bakis, E etkilesim, Tab dosya, Esc menu.\n" +
                    "Mobil: Joystick hareket, sag alan bakis, AL etkilesim, DOSYA vaka dosyasi.";
            }

            if (_hintText != null)
            {
                _hintText.text = session == null ? "Aktif oturum bekleniyor." : session.GetRecommendedNextStep();
            }
            if (_settingsSummaryText != null)
            {
                _settingsSummaryText.text = $"Ses %{Mathf.RoundToInt(logic.MasterVolume * 100f)}  |  Bakis {logic.CameraSensitivity:0.0}";
            }
            if (_intelPrimaryText != null)
            {
                _intelPrimaryText.text = session == null
                    ? "Sahne kurulumundan sonra ilk dijital izi topla."
                    : CompactIntelLine(session);
            }

            if (_intelSecondaryText != null)
            {
                _intelSecondaryText.text = bootstrap == null
                    ? "Offline akis hazir. Tek kisilik test icin uygun."
                    : CompactSessionLine(bootstrap, networkCaseState);
            }
            if (_commandDeckStatusText != null)
            {
                _commandDeckStatusText.text = BuildCommandDeckStatus(bootstrap, networkCaseState);
            }

            if (_commandDeckCodeText != null)
            {
                _commandDeckCodeText.text = bootstrap == null || string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode) ? "YOK" : bootstrap.CurrentJoinCode;
            }

            if (_commandDeckHintText != null)
            {
                _commandDeckHintText.text = BuildCommandDeckHint(bootstrap, networkCaseState);
            }

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
                    $"Takim notu: {session.TeamNotes.Count}\n" +
                    $"Solo kayit: {BuildSaveSummary(session)}";
            }
            else
            {
                _missionBriefText.text = "Aktif vaka bilgisi bekleniyor.";
                _missionNextStepText.text = "Kurulum tamamlandiginda ilk delili toplamaya basla.";
                _missionStatsText.text = "Delil ve sorgu verileri daha sonra burada dolacak.";
            }

            _hostButton.interactable = bootstrap == null || !bootstrap.IsBusy;
            _joinButton.interactable = bootstrap == null || !bootstrap.IsBusy;
            if (_closeButton != null)
            {
                var canClose = menuMode == MainMenuHud.RuntimeMenuMode.Pause || logic.HasStartedGameplay;
                _closeButton.gameObject.SetActive(canClose);
                _closeButton.interactable = canClose;
                if (_closeButtonText != null)
                {
                    _closeButtonText.text = "Oyuna Don";
                }
            }
            if (_reconnectButton != null)
            {
                var canReconnect = bootstrap != null && bootstrap.CanReconnectLastSession && !bootstrap.IsBusy;
                _reconnectButton.interactable = canReconnect;
                if (MobileInvestigationOverlay.IsMobileUiAllowed)
                {
                    _reconnectButton.gameObject.SetActive(canReconnect);
                }
            }

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

            var canStart = bootstrap != null &&
                bootstrap.IsOnlineSessionActive &&
                bootstrap.CurrentMode == "Host" &&
                networkCaseState.IsLobbyPhase &&
                networkCaseState.AreAllRegisteredPlayersReady;

            _readyButton.interactable = networkCaseState.IsLobbyPhase;
            _readyButtonText.text = canStart
                ? "Operasyonu Baslat"
                : (IsLocalPlayerReady(networkCaseState) ? "Beklemeye Al" : "Hazirim");
        }

        private void CreateRosterLine(string text, Color color, bool ready)
        {
            var card = RuntimeUiFactory.CreateCard("RosterLine", _rosterContent, new Color(0.1f, 0.12f, 0.15f, 0.94f), ready ? ModernGuiTheme.AccentWarmColor : ModernGuiTheme.BorderColor);
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 46f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(14, 14, 12, 10));
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

            var bootstrap = logic != null ? logic.Bootstrap : null;
            if (bootstrap != null &&
                bootstrap.IsOnlineSessionActive &&
                bootstrap.CurrentMode == "Host" &&
                networkCaseState.IsLobbyPhase &&
                networkCaseState.AreAllRegisteredPlayersReady)
            {
                networkCaseState.RequestStartInvestigation();
                RefreshImmediate();
                return;
            }

            if (!networkCaseState.IsLobbyPhase)
            {
                return;
            }

            networkCaseState.RequestSetReady(!IsLocalPlayerReady(networkCaseState));
            RefreshImmediate();
        }

        private void ClearSoloSave()
        {
            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null)
            {
                return;
            }

            var saveManager = CaseSaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.ClearSaveForCase(session.ActiveCase.CaseId);
            }

            session.RestartCurrentCase();
            session.PublishMessage("Solo kayit sifirlandi. Vaka temiz baslatildi.");
            RefreshImmediate();
        }

        private void AdjustVolume(float delta)
        {
            if (logic == null)
            {
                return;
            }

            logic.SetMasterVolume(logic.MasterVolume + delta);
            RefreshImmediate();
        }

        private void AdjustLookSensitivity(float delta)
        {
            if (logic == null)
            {
                return;
            }

            logic.SetCameraSensitivity(logic.CameraSensitivity + delta);
            RefreshImmediate();
        }

        private void CopyJoinCode()
        {
            if (logic == null)
            {
                return;
            }

            var code = logic.Bootstrap != null && !string.IsNullOrWhiteSpace(logic.Bootstrap.CurrentJoinCode)
                ? logic.Bootstrap.CurrentJoinCode
                : logic.JoinCodeInput;
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = code;
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
            _collapsedRoot.anchoredPosition = MobileInvestigationOverlay.IsMobileHudVisible
                ? new Vector2(24f, 242f)
                : new Vector2(24f, 218f);
            if (_collapsedHintText != null)
            {
                _collapsedHintText.gameObject.SetActive(!MobileInvestigationOverlay.IsMobileHudVisible);
                _collapsedHintText.text = string.IsNullOrWhiteSpace(logic.PlayerName) ? "Dosya merkezi" : logic.PlayerName;
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (!_built || _windowRoot == null)
            {
                return;
            }

            var mobileLayout = MobileInvestigationOverlay.IsMobileUiAllowed;
            _windowRoot.sizeDelta = mobileLayout ? new Vector2(1040f, 640f) : new Vector2(1160f, 720f);

            SetPreferredHeight(_headerRoot, mobileLayout ? 74f : 86f);
            SetPreferredHeight(_metricsRoot, mobileLayout ? 0f : 58f);
            SetPreferredHeight(_bodyRoot, mobileLayout ? 430f : 416f);
            SetPreferredHeight(_footerRoot, mobileLayout ? 38f : 36f);
            _metricsRoot?.gameObject.SetActive(!mobileLayout);

            if (_leftColumnRoot != null)
            {
                var layout = RuntimeUiFactory.EnsureLayoutElement(_leftColumnRoot, preferredWidth: mobileLayout ? 332f : 360f, flexibleHeight: 1f);
                layout.flexibleWidth = 0f;
            }

            if (_rightColumnRoot != null)
            {
                var layout = RuntimeUiFactory.EnsureLayoutElement(_rightColumnRoot, flexibleWidth: 1f, flexibleHeight: 1f);
                layout.preferredWidth = mobileLayout ? 560f : -1f;
            }

            if (_hintText != null)
            {
                _hintText.gameObject.SetActive(!mobileLayout);
            }

            if (_footerNoteText != null)
            {
                _footerNoteText.gameObject.SetActive(!mobileLayout);
            }

            if (_secondaryActionsRoot != null)
            {
                SetPreferredHeight(_secondaryActionsRoot, mobileLayout ? 38f : 40f);
            }

            if (_reconnectButton != null)
            {
                var canReconnect = logic != null && logic.Bootstrap != null && logic.Bootstrap.CanReconnectLastSession && !logic.Bootstrap.IsBusy;
                _reconnectButton.gameObject.SetActive(!mobileLayout || canReconnect);
                RuntimeUiFactory.EnsureLayoutElement(_reconnectButton.transform, flexibleWidth: 1f, preferredHeight: mobileLayout ? 38f : 40f);
            }

            if (_joinButton != null)
            {
                RuntimeUiFactory.EnsureLayoutElement(_joinButton.transform, flexibleWidth: 1f, preferredHeight: mobileLayout ? 38f : 40f);
            }

            if (_resetSaveButton != null)
            {
                RuntimeUiFactory.EnsureLayoutElement(_resetSaveButton.transform, preferredWidth: mobileLayout ? 132f : 148f, preferredHeight: mobileLayout ? 34f : 36f);
            }
        }

        private static void SetPreferredHeight(RectTransform target, float height)
        {
            if (target == null)
            {
                return;
            }

            var layout = RuntimeUiFactory.EnsureLayoutElement(target, preferredHeight: height);
            layout.minHeight = height;
            layout.flexibleHeight = height <= 0f ? 0f : layout.flexibleHeight;
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

        private void CreateAmbientCard(
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            string title,
            string body,
            Color panelColor,
            Color accentColor,
            Vector2 motionAmplitude,
            float speed,
            float phase)
        {
            var card = RuntimeUiFactory.CreateCard(name, _overlayRoot, panelColor, accentColor);
            card.anchorMin = anchor;
            card.anchorMax = anchor;
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = anchoredPosition;
            card.sizeDelta = size;
            RuntimeUiFactory.AddVerticalLayout(card, 3f, new RectOffset(16, 16, 18, 14));
            RuntimeUiFactory.CreateText("Title", card, title, 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("Body", card, body, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            AddFloatMotion(card, card.GetComponent<Image>(), motionAmplitude, speed, 0.05f, 0.02f, phase);
        }

        private static void AddFloatMotion(RectTransform target, Graphic graphic, Vector2 amplitude, float speed, float alphaPulse, float scalePulse, float phase)
        {
            var motion = target.gameObject.GetComponent<UiFloatMotion>();
            if (motion == null)
            {
                motion = target.gameObject.AddComponent<UiFloatMotion>();
            }
            motion.Configure(target, graphic, amplitude, speed, alphaPulse, scalePulse, phase);
        }

        private static string CompactIntelLine(CaseSessionManager session)
        {
            var evidence = session.CollectedEvidenceIds.Count;
            var totalEvidence = session.ActiveCase == null ? 0 : session.ActiveCase.EvidenceItems.Count;
            return $"Delil {evidence}/{totalEvidence}  |  Sorgu {session.InterviewedNpcCount}  |  Not {session.TeamNotes.Count}";
        }

        private static string CompactSessionLine(RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            if (bootstrap == null)
            {
                return "Ag katmani hazir degil.";
            }

            if (networkCaseState == null)
            {
                return bootstrap.CurrentStatus;
            }

            var joinCode = string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode) ? "JOIN YOK" : bootstrap.CurrentJoinCode;
            return $"{bootstrap.CurrentMode}  |  {networkCaseState.CurrentPhaseLabel}  |  Oyuncu {networkCaseState.RegisteredPlayerCount}  |  {joinCode}";
        }

        private static string BuildHeroMeta(
            MainMenuHud.RuntimeMenuMode menuMode,
            CaseSessionManager session,
            RelayNetworkBootstrap bootstrap,
            NetworkCaseState networkCaseState)
        {
            if (menuMode == MainMenuHud.RuntimeMenuMode.Opening)
            {
                return "Solo basla ya da co-op lobby kurup ekibi hazirla.";
            }

            if (menuMode == MainMenuHud.RuntimeMenuMode.Lobby)
            {
                if (bootstrap == null || !bootstrap.IsOnlineSessionActive)
                {
                    return "Host ac veya join code girerek lobiye katil.";
                }

                if (networkCaseState == null)
                {
                    return "Online baglanti hazir, roster bekleniyor.";
                }

                return networkCaseState.AreAllRegisteredPlayersReady
                    ? "Ekip hazir. Host operasyonu baslatabilir."
                    : "Oyuncular hazir durumuna gecince operasyon baslayacak.";
            }

            return session == null ? "Oyuna donup ilk delili topla." : session.GetRecommendedNextStep();
        }

        private static string BuildCommandDeckStatus(RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            if (bootstrap == null)
            {
                return "Offline operasyon hazir.";
            }

            if (bootstrap.IsBusy)
            {
                return "Baglanti islemi suruyor...";
            }

            if (!bootstrap.IsOnlineSessionActive)
            {
                return bootstrap.CanReconnectLastSession
                    ? "Baglanti koptu. Son relay oturumuna yeniden baglanabilirsin."
                    : "Online oturum acik degil. Host baslat veya bir koda katil.";
            }

            if (networkCaseState == null)
            {
                return $"{bootstrap.CurrentMode} aktif. Oyuncu rosteri bekleniyor.";
            }

            return $"{bootstrap.CurrentMode} aktif  |  Faz: {networkCaseState.CurrentPhaseLabel}  |  {networkCaseState.ReadyPlayerCount}/{networkCaseState.RegisteredPlayerCount} hazir";
        }

        private static string BuildSaveSummary(CaseSessionManager session)
        {
            if (session == null || session.ActiveCase == null)
            {
                return "hazir degil";
            }

            var saveManager = CaseSaveManager.Instance;
            return saveManager == null ? "hazirlaniyor" : saveManager.GetCurrentSaveSummary(session.ActiveCase);
        }

        private static string BuildCommandDeckHint(RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            if (bootstrap == null)
            {
                return "Tek kisilik test icin dogrudan baslayabilirsin.";
            }

            if (!bootstrap.IsOnlineSessionActive)
            {
                return bootstrap.CanReconnectLastSession
                    ? "Baglanti koptuysa ayni panelden yeniden baglanmayi deneyebilirsin."
                    : "Host acarsan join code uretilir; client isen kodu girip dogrudan katil.";
            }

            if (networkCaseState == null || networkCaseState.RegisteredPlayerCount <= 1)
            {
                return "Kod hazir. Takim arkadaslarina gonderip lobiyi doldur.";
            }

            if (networkCaseState.ReadyPlayerCount < networkCaseState.RegisteredPlayerCount)
            {
                return "Butun oyuncular hazir olunca soru zincirini ayni anda baslatmak daha temiz olur.";
            }

            if (bootstrap.CurrentMode == "Host" && networkCaseState.IsLobbyPhase)
            {
                return "Tum ekip hazir. Host artik 'Operasyonu Baslat' ile ayni anda sahaya indirebilir.";
            }

            return networkCaseState.IsGameplayPhase
                ? "Operasyon canli. Takim ayni anda sahada."
                : "Takim hazir. Hostun operasyonu baslatmasi bekleniyor.";
        }
    }
}







