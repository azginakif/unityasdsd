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
        [SerializeField] private float interactionFocusDuration = 5.5f;

        private Transform _focusedInteractor;
        private float _focusUntil;

        public string NpcDisplayName => npcDisplayName;
        public string MarkerLabel => npcDisplayName;
        public Color MarkerColor => markerColor.a <= 0f ? new Color(1f, 0.78f, 0.3f, 1f) : markerColor;
        public bool IsMarkerVisible => isActiveAndEnabled && CaseSessionManager.Instance != null && !CaseSessionManager.Instance.IsCaseResolved;
        public bool IsInteractionFocused => _focusedInteractor != null && Time.time < _focusUntil;
        public Transform FocusedInteractor => IsInteractionFocused ? _focusedInteractor : null;

        private void Update()
        {
            if (!IsInteractionFocused)
            {
                return;
            }

            FaceInteractor(_focusedInteractor.gameObject);
        }

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

            var line = hasRequiredEvidence
                ? evidenceLine
                : defaultLine;
            var revealsNewLead =
                hasRequiredEvidence &&
                !string.IsNullOrWhiteSpace(witnessEvidenceId) &&
                (!collectWitnessEvidenceOnce || !witnessAlreadyCollected);

            BeginInteractionFocus(interactor);

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

        private void FaceInteractor(GameObject interactor)
        {
            if (interactor == null)
            {
                return;
            }

            var lookDirection = interactor.transform.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private void BeginInteractionFocus(GameObject interactor)
        {
            if (interactor == null)
            {
                return;
            }

            _focusedInteractor = interactor.transform;
            _focusUntil = Time.time + Mathf.Max(1.5f, interactionFocusDuration);
            FaceInteractor(interactor);
        }
    }
}
