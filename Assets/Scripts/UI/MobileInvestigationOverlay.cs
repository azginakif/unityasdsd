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
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private bool showOnDesktop;
        [SerializeField] private float refreshInterval = 0.15f;
        [SerializeField] private float fadeSpeed = 10f;

        private static bool _isMobileHudVisible;
        private static bool _isMobileUiAllowed;
        private float _nextRefreshAt;
        private float _contextAlpha;
        private PrototypeFirstPersonController _movementController;
        private InvestigationScanner _scanner;
        private PlayerStealthController _stealthController;
        private bool _layoutApplied;

        public static bool IsMobileHudVisible => _isMobileHudVisible;
        public static bool IsMobileUiAllowed => _isMobileUiAllowed;

        private void Awake()
        {
            ApplyControlsOnlyLayout();
            ResolveReferences();
        }

        private void OnDisable()
        {
            _isMobileHudVisible = false;
            _isMobileUiAllowed = false;
        }

        private void Update()
        {
            ResolveReferences();
            ApplyControlsOnlyLayout();

            var session = CaseSessionManager.Instance;
            var mobileUiAllowed = ShouldShowMobileUi();
            _isMobileUiAllowed = mobileUiAllowed;
            var hidden = MainMenuHud.IsBlockingGameplay ||
                CaseNotebookHud.IsAnyNotebookOpen ||
                (session != null && session.IsCaseResolved) ||
                !mobileUiAllowed;

            if (rootGroup != null)
            {
                var isVisible = !hidden;
                _isMobileHudVisible = isVisible;
                rootGroup.alpha = isVisible ? 1f : 0f;
                rootGroup.interactable = isVisible;
                rootGroup.blocksRaycasts = isVisible;
            }
            else
            {
                _isMobileHudVisible = !hidden;
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
                UpdateContextVisuals(false, string.Empty);
                return;
            }

            UpdateContextVisuals(false, string.Empty);
        }

        private void UpdateContextVisuals(bool hasTarget, string message)
        {
            _contextAlpha = hasTarget ? 1f : 0f;
            if (contextText != null)
            {
                contextText.text = hasTarget ? message : string.Empty;
            }
        }

        private void ApplyControlsOnlyLayout()
        {
            if (_layoutApplied)
            {
                return;
            }

            _layoutApplied = true;
            SetChildActive("MobileHudHeader", false);
            SetChildActive("MobileObjectivePanel", false);
            SetChildActive("MoveHintPanel", false);
            SetChildActive("LookHintPanel", false);
            SetChildActive("MobileContextPanel", true);

            ConfigureControlRect("MoveJoystick", new Vector2(0f, 0f), new Vector2(128f, 130f), new Vector2(150f, 150f));
            ConfigureJoystickVisuals();

            ConfigureControlRect("SprintButton", new Vector2(1f, 0f), new Vector2(-214f, 118f), new Vector2(88f, 88f));
            ConfigureButtonVisuals("SprintButton", new Color(0.12f, 0.2f, 0.25f, 0.42f), new Color(0.28f, 0.62f, 0.64f, 0.82f), 18);

            ConfigureControlRect("InteractButton", new Vector2(1f, 0f), new Vector2(-100f, 154f), new Vector2(104f, 104f));
            ConfigureButtonVisuals("InteractButton", new Color(0.08f, 0.3f, 0.25f, 0.5f), new Color(0.25f, 0.74f, 0.64f, 0.86f), 23);

            ConfigureControlRect("ScanButton", new Vector2(1f, 0f), new Vector2(-214f, 228f), new Vector2(92f, 58f));
            ConfigureButtonVisuals("ScanButton", new Color(0.08f, 0.25f, 0.29f, 0.4f), new Color(0.27f, 0.72f, 0.76f, 0.82f), 17);

            ConfigureControlRect("NotebookButton", new Vector2(1f, 0f), new Vector2(-100f, 272f), new Vector2(100f, 58f));
            ConfigureButtonVisuals("NotebookButton", new Color(0.11f, 0.1f, 0.12f, 0.44f), new Color(0.44f, 0.34f, 0.18f, 0.82f), 17);

            ConfigureControlRect("FlashlightButton", new Vector2(1f, 0f), new Vector2(-214f, 328f), new Vector2(92f, 58f));
            ConfigureButtonVisuals("FlashlightButton", new Color(0.14f, 0.13f, 0.08f, 0.42f), new Color(0.96f, 0.92f, 0.72f, 0.82f), 17);

            ConfigureControlRect("MobileContextPanel", new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(430f, 64f));
            ConfigureControlRect("LookArea", new Vector2(1f, 0.5f), new Vector2(-480f, 0f), new Vector2(960f, 1080f));

            // Connect flashlight button to controller
            ConnectFlashlightButton();
        }

        private void ConnectFlashlightButton()
        {
            var flashlight = Object.FindAnyObjectByType<MobilOfl.Gameplay.FlashlightController>();
            if (flashlight == null)
            {
                return;
            }

            var flashBtn = FindControlByName("FlashlightButton");
            if (flashBtn != null)
            {
                var mobileButton = flashBtn.GetComponent<MobileButton>();
                if (mobileButton != null)
                {
                    flashlight.SetMobileButton(mobileButton);
                }
            }
        }

        private Transform FindControlByName(string controlName)
        {
            return transform.Find(controlName);
        }

        private void SetChildActive(string childName, bool active)
        {
            var child = transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private void ConfigureControlRect(string childName, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = transform.Find(childName) as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.gameObject.SetActive(true);
            RuntimeUiFactory.ApplyOneUiRounding(rect.gameObject, Mathf.Min(size.x, size.y) * 0.42f);
            SoftenGraphicEffects(rect.gameObject, 0.18f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var content = rect.Find("Content") as RectTransform;
            if (content != null)
            {
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = size;
            }

            var accent = rect.Find("Accent") as RectTransform;
            if (accent != null)
            {
                accent.gameObject.SetActive(false);
            }
        }

        private void ConfigureJoystickVisuals()
        {
            var root = transform.Find("MoveJoystick");
            if (root != null)
            {
                RuntimeUiFactory.ApplyOneUiRounding(root.gameObject, 58f);
                SoftenGraphicEffects(root.gameObject, 0.12f);
            }

            ConfigureNestedRect("MoveJoystick/PulseRing", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124f, 124f));
            ConfigureNestedRect("MoveJoystick/InnerPlate", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            ConfigureNestedRect("MoveJoystick/Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
            ConfigureNestedRect("MoveJoystick/Handle/CenterDot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
            ApplyNestedRounding("MoveJoystick/PulseRing", 46f);
            ApplyNestedRounding("MoveJoystick/InnerPlate", 38f);
            ApplyNestedRounding("MoveJoystick/Handle", 26f);
            ApplyNestedRounding("MoveJoystick/Handle/CenterDot", 6f);

            var joystick = transform.Find("MoveJoystick")?.GetComponent<MobileJoystick>();
            if (joystick != null)
            {
                joystick.ConfigureMobileVisuals(44f);
            }
        }

        private void ConfigureButtonVisuals(string childName, Color idleColor, Color pressedColor, int fontSize)
        {
            var rect = transform.Find(childName) as RectTransform;
            if (rect == null)
            {
                return;
            }

            var button = rect.GetComponent<MobileButton>();
            if (button != null)
            {
                button.ConfigureMobileVisuals(idleColor, pressedColor);
            }

            var label = rect.Find("Content/Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.fontSize = fontSize;
                label.color = new Color(0.97f, 0.95f, 0.9f, 0.86f);
            }

            var subLabel = rect.Find("Content/SubLabel");
            if (subLabel != null)
            {
                subLabel.gameObject.SetActive(false);
            }

            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.color = idleColor;
            }

            RuntimeUiFactory.ApplyOneUiRounding(rect.gameObject, Mathf.Min(rect.sizeDelta.x, rect.sizeDelta.y) * 0.42f);
            SoftenGraphicEffects(rect.gameObject, 0.14f);
        }

        private void ConfigureNestedRect(string path, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = transform.Find(path) as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void ApplyNestedRounding(string path, float radius)
        {
            var target = transform.Find(path);
            if (target != null)
            {
                RuntimeUiFactory.ApplyOneUiRounding(target.gameObject, radius);
            }
        }

        private static void SoftenGraphicEffects(GameObject target, float maxAlpha)
        {
            var effects = target.GetComponents<Shadow>();
            for (var i = 0; i < effects.Length; i++)
            {
                var color = effects[i].effectColor;
                color.a = Mathf.Min(color.a, maxAlpha);
                effects[i].effectColor = color;
            }
        }

        private void ResolveReferences()
        {
            if (playerInteraction == null || !playerInteraction.isActiveAndEnabled)
            {
                playerInteraction = Object.FindAnyObjectByType<PlayerInteractionController>();
            }

            if (_movementController == null || !_movementController.isActiveAndEnabled)
            {
                _movementController = Object.FindAnyObjectByType<PrototypeFirstPersonController>();
            }

            if (_scanner == null || !_scanner.isActiveAndEnabled)
            {
                _scanner = Object.FindAnyObjectByType<InvestigationScanner>();
            }

            if (_stealthController == null || !_stealthController.isActiveAndEnabled)
            {
                _stealthController = Object.FindAnyObjectByType<PlayerStealthController>();
            }

            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            if (headerSubtitleText == null)
            {
                headerSubtitleText = transform.Find("MobileHudHeader/HeaderSubtitle")?.GetComponent<Text>();
            }

            if (zoneText == null)
            {
                zoneText = transform.Find("MobileHudHeader/ZoneText")?.GetComponent<Text>();
            }

            if (objectiveText == null)
            {
                objectiveText = transform.Find("MobileObjectivePanel/ObjectiveText")?.GetComponent<Text>();
            }

            if (contextText == null)
            {
                contextText = transform.Find("MobileContextPanel/ContextText")?.GetComponent<Text>();
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

        private bool ShouldShowMobileUi()
        {
            if (Application.isMobilePlatform)
            {
                return true;
            }

            if (Application.isEditor && showInEditor)
            {
                return true;
            }

            if (showOnDesktop)
            {
                return true;
            }

            return Input.touchSupported && !Application.isEditor;
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
