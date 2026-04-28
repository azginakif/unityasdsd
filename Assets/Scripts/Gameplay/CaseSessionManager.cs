using System;
using System.Collections.Generic;
using System.Linq;
using MobilOfl.Case;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class CaseSessionManager : MonoBehaviour
    {
        public static CaseSessionManager Instance { get; private set; }

        [SerializeField] private CaseDefinition activeCase;

        private readonly HashSet<string> _collectedEvidenceIds = new HashSet<string>();
        private readonly Dictionary<string, EvidenceData> _evidenceById = new Dictionary<string, EvidenceData>();
        private readonly List<string> _messageHistory = new List<string>();
        private readonly List<string> _conversationHistory = new List<string>();
        private readonly List<string> _teamNotes = new List<string>();
        private readonly HashSet<string> _interviewedNpcIds = new HashSet<string>();
        private float _caseStartedAt;
        private float _lastProgressAt;

        public event Action<EvidenceData> EvidenceCollected;
        public event Action<CaseDefinition> CaseStarted;
        public event Action<string> SessionMessagePublished;
        public event Action<bool, string> CaseResolved;
        public event Action<string, string, string, bool> NpcConversationRegistered;
        public event Action<string, string> TeamNoteAdded;

        public CaseDefinition ActiveCase => activeCase;
        public IReadOnlyCollection<string> CollectedEvidenceIds => _collectedEvidenceIds;
        public IReadOnlyList<string> MessageHistory => _messageHistory;
        public IReadOnlyList<string> ConversationHistory => _conversationHistory;
        public IReadOnlyList<string> TeamNotes => _teamNotes;
        public bool IsCaseResolved { get; private set; }
        public int InterviewedNpcCount => _interviewedNpcIds.Count;
        public float ElapsedCaseTimeSeconds => Mathf.Max(0f, Time.time - _caseStartedAt);
        public float LastProgressTime => _lastProgressAt;
        public int CollectedCriticalEvidenceCount => _collectedEvidenceIds.Count(IsCriticalEvidence);
        public int TotalCriticalEvidenceCount => _evidenceById.Values.Count(item => item != null && item.IsCritical);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetActiveCase(activeCase);
        }

        public void SetActiveCase(CaseDefinition caseDefinition)
        {
            activeCase = caseDefinition;
            _collectedEvidenceIds.Clear();
            _evidenceById.Clear();
            _messageHistory.Clear();
            _conversationHistory.Clear();
            _teamNotes.Clear();
            _interviewedNpcIds.Clear();
            IsCaseResolved = false;
            _caseStartedAt = Time.time;
            _lastProgressAt = Time.time;

            if (activeCase == null)
            {
                return;
            }

            foreach (var evidence in activeCase.EvidenceItems)
            {
                if (evidence == null || string.IsNullOrWhiteSpace(evidence.Id))
                {
                    continue;
                }

                _evidenceById[evidence.Id] = evidence;
            }

            CaseStarted?.Invoke(activeCase);
            PublishMessage(activeCase.OpeningBrief);
        }

        public void RestartCurrentCase()
        {
            SetActiveCase(activeCase);
        }

        public bool TryCollectEvidence(CaseDefinition sourceCase, string evidenceId)
        {
            if (IsCaseResolved)
            {
                PublishMessage("Vaka zaten tamamlandi.");
                return false;
            }

            if (activeCase == null || sourceCase != activeCase)
            {
                return false;
            }

            if (!_evidenceById.TryGetValue(evidenceId, out var evidence))
            {
                return false;
            }

            if (!_collectedEvidenceIds.Add(evidenceId))
            {
                return false;
            }

            EvidenceCollected?.Invoke(evidence);
            RegisterProgress();
            PublishMessage($"Delil toplandi: {evidence.Title}");
            return true;
        }

        public bool TryApplyNetworkEvidence(string evidenceId, bool publishSyncMessage)
        {
            if (activeCase == null)
            {
                return false;
            }

            if (!_evidenceById.TryGetValue(evidenceId, out var evidence))
            {
                return false;
            }

            if (!_collectedEvidenceIds.Add(evidenceId))
            {
                return false;
            }

            EvidenceCollected?.Invoke(evidence);
            RegisterProgress();

            if (publishSyncMessage)
            {
                PublishMessage($"Takim delili senkronize edildi: {evidence.Title}");
            }

            return true;
        }

        public void PublishMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _messageHistory.Add(message);
            TrimHistory(_messageHistory, 12);
            SessionMessagePublished?.Invoke(message);
        }

        public void RegisterNpcConversation(string npcId, string npcDisplayName, string line, bool revealedLead)
        {
            if (string.IsNullOrWhiteSpace(npcDisplayName) || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var resolvedNpcId = string.IsNullOrWhiteSpace(npcId) ? npcDisplayName : npcId;
            _interviewedNpcIds.Add(resolvedNpcId);

            var conversationEntry = $"{npcDisplayName}: {line}";
            _conversationHistory.Add(conversationEntry);
            TrimHistory(_conversationHistory, 18);
            RegisterProgress();

            NpcConversationRegistered?.Invoke(resolvedNpcId, npcDisplayName, line, revealedLead);
            PublishMessage(revealedLead ? $"{conversationEntry} [Yeni ipucu]" : conversationEntry);
        }

        public void ApplyNetworkConversation(string npcId, string npcDisplayName, string line)
        {
            if (string.IsNullOrWhiteSpace(npcDisplayName) || string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var resolvedNpcId = string.IsNullOrWhiteSpace(npcId) ? npcDisplayName : npcId;
            if (!_interviewedNpcIds.Add(resolvedNpcId))
            {
                return;
            }

            var conversationEntry = $"{npcDisplayName}: {line}";
            _conversationHistory.Add(conversationEntry);
            TrimHistory(_conversationHistory, 18);
            RegisterProgress();
            PublishMessage($"{conversationEntry} [Takim senkronize]");
        }

        public bool AddTeamNote(string authorName, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
            {
                return false;
            }

            var trimmedNote = noteText.Trim();
            var resolvedAuthor = string.IsNullOrWhiteSpace(authorName) ? "Takim" : authorName.Trim();
            var noteEntry = $"{resolvedAuthor}: {trimmedNote}";

            _teamNotes.Add(noteEntry);
            TrimHistory(_teamNotes, 18);
            RegisterProgress();
            TeamNoteAdded?.Invoke(resolvedAuthor, trimmedNote);
            PublishMessage($"{resolvedAuthor} yeni not ekledi.");
            return true;
        }

        public bool ApplyNetworkTeamNote(string authorName, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
            {
                return false;
            }

            var trimmedNote = noteText.Trim();
            var resolvedAuthor = string.IsNullOrWhiteSpace(authorName) ? "Takim" : authorName.Trim();
            var noteEntry = $"{resolvedAuthor}: {trimmedNote}";

            _teamNotes.Add(noteEntry);
            TrimHistory(_teamNotes, 18);
            RegisterProgress();
            PublishMessage($"{resolvedAuthor} notu takimla senkronize edildi.");
            return true;
        }

        public bool HasEvidence(string evidenceId)
        {
            return _collectedEvidenceIds.Contains(evidenceId);
        }

        public EvidenceData GetEvidence(string evidenceId)
        {
            _evidenceById.TryGetValue(evidenceId, out var evidence);
            return evidence;
        }

        public int GetSuspectEvidenceMatchCount(SuspectData suspect)
        {
            if (suspect == null)
            {
                return 0;
            }

            var collected = 0;
            foreach (var evidenceId in suspect.RequiredEvidenceIds)
            {
                if (HasEvidence(evidenceId))
                {
                    collected++;
                }
            }

            return collected;
        }

        public int GetSuspectConfidencePercent(string suspectId)
        {
            if (activeCase == null)
            {
                return 0;
            }

            var suspect = activeCase.Suspects.FirstOrDefault(item => item != null && item.Id == suspectId);
            if (suspect == null || suspect.RequiredEvidenceIds.Count == 0)
            {
                return 0;
            }

            return Mathf.RoundToInt((float)GetSuspectEvidenceMatchCount(suspect) / suspect.RequiredEvidenceIds.Count * 100f);
        }

        public string GetMissingEvidenceSummary(SuspectData suspect)
        {
            if (suspect == null || suspect.RequiredEvidenceIds.Count == 0)
            {
                return "Ek delil gerekmiyor.";
            }

            var missingEvidenceTitles = new List<string>();

            foreach (var evidenceId in suspect.RequiredEvidenceIds)
            {
                if (HasEvidence(evidenceId))
                {
                    continue;
                }

                var evidence = GetEvidence(evidenceId);
                missingEvidenceTitles.Add(evidence != null ? evidence.Title : evidenceId);
            }

            if (missingEvidenceTitles.Count == 0)
            {
                return "Tum gerekli deliller toplandi.";
            }

            return "Eksik delil: " + string.Join(", ", missingEvidenceTitles);
        }

        public string GetRecommendedNextStep()
        {
            if (activeCase == null)
            {
                return "Aktif vaka bulunamadi.";
            }

            if (IsCaseResolved)
            {
                return "Vaka tamamlandi. Sonuc ekranini incele.";
            }

            if (!HasEvidence("evidence.security-log"))
            {
                return "Guvenlik odasina git ve gece hareket kaydini topla.";
            }

            if (!HasEvidence("evidence.answer-key-note"))
            {
                return "Kutuphane masasini tara; not kagidi ilk fiziksel izi verecek.";
            }

            if (!HasEvidence("evidence.guard-testimony"))
            {
                return "Guvenlik gorevlisine geri don. Kamera kaydi yeni ifade acacak.";
            }

            if (!HasEvidence("evidence.student-testimony"))
            {
                return "Kutuphane ogrencisiyle tekrar konus; notun kaynagi netlesecek.";
            }

            if (!HasEvidence("evidence.archive-ledger"))
            {
                return "Arsiv kanadina gec. Giris defteri suphelinin onceki erisim izini sakliyor.";
            }

            if (!HasEvidence("evidence.canteen-testimony"))
            {
                return "Kantin calisaniyla konus. Gec saat hareketi burada teyit edilecek.";
            }

            if (!HasEvidence("evidence.locker-key"))
            {
                return "Ogretmenler odasindaki yedek anahtari bularak erisim zincirini tamamla.";
            }

            var readySuspect = activeCase.Suspects.FirstOrDefault(item => item != null && CanAccuse(item.Id));
            if (readySuspect != null)
            {
                return $"{readySuspect.DisplayName} icin yeterli delil var. Vaka masasindan suclama yapabilirsin.";
            }

            return "Suphelilerin eksik delillerini dosyada karsilastir ve son ipuclarini topla.";
        }

        public bool CanAccuse(string suspectId)
        {
            if (activeCase == null || IsCaseResolved)
            {
                return false;
            }

            var suspect = activeCase.Suspects.FirstOrDefault(item => item != null && item.Id == suspectId);
            if (suspect == null)
            {
                return false;
            }

            return suspect.RequiredEvidenceIds.All(HasEvidence);
        }

        public bool HasAnyAccusableSuspect()
        {
            if (activeCase == null || IsCaseResolved)
            {
                return false;
            }

            foreach (var suspect in activeCase.Suspects)
            {
                if (suspect != null && CanAccuse(suspect.Id))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryResolveCase(string suspectId, out string resultMessage)
        {
            resultMessage = string.Empty;

            if (activeCase == null)
            {
                resultMessage = "Aktif vaka bulunamadi.";
                PublishMessage(resultMessage);
                CaseResolved?.Invoke(false, resultMessage);
                return false;
            }

            if (IsCaseResolved)
            {
                resultMessage = "Vaka zaten tamamlandi.";
                PublishMessage(resultMessage);
                return false;
            }

            if (!CanAccuse(suspectId))
            {
                resultMessage = "Bu supheliyi suclamak icin yeterli delil toplanmadi.";
                PublishMessage(resultMessage);
                CaseResolved?.Invoke(false, resultMessage);
                return false;
            }

            if (suspectId == activeCase.CulpritSuspectId)
            {
                resultMessage =
                    $"Dogru karar. Motivasyon: {activeCase.CulpritMotive}\nZaman cizelgesi: {activeCase.CulpritTimeline}";
                IsCaseResolved = true;
                RegisterProgress();
                PublishMessage(resultMessage);
                CaseResolved?.Invoke(true, resultMessage);
                return true;
            }

            resultMessage = "Yanlis supheli secildi.";
            PublishMessage(resultMessage);
            CaseResolved?.Invoke(false, resultMessage);
            return false;
        }

        public void ApplyNetworkResolution(bool success, string resultMessage)
        {
            if (success)
            {
                IsCaseResolved = true;
            }

            RegisterProgress();
            PublishMessage(resultMessage);
            CaseResolved?.Invoke(success, resultMessage);
        }

        public string GetReasoningSummary()
        {
            if (activeCase == null)
            {
                return "Aktif vaka yok. Analiz olusturulamiyor.";
            }

            var segments = new List<string>();

            if (HasEvidence("evidence.security-log"))
            {
                segments.Add("Guvenlik kaydi, gece laboratuvar koridorunda planli bir hareket oldugunu dogruluyor.");
            }

            if (HasEvidence("evidence.answer-key-note"))
            {
                segments.Add("Kutuphanedeki not, soru sizintisinin fiziksel olarak elde dolastigini gosteriyor.");
            }

            if (HasEvidence("evidence.student-testimony"))
            {
                segments.Add("Kutuphane tanigi notu dogrudan bilisim kulubu ogrencisine bagliyor.");
            }

            if (HasEvidence("evidence.guard-testimony"))
            {
                segments.Add("Guvenlik ifadesi, suphelinin gece koridorda bulundugunu insan tanikla destekliyor.");
            }

            if (HasEvidence("evidence.archive-ledger"))
            {
                segments.Add("Arsiv defteri, suphelinin onceki gunlerde de hassas erisim yollarini aradigini gosteriyor.");
            }

            if (HasEvidence("evidence.canteen-testimony"))
            {
                segments.Add("Kantin ifadesi, olay saatine yakin hizli ve amacli hareket zincirini tamamliyor.");
            }

            if (HasEvidence("evidence.locker-key"))
            {
                segments.Add("Yedek anahtar, soru dolabina fiziksel erisimin nasil saglandigini acikliyor.");
            }

            if (segments.Count == 0)
            {
                return "Henuz yeterli veri yok. Ilk delili toplayip olay zincirini kurmaya basla.";
            }

            if (HasAnyAccusableSuspect())
            {
                segments.Add("Toplanan deliller artik net bir zaman, mekan ve erisim zinciri kuruyor.");
            }

            return string.Join(" ", segments);
        }

        private void RegisterProgress()
        {
            _lastProgressAt = Time.time;
        }

        private bool IsCriticalEvidence(string evidenceId)
        {
            return _evidenceById.TryGetValue(evidenceId, out var evidence) && evidence != null && evidence.IsCritical;
        }

        private static void TrimHistory(List<string> history, int maxEntries)
        {
            while (history.Count > maxEntries)
            {
                history.RemoveAt(0);
            }
        }
    }
}
