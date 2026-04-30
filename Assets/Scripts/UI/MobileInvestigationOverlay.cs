using MobilOfl.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class MobileInvestigationOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text headerSubtitleText;
        [SerializeField] private Text zoneText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text contextText;
        [SerializeField] private CanvasGroup contextGroup;
        [SerializeField] private float refreshInterval = 0.15f;
        [SerializeField] private float fadeSpeed = 10f;

        private float _nextRefreshAt;
        private float _contextAlpha;
        private PrototypeFirstPersonController _movementController;
        private InvestigationScanner _scanner;
        private PlayerStealthController _stealthController;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();

            var session = CaseSessionManager.Instance;
            var hidden = MainMenuHud.IsBlockingGameplay ||
                CaseNotebookHud.IsAnyNotebookOpen ||
                (session != null && session.IsCaseResolved);

            if (rootGroup != null)
            {
                rootGroup.alpha = hidden ? 0f : 1f;
                rootGroup.interactable = !hidden;
                rootGroup.blocksRaycasts = !hidden;
            }

            if (hidden)
            {
                UpdateContextVisuals(false, "Etkilesim bekleniyor.");
                return;
            }

            if (Time.unscaledTime >= _nextRefreshAt)
            {
                Refresh(session);
                _nextRefreshAt = Time.unscaledTime + refreshInterval;
            }

            if (contextGroup != null)
            {
                contextGroup.alpha = Mathf.MoveTowards(contextGroup.alpha, _contextAlpha, Time.unscaledDeltaTime * fadeSpeed);
            }
        }

        private void Refresh(CaseSessionManager session)
        {
            if (playerInteraction == null)
            {
                return;
            }

            var zoneTitle = SchoolLocationUtility.GetZoneTitle(playerInteraction.transform.position);
            if (zoneText != null)
            {
                zoneText.text = zoneTitle;
            }

            if (headerSubtitleText != null)
            {
                var elapsed = session == null ? "00:00" : FormatElapsedTime(session.ElapsedCaseTimeSeconds);
                var mobility = BuildMobilitySummary();
                headerSubtitleText.text = $"{zoneTitle}  |  Sure {elapsed}  |  {mobility}";
            }

            if (objectiveText != null)
            {
                objectiveText.text = session == null
                    ? "Vaka bilgisi bekleniyor."
                    : TrimForMobile(session.GetRecommendedNextStep(), 96);
            }

            var currentInteractable = playerInteraction.CurrentInteractable;
            if (currentInteractable != null)
            {
                UpdateContextVisuals(true, TrimForMobile(currentInteractable.PromptText, 56));
                return;
            }

            if (InvestigationScanner.IsScanActive)
            {
                UpdateContextVisuals(true, TrimForMobile(InvestigationScanner.LastScanSummary, 72));
                return;
            }

            UpdateContextVisuals(false, "Delil veya NPC hedefine yaklas.");
        }

        private void UpdateContextVisuals(bool hasTarget, string message)
        {
            _contextAlpha = hasTarget ? 1f : 0.7f;
            if (contextText != null)
            {
                contextText.text = hasTarget ? "ETKILESIM HAZIR\n" + message : message;
            }
        }

        private void ResolveReferences()
        {
            if (playerInteraction == null || !playerInteraction.isActiveAndEnabled)
            {
                playerInteraction = Object.FindFirstObjectByType<PlayerInteractionController>();
            }

            if (_movementController == null || !_movementController.isActiveAndEnabled)
            {
                _movementController = Object.FindFirstObjectByType<PrototypeFirstPersonController>();
            }

            if (_scanner == null || !_scanner.isActiveAndEnabled)
            {
                _scanner = Object.FindFirstObjectByType<InvestigationScanner>();
            }

            if (_stealthController == null || !_stealthController.isActiveAndEnabled)
            {
                _stealthController = Object.FindFirstObjectByType<PlayerStealthController>();
            }

            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            if (contextGroup == null)
            {
                var contextPanel = transform.Find("MobileContextPanel");
                if (contextPanel != null)
                {
                    contextGroup = contextPanel.GetComponent<CanvasGroup>();
                }
            }
        }

        private string BuildMobilitySummary()
        {
            var staminaPercent = _movementController == null ? 100 : Mathf.RoundToInt(_movementController.SprintStamina01 * 100f);
            var stance = _movementController != null && _movementController.IsCrouching ? "Gizli" : "Serbest";
            var alertPercent = _stealthController == null ? 0 : Mathf.RoundToInt(_stealthController.AlertLevel01 * 100f);
            var scanState = _scanner != null && InvestigationScanner.IsScanActive
                ? "Tarama acik"
                : (_scanner != null && !_scanner.IsReady ? $"Tarama {Mathf.CeilToInt(_scanner.CooldownRemaining)} sn" : "Tarama hazir");
            return $"Enerji %{staminaPercent}  |  Risk %{alertPercent}  |  {stance}  |  {scanState}";
        }

        private static string TrimForMobile(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 3).TrimEnd() + "...";
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
