using MobilOfl.Case;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class NpcInteractable : InteractableBase
    {
        [SerializeField] private CaseDefinition caseDefinition;
        [SerializeField] private string npcId = "npc.default";
        [SerializeField] private string npcDisplayName = "NPC";
        [SerializeField] private Color markerColor = default;
        [SerializeField] [TextArea] private string defaultLine = "Simdi konusamam.";
        [SerializeField] private string requiredEvidenceId;
        [SerializeField] [TextArea] private string evidenceLine = "Bunu soylemem gerekiyordu.";
        [SerializeField] private string witnessEvidenceId;
        [SerializeField] private bool collectWitnessEvidenceOnce = true;

        public string NpcDisplayName => npcDisplayName;
        public string MarkerLabel => npcDisplayName;
        public Color MarkerColor => markerColor.a <= 0f ? new Color(1f, 0.78f, 0.3f, 1f) : markerColor;
        public bool IsMarkerVisible => isActiveAndEnabled && CaseSessionManager.Instance != null && !CaseSessionManager.Instance.IsCaseResolved;

        public override bool TryInteract(GameObject interactor)
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

            var stealth = interactor != null ? interactor.GetComponent<PlayerStealthController>() : null;
            if (stealth != null && !stealth.CanStartCalmConversation(out var stealthReason))
            {
                CaseSessionManager.Instance.PublishMessage(stealthReason);
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

            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState != null && networkCaseState.IsOnlineSessionActive)
            {
                return networkCaseState.RequestNpcInteraction(
                    npcId,
                    npcDisplayName,
                    defaultLine,
                    evidenceLine,
                    requiredEvidenceId,
                    witnessEvidenceId,
                    collectWitnessEvidenceOnce);
            }

            CaseSessionManager.Instance.RegisterNpcConversation(npcId, npcDisplayName, line, revealsNewLead);

            if (!hasRequiredEvidence || string.IsNullOrWhiteSpace(witnessEvidenceId))
            {
                return true;
            }

            if (collectWitnessEvidenceOnce && witnessAlreadyCollected)
            {
                return true;
            }

            if (caseDefinition != null)
            {
                CaseSessionManager.Instance.TryCollectEvidence(caseDefinition, witnessEvidenceId);
            }

            return true;
        }
    }
}
