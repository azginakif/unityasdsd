using MobilOfl.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiReticleHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private float size = 18f;

        private RectTransform _root;
        private Image[] _reticleLines;
        private Text _promptText;
        private CanvasGroup _promptGroup;
        private bool _built;

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            BuildIfNeeded();
            DisableLegacy();
        }

        private void Update()
        {
            BuildIfNeeded();
            DisableLegacy();
            ResolveInteraction();
            Refresh();
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            var canvasTransform = transform.Find("UguiReticleCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiReticleCanvas", transform);
            }

            var canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 72;

            var scaler = canvasTransform.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RuntimeUiFactory.Stretch(canvasTransform);
            RuntimeUiFactory.ClearChildren(canvasTransform);
            _root = canvasTransform;

            var center = RuntimeUiFactory.CreateUiRoot("ReticleCenter", _root);
            center.anchorMin = new Vector2(0.5f, 0.5f);
            center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.sizeDelta = new Vector2(1f, 1f);

            _reticleLines = new Image[4];
            _reticleLines[0] = CreateLine(center, "Left", new Vector2(-size - 12f, 0f), new Vector2(size, 2f));
            _reticleLines[1] = CreateLine(center, "Right", new Vector2(12f, 0f), new Vector2(size, 2f));
            _reticleLines[2] = CreateLine(center, "Top", new Vector2(0f, size + 12f), new Vector2(2f, size));
            _reticleLines[3] = CreateLine(center, "Bottom", new Vector2(0f, -12f), new Vector2(2f, size));

            var promptCard = RuntimeUiFactory.CreateCard("PromptCard", _root, new Color(0.07f, 0.09f, 0.12f, 0.96f), ModernGuiTheme.AccentColor);
            promptCard.anchorMin = new Vector2(0.5f, 0.5f);
            promptCard.anchorMax = new Vector2(0.5f, 0.5f);
            promptCard.pivot = new Vector2(0.5f, 0f);
            promptCard.anchoredPosition = new Vector2(0f, -84f);
            promptCard.sizeDelta = new Vector2(360f, 54f);
            RuntimeUiFactory.AddVerticalLayout(promptCard, 0f, new RectOffset(12, 12, 12, 10), false);
            _promptText = RuntimeUiFactory.CreateText("PromptText", promptCard, string.Empty, 17, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptGroup = promptCard.gameObject.GetComponent<CanvasGroup>();
            if (_promptGroup == null)
            {
                _promptGroup = promptCard.gameObject.AddComponent<CanvasGroup>();
            }

            _built = true;
        }

        private Image CreateLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = RuntimeUiFactory.CreateUiRoot(name, parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return RuntimeUiFactory.AddImage(rect.gameObject, new Color(1f, 1f, 1f, 0.55f));
        }

        private void Refresh()
        {
            if (_root == null)
            {
                return;
            }

            var hidden = MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen || (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved);
            _root.gameObject.SetActive(!hidden);
            if (hidden)
            {
                return;
            }

            var hasTarget = playerInteraction != null && playerInteraction.CurrentInteractable != null;
            var color = hasTarget ? new Color(0.2f, 0.95f, 0.65f, 0.95f) : new Color(1f, 1f, 1f, 0.55f);
            for (var i = 0; i < _reticleLines.Length; i++)
            {
                _reticleLines[i].color = color;
            }

            _promptGroup.alpha = hasTarget ? 1f : 0f;
            _promptText.text = hasTarget ? playerInteraction.CurrentInteractable.PromptText : string.Empty;
        }

        private void ResolveInteraction()
        {
            if (playerInteraction != null && playerInteraction.isActiveAndEnabled)
            {
                return;
            }

            var candidates = Object.FindObjectsByType<PlayerInteractionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].isActiveAndEnabled)
                {
                    playerInteraction = candidates[i];
                    return;
                }
            }
        }

        private void DisableLegacy()
        {
            var legacy = Object.FindFirstObjectByType<FocusReticleHud>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }
        }
    }
}
