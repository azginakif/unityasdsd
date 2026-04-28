using MobilOfl.Case;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class EvidenceInteractable : InteractableBase
    {
        [SerializeField] private CaseDefinition caseDefinition;
        [SerializeField] private string evidenceId;
        [SerializeField] private string markerLabel = "Delil";
        [SerializeField] private Color markerColor = default;
        [SerializeField] private GameObject collectedVisual;
        [SerializeField] private bool disableObjectOnCollect = true;
        private bool _isSubscribed;

        public string MarkerLabel => string.IsNullOrWhiteSpace(markerLabel) ? "Delil" : markerLabel;
        public Color MarkerColor => markerColor.a <= 0f ? new Color(0.24f, 0.86f, 1f, 1f) : markerColor;
        public bool IsMarkerVisible =>
            isActiveAndEnabled &&
            CaseSessionManager.Instance != null &&
            !CaseSessionManager.Instance.HasEvidence(evidenceId);

        private void OnEnable()
        {
            TrySubscribe();
            RefreshCollectedState();
        }

        private void Update()
        {
            if (!_isSubscribed)
            {
                TrySubscribe();
            }
        }

        private void OnDisable()
        {
            if (CaseSessionManager.Instance != null)
            {
                CaseSessionManager.Instance.EvidenceCollected -= HandleEvidenceCollected;
                CaseSessionManager.Instance.CaseStarted -= HandleCaseStarted;
            }

            _isSubscribed = false;
        }

        public override bool TryInteract(GameObject interactor)
        {
            if (caseDefinition == null || string.IsNullOrWhiteSpace(evidenceId))
            {
                return false;
            }

            if (CaseSessionManager.Instance == null)
            {
                return false;
            }

            if (CaseSessionManager.Instance.HasEvidence(evidenceId))
            {
                ApplyCollectedVisualState();
                return false;
            }

            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState != null && networkCaseState.IsOnlineSessionActive)
            {
                return networkCaseState.RequestCollectEvidence(evidenceId);
            }

            if (!CaseSessionManager.Instance.TryCollectEvidence(caseDefinition, evidenceId))
            {
                return false;
            }

            ApplyCollectedVisualState();
            return true;
        }

        private void HandleEvidenceCollected(EvidenceData evidence)
        {
            if (evidence != null && evidence.Id == evidenceId)
            {
                ApplyCollectedVisualState();
            }
        }

        private void HandleCaseStarted(CaseDefinition _)
        {
            RefreshCollectedState();
        }

        private void TrySubscribe()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            CaseSessionManager.Instance.EvidenceCollected -= HandleEvidenceCollected;
            CaseSessionManager.Instance.CaseStarted -= HandleCaseStarted;
            CaseSessionManager.Instance.EvidenceCollected += HandleEvidenceCollected;
            CaseSessionManager.Instance.CaseStarted += HandleCaseStarted;
            _isSubscribed = true;
        }

        private void RefreshCollectedState()
        {
            if (CaseSessionManager.Instance != null && CaseSessionManager.Instance.HasEvidence(evidenceId))
            {
                ApplyCollectedVisualState();
            }
        }

        private void ApplyCollectedVisualState()
        {
            if (collectedVisual != null)
            {
                collectedVisual.SetActive(true);
            }

            if (disableObjectOnCollect)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
