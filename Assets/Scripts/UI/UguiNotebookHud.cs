
using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiNotebookHud : MonoBehaviour
    {
        private enum NotebookTab
        {
            Overview,
            Evidence,
            Interviews,
            Notes,
            Suspects
        }

        [SerializeField] private CaseNotebookHud logic;

        private Canvas _canvas;
        private RectTransform _overlayRoot;
        private RectTransform _panelRoot;
        private CanvasGroup _overlayGroup;
        private float _openBlend;
        private RectTransform _contentArea;
        private Button[] _tabButtons;
        private Text[] _tabButtonTexts;
        private Text _caseTitleText;
        private Text _statsText;
        private Text _reasoningText;
        private RectTransform _overviewContent;
        private RectTransform _evidenceListContent;
        private RectTransform _interviewContent;
        private RectTransform _notesListContent;
        private RectTransform _suspectsContent;
        private Text _evidenceDetailsText;
        private InputField _noteInput;
        private Text _noteCounterText;
        private string _selectedEvidenceId;
        private NotebookTab _selectedTab;
        private float _nextRefreshAt;
        private bool _built;
        private bool _syncingNoteField;

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
                logic = GetComponent<CaseNotebookHud>();
                if (logic == null)
                {
                    logic = CaseNotebookHud.Instance;
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

            if (logic == null)
            {
                return;
            }

            var shouldShow = logic.IsOpen && !MainMenuHud.IsBlockingGameplay;
            _overlayRoot.gameObject.SetActive(true);
            AnimateNotebook(shouldShow);
            if (!shouldShow && _openBlend <= 0.01f)
            {
                return;
            }

            var desiredTab = (NotebookTab)Mathf.Clamp(logic.CurrentTabIndex, 0, 4);
            if (desiredTab != _selectedTab)
            {
                SetSelectedTab(desiredTab, false);
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

            if (logic == null)
            {
                logic = GetComponent<CaseNotebookHud>();
            }

            if (logic != null)
            {
                logic.RenderWithOnGui = false;
            }

            var canvasTransform = transform.Find("UguiNotebookCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiNotebookCanvas", transform);
            }

            _canvas = canvasTransform.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 86;

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

            BuildOverlay(canvasTransform);

            _built = true;
            _openBlend = logic != null && logic.IsOpen ? 1f : 0f;
            if (logic != null)
            {
                _selectedTab = (NotebookTab)Mathf.Clamp(logic.CurrentTabIndex, 0, 4);
            }
            SetSelectedTab(_selectedTab, false);
            AnimateNotebook(logic != null && logic.IsOpen);
        }

        private void BuildOverlay(RectTransform canvasTransform)
        {
            _overlayRoot = RuntimeUiFactory.CreateUiRoot("OverlayRoot", canvasTransform);
            RuntimeUiFactory.Stretch(_overlayRoot);
            RuntimeUiFactory.AddImage(_overlayRoot.gameObject, new Color(0.015f, 0.025f, 0.035f, 0.84f));
            _overlayGroup = _overlayRoot.gameObject.GetComponent<CanvasGroup>();
            if (_overlayGroup == null)
            {
                _overlayGroup = _overlayRoot.gameObject.AddComponent<CanvasGroup>();
            }

            _panelRoot = RuntimeUiFactory.CreateCard("NotebookPanel", _overlayRoot, ModernGuiTheme.PanelColor, ModernGuiTheme.AccentColor);
            _panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRoot.pivot = new Vector2(0.5f, 0.5f);
            _panelRoot.sizeDelta = new Vector2(1480f, 860f);
            _panelRoot.anchoredPosition = Vector2.zero;

            var frame = RuntimeUiFactory.CreateUiRoot("Frame", _panelRoot);
            RuntimeUiFactory.Stretch(frame);
            var bodyLayout = RuntimeUiFactory.AddHorizontalLayout(frame, 18f, new RectOffset(24, 24, 24, 24), true);
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            BuildSidebar(frame);
            BuildContent(frame);
        }

        private void BuildSidebar(Transform parent)
        {
            var sidebar = RuntimeUiFactory.CreateCard("Sidebar", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(sidebar, preferredWidth: 290f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(sidebar, 12f, new RectOffset(18, 18, 18, 18), false);

            RuntimeUiFactory.CreateText("SidebarTitle", sidebar, "VAKA DOSYASI", 34, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _caseTitleText = RuntimeUiFactory.CreateText("CaseTitle", sidebar, "-", 17, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            var statsCard = RuntimeUiFactory.CreateCard("StatsCard", sidebar, new Color(0.08f, 0.1f, 0.13f, 0.95f), ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(statsCard, preferredHeight: 140f);
            RuntimeUiFactory.AddVerticalLayout(statsCard, 6f, new RectOffset(14, 14, 16, 14), false);
            RuntimeUiFactory.CreateText("StatsLabel", statsCard, "OTURUM OZETI", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _statsText = RuntimeUiFactory.CreateText("StatsText", statsCard, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var tabsHost = RuntimeUiFactory.CreateUiRoot("Tabs", sidebar);
            RuntimeUiFactory.AddVerticalLayout(tabsHost, 8f, new RectOffset(0, 0, 0, 0), false);
            _tabButtons = new Button[5];
            _tabButtonTexts = new Text[5];
            CreateTabButton(tabsHost, NotebookTab.Overview, "Genel Durum");
            CreateTabButton(tabsHost, NotebookTab.Evidence, "Deliller");
            CreateTabButton(tabsHost, NotebookTab.Interviews, "Sorgular");
            CreateTabButton(tabsHost, NotebookTab.Notes, "Notlar");
            CreateTabButton(tabsHost, NotebookTab.Suspects, "Supheliler");

            var quickCard = RuntimeUiFactory.CreateCard("QuickCard", sidebar, new Color(0.08f, 0.1f, 0.13f, 0.95f), ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(quickCard, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(quickCard, 8f, new RectOffset(14, 14, 16, 14), false);
            RuntimeUiFactory.CreateText("QuickLabel", quickCard, "DOSYA ANALIZI", 12, ModernGuiTheme.MutedTextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _reasoningText = RuntimeUiFactory.CreateText("ReasoningText", quickCard, string.Empty, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var closeButton = RuntimeUiFactory.CreateButton("CloseNotebookButton", sidebar, "Dosyayi Kapat", new Color(0.12f, 0.12f, 0.15f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(closeButton.transform, preferredHeight: 48f);
            closeButton.onClick.AddListener(() => logic?.CloseNotebook());
        }

        private void CreateTabButton(Transform parent, NotebookTab tab, string label)
        {
            var button = RuntimeUiFactory.CreateButton(tab + "TabButton", parent, label, new Color(0.11f, 0.13f, 0.16f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(button.transform, preferredHeight: 50f);
            button.onClick.AddListener(() => SetSelectedTab(tab, true));
            _tabButtons[(int)tab] = button;
            _tabButtonTexts[(int)tab] = button.GetComponentInChildren<Text>();
        }
        private void BuildContent(Transform parent)
        {
            _contentArea = RuntimeUiFactory.CreateUiRoot("ContentArea", parent);
            RuntimeUiFactory.EnsureLayoutElement(_contentArea, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(_contentArea, 0f, new RectOffset(0, 0, 0, 0), false);

            BuildOverviewTab();
            BuildEvidenceTab();
            BuildInterviewsTab();
            BuildNotesTab();
            BuildSuspectsTab();
        }

        private void BuildOverviewTab()
        {
            var tabRoot = CreateTabRoot("OverviewTab");
            var scroll = RuntimeUiFactory.CreateScrollView("OverviewScroll", tabRoot, out _overviewContent);
            RuntimeUiFactory.Stretch(scroll.GetComponent<RectTransform>());
            RuntimeUiFactory.AddVerticalLayout(_overviewContent, 12f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_overviewContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildEvidenceTab()
        {
            var tabRoot = CreateTabRoot("EvidenceTab");
            var row = RuntimeUiFactory.AddHorizontalLayout(tabRoot, 14f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var listCard = RuntimeUiFactory.CreateCard("EvidenceListCard", tabRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(listCard, preferredWidth: 410f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(listCard, 10f, new RectOffset(16, 16, 16, 16), false);
            RuntimeUiFactory.CreateText("EvidenceListLabel", listCard, "BULUNAN DELILLER", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            var listScroll = RuntimeUiFactory.CreateScrollView("EvidenceListScroll", listCard, out _evidenceListContent);
            RuntimeUiFactory.EnsureLayoutElement(listScroll.transform, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(_evidenceListContent, 8f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_evidenceListContent, ContentSizeFitter.FitMode.PreferredSize);

            var detailCard = RuntimeUiFactory.CreateCard("EvidenceDetailCard", tabRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(detailCard, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(detailCard, 10f, new RectOffset(18, 18, 18, 18), false);
            RuntimeUiFactory.CreateText("EvidenceDetailLabel", detailCard, "DELIL DETAYI", 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            _evidenceDetailsText = RuntimeUiFactory.CreateText("EvidenceDetailText", detailCard, "Bir delil sec.", 15, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void BuildInterviewsTab()
        {
            var tabRoot = CreateTabRoot("InterviewsTab");
            var scroll = RuntimeUiFactory.CreateScrollView("InterviewsScroll", tabRoot, out _interviewContent);
            RuntimeUiFactory.Stretch(scroll.GetComponent<RectTransform>());
            RuntimeUiFactory.AddVerticalLayout(_interviewContent, 10f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_interviewContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private void BuildNotesTab()
        {
            var tabRoot = CreateTabRoot("NotesTab");
            var row = RuntimeUiFactory.AddHorizontalLayout(tabRoot, 14f, new RectOffset(0, 0, 0, 0), true);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var notesCard = RuntimeUiFactory.CreateCard("NotesListCard", tabRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.EnsureLayoutElement(notesCard, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(notesCard, 10f, new RectOffset(16, 16, 16, 16), false);
            RuntimeUiFactory.CreateText("NotesListLabel", notesCard, "TAKIM NOTLARI", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            var notesScroll = RuntimeUiFactory.CreateScrollView("NotesListScroll", notesCard, out _notesListContent);
            RuntimeUiFactory.EnsureLayoutElement(notesScroll.transform, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(_notesListContent, 8f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_notesListContent, ContentSizeFitter.FitMode.PreferredSize);

            var composeCard = RuntimeUiFactory.CreateCard("ComposeCard", tabRoot, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.EnsureLayoutElement(composeCard, preferredWidth: 360f, flexibleHeight: 1f);
            RuntimeUiFactory.AddVerticalLayout(composeCard, 10f, new RectOffset(18, 18, 18, 18), false);
            RuntimeUiFactory.CreateText("ComposeLabel", composeCard, "YENI NOT", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText("ComposeHint", composeCard, "Kisa, net ve tek satir notlar ortak dosyada daha temiz gorunur.", 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _noteInput = RuntimeUiFactory.CreateInputField("NoteInput", composeCard, "Notunu yaz", 18);
            RuntimeUiFactory.EnsureLayoutElement(_noteInput.transform, preferredHeight: 54f);
            _noteInput.onValueChanged.AddListener(HandleNoteChanged);
            _noteCounterText = RuntimeUiFactory.CreateText("NoteCounter", composeCard, "0/140", 13, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);

            var actions = RuntimeUiFactory.CreateUiRoot("NoteActions", composeCard);
            RuntimeUiFactory.EnsureLayoutElement(actions, preferredHeight: 46f);
            var actionsLayout = RuntimeUiFactory.AddHorizontalLayout(actions, 10f, new RectOffset(0, 0, 0, 0), true);
            actionsLayout.childForceExpandWidth = true;

            var addButton = RuntimeUiFactory.CreateButton("AddNoteButton", actions, "Takima Ekle", new Color(0.13f, 0.2f, 0.16f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(addButton.transform, flexibleWidth: 1f, preferredHeight: 46f);
            addButton.onClick.AddListener(SubmitTeamNote);

            var clearButton = RuntimeUiFactory.CreateButton("ClearNoteButton", actions, "Temizle", new Color(0.12f, 0.12f, 0.15f, 1f), 16);
            RuntimeUiFactory.EnsureLayoutElement(clearButton.transform, flexibleWidth: 1f, preferredHeight: 46f);
            clearButton.onClick.AddListener(ClearNoteDraft);
        }

        private void BuildSuspectsTab()
        {
            var tabRoot = CreateTabRoot("SuspectsTab");
            var scroll = RuntimeUiFactory.CreateScrollView("SuspectsScroll", tabRoot, out _suspectsContent);
            RuntimeUiFactory.Stretch(scroll.GetComponent<RectTransform>());
            RuntimeUiFactory.AddVerticalLayout(_suspectsContent, 10f, new RectOffset(0, 0, 0, 0), false);
            RuntimeUiFactory.AddContentSizeFitter(_suspectsContent, ContentSizeFitter.FitMode.PreferredSize);
        }

        private RectTransform CreateTabRoot(string name)
        {
            var tabRoot = RuntimeUiFactory.CreateUiRoot(name, _contentArea);
            RuntimeUiFactory.EnsureLayoutElement(tabRoot, flexibleWidth: 1f, flexibleHeight: 1f);
            RuntimeUiFactory.Stretch(tabRoot);
            return tabRoot;
        }

        private void SetSelectedTab(NotebookTab tab, bool pushToLogic)
        {
            _selectedTab = tab;
            if (pushToLogic)
            {
                logic?.SetTabIndex((int)tab);
            }

            for (var i = 0; i < _contentArea.childCount; i++)
            {
                _contentArea.GetChild(i).gameObject.SetActive(i == (int)_selectedTab);
            }

            for (var i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null)
                {
                    continue;
                }

                var image = _tabButtons[i].GetComponent<Image>();
                image.color = i == (int)_selectedTab
                    ? new Color(0.26f, 0.21f, 0.1f, 1f)
                    : new Color(0.11f, 0.13f, 0.16f, 1f);
                _tabButtonTexts[i].color = i == (int)_selectedTab ? ModernGuiTheme.TextColor : ModernGuiTheme.MutedTextColor;
            }

            RefreshImmediate();
        }

        private void RefreshImmediate()
        {
            if (!_built || logic == null)
            {
                return;
            }

            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null)
            {
                _caseTitleText.text = "Aktif vaka bekleniyor";
                _statsText.text = "Delil, sure ve not bilgisi daha sonra burada dolacak.";
                _reasoningText.text = "Ilk delili toplayinca dosya analizi otomatik guncellenecek.";
                return;
            }

            _caseTitleText.text = session.ActiveCase.CaseTitle;
            _statsText.text =
                $"Sure: {FormatTime(session.ElapsedCaseTimeSeconds)}\n" +
                $"Delil: {session.CollectedEvidenceIds.Count}/{session.ActiveCase.EvidenceItems.Count}\n" +
                $"Kritik: {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}\n" +
                $"Takim notu: {session.TeamNotes.Count}";
            _reasoningText.text = session.GetReasoningSummary();
            RefreshTabLabels(session);

            if (_noteInput != null)
            {
                _syncingNoteField = true;
                if (_noteInput.text.Length > 140)
                {
                    _noteInput.text = _noteInput.text.Substring(0, 140);
                }
                _syncingNoteField = false;
                _noteCounterText.text = $"{_noteInput.text.Length}/140";
            }

            switch (_selectedTab)
            {
                case NotebookTab.Overview:
                    RebuildOverview(session);
                    break;
                case NotebookTab.Evidence:
                    RebuildEvidence(session);
                    break;
                case NotebookTab.Interviews:
                    RebuildInterviews(session);
                    break;
                case NotebookTab.Notes:
                    RebuildNotes(session);
                    break;
                case NotebookTab.Suspects:
                    RebuildSuspects(session);
                    break;
            }
        }
        private void RebuildOverview(CaseSessionManager session)
        {
            RuntimeUiFactory.ClearChildren(_overviewContent);
            AddSectionCard(_overviewContent, "Brifing", session.ActiveCase.OpeningBrief);
            AddSectionCard(_overviewContent, "Siradaki Mantikli Hamle", session.GetRecommendedNextStep());
            AddSectionCard(_overviewContent, "Dosya Analizi", session.GetReasoningSummary());

            var exploration = Object.FindFirstObjectByType<SchoolExplorationTracker>();
            var explorationText = exploration == null || exploration.VisitedZoneCount == 0
                ? "Henuz bolge kaydi yok."
                : string.Join("  |  ", exploration.VisitedZones);
            AddSectionCard(_overviewContent, "Gezilen Bolgeler", explorationText);

            var bootstrap = Object.FindFirstObjectByType<RelayNetworkBootstrap>();
            if (bootstrap != null)
            {
                var onlineText = bootstrap.CurrentStatus;
                if (!string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode))
                {
                    onlineText += "\nJoin code: " + bootstrap.CurrentJoinCode;
                }

                var networkCaseState = NetworkCaseState.Instance;
                if (networkCaseState != null && bootstrap.IsOnlineSessionActive)
                {
                    onlineText += $"\nHazir oyuncu: {networkCaseState.ReadyPlayerCount}/{networkCaseState.RegisteredPlayerCount}";
                }

                AddSectionCard(_overviewContent, "Co-op Durumu", onlineText);
            }

            var updates = RuntimeUiFactory.CreateCard("UpdatesCard", _overviewContent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
            RuntimeUiFactory.AddVerticalLayout(updates, 8f, new RectOffset(16, 16, 16, 14), false);
            RuntimeUiFactory.CreateText("UpdatesLabel", updates, "Son Gelismeler", 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);

            if (session.MessageHistory.Count == 0)
            {
                RuntimeUiFactory.CreateText("UpdatesEmpty", updates, "Henuz oturum kaydi yok.", 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
                return;
            }

            for (var i = session.MessageHistory.Count - 1; i >= 0; i--)
            {
                RuntimeUiFactory.CreateText("Update" + i, updates, "- " + session.MessageHistory[i], 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
            }
        }

        private void RebuildEvidence(CaseSessionManager session)
        {
            RuntimeUiFactory.ClearChildren(_evidenceListContent);
            if (string.IsNullOrWhiteSpace(_selectedEvidenceId) || !session.HasEvidence(_selectedEvidenceId))
            {
                _selectedEvidenceId = null;
            }

            foreach (var evidence in session.ActiveCase.EvidenceItems)
            {
                if (evidence == null || !session.HasEvidence(evidence.Id))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(_selectedEvidenceId))
                {
                    _selectedEvidenceId = evidence.Id;
                }

                var color = evidence.Id == _selectedEvidenceId
                    ? new Color(0.26f, 0.21f, 0.1f, 1f)
                    : new Color(0.1f, 0.12f, 0.15f, 1f);
                var button = RuntimeUiFactory.CreateButton("EvidenceButton" + evidence.Id, _evidenceListContent, evidence.Title, color, 15);
                RuntimeUiFactory.EnsureLayoutElement(button.transform, preferredHeight: 46f);
                var capturedId = evidence.Id;
                button.onClick.AddListener(() =>
                {
                    _selectedEvidenceId = capturedId;
                    RefreshImmediate();
                });
            }

            if (string.IsNullOrWhiteSpace(_selectedEvidenceId))
            {
                _evidenceDetailsText.text = "Henuz kilidi acilmis delil yok. Sahneyi tara ve ilk fiziksel izi topla.";
                return;
            }

            var selectedEvidence = session.GetEvidence(_selectedEvidenceId);
            _evidenceDetailsText.text = selectedEvidence == null
                ? "Secili delil bilgisi yuklenemedi."
                : selectedEvidence.Title + "\n\n" + selectedEvidence.Description + "\n\nKategori: " + selectedEvidence.Category;
        }

        private void RebuildInterviews(CaseSessionManager session)
        {
            RuntimeUiFactory.ClearChildren(_interviewContent);
            if (session.ConversationHistory.Count == 0)
            {
                AddSectionCard(_interviewContent, "NPC Sorgulari", "Henuz sorgu kaydi yok. Delil topladikca yeni diyaloglar acilacak.");
                return;
            }

            for (var i = session.ConversationHistory.Count - 1; i >= 0; i--)
            {
                AddSectionCard(_interviewContent, "Kayit " + (session.ConversationHistory.Count - i), session.ConversationHistory[i]);
            }
        }

        private void RebuildNotes(CaseSessionManager session)
        {
            RuntimeUiFactory.ClearChildren(_notesListContent);
            if (session.TeamNotes.Count == 0)
            {
                AddSectionCard(_notesListContent, "Takim Notlari", "Henuz takim notu yok.");
                return;
            }

            for (var i = session.TeamNotes.Count - 1; i >= 0; i--)
            {
                AddSectionCard(_notesListContent, "Not " + (session.TeamNotes.Count - i), session.TeamNotes[i]);
            }
        }

        private void RebuildSuspects(CaseSessionManager session)
        {
            RuntimeUiFactory.ClearChildren(_suspectsContent);
            foreach (var suspect in session.ActiveCase.Suspects)
            {
                if (suspect == null)
                {
                    continue;
                }

                var card = RuntimeUiFactory.CreateCard("SuspectCard" + suspect.Id, _suspectsContent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentWarmColor);
                RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 16, 14), false);
                RuntimeUiFactory.CreateText("Name", card, suspect.DisplayName, 18, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
                RuntimeUiFactory.CreateText("Summary", card, suspect.Summary, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);

                var matchCount = session.GetSuspectEvidenceMatchCount(suspect);
                var confidence = session.GetSuspectConfidencePercent(suspect.Id);
                RuntimeUiFactory.CreateText("EvidenceMatch", card, $"Eslesen delil: {matchCount}/{suspect.RequiredEvidenceIds.Count}", 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
                RuntimeUiFactory.CreateText("Confidence", card, $"Suphe yogunlugu: %{confidence}", 14, ModernGuiTheme.MutedTextColor, FontStyle.Normal, TextAnchor.UpperLeft);
                CreateProgressBar(card, confidence / 100f, $"Guven %{confidence}");
                RuntimeUiFactory.CreateText("Missing", card, session.GetMissingEvidenceSummary(suspect), 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);

                var accuseButton = RuntimeUiFactory.CreateButton(
                    "AccuseButton" + suspect.Id,
                    card,
                    session.CanAccuse(suspect.Id) && !session.IsCaseResolved ? "Bu supheliyi sucla" : "Daha fazla delil gerekiyor",
                    session.CanAccuse(suspect.Id) && !session.IsCaseResolved ? new Color(0.24f, 0.18f, 0.08f, 1f) : new Color(0.11f, 0.13f, 0.15f, 1f),
                    15);
                RuntimeUiFactory.EnsureLayoutElement(accuseButton.transform, preferredHeight: 46f);
                accuseButton.interactable = session.CanAccuse(suspect.Id) && !session.IsCaseResolved;
                var capturedSuspectId = suspect.Id;
                accuseButton.onClick.AddListener(() => TryAccuse(capturedSuspectId));
            }
        }

        private void AddSectionCard(Transform parent, string title, string body)
        {
            var card = RuntimeUiFactory.CreateCard(title + "Card", parent, ModernGuiTheme.PanelSoftColor, ModernGuiTheme.AccentColor);
            RuntimeUiFactory.AddVerticalLayout(card, 8f, new RectOffset(16, 16, 16, 14), false);
            RuntimeUiFactory.CreateText(title + "Title", card, title, 16, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.UpperLeft);
            RuntimeUiFactory.CreateText(title + "Body", card, body, 14, ModernGuiTheme.TextColor, FontStyle.Normal, TextAnchor.UpperLeft);
        }

        private void CreateProgressBar(Transform parent, float normalized, string label)
        {
            var shell = RuntimeUiFactory.CreateUiRoot("ProgressShell", parent);
            RuntimeUiFactory.EnsureLayoutElement(shell, preferredHeight: 24f);
            var background = RuntimeUiFactory.AddImage(shell.gameObject, new Color(0.08f, 0.1f, 0.12f, 1f));
            background.raycastTarget = false;
            RuntimeUiFactory.AddOutline(shell.gameObject, new Color(0f, 0f, 0f, 0.4f), new Vector2(1f, -1f));

            var fill = RuntimeUiFactory.CreateUiRoot("Fill", shell);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
            RuntimeUiFactory.AddImage(fill.gameObject, new Color(0.18f, 0.7f, 0.44f, 1f)).raycastTarget = false;

            var labelText = RuntimeUiFactory.CreateText("Label", shell, label, 13, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.raycastTarget = false;
        }
        private void HandleNoteChanged(string value)
        {
            if (_syncingNoteField)
            {
                return;
            }

            if (value.Length > 140)
            {
                _syncingNoteField = true;
                _noteInput.text = value.Substring(0, 140);
                _syncingNoteField = false;
            }

            _noteCounterText.text = $"{_noteInput.text.Length}/140";
        }

        private void SubmitTeamNote()
        {
            var trimmedNote = _noteInput.text.Trim();
            if (string.IsNullOrWhiteSpace(trimmedNote))
            {
                return;
            }

            var networkCaseState = NetworkCaseState.Instance;
            var session = CaseSessionManager.Instance;
            var added = false;

            if (networkCaseState != null && networkCaseState.IsOnlineSessionActive)
            {
                added = networkCaseState.RequestAddTeamNote(networkCaseState.GetLocalPlayerLabel(), trimmedNote);
            }
            else if (session != null)
            {
                added = session.AddTeamNote("Sen", trimmedNote);
            }

            if (!added)
            {
                return;
            }

            _syncingNoteField = true;
            _noteInput.text = string.Empty;
            _syncingNoteField = false;
            _noteCounterText.text = "0/140";
            RefreshImmediate();
        }

        private void ClearNoteDraft()
        {
            _syncingNoteField = true;
            _noteInput.text = string.Empty;
            _syncingNoteField = false;
            _noteCounterText.text = "0/140";
        }

        private void TryAccuse(string suspectId)
        {
            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState != null && networkCaseState.IsOnlineSessionActive)
            {
                networkCaseState.RequestResolveSuspect(suspectId);
            }
            else if (CaseSessionManager.Instance != null)
            {
                CaseSessionManager.Instance.TryResolveCase(suspectId, out _);
            }

            RefreshImmediate();
        }

        private void RefreshTabLabels(CaseSessionManager session)
        {
            if (_tabButtonTexts == null || _tabButtonTexts.Length < 5)
            {
                return;
            }

            _tabButtonTexts[0].text = _tabBaseLabels[0];
            _tabButtonTexts[1].text = $"{_tabBaseLabels[1]} ({session.CollectedEvidenceIds.Count})";
            _tabButtonTexts[2].text = $"{_tabBaseLabels[2]} ({session.ConversationHistory.Count})";
            _tabButtonTexts[3].text = $"{_tabBaseLabels[3]} ({session.TeamNotes.Count})";
            _tabButtonTexts[4].text = _tabBaseLabels[4];
        }

        private void AnimateNotebook(bool visible)
        {
            if (_overlayGroup == null || _panelRoot == null)
            {
                return;
            }

            var target = visible ? 1f : 0f;
            _openBlend = Mathf.MoveTowards(_openBlend, target, Time.unscaledDeltaTime * 6f);
            _overlayGroup.alpha = _openBlend;
            _overlayGroup.interactable = _openBlend > 0.98f;
            _overlayGroup.blocksRaycasts = _openBlend > 0.02f;
            _panelRoot.localScale = Vector3.Lerp(new Vector3(0.95f, 0.98f, 1f), Vector3.one, _openBlend);
            _panelRoot.anchoredPosition = Vector2.Lerp(new Vector2(0f, 18f), Vector2.zero, _openBlend);
        }

        private static string FormatTime(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            var minutes = totalSeconds / 60;
            var remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}





