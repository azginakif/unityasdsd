using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class InvestigationHintDirector : MonoBehaviour
    {
        [SerializeField] private float hintCooldownSeconds = 70f;
        [SerializeField] private float minimumCaseTimeBeforeHints = 35f;

        private float _nextHintAt;

        private void Update()
        {
            var session = CaseSessionManager.Instance;
            if (session == null || session.ActiveCase == null || session.IsCaseResolved)
            {
                return;
            }

            if (MainMenuBlocked())
            {
                return;
            }

            if (session.ElapsedCaseTimeSeconds < minimumCaseTimeBeforeHints)
            {
                return;
            }

            if (Time.time < _nextHintAt)
            {
                return;
            }

            if (Time.time - session.LastProgressTime < hintCooldownSeconds)
            {
                return;
            }

            session.PublishMessage("Dosya yardimi: " + session.GetRecommendedNextStep());
            _nextHintAt = Time.time + hintCooldownSeconds;
        }

        private static bool MainMenuBlocked()
        {
            return MobilOfl.UI.MainMenuHud.IsBlockingGameplay || MobilOfl.UI.CaseNotebookHud.IsAnyNotebookOpen;
        }
    }
}
