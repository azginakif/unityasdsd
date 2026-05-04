using System;
using MobilOfl.Case;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class CaseSaveManager : MonoBehaviour
    {
        private const string SaveKeyPrefix = "mobilofl.case.save.";
        private const string SaveVersion = "1";

        private CaseSessionManager _session;
        private string _restoreAttemptedCaseId = string.Empty;
        private float _saveAt = -1f;
        private bool _isRestoring;

        public static CaseSaveManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null || FindAnyObjectByType<CaseSaveManager>() != null)
            {
                return;
            }

            var gameObject = new GameObject("CaseSaveManager");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<CaseSaveManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            TrySubscribe();

            if (_saveAt > 0f && Time.unscaledTime >= _saveAt)
            {
                _saveAt = -1f;
                SaveCurrentSession();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveCurrentSession();
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentSession();
        }

        public bool HasSaveForCase(string caseId)
        {
            return !string.IsNullOrWhiteSpace(caseId) && PlayerPrefs.HasKey(BuildSaveKey(caseId));
        }

        public string GetCurrentSaveSummary(CaseDefinition caseDefinition)
        {
            if (caseDefinition == null || !TryLoadSnapshot(caseDefinition.CaseId, out var snapshot))
            {
                return "Kayit yok";
            }

            var minutes = Mathf.FloorToInt(Mathf.Max(0f, snapshot.ElapsedSeconds) / 60f);
            return $"Kayit var: {snapshot.EvidenceIds.Count} delil, {snapshot.Tools.Count} ekipman, {minutes} dk";
        }

        public void ClearSaveForCase(string caseId)
        {
            if (string.IsNullOrWhiteSpace(caseId))
            {
                return;
            }

            PlayerPrefs.DeleteKey(BuildSaveKey(caseId));
            PlayerPrefs.Save();
        }

        private void TrySubscribe()
        {
            if (CaseSessionManager.Instance == null || _session == CaseSessionManager.Instance)
            {
                return;
            }

            Unsubscribe();
            _session = CaseSessionManager.Instance;
            _session.CaseStarted += HandleCaseStarted;
            _session.EvidenceCollected += HandleEvidenceCollected;
            _session.ToolUnlocked += HandleToolUnlocked;
            _session.NpcConversationRegistered += HandleNpcConversationRegistered;
            _session.TeamNoteAdded += HandleTeamNoteAdded;
            _session.CaseResolved += HandleCaseResolved;

            if (_session.ActiveCase != null)
            {
                HandleCaseStarted(_session.ActiveCase);
            }
        }

        private void Unsubscribe()
        {
            if (_session == null)
            {
                return;
            }

            _session.CaseStarted -= HandleCaseStarted;
            _session.EvidenceCollected -= HandleEvidenceCollected;
            _session.ToolUnlocked -= HandleToolUnlocked;
            _session.NpcConversationRegistered -= HandleNpcConversationRegistered;
            _session.TeamNoteAdded -= HandleTeamNoteAdded;
            _session.CaseResolved -= HandleCaseResolved;
            _session = null;
        }

        private void HandleCaseStarted(CaseDefinition caseDefinition)
        {
            if (caseDefinition == null || IsOnlineSessionActive())
            {
                return;
            }

            if (_restoreAttemptedCaseId != caseDefinition.CaseId)
            {
                _restoreAttemptedCaseId = caseDefinition.CaseId;
                if (TryLoadSnapshot(caseDefinition.CaseId, out var snapshot))
                {
                    _isRestoring = true;
                    _session.RestoreSnapshot(snapshot, true);
                    _isRestoring = false;
                    return;
                }
            }

            MarkDirty();
        }

        private void HandleEvidenceCollected(EvidenceData _)
        {
            MarkDirty();
        }

        private void HandleToolUnlocked(string _, string __)
        {
            MarkDirty();
        }

        private void HandleNpcConversationRegistered(string _, string __, string ___, bool ____)
        {
            MarkDirty();
        }

        private void HandleTeamNoteAdded(string _, string __)
        {
            MarkDirty();
        }

        private void HandleCaseResolved(bool _, string __)
        {
            MarkDirty();
        }

        private void MarkDirty()
        {
            if (_isRestoring || IsOnlineSessionActive())
            {
                return;
            }

            _saveAt = Time.unscaledTime + 0.5f;
        }

        private void SaveCurrentSession()
        {
            if (_isRestoring || _session == null || _session.ActiveCase == null || IsOnlineSessionActive())
            {
                return;
            }

            var snapshot = _session.CreateSnapshot();
            if (string.IsNullOrWhiteSpace(snapshot.CaseId))
            {
                return;
            }

            PlayerPrefs.SetString(BuildSaveKey(snapshot.CaseId), JsonUtility.ToJson(new SaveEnvelope
            {
                Version = SaveVersion,
                SavedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Snapshot = snapshot
            }));
            PlayerPrefs.Save();
        }

        private bool TryLoadSnapshot(string caseId, out CaseSessionManager.CaseSessionSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(caseId))
            {
                return false;
            }

            var raw = PlayerPrefs.GetString(BuildSaveKey(caseId), string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);
                if (envelope == null || envelope.Version != SaveVersion || envelope.Snapshot == null)
                {
                    return false;
                }

                snapshot = envelope.Snapshot;
                return snapshot.CaseId == caseId;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Case save load failed: " + exception.Message);
                return false;
            }
        }

        private static bool IsOnlineSessionActive()
        {
            var networkCaseState = NetworkCaseState.Instance;
            return networkCaseState != null && networkCaseState.IsOnlineSessionActive;
        }

        private static string BuildSaveKey(string caseId)
        {
            return SaveKeyPrefix + caseId;
        }

        [Serializable]
        private class SaveEnvelope
        {
            public string Version;
            public long SavedAtUnixSeconds;
            public CaseSessionManager.CaseSessionSnapshot Snapshot;
        }
    }
}
