using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class OnboardingHintDirector : MonoBehaviour
    {
        private CaseSessionManager _session;
        private bool _publishedOpening;
        private bool _publishedScanHint;
        private bool _publishedNotebookHint;
        private bool _publishedWitnessHint;
        private bool _publishedToolHint;
        private bool _publishedAccuseHint;
        private float _nextAllowedHintAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Object.FindAnyObjectByType<OnboardingHintDirector>() != null)
            {
                return;
            }

            var gameObject = new GameObject("OnboardingHintDirector");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<OnboardingHintDirector>();
        }

        private void Update()
        {
            if (CaseSessionManager.Instance == null || CaseSessionManager.Instance.ActiveCase == null)
            {
                _session = null;
                return;
            }

            if (_session != CaseSessionManager.Instance)
            {
                _session = CaseSessionManager.Instance;
                ResetHints();
            }

            if (MainMenuBlocked() || _session.IsCaseResolved || Time.time < _nextAllowedHintAt)
            {
                return;
            }

            EvaluateHints();
        }

        private void ResetHints()
        {
            _publishedOpening = false;
            _publishedScanHint = false;
            _publishedNotebookHint = false;
            _publishedWitnessHint = false;
            _publishedToolHint = false;
            _publishedAccuseHint = false;
            _nextAllowedHintAt = Time.time + 1.2f;
        }

        private void EvaluateHints()
        {
            if (!_publishedOpening && _session.ElapsedCaseTimeSeconds >= 1.5f)
            {
                Publish("Baslangic: once guvenlik odasindaki kaydi ve kutuphanedeki notu bul. Q ile tarama yapabilirsin.");
                _publishedOpening = true;
                return;
            }

            if (!_publishedScanHint && _session.ElapsedCaseTimeSeconds >= 18f && _session.CollectedEvidenceIds.Count == 0)
            {
                Publish("Tarama ipucu: Q veya mobil TARA butonu yakindaki delil, ekipman ve NPC sinyallerini listeler.");
                _publishedScanHint = true;
                return;
            }

            if (!_publishedNotebookHint && _session.CollectedEvidenceIds.Count >= 1)
            {
                Publish("Dosya ipucu: TAB veya DOSYA butonu ile delilleri, suphelileri ve takim notlarini kontrol et.");
                _publishedNotebookHint = true;
                return;
            }

            if (!_publishedWitnessHint &&
                (_session.HasEvidence("evidence.security-log") || _session.HasEvidence("evidence.answer-key-note")) &&
                _session.InterviewedNpcCount == 0)
            {
                Publish("Sorgu ipucu: yeni delil bulduktan sonra ilgili NPC'ye tekrar git. Bazi ifadeler delile bagli acilir.");
                _publishedWitnessHint = true;
                return;
            }

            if (!_publishedToolHint &&
                _session.HasEvidence("evidence.student-testimony") &&
                !_session.HasTool("tool.archive-pass"))
            {
                Publish("Ekipman ipucu: arama noktalarinin bir kismi kart veya maymuncuk ister. Once ekipmani bul.");
                _publishedToolHint = true;
                return;
            }

            if (!_publishedAccuseHint && _session.HasAnyAccusableSuspect())
            {
                Publish("Final hazir: Vaka Masasi'na git, dosyada Supheliler sekmesini ac ve son suclamayi yap.");
                _publishedAccuseHint = true;
            }
        }

        private void Publish(string message)
        {
            _session.PublishMessage(message);
            _nextAllowedHintAt = Time.time + 14f;
        }

        private static bool MainMenuBlocked()
        {
            return MobilOfl.UI.MainMenuHud.IsBlockingGameplay || MobilOfl.UI.CaseNotebookHud.IsAnyNotebookOpen;
        }
    }
}
