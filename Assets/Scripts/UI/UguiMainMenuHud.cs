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
        private RectTransform _openingRoot;
        private RectTransform _lobbyRoot;
        private RectTransform _pauseRoot;
        private RectTransform _rosterContent;
        private CanvasGroup _overlayGroup;
        private InputField _playerNameField;
        private InputField _openingJoinField;
        private InputField _lobbyJoinField;
        private Text _titleText;
        private Text _subtitleText;
        private Text _modeBadgeText;
        private Text _statusText;
        private Text _casePreviewText;
        private Text _openingSettingsText;
        private Text _lobbyCodeText;
        private Text _lobbyHintText;
        private Text _readyButtonText;
        private Text _pauseSummaryText;
        private Text _pauseSettingsText;
        private Text _roleButtonText;
        private Button _soloButton;
        private Button _hostButton;
        private Button _openingJoinButton;
        private Button _lobbyJoinButton;
        private Button _reconnectButton;
        private Button _readyButton;
        private Button _closeButton;
        private Button _resetSaveButton;
        private Button _roleButton;
        private float _openBlend;
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
            ResolveLogic();
            BuildIfNeeded();
            SyncVisibility();
            ApplyResponsiveLayout();
            AnimateMenu(logic != null && logic.IsOpen);

            if (Time.unscaledTime >= _nextRefreshAt)
            {
                RefreshImmediate();
                _nextRefreshAt = Time.unscaledTime + 0.15f;
            }
        }

        private void ResolveLogic()
        {
            if (logic != null)
            {
                return;
            }

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

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            ResolveLogic();
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
            RefreshImmediate();
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

            var openButton = RuntimeUiFactory.CreateButton("OpenMenuButton", _collapsedRoot, "MENU", new Color(0.1f, 0.13f, 0.16f, 0.82f), 16);
            RuntimeUiFactory.Stretch(openButton.GetComponent<RectTransform>());
            openButton.onClick.AddListener(() => logic?.OpenMenu());
        }

        private void BuildOverlay(RectTransform canvasTransform)
        {
            _overlayRoot = RuntimeUiFactory.CreateUiRoot("OverlayRoot", canvasTransform);
            RuntimeUiFactory.Stretch(_overlayRoot);
            RuntimeUiFactory.AddImage(_overlayRoot.gameObject, new Color(0.015f, 0.02f, 0.026f, 0.92f));

            _overlayGroup = _overlayRoot.gameObject.GetComponent<CanvasGroup>();
            if (_overlayGroup == null)
            {
                _overlayGroup = _overlayRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var leftWash = RuntimeUiFactory.CreateUiRoot("LeftWash", _overlayRoot);
            leftWash.anchorMin = new Vector2(0f, 0f);
            leftWash.anchorMax = new Vector2(0.34f, 1f);
            leftWash.offsetMin = Vector2.zero;
            leftWash.offsetMax = Vector2.zero;
            RuntimeUiFactory.AddImage(leftWash.gameObject, new Color(0.04f, 0.18f, 0.2f, 0.22f));

            var topRule = RuntimeUiFactory.CreateUiRoot("TopRule", _overlayRoot);
            topRule.anchorMin = new Vector2(0.08f, 1f);
            topRule.anchorMax = new Vector2(0.92f, 1f);
            topRule.pivot = new Vector2(0.5f, 1f);
            topRule.sizeDelta = new Vector2(0f, 3f);
            topRule.anchoredPosition = new Vector2(0f, -28f);
            RuntimeUiFactory.AddImage(topRule.gameObject, new Color(0.96f, 0.68f, 0.28f, 0.52f));

            _windowRoot = RuntimeUiFactory.CreateCard("MenuWindow", _overlayRoot, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentWarmColor);
            _windowRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _windowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _windowRoot.pivot = new Vector2(0.5f, 0.5f);
            _windowRoot.sizeDelta = new Vector2(1080f, 620f);
            _windowRoot.anchoredPosition = Vector2.zero;
            RuntimeUiFactory.AddVerticalLayout(_windowRoot, 14f, new RectOffset(22, 22, 22, 22));

            BuildHeader(_windowRoot);

            var contentHost = RuntimeUiFactory.CreateUiRoot("ContentHost", _windowRoot);
            RuntimeUiFactory.EnsureLayoutElement(contentHost, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.Stretch(contentHost);

            BuildOpeningView(contentHost);
            BuildLobbyView(contentHost);
            BuildPauseView(contentHost);
        }

        private void BuildHeader(Transform parent)
        {
            _headerRoot = RuntimeUiFactory.CreateUiRoot("Header", parent);
            RuntimeUiFactory.EnsureLayoutElement(_headerRoot, preferredHeight: 146f);
            var row = RuntimeUiFactory.AddHorizontalLayout(_headerRoot, 16f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = false;

            var brand = RuntimeUiFactory.CreateUiRoot("Brand", _headerRoot);
            RuntimeUiFactory.EnsureLayoutElement(brand, flexibleWidth: 1f);
            RuntimeUiFactory.AddVerticalLayout(brand, 4f, new RectOffset(0, 0, 0, 0));
            _titleText = RuntimeUiFactory.CreateText("Title", brand, "MOBIL OFL", 42, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _subtitleText = RuntimeUiFactory.CreateText("Subtitle", brand, "Okul dosyasi, gizlilik ve co-op arastirma.", 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var modeBadge = RuntimeUiFactory.CreateCard("ModeBadge", brand, new Color(0.09f, 0.12f, 0.15f, 0.96f), ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(modeBadge, preferredHeight: 36f);
            RuntimeUiFactory.AddVerticalLayout(modeBadge, 0f, new RectOffset(14, 14, 9, 7));
            _modeBadgeText = RuntimeUiFactory.CreateText("ModeBadgeText", modeBadge, "BASLANGIC", 14, ModernGuiTheme.AccentWarmColor, FontStyle.Bold, TextAnchor.MiddleLeft);

            var profile = RuntimeUiFactory.CreateCard("ProfileCard", _headerRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(profile, preferredWidth: 360f, preferredHeight: 138f);
            RuntimeUiFactory.AddVerticalLayout(profile, 8f, new RectOffset(16, 16, 14, 14));
            RuntimeUiFactory.CreateText("ProfileLabel", profile, "OYUNCU", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _playerNameField = RuntimeUiFactory.CreateInputField("PlayerNameField", profile, "Oyuncu adi", 20);
            RuntimeUiFactory.EnsureLayoutElement(_playerNameField.transform, preferredHeight: 44f);
            _playerNameField.onValueChanged.AddListener(OnPlayerNameChanged);

            _roleButton = RuntimeUiFactory.CreateButton("ProfileButton", profile, "Dedektif Profili", new Color(0.1f, 0.13f, 0.16f, 1f), 14);
            RuntimeUiFactory.EnsureLayoutElement(_roleButton.transform, preferredHeight: 36f);
            _roleButtonText = _roleButton.GetComponentInChildren<Text>();
        }

        private void BuildOpeningView(Transform parent)
        {
            _openingRoot = CreateModeRoot("OpeningRoot", parent);
            var row = RuntimeUiFactory.AddHorizontalLayout(_openingRoot, 18f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            var actions = RuntimeUiFactory.CreateCard("StartActions", _openingRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(actions, preferredWidth: 430f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(actions, 12f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("StartTitle", actions, "BASLANGIC", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statusText = RuntimeUiFactory.CreateText("Status", actions, "Offline hazir.", 17, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_statusText.transform, preferredHeight: 34f);

            _soloButton = RuntimeUiFactory.CreateButton("SoloButton", actions, "Solo Basla", new Color(0.12f, 0.17f, 0.2f, 1f), 19);
            RuntimeUiFactory.EnsureLayoutElement(_soloButton.transform, preferredHeight: 62f);
            _soloButton.onClick.AddListener(() => logic?.StartSoloFromUi());

            _hostButton = RuntimeUiFactory.CreateButton("HostButton", actions, "Lobi Kur", new Color(0.28f, 0.2f, 0.09f, 1f), 19);
            RuntimeUiFactory.EnsureLayoutElement(_hostButton.transform, preferredHeight: 62f);
            _hostButton.onClick.AddListener(() => logic?.StartHostFromUi());

            RuntimeUiFactory.CreateText("JoinLabel", actions, "DAVET KODU", 13, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _openingJoinField = RuntimeUiFactory.CreateInputField("OpeningJoinCode", actions, "ABC123", 20);
            RuntimeUiFactory.EnsureLayoutElement(_openingJoinField.transform, preferredHeight: 48f);
            _openingJoinField.onValueChanged.AddListener(OnJoinCodeChanged);

            _openingJoinButton = RuntimeUiFactory.CreateButton("JoinButton", actions, "Koda Katil", new Color(0.12f, 0.22f, 0.17f, 1f), 18);
            RuntimeUiFactory.EnsureLayoutElement(_openingJoinButton.transform, preferredHeight: 50f);
            _openingJoinButton.onClick.AddListener(() => logic?.JoinCurrentCodeFromUi());

            _reconnectButton = RuntimeUiFactory.CreateButton("ReconnectButton", actions, "Son Lobiye Don", new Color(0.14f, 0.13f, 0.17f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_reconnectButton.transform, preferredHeight: 44f);
            _reconnectButton.onClick.AddListener(() => logic?.ReconnectFromUi());

            var dossier = RuntimeUiFactory.CreateCard("StartDossier", _openingRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(dossier, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(dossier, 12f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("DossierTitle", dossier, "DOSYA", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _casePreviewText = RuntimeUiFactory.CreateText("CasePreview", dossier, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_casePreviewText.transform, preferredHeight: 120f);
            RuntimeUiFactory.CreateText("InviteHint", dossier, "Lobi kurunca davet kodu otomatik uretilir. Takim hazir olunca host operasyonu baslatir.", 16, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var settings = RuntimeUiFactory.CreateCard("StartSettings", dossier, new Color(0.08f, 0.1f, 0.13f, 0.94f), ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(settings, preferredHeight: 142f);
            RuntimeUiFactory.AddVerticalLayout(settings, 10f, new RectOffset(16, 16, 15, 15));
            RuntimeUiFactory.CreateText("SettingsTitle", settings, "HIZLI AYAR", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _openingSettingsText = RuntimeUiFactory.CreateText("SettingsSummary", settings, string.Empty, 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_openingSettingsText.transform, preferredHeight: 26f);
            BuildSettingsButtons(settings);
        }

        private void BuildLobbyView(Transform parent)
        {
            _lobbyRoot = CreateModeRoot("LobbyRoot", parent);
            var row = RuntimeUiFactory.AddHorizontalLayout(_lobbyRoot, 18f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            var invite = RuntimeUiFactory.CreateCard("InviteCard", _lobbyRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(invite, preferredWidth: 430f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(invite, 12f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("InviteTitle", invite, "LOBI DAVETI", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _lobbyCodeText = RuntimeUiFactory.CreateText("LobbyCode", invite, "KOD YOK", 38, ModernGuiTheme.AccentWarmColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(_lobbyCodeText.transform, preferredHeight: 64f);
            _lobbyHintText = RuntimeUiFactory.CreateText("LobbyHint", invite, string.Empty, 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_lobbyHintText.transform, preferredHeight: 78f);

            var copyButton = RuntimeUiFactory.CreateButton("CopyInviteButton", invite, "Davet Kodunu Kopyala", new Color(0.12f, 0.17f, 0.2f, 1f), 17);
            RuntimeUiFactory.EnsureLayoutElement(copyButton.transform, preferredHeight: 48f);
            copyButton.onClick.AddListener(CopyJoinCode);

            _lobbyJoinField = RuntimeUiFactory.CreateInputField("LobbyJoinCode", invite, "Koda katil", 18);
            RuntimeUiFactory.EnsureLayoutElement(_lobbyJoinField.transform, preferredHeight: 44f);
            _lobbyJoinField.onValueChanged.AddListener(OnJoinCodeChanged);

            _lobbyJoinButton = RuntimeUiFactory.CreateButton("LobbyJoinButton", invite, "Koda Katil", new Color(0.12f, 0.22f, 0.17f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_lobbyJoinButton.transform, preferredHeight: 44f);
            _lobbyJoinButton.onClick.AddListener(() => logic?.JoinCurrentCodeFromUi());

            var closeSessionButton = RuntimeUiFactory.CreateButton("CloseSessionButton", invite, "Lobiyi Kapat", new Color(0.22f, 0.1f, 0.09f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(closeSessionButton.transform, preferredHeight: 44f);
            closeSessionButton.onClick.AddListener(ShutdownSession);

            var roster = RuntimeUiFactory.CreateCard("RosterCard", _lobbyRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(roster, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(roster, 12f, new RectOffset(18, 18, 18, 18));

            var header = RuntimeUiFactory.CreateUiRoot("RosterHeader", roster);
            RuntimeUiFactory.EnsureLayoutElement(header, preferredHeight: 48f);
            var headerRow = RuntimeUiFactory.AddHorizontalLayout(header, 12f, new RectOffset(0, 0, 0, 0), true);
            headerRow.childForceExpandWidth = false;
            RuntimeUiFactory.CreateText("RosterTitle", header, "TAKIM", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            RuntimeUiFactory.EnsureLayoutElement(header.GetChild(0), flexibleWidth: 1f);

            _readyButton = RuntimeUiFactory.CreateButton("ReadyButton", header, "Hazirim", new Color(0.13f, 0.22f, 0.17f, 1f), 17);
            RuntimeUiFactory.EnsureLayoutElement(_readyButton.transform, preferredWidth: 190f, preferredHeight: 46f);
            _readyButtonText = _readyButton.GetComponentInChildren<Text>();
            _readyButton.onClick.AddListener(ToggleReady);

            var scrollView = RuntimeUiFactory.CreateScrollView("RosterScroll", roster, out _rosterContent);
            RuntimeUiFactory.EnsureLayoutElement(scrollView.transform, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(_rosterContent, 8f, new RectOffset(0, 0, 0, 0));
            RuntimeUiFactory.AddContentSizeFitter(_rosterContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildPauseView(Transform parent)
        {
            _pauseRoot = CreateModeRoot("PauseRoot", parent);
            var row = RuntimeUiFactory.AddHorizontalLayout(_pauseRoot, 18f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            var actions = RuntimeUiFactory.CreateCard("PauseActions", _pauseRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(actions, preferredWidth: 380f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(actions, 12f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("PauseTitle", actions, "DURAKLATILDI", 26, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            _closeButton = RuntimeUiFactory.CreateButton("ResumeButton", actions, "Oyuna Don", new Color(0.13f, 0.22f, 0.17f, 1f), 19);
            RuntimeUiFactory.EnsureLayoutElement(_closeButton.transform, preferredHeight: 58f);
            _closeButton.onClick.AddListener(() => logic?.CloseMenu());

            var notebookButton = RuntimeUiFactory.CreateButton("NotebookButton", actions, "Vaka Dosyasi", new Color(0.12f, 0.17f, 0.2f, 1f), 18);
            RuntimeUiFactory.EnsureLayoutElement(notebookButton.transform, preferredHeight: 52f);
            notebookButton.onClick.AddListener(OpenNotebookFromPause);

            var newSoloButton = RuntimeUiFactory.CreateButton("NewSoloButton", actions, "Yeni Solo Vaka", new Color(0.21f, 0.15f, 0.08f, 1f), 17);
            RuntimeUiFactory.EnsureLayoutElement(newSoloButton.transform, preferredHeight: 50f);
            newSoloButton.onClick.AddListener(() => logic?.StartSoloFromUi());

            _resetSaveButton = RuntimeUiFactory.CreateButton("ResetSaveButton", actions, "Kaydi Sifirla", new Color(0.22f, 0.1f, 0.09f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(_resetSaveButton.transform, preferredHeight: 46f);
            _resetSaveButton.onClick.AddListener(ClearSoloSave);

            var onlineCloseButton = RuntimeUiFactory.CreateButton("OnlineCloseButton", actions, "Online Oturumu Kapat", new Color(0.14f, 0.13f, 0.17f, 1f), 15);
            RuntimeUiFactory.EnsureLayoutElement(onlineCloseButton.transform, preferredHeight: 44f);
            onlineCloseButton.onClick.AddListener(ShutdownSession);

            var summary = RuntimeUiFactory.CreateCard("PauseSummary", _pauseRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(summary, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(summary, 14f, new RectOffset(18, 18, 18, 18));
            RuntimeUiFactory.CreateText("SummaryTitle", summary, "OTURUM OZETI", 24, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _pauseSummaryText = RuntimeUiFactory.CreateText("SummaryText", summary, string.Empty, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_pauseSummaryText.transform, preferredHeight: 150f);

            var settings = RuntimeUiFactory.CreateCard("PauseSettings", summary, new Color(0.08f, 0.1f, 0.13f, 0.94f), ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(settings, preferredHeight: 148f);
            RuntimeUiFactory.AddVerticalLayout(settings, 10f, new RectOffset(16, 16, 15, 15));
            RuntimeUiFactory.CreateText("PauseSettingsTitle", settings, "AYARLAR", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _pauseSettingsText = RuntimeUiFactory.CreateText("PauseSettingsText", settings, string.Empty, 15, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            RuntimeUiFactory.EnsureLayoutElement(_pauseSettingsText.transform, preferredHeight: 26f);
            BuildSettingsButtons(settings);
        }

        private RectTransform CreateModeRoot(string name, Transform parent)
        {
            var root = RuntimeUiFactory.CreateUiRoot(name, parent);
            RuntimeUiFactory.EnsureLayoutElement(root, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.Stretch(root);
            return root;
        }

        private void BuildSettingsButtons(Transform parent)
        {
            var buttons = RuntimeUiFactory.CreateUiRoot("SettingsButtons", parent);
            RuntimeUiFactory.EnsureLayoutElement(buttons, preferredHeight: 42f);
            var row = RuntimeUiFactory.AddHorizontalLayout(buttons, 8f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;

            var volumeDown = RuntimeUiFactory.CreateButton("VolumeDown", buttons, "Ses -", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(volumeDown.transform, flexibleWidth: 1f, preferredHeight: 42f);
            volumeDown.onClick.AddListener(() => AdjustVolume(-0.1f));

            var volumeUp = RuntimeUiFactory.CreateButton("VolumeUp", buttons, "Ses +", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(volumeUp.transform, flexibleWidth: 1f, preferredHeight: 42f);
            volumeUp.onClick.AddListener(() => AdjustVolume(0.1f));

            var lookDown = RuntimeUiFactory.CreateButton("LookDown", buttons, "Bakis -", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(lookDown.transform, flexibleWidth: 1f, preferredHeight: 42f);
            lookDown.onClick.AddListener(() => AdjustLookSensitivity(-0.2f));

            var lookUp = RuntimeUiFactory.CreateButton("LookUp", buttons, "Bakis +", new Color(0.11f, 0.13f, 0.16f, 1f), 13);
            RuntimeUiFactory.EnsureLayoutElement(lookUp.transform, flexibleWidth: 1f, preferredHeight: 42f);
            lookUp.onClick.AddListener(() => AdjustLookSensitivity(0.2f));
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

            SyncFieldsFromLogic();
            ApplyMode(menuMode);

            var isBusy = bootstrap != null && bootstrap.IsBusy;
            _hostButton.interactable = !isBusy;
            _openingJoinButton.interactable = !isBusy;
            _lobbyJoinButton.interactable = !isBusy && (bootstrap == null || !bootstrap.IsOnlineSessionActive);
            _reconnectButton.gameObject.SetActive(bootstrap != null && bootstrap.CanReconnectLastSession);
            _reconnectButton.interactable = bootstrap != null && bootstrap.CanReconnectLastSession && !bootstrap.IsBusy;

            _statusText.text = logic.CurrentStatus;
            _modeBadgeText.text = BuildModeBadge(menuMode, bootstrap, networkCaseState);
            _casePreviewText.text = BuildCasePreview(session);
            var settingsText = $"Ses %{Mathf.RoundToInt(logic.MasterVolume * 100f)}  |  Bakis {logic.CameraSensitivity:0.0}";
            _openingSettingsText.text = settingsText;
            _pauseSettingsText.text = settingsText;
            if (_roleButtonText != null)
            {
                _roleButtonText.text = "Dedektif Profili";
            }
            _pauseSummaryText.text = BuildPauseSummary(session, bootstrap, networkCaseState);
            _lobbyCodeText.text = BuildLobbyCode(bootstrap);
            _lobbyHintText.text = BuildLobbyHint(bootstrap, networkCaseState);

            RebuildRoster(networkCaseState, bootstrap);
        }

        private void SyncFieldsFromLogic()
        {
            _syncingFields = true;

            if (_playerNameField != null && !_playerNameField.isFocused)
            {
                _playerNameField.text = logic.PlayerName;
            }

            if (_openingJoinField != null && !_openingJoinField.isFocused)
            {
                _openingJoinField.text = logic.JoinCodeInput;
            }

            if (_lobbyJoinField != null && !_lobbyJoinField.isFocused)
            {
                _lobbyJoinField.text = logic.JoinCodeInput;
            }

            _syncingFields = false;
        }

        private void ApplyMode(MainMenuHud.RuntimeMenuMode menuMode)
        {
            _openingRoot.gameObject.SetActive(menuMode == MainMenuHud.RuntimeMenuMode.Opening);
            _lobbyRoot.gameObject.SetActive(menuMode == MainMenuHud.RuntimeMenuMode.Lobby);
            _pauseRoot.gameObject.SetActive(menuMode == MainMenuHud.RuntimeMenuMode.Pause);

            _titleText.text = menuMode == MainMenuHud.RuntimeMenuMode.Pause ? "MOBIL OFL" : "MOBIL OFL";
            _subtitleText.text = menuMode == MainMenuHud.RuntimeMenuMode.Pause
                ? "Oyun duraklatildi."
                : "Okul dosyasi, gizlilik ve co-op arastirma.";
        }

        private void RebuildRoster(NetworkCaseState networkCaseState, RelayNetworkBootstrap bootstrap)
        {
            RuntimeUiFactory.ClearChildren(_rosterContent);

            if (bootstrap == null || !bootstrap.IsOnlineSessionActive || networkCaseState == null)
            {
                CreateRosterLine("Lobi acildiginda oyuncular burada listelenir.", ModernGuiTheme.MutedTextColor, false);
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
                    CreateRosterLine(
                        entry.DisplayName + (entry.IsReady ? "  |  Hazir" : "  |  Beklemede"),
                        entry.IsReady ? ModernGuiTheme.TextColor : ModernGuiTheme.MutedTextColor,
                        entry.IsReady);
                }
            }

            var canStart = bootstrap.IsOnlineSessionActive &&
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
            RuntimeUiFactory.EnsureLayoutElement(card, preferredHeight: 48f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(14, 14, 12, 10));
            RuntimeUiFactory.CreateText("LineText", card, text, 15, color, FontStyle.Bold, TextAnchor.MiddleLeft);
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

            if (networkCaseState.IsLobbyPhase)
            {
                networkCaseState.RequestSetReady(!IsLocalPlayerReady(networkCaseState));
                RefreshImmediate();
            }
        }

        private void OpenNotebookFromPause()
        {
            logic?.CloseMenu();
            CaseNotebookHud.Instance?.OpenNotebook();
        }

        private void ShutdownSession()
        {
            var bootstrap = logic != null ? logic.Bootstrap : null;
            if (bootstrap == null || !bootstrap.IsOnlineSessionActive)
            {
                return;
            }

            bootstrap.ShutdownSession();
            logic?.OpenMenu(bootstrap.CurrentStatus);
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
        }

        private void ApplyResponsiveLayout()
        {
            if (!_built || _windowRoot == null || logic == null)
            {
                return;
            }

            var mobileLayout = MobileInvestigationOverlay.IsMobileUiAllowed || Screen.width < 1500;
            var menuMode = logic.CurrentMenuMode;
            _windowRoot.sizeDelta = menuMode == MainMenuHud.RuntimeMenuMode.Pause
                ? (mobileLayout ? new Vector2(880f, 560f) : new Vector2(920f, 560f))
                : (mobileLayout ? new Vector2(1040f, 640f) : new Vector2(1080f, 620f));
            RuntimeUiFactory.EnsureLayoutElement(_headerRoot, preferredHeight: mobileLayout ? 92f : 102f);
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

        private static string BuildModeBadge(MainMenuHud.RuntimeMenuMode mode, RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            if (mode == MainMenuHud.RuntimeMenuMode.Opening)
            {
                return "BASLANGIC";
            }

            if (mode == MainMenuHud.RuntimeMenuMode.Pause)
            {
                return "OYUN MENUSU";
            }

            if (bootstrap == null || !bootstrap.IsOnlineSessionActive)
            {
                return "LOBI HAZIR DEGIL";
            }

            return networkCaseState == null
                ? "LOBI"
                : $"LOBI  |  {networkCaseState.ReadyPlayerCount}/{networkCaseState.RegisteredPlayerCount} HAZIR";
        }

        private static string BuildCasePreview(CaseSessionManager session)
        {
            if (session == null || session.ActiveCase == null)
            {
                return "Vaka bilgisi yukleniyor.\n\nSolo veya co-op oturum baslatinca operasyon hazirlanacak.";
            }

            return session.ActiveCase.CaseTitle + "\n\n" +
                session.ActiveCase.OpeningBrief + "\n\n" +
                "Siradaki hedef: " + session.GetRecommendedNextStep();
        }

        private static string BuildLobbyCode(RelayNetworkBootstrap bootstrap)
        {
            if (bootstrap == null || string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode))
            {
                return "KOD YOK";
            }

            return bootstrap.CurrentJoinCode;
        }

        private static string BuildLobbyHint(RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            if (bootstrap == null || !bootstrap.IsOnlineSessionActive)
            {
                return "Lobi kur ya da arkadasindan aldigin kodla katil.";
            }

            if (networkCaseState == null)
            {
                return "Baglanti kuruldu. Oyuncu listesi bekleniyor.";
            }

            if (networkCaseState.AreAllRegisteredPlayersReady && bootstrap.CurrentMode == "Host")
            {
                return "Herkes hazir. Operasyonu baslatabilirsin.";
            }

            return "Kodu arkadasina gonder. Hazir olunca ekip ayni anda sahaya iner.";
        }

        private static string BuildPauseSummary(CaseSessionManager session, RelayNetworkBootstrap bootstrap, NetworkCaseState networkCaseState)
        {
            var mode = bootstrap == null ? "Offline" : bootstrap.CurrentMode;
            var phase = networkCaseState == null ? "Serbest kesif" : networkCaseState.CurrentPhaseLabel;

            if (session == null || session.ActiveCase == null)
            {
                return $"Mod: {mode}\nFaz: {phase}\nVaka bilgisi bekleniyor.";
            }

            return $"{session.ActiveCase.CaseTitle}\n" +
                $"Mod: {mode}  |  Faz: {phase}\n" +
                $"Delil: {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}  |  Sorgu: {session.InterviewedNpcCount}\n" +
                session.GetRecommendedNextStep();
        }
    }
}
