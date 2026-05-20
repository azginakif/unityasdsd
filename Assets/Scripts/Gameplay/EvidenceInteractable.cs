using MobilOfl.Case;
using MobilOfl.Online;
using MobilOfl.Visuals;
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
        private Renderer[] _cachedRenderers;
        private Collider[] _cachedColliders;
        private Behaviour[] _cachedBehaviours;

        public string MarkerLabel => string.IsNullOrWhiteSpace(markerLabel) ? "Delil" : markerLabel;
        public Color MarkerColor => markerColor.a <= 0f ? new Color(0.24f, 0.86f, 1f, 1f) : markerColor;
        public bool IsMarkerVisible =>
            isActiveAndEnabled &&
            !string.IsNullOrWhiteSpace(evidenceId) &&
            CaseSessionManager.Instance != null &&
            !CaseSessionManager.Instance.HasEvidence(evidenceId);

        public override bool CanShowInteractionPrompt(GameObject interactor)
        {
            return IsMarkerVisible;
        }

        private void OnEnable()
        {
            CacheVisualTargets();
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
            var sourceCase = caseDefinition != null
                ? caseDefinition
                : (CaseSessionManager.Instance != null ? CaseSessionManager.Instance.ActiveCase : null);
            if (sourceCase == null || string.IsNullOrWhiteSpace(evidenceId))
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

            if (!CaseSessionManager.Instance.TryCollectEvidence(sourceCase, evidenceId))
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
                return;
            }

            RestoreUncollectedState();
        }

        private void ApplyCollectedVisualState()
        {
            if (collectedVisual != null)
            {
                collectedVisual.SetActive(true);
            }

            if (disableObjectOnCollect)
            {
                SetInteractableVisualState(false);
            }
        }

        private void RestoreUncollectedState()
        {
            if (collectedVisual != null)
            {
                collectedVisual.SetActive(false);
            }

            SetInteractableVisualState(true);
        }

        private void CacheVisualTargets()
        {
            if (_cachedRenderers == null)
            {
                _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (_cachedColliders == null)
            {
                _cachedColliders = GetComponentsInChildren<Collider>(true);
            }

            if (_cachedBehaviours == null)
            {
                _cachedBehaviours = GetComponentsInChildren<Behaviour>(true);
            }
        }

        private void SetInteractableVisualState(bool isVisible)
        {
            CacheVisualTargets();

            if (_cachedRenderers != null)
            {
                for (var i = 0; i < _cachedRenderers.Length; i++)
                {
                    if (_cachedRenderers[i] != null)
                    {
                        _cachedRenderers[i].enabled = isVisible;
                    }
                }
            }

            if (_cachedColliders != null)
            {
                for (var i = 0; i < _cachedColliders.Length; i++)
                {
                    if (_cachedColliders[i] != null)
                    {
                        _cachedColliders[i].enabled = isVisible;
                    }
                }
            }

            if (_cachedBehaviours == null)
            {
                return;
            }

            for (var i = 0; i < _cachedBehaviours.Length; i++)
            {
                var behaviour = _cachedBehaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                if (behaviour is EvidenceVisualPulse)
                {
                    behaviour.enabled = isVisible;
                }
            }
        }
    }
}
