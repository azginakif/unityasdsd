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
        private readonly NetworkList<FixedString512Bytes> _conversationEntries = new NetworkList<FixedString512Bytes>();
        private readonly NetworkList<FixedString512Bytes> _teamNoteEntries = new NetworkList<FixedString512Bytes>();
        private readonly NetworkList<FixedString128Bytes> _readyEntries = new NetworkList<FixedString128Bytes>();
        private readonly NetworkVariable<FixedString64Bytes> _relayJoinCode = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<bool> _caseResolved = new NetworkVariable<bool>();
        private readonly NetworkVariable<FixedString512Bytes> _resolutionMessage = new NetworkVariable<FixedString512Bytes>();

        public string RelayJoinCode => _relayJoinCode.Value.ToString();
        public bool IsOnlineSessionActive => NetworkManager != null && NetworkManager.IsListening;
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

        private void Awake()
        {
            Instance = this;
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

            if (CaseSessionManager.Instance != null && caseDefinition != null && CaseSessionManager.Instance.ActiveCase != caseDefinition)
            {
                CaseSessionManager.Instance.SetActiveCase(caseDefinition);
            }

            _collectedEvidenceIds.OnListChanged += HandleEvidenceListChanged;
            _conversationEntries.OnListChanged += HandleConversationListChanged;
            _teamNoteEntries.OnListChanged += HandleTeamNoteListChanged;
            _readyEntries.OnListChanged += HandleReadyEntriesChanged;
            _caseResolved.OnValueChanged += HandleCaseResolvedChanged;

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

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _collectedEvidenceIds.OnListChanged -= HandleEvidenceListChanged;
            _conversationEntries.OnListChanged -= HandleConversationListChanged;
            _teamNoteEntries.OnListChanged -= HandleTeamNoteListChanged;
            _readyEntries.OnListChanged -= HandleReadyEntriesChanged;
            _caseResolved.OnValueChanged -= HandleCaseResolvedChanged;

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
            _conversationEntries.Clear();
            _teamNoteEntries.Clear();
            _readyEntries.Clear();
            _caseResolved.Value = false;
            _resolutionMessage.Value = default;

            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            foreach (var evidenceId in CaseSessionManager.Instance.CollectedEvidenceIds)
            {
                _collectedEvidenceIds.Add(new FixedString64Bytes(evidenceId));
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

        private void HandleLocalCaseResolved(bool success, string message)
        {
            if (!IsServer)
            {
                return;
            }

            _caseResolved.Value = success;
            _resolutionMessage.Value = new FixedString512Bytes(message);
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

            var line = hasRequiredEvidence ? evidenceLine : defaultLine;
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
