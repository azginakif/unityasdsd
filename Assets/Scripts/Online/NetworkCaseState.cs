using System.Collections.Generic;
using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MobilOfl.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkCaseState : NetworkBehaviour
    {
        public enum SessionPhase
        {
            Lobby = 0,
            Investigation = 1,
            Results = 2
        }

        public readonly struct ReadyPlayerState
        {
            public ReadyPlayerState(ulong clientId, bool isReady, string displayName)
            {
                ClientId = clientId;
                IsReady = isReady;
                DisplayName = displayName;
            }

            public ulong ClientId { get; }
            public bool IsReady { get; }
            public string DisplayName { get; }
        }

        public static NetworkCaseState Instance { get; private set; }

        [SerializeField] private CaseDefinition caseDefinition;

        private readonly NetworkList<FixedString64Bytes> _collectedEvidenceIds = new NetworkList<FixedString64Bytes>();
        private readonly NetworkList<FixedString128Bytes> _toolEntries = new NetworkList<FixedString128Bytes>();
        private readonly NetworkList<FixedString512Bytes> _conversationEntries = new NetworkList<FixedString512Bytes>();
        private readonly NetworkList<FixedString512Bytes> _teamNoteEntries = new NetworkList<FixedString512Bytes>();
        private readonly NetworkList<FixedString128Bytes> _readyEntries = new NetworkList<FixedString128Bytes>();
        private readonly NetworkVariable<FixedString64Bytes> _relayJoinCode = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<bool> _caseResolved = new NetworkVariable<bool>();
        private readonly NetworkVariable<FixedString512Bytes> _resolutionMessage = new NetworkVariable<FixedString512Bytes>();
        private readonly NetworkVariable<int> _sessionPhase = new NetworkVariable<int>((int)SessionPhase.Lobby);
        private readonly NetworkVariable<int> _sessionRevision = new NetworkVariable<int>(0);
        private readonly NetworkVariable<Vector3> _sharedPingPosition = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<FixedString64Bytes> _sharedPingLabel = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<int> _sharedPingRevision = new NetworkVariable<int>(0);

        private float _localPingVisibleUntil;

        public string RelayJoinCode => _relayJoinCode.Value.ToString();
        public bool IsOnlineSessionActive => NetworkManager != null && NetworkManager.IsListening;
        public SessionPhase CurrentPhase => (SessionPhase)_sessionPhase.Value;
        public bool IsLobbyPhase => CurrentPhase == SessionPhase.Lobby;
        public bool IsGameplayPhase => CurrentPhase == SessionPhase.Investigation;
        public bool IsResultsPhase => CurrentPhase == SessionPhase.Results;
        public bool CanHostStartInvestigation => IsLobbyPhase && IsServer && (AreAllRegisteredPlayersReady || IsLocalOnlyHostSession);
        public bool HasActiveSharedPing => _sharedPingRevision.Value > 0 && Time.time <= _localPingVisibleUntil && !string.IsNullOrWhiteSpace(_sharedPingLabel.Value.ToString());
        public Vector3 SharedPingPosition => _sharedPingPosition.Value;
        public string SharedPingLabel => _sharedPingLabel.Value.ToString();
        public string CurrentPhaseLabel =>
            CurrentPhase == SessionPhase.Lobby ? "Lobi" :
            CurrentPhase == SessionPhase.Investigation ? "Operasyon" :
            "Sonuc";
        public int RegisteredPlayerCount => _readyEntries.Count;
        public int ReadyPlayerCount
        {
            get
            {
                var readyCount = 0;
                foreach (var entry in _readyEntries)
                {
                if (TryParseReadyPayload(entry.ToString(), out _, out var isReady, out _) && isReady)
                    {
                        readyCount++;
                    }
                }

                return readyCount;
            }
        }

        public bool AreAllRegisteredPlayersReady => RegisteredPlayerCount > 0 && ReadyPlayerCount == RegisteredPlayerCount;
        private bool IsLocalOnlyHostSession => IsOnlineSessionActive && NetworkManager != null && NetworkManager.IsHost && NetworkManager.ConnectedClientsIds.Count <= 1;

        private void Awake()
        {
            Instance = this;
            ResolveCaseDefinition();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ResolveCaseDefinition();

            if (CaseSessionManager.Instance != null && caseDefinition != null && CaseSessionManager.Instance.ActiveCase != caseDefinition)
            {
                CaseSessionManager.Instance.SetActiveCase(caseDefinition);
            }

            _collectedEvidenceIds.OnListChanged += HandleEvidenceListChanged;
            _toolEntries.OnListChanged += HandleToolListChanged;
            _conversationEntries.OnListChanged += HandleConversationListChanged;
            _teamNoteEntries.OnListChanged += HandleTeamNoteListChanged;
            _readyEntries.OnListChanged += HandleReadyEntriesChanged;
            _caseResolved.OnValueChanged += HandleCaseResolvedChanged;
            _sessionPhase.OnValueChanged += HandleSessionPhaseChanged;
            _sessionRevision.OnValueChanged += HandleSessionRevisionChanged;
            _sharedPingRevision.OnValueChanged += HandleSharedPingRevisionChanged;

            if (IsServer)
            {
                SubscribeToSession();
                SubscribeToClientLifecycle();
                BootstrapServerState();
            }
            else
            {
                ApplyFullStateFromNetwork();
            }

            TryRegisterLocalReadyState(false);
        }

        private void ResolveCaseDefinition()
        {
            if (caseDefinition == null && CaseSessionManager.Instance != null)
            {
                caseDefinition = CaseSessionManager.Instance.ActiveCase;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _collectedEvidenceIds.OnListChanged -= HandleEvidenceListChanged;
            _toolEntries.OnListChanged -= HandleToolListChanged;
            _conversationEntries.OnListChanged -= HandleConversationListChanged;
            _teamNoteEntries.OnListChanged -= HandleTeamNoteListChanged;
            _readyEntries.OnListChanged -= HandleReadyEntriesChanged;
            _caseResolved.OnValueChanged -= HandleCaseResolvedChanged;
            _sessionPhase.OnValueChanged -= HandleSessionPhaseChanged;
            _sessionRevision.OnValueChanged -= HandleSessionRevisionChanged;
            _sharedPingRevision.OnValueChanged -= HandleSharedPingRevisionChanged;

            if (IsServer)
            {
                UnsubscribeFromSession();
                UnsubscribeFromClientLifecycle();
            }
        }

        public bool RequestCollectEvidence(string evidenceId)
        {
            if (string.IsNullOrWhiteSpace(evidenceId) || CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (IsServer)
            {
                return CaseSessionManager.Instance.TryCollectEvidence(caseDefinition, evidenceId);
            }

            RequestCollectEvidenceServerRpc(evidenceId);
            return true;
        }

        public bool RequestUnlockTool(string toolId, string displayName, string pickupMessage)
        {
            if (CaseSessionManager.Instance == null || string.IsNullOrWhiteSpace(toolId))
            {
                return false;
            }

            if (IsServer)
            {
                return CaseSessionManager.Instance.TryUnlockTool(toolId, displayName, pickupMessage);
            }

            RequestUnlockToolServerRpc(toolId, displayName ?? string.Empty, pickupMessage ?? string.Empty);
            return true;
        }

        public bool RequestNpcInteraction(
            string npcId,
            string npcDisplayName,
            string defaultLine,
            string evidenceLine,
            string requiredEvidenceId,
            string witnessEvidenceId,
            bool collectWitnessEvidenceOnce)
        {
            if (CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (IsServer)
            {
                return ExecuteNpcInteractionServerLogic(
                    npcId,
                    npcDisplayName,
                    defaultLine,
                    evidenceLine,
                    requiredEvidenceId,
                    witnessEvidenceId,
                    collectWitnessEvidenceOnce);
            }

            RequestNpcInteractionServerRpc(
                npcId ?? string.Empty,
                npcDisplayName ?? string.Empty,
                defaultLine ?? string.Empty,
                evidenceLine ?? string.Empty,
                requiredEvidenceId ?? string.Empty,
                witnessEvidenceId ?? string.Empty,
                collectWitnessEvidenceOnce);
            return true;
        }

        [System.Obsolete("Use RequestResolveSuspect(string suspectId, string motive, string timeline) for player-facing accusations.")]
        public bool RequestResolveSuspect(string suspectId)
        {
            if (CaseSessionManager.Instance == null || string.IsNullOrWhiteSpace(suspectId))
            {
                return false;
            }

            if (IsServer)
            {
                return CaseSessionManager.Instance.TryResolveCase(suspectId, out _);
            }

            RequestResolveSuspectServerRpc(suspectId);
            return true;
        }

        public bool RequestResolveSuspect(string suspectId, string motive, string timeline)
        {
            if (CaseSessionManager.Instance == null || string.IsNullOrWhiteSpace(suspectId))
            {
                return false;
            }

            if (IsServer)
            {
                return CaseSessionManager.Instance.TryResolveCase(suspectId, motive, timeline, out _);
            }

            RequestResolveFinalAccusationServerRpc(suspectId, motive ?? string.Empty, timeline ?? string.Empty);
            return true;
        }

        public bool RequestAddTeamNote(string authorName, string noteText)
        {
            if (CaseSessionManager.Instance == null || string.IsNullOrWhiteSpace(noteText))
            {
                return false;
            }

            if (IsServer)
            {
                return CaseSessionManager.Instance.AddTeamNote(authorName, noteText);
            }

            RequestAddTeamNoteServerRpc(authorName ?? string.Empty, noteText ?? string.Empty);
            return true;
        }

        public bool RequestStartInvestigation()
        {
            if (!IsOnlineSessionActive)
            {
                return false;
            }

            if (IsServer)
            {
                return StartInvestigationServerLogic();
            }

            RequestStartInvestigationServerRpc();
            return true;
        }

        public bool RequestStartImmediateInvestigationForHost(string message = "Operasyon basladi.")
        {
            if (!IsOnlineSessionActive || !IsServer)
            {
                return false;
            }

            if (IsGameplayPhase)
            {
                return true;
            }

            if (!IsLobbyPhase)
            {
                return false;
            }

            _sessionPhase.Value = (int)SessionPhase.Investigation;
            CaseSessionManager.Instance?.PublishMessage(message);
            return true;
        }

        public bool RequestRestartSession()
        {
            if (CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (!IsOnlineSessionActive)
            {
                CaseSessionManager.Instance.RestartCurrentCase();
                return true;
            }

            if (IsServer)
            {
                RestartSessionServerLogic();
                return true;
            }

            RequestRestartSessionServerRpc();
            return true;
        }

        public bool RequestSharedPing(Vector3 worldPosition, string label)
        {
            if (!IsOnlineSessionActive)
            {
                return false;
            }

            if (IsServer)
            {
                SetSharedPing(worldPosition, label);
                return true;
            }

            RequestSharedPingServerRpc(worldPosition, label ?? string.Empty);
            return true;
        }

        public bool RequestSetReady(bool isReady)
        {
            if (!IsOnlineSessionActive || NetworkManager == null)
            {
                return false;
            }

            if (IsServer)
            {
                UpsertReadyEntry(NetworkManager.LocalClientId, isReady, PlayerProfileSettings.LoadPlayerName());
                return true;
            }

            RequestSetReadyServerRpc(isReady, PlayerProfileSettings.LoadPlayerName());
            return true;
        }

        public bool GetLocalReadyState()
        {
            return NetworkManager != null && TryGetReadyState(NetworkManager.LocalClientId, out var isReady) && isReady;
        }

        public string GetLocalPlayerLabel()
        {
            return PlayerProfileSettings.LoadPlayerName();
        }

        public List<ReadyPlayerState> GetReadyRoster()
        {
            var roster = new List<ReadyPlayerState>(_readyEntries.Count);
            foreach (var entry in _readyEntries)
            {
                if (TryParseReadyPayload(entry.ToString(), out var clientId, out var isReady, out var playerName))
                {
                    roster.Add(new ReadyPlayerState(clientId, isReady, BuildPlayerLabel(clientId, playerName)));
                }
            }

            return roster;
        }

        public void SetRelayJoinCode(string joinCode)
        {
            if (!IsServer)
            {
                return;
            }

            _relayJoinCode.Value = new FixedString64Bytes(joinCode);
        }

        private void BootstrapServerState()
        {
            _collectedEvidenceIds.Clear();
            _toolEntries.Clear();
            _conversationEntries.Clear();
            _teamNoteEntries.Clear();
            _caseResolved.Value = false;
            _resolutionMessage.Value = default;
            _sessionPhase.Value = (int)SessionPhase.Lobby;
            _sharedPingLabel.Value = default;
            _sharedPingPosition.Value = Vector3.zero;
            _sharedPingRevision.Value = 0;

            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            foreach (var evidenceId in CaseSessionManager.Instance.CollectedEvidenceIds)
            {
                _collectedEvidenceIds.Add(new FixedString64Bytes(evidenceId));
            }

            foreach (var toolId in CaseSessionManager.Instance.UnlockedToolIds)
            {
                _toolEntries.Add(new FixedString128Bytes(BuildToolPayload(toolId, CaseSessionManager.Instance.GetToolDisplayName(toolId))));
            }

            foreach (var noteEntry in CaseSessionManager.Instance.TeamNotes)
            {
                if (!TryParseTeamNoteEntry(noteEntry, out var authorName, out var noteText))
                {
                    continue;
                }

                _teamNoteEntries.Add(new FixedString512Bytes(BuildTeamNotePayload(authorName, noteText)));
            }
        }

        private void SubscribeToSession()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            UnsubscribeFromSession();
            CaseSessionManager.Instance.EvidenceCollected += HandleLocalEvidenceCollected;
            CaseSessionManager.Instance.ToolUnlocked += HandleLocalToolUnlocked;
            CaseSessionManager.Instance.CaseResolved += HandleLocalCaseResolved;
            CaseSessionManager.Instance.NpcConversationRegistered += HandleLocalNpcConversation;
            CaseSessionManager.Instance.TeamNoteAdded += HandleLocalTeamNoteAdded;
        }

        private void UnsubscribeFromSession()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.EvidenceCollected -= HandleLocalEvidenceCollected;
            CaseSessionManager.Instance.ToolUnlocked -= HandleLocalToolUnlocked;
            CaseSessionManager.Instance.CaseResolved -= HandleLocalCaseResolved;
            CaseSessionManager.Instance.NpcConversationRegistered -= HandleLocalNpcConversation;
            CaseSessionManager.Instance.TeamNoteAdded -= HandleLocalTeamNoteAdded;
        }

        private void SubscribeToClientLifecycle()
        {
            if (NetworkManager == null)
            {
                return;
            }

            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void UnsubscribeFromClientLifecycle()
        {
            if (NetworkManager == null)
            {
                return;
            }

            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        private void HandleLocalEvidenceCollected(EvidenceData evidence)
        {
            if (!IsServer || evidence == null || string.IsNullOrWhiteSpace(evidence.Id))
            {
                return;
            }

            foreach (var existing in _collectedEvidenceIds)
            {
                if (existing.ToString() == evidence.Id)
                {
                    return;
                }
            }

            _collectedEvidenceIds.Add(new FixedString64Bytes(evidence.Id));
        }

        private void HandleLocalToolUnlocked(string toolId, string displayName)
        {
            if (!IsServer || string.IsNullOrWhiteSpace(toolId))
            {
                return;
            }

            for (var i = 0; i < _toolEntries.Count; i++)
            {
                if (!TryParseToolPayload(_toolEntries[i].ToString(), out var existingToolId, out _))
                {
                    continue;
                }

                if (existingToolId == toolId)
                {
                    return;
                }
            }

            _toolEntries.Add(new FixedString128Bytes(BuildToolPayload(toolId, displayName)));
        }

        private void HandleLocalCaseResolved(bool success, string message)
        {
            if (!IsServer)
            {
                return;
            }

            _caseResolved.Value = success;
            _resolutionMessage.Value = new FixedString512Bytes(message);
            if (success)
            {
                _sessionPhase.Value = (int)SessionPhase.Results;
            }
        }

        private void HandleLocalNpcConversation(string npcId, string npcDisplayName, string line, bool _)
        {
            if (!IsServer)
            {
                return;
            }

            _conversationEntries.Add(new FixedString512Bytes(BuildConversationPayload(npcId, npcDisplayName, line)));
        }

        private void HandleLocalTeamNoteAdded(string authorName, string noteText)
        {
            if (!IsServer)
            {
                return;
            }

            _teamNoteEntries.Add(new FixedString512Bytes(BuildTeamNotePayload(authorName, noteText)));
        }

        private void HandleEvidenceListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            if (IsServer || CaseSessionManager.Instance == null)
            {
                return;
            }

            if (changeEvent.Type == NetworkListEvent<FixedString64Bytes>.EventType.Add)
            {
                CaseSessionManager.Instance.TryApplyNetworkEvidence(changeEvent.Value.ToString(), true);
            }
        }

        private void HandleToolListChanged(NetworkListEvent<FixedString128Bytes> changeEvent)
        {
            if (IsServer || CaseSessionManager.Instance == null)
            {
                return;
            }

            if (changeEvent.Type != NetworkListEvent<FixedString128Bytes>.EventType.Add)
            {
                return;
            }

            if (!TryParseToolPayload(changeEvent.Value.ToString(), out var toolId, out var displayName))
            {
                return;
            }

            CaseSessionManager.Instance.TryApplyNetworkTool(toolId, displayName, true);
        }

        private void HandleConversationListChanged(NetworkListEvent<FixedString512Bytes> changeEvent)
        {
            if (IsServer || CaseSessionManager.Instance == null)
            {
                return;
            }

            if (changeEvent.Type != NetworkListEvent<FixedString512Bytes>.EventType.Add)
            {
                return;
            }

            if (!TryParseConversationPayload(changeEvent.Value.ToString(), out var npcId, out var npcDisplayName, out var line))
            {
                return;
            }

            CaseSessionManager.Instance.ApplyNetworkConversation(npcId, npcDisplayName, line);
        }

        private void HandleTeamNoteListChanged(NetworkListEvent<FixedString512Bytes> changeEvent)
        {
            if (IsServer || CaseSessionManager.Instance == null)
            {
                return;
            }

            if (changeEvent.Type != NetworkListEvent<FixedString512Bytes>.EventType.Add)
            {
                return;
            }

            if (!TryParseTeamNotePayload(changeEvent.Value.ToString(), out var authorName, out var noteText))
            {
                return;
            }

            CaseSessionManager.Instance.ApplyNetworkTeamNote(authorName, noteText);
        }

        private void HandleReadyEntriesChanged(NetworkListEvent<FixedString128Bytes> changeEvent)
        {
            if (!IsClient || NetworkManager == null)
            {
                return;
            }

            if (!TryGetReadyState(NetworkManager.LocalClientId, out _))
            {
                TryRegisterLocalReadyState(false);
            }
        }

        private void HandleCaseResolvedChanged(bool previousValue, bool newValue)
        {
            if (IsServer || !newValue || CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.ApplyNetworkResolution(true, _resolutionMessage.Value.ToString());
        }

        private void HandleSessionPhaseChanged(int previousValue, int newValue)
        {
            if (previousValue == newValue)
            {
                return;
            }

            var menu = MainMenuHud.Instance;
            if (menu == null)
            {
                return;
            }

            if ((SessionPhase)newValue == SessionPhase.Investigation)
            {
                menu.CloseMenu();
                return;
            }

            menu.OpenMenu((SessionPhase)newValue == SessionPhase.Results
                ? "Operasyon tamamlandi."
                : "Lobi hazir. Tum ekip senkron bekliyor.");
        }

        private void HandleSessionRevisionChanged(int previousValue, int newValue)
        {
            if (IsServer || previousValue == newValue || CaseSessionManager.Instance == null || caseDefinition == null)
            {
                return;
            }

            CaseSessionManager.Instance.SetActiveCase(caseDefinition);
            ApplyFullStateFromNetwork();
        }

        private void HandleSharedPingRevisionChanged(int previousValue, int newValue)
        {
            if (previousValue == newValue || string.IsNullOrWhiteSpace(_sharedPingLabel.Value.ToString()))
            {
                return;
            }

            _localPingVisibleUntil = Time.time + 5f;
            if (CaseSessionManager.Instance != null)
            {
                CaseSessionManager.Instance.PublishMessage("Takim pingi: " + _sharedPingLabel.Value);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            RemoveReadyEntry(clientId);
        }

        private void ApplyFullStateFromNetwork()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            foreach (var evidenceId in _collectedEvidenceIds)
            {
                CaseSessionManager.Instance.TryApplyNetworkEvidence(evidenceId.ToString(), false);
            }

            foreach (var toolEntry in _toolEntries)
            {
                if (TryParseToolPayload(toolEntry.ToString(), out var toolId, out var displayName))
                {
                    CaseSessionManager.Instance.TryApplyNetworkTool(toolId, displayName, false);
                }
            }

            foreach (var conversationEntry in _conversationEntries)
            {
                if (TryParseConversationPayload(conversationEntry.ToString(), out var npcId, out var npcDisplayName, out var line))
                {
                    CaseSessionManager.Instance.ApplyNetworkConversation(npcId, npcDisplayName, line);
                }
            }

            foreach (var noteEntry in _teamNoteEntries)
            {
                if (TryParseTeamNotePayload(noteEntry.ToString(), out var authorName, out var noteText))
                {
                    CaseSessionManager.Instance.ApplyNetworkTeamNote(authorName, noteText);
                }
            }

            if (_caseResolved.Value)
            {
                CaseSessionManager.Instance.ApplyNetworkResolution(true, _resolutionMessage.Value.ToString());
            }
        }

        private void TryRegisterLocalReadyState(bool isReady)
        {
            if (!IsOnlineSessionActive || NetworkManager == null)
            {
                return;
            }

            if (IsServer)
            {
                UpsertReadyEntry(NetworkManager.LocalClientId, isReady, PlayerProfileSettings.LoadPlayerName());
                return;
            }

            RequestSetReadyServerRpc(isReady, PlayerProfileSettings.LoadPlayerName());
        }

        private bool StartInvestigationServerLogic()
        {
            if (CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (!IsLobbyPhase)
            {
                CaseSessionManager.Instance.PublishMessage("Operasyon zaten aktif.");
                return false;
            }

            if (!CanHostStartInvestigation)
            {
                CaseSessionManager.Instance.PublishMessage("Tum oyuncular hazir olmadan operasyon baslatilamaz.");
                return false;
            }

            _sessionPhase.Value = (int)SessionPhase.Investigation;
            CaseSessionManager.Instance.PublishMessage("Tum ekip hazir. Operasyon ayni anda basladi.");
            return true;
        }

        private void RestartSessionServerLogic()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.RestartCurrentCase();
            BootstrapServerState();
            ResetReadyEntriesToWaiting();
            _sessionRevision.Value++;
            CaseSessionManager.Instance.PublishMessage("Vaka sifirlandi. Herkes yeniden hazirlik vermeli.");
        }

        private void SetSharedPing(Vector3 worldPosition, string label)
        {
            _sharedPingPosition.Value = worldPosition;
            _sharedPingLabel.Value = new FixedString64Bytes(PlayerProfileSettings.Sanitize(string.IsNullOrWhiteSpace(label) ? "Takim pingi" : label));
            _sharedPingRevision.Value++;
            _localPingVisibleUntil = Time.time + 5f;

            if (CaseSessionManager.Instance != null)
            {
                CaseSessionManager.Instance.PublishMessage("Takim pingi: " + _sharedPingLabel.Value);
            }
        }

        private bool TryGetReadyState(ulong clientId, out bool isReady)
        {
            foreach (var entry in _readyEntries)
            {
                if (!TryParseReadyPayload(entry.ToString(), out var entryClientId, out var entryIsReady, out _))
                {
                    continue;
                }

                if (entryClientId != clientId)
                {
                    continue;
                }

                isReady = entryIsReady;
                return true;
            }

            isReady = false;
            return false;
        }

        private void UpsertReadyEntry(ulong clientId, bool isReady, string playerName)
        {
            var payload = new FixedString128Bytes(BuildReadyPayload(clientId, isReady, playerName));
            for (var i = 0; i < _readyEntries.Count; i++)
            {
                if (!TryParseReadyPayload(_readyEntries[i].ToString(), out var existingClientId, out _, out _))
                {
                    continue;
                }

                if (existingClientId != clientId)
                {
                    continue;
                }

                _readyEntries[i] = payload;
                return;
            }

            _readyEntries.Add(payload);
        }

        private void ResetReadyEntriesToWaiting()
        {
            for (var i = 0; i < _readyEntries.Count; i++)
            {
                if (!TryParseReadyPayload(_readyEntries[i].ToString(), out var clientId, out _, out var playerName))
                {
                    continue;
                }

                _readyEntries[i] = new FixedString128Bytes(BuildReadyPayload(clientId, false, playerName));
            }
        }

        private void RemoveReadyEntry(ulong clientId)
        {
            for (var i = _readyEntries.Count - 1; i >= 0; i--)
            {
                if (!TryParseReadyPayload(_readyEntries[i].ToString(), out var existingClientId, out _, out _))
                {
                    continue;
                }

                if (existingClientId == clientId)
                {
                    _readyEntries.RemoveAt(i);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestCollectEvidenceServerRpc(string evidenceId)
        {
            if (CaseSessionManager.Instance == null || caseDefinition == null)
            {
                return;
            }

            CaseSessionManager.Instance.TryCollectEvidence(caseDefinition, evidenceId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestUnlockToolServerRpc(string toolId, string displayName, string pickupMessage)
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.TryUnlockTool(toolId, displayName, pickupMessage);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestNpcInteractionServerRpc(
            string npcId,
            string npcDisplayName,
            string defaultLine,
            string evidenceLine,
            string requiredEvidenceId,
            string witnessEvidenceId,
            bool collectWitnessEvidenceOnce)
        {
            ExecuteNpcInteractionServerLogic(
                npcId,
                npcDisplayName,
                defaultLine,
                evidenceLine,
                requiredEvidenceId,
                witnessEvidenceId,
                collectWitnessEvidenceOnce);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestResolveSuspectServerRpc(string suspectId)
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.TryResolveCase(suspectId, out _);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestResolveFinalAccusationServerRpc(string suspectId, string motive, string timeline)
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.TryResolveCase(suspectId, motive, timeline, out _);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestAddTeamNoteServerRpc(string authorName, string noteText)
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.AddTeamNote(authorName, noteText);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestSetReadyServerRpc(bool isReady, string playerName, RpcParams rpcParams = default)
        {
            UpsertReadyEntry(rpcParams.Receive.SenderClientId, isReady, playerName);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestStartInvestigationServerRpc(RpcParams rpcParams = default)
        {
            if (NetworkManager == null || rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            {
                return;
            }

            StartInvestigationServerLogic();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestRestartSessionServerRpc(RpcParams rpcParams = default)
        {
            if (NetworkManager == null || rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            {
                return;
            }

            RestartSessionServerLogic();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestSharedPingServerRpc(Vector3 worldPosition, string label)
        {
            SetSharedPing(worldPosition, label);
        }

        private bool ExecuteNpcInteractionServerLogic(
            string npcId,
            string npcDisplayName,
            string defaultLine,
            string evidenceLine,
            string requiredEvidenceId,
            string witnessEvidenceId,
            bool collectWitnessEvidenceOnce)
        {
            if (CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (CaseSessionManager.Instance.IsCaseResolved)
            {
                CaseSessionManager.Instance.PublishMessage("Vaka tamamlandi. Artik yeni sorgu yapilamaz.");
                return false;
            }

            var hasRequiredEvidence =
                string.IsNullOrWhiteSpace(requiredEvidenceId) ||
                CaseSessionManager.Instance.HasEvidence(requiredEvidenceId);
            var witnessAlreadyCollected =
                !string.IsNullOrWhiteSpace(witnessEvidenceId) &&
                CaseSessionManager.Instance.HasEvidence(witnessEvidenceId);

            var line = hasRequiredEvidence
                ? evidenceLine
                : defaultLine;
            var revealsNewLead =
                hasRequiredEvidence &&
                !string.IsNullOrWhiteSpace(witnessEvidenceId) &&
                (!collectWitnessEvidenceOnce || !witnessAlreadyCollected);

            CaseSessionManager.Instance.RegisterNpcConversation(npcId, npcDisplayName, line, revealsNewLead);

            if (!hasRequiredEvidence || string.IsNullOrWhiteSpace(witnessEvidenceId))
            {
                return true;
            }

            if (collectWitnessEvidenceOnce && witnessAlreadyCollected)
            {
                return true;
            }

            return caseDefinition != null && CaseSessionManager.Instance.TryCollectEvidence(caseDefinition, witnessEvidenceId);
        }

        private static string BuildConversationPayload(string npcId, string npcDisplayName, string line)
        {
            return $"{npcId}\t{npcDisplayName}\t{line}";
        }

        private static string BuildToolPayload(string toolId, string displayName)
        {
            return $"{toolId}\t{PlayerProfileSettings.Sanitize(displayName)}";
        }

        private static bool TryParseToolPayload(string payload, out string toolId, out string displayName)
        {
            toolId = string.Empty;
            displayName = string.Empty;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var separatorIndex = payload.IndexOf('\t');
            if (separatorIndex < 0)
            {
                toolId = payload;
                return !string.IsNullOrWhiteSpace(toolId);
            }

            toolId = payload.Substring(0, separatorIndex);
            displayName = PlayerProfileSettings.Sanitize(payload.Substring(separatorIndex + 1));
            return !string.IsNullOrWhiteSpace(toolId);
        }

        private static bool TryParseConversationPayload(string payload, out string npcId, out string npcDisplayName, out string line)
        {
            npcId = string.Empty;
            npcDisplayName = string.Empty;
            line = string.Empty;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var parts = payload.Split('\t');
            if (parts.Length < 3)
            {
                return false;
            }

            npcId = parts[0];
            npcDisplayName = parts[1];
            line = parts[2];
            return true;
        }

        private static string BuildTeamNotePayload(string authorName, string noteText)
        {
            return $"{authorName}\t{noteText}";
        }

        private static bool TryParseTeamNotePayload(string payload, out string authorName, out string noteText)
        {
            authorName = string.Empty;
            noteText = string.Empty;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var separatorIndex = payload.IndexOf('\t');
            if (separatorIndex < 0)
            {
                return false;
            }

            authorName = payload.Substring(0, separatorIndex);
            noteText = payload.Substring(separatorIndex + 1);
            return !string.IsNullOrWhiteSpace(noteText);
        }

        private static bool TryParseTeamNoteEntry(string entry, out string authorName, out string noteText)
        {
            authorName = "Takim";
            noteText = string.Empty;

            if (string.IsNullOrWhiteSpace(entry))
            {
                return false;
            }

            var separatorIndex = entry.IndexOf(": ");
            if (separatorIndex < 0)
            {
                noteText = entry;
                return true;
            }

            authorName = entry.Substring(0, separatorIndex);
            noteText = entry.Substring(separatorIndex + 2);
            return !string.IsNullOrWhiteSpace(noteText);
        }

        private static string BuildReadyPayload(ulong clientId, bool isReady, string playerName)
        {
            return $"{clientId}\t{(isReady ? 1 : 0)}\t{PlayerProfileSettings.Sanitize(playerName)}";
        }

        private static bool TryParseReadyPayload(string payload, out ulong clientId, out bool isReady, out string playerName)
        {
            clientId = 0UL;
            isReady = false;
            playerName = string.Empty;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var parts = payload.Split('\t');
            if (parts.Length < 2 || !ulong.TryParse(parts[0], out clientId))
            {
                return false;
            }

            isReady = parts[1] == "1";
            playerName = parts.Length >= 3 ? PlayerProfileSettings.Sanitize(parts[2]) : string.Empty;
            return true;
        }

        private static string BuildPlayerLabel(ulong clientId, string playerName)
        {
            return string.IsNullOrWhiteSpace(playerName) ? $"Dedektif {clientId + 1}" : PlayerProfileSettings.Sanitize(playerName);
        }
    }
}
