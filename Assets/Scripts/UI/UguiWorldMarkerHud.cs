using MobilOfl.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UguiWorldMarkerHud : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float maxDistance = 28f;

        private RectTransform _root;
        private bool _built;
        private float _nextRefreshAt;

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
            ResolveCamera();
            if (_root != null)
            {
                _root.gameObject.SetActive(!MainMenuHud.IsBlockingGameplay && !CaseNotebookHud.IsAnyNotebookOpen);
            }

            if (Time.unscaledTime >= _nextRefreshAt)
            {
                Refresh();
                _nextRefreshAt = Time.unscaledTime + 0.12f;
            }
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            var canvasTransform = transform.Find("UguiWorldMarkerCanvas") as RectTransform;
            if (canvasTransform == null)
            {
                canvasTransform = RuntimeUiFactory.CreateUiRoot("UguiWorldMarkerCanvas", transform);
            }

            var canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 73;

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
            _built = true;
        }

        private void Refresh()
        {
            if (_root == null || targetCamera == null)
            {
                return;
            }

            RuntimeUiFactory.ClearChildren(_root);
            DrawEvidenceMarkers();
            DrawNpcMarkers();
        }

        private void DrawEvidenceMarkers()
        {
            var evidenceList = Object.FindObjectsByType<EvidenceInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < evidenceList.Length; i++)
            {
                var evidence = evidenceList[i];
                if (evidence == null || !evidence.IsMarkerVisible)
                {
                    continue;
                }

                DrawMarker(evidence.transform.position + Vector3.up * 1.1f, evidence.MarkerLabel, evidence.MarkerColor);
            }
        }

        private void DrawNpcMarkers()
        {
            var npcList = Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < npcList.Length; i++)
            {
                var npc = npcList[i];
                if (npc == null || !npc.IsMarkerVisible)
                {
                    continue;
                }

                DrawMarker(npc.transform.position + Vector3.up * 2.1f, npc.MarkerLabel, npc.MarkerColor);
            }
        }

        private void DrawMarker(Vector3 worldPosition, string label, Color color)
        {
            var cameraPosition = targetCamera.transform.position;
            var distance = Vector3.Distance(cameraPosition, worldPosition);
            if (distance > maxDistance)
            {
                return;
            }

            var viewport = targetCamera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                return;
            }

            var anchor = new Vector2(viewport.x, viewport.y);
            var card = RuntimeUiFactory.CreateCard("Marker", _root, new Color(0.05f, 0.08f, 0.11f, 0.84f), color);
            card.anchorMin = anchor;
            card.anchorMax = anchor;
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0f, 0f);
            card.sizeDelta = new Vector2(180f, 30f);
            RuntimeUiFactory.AddVerticalLayout(card, 0f, new RectOffset(10, 10, 8, 6), false);
            var text = RuntimeUiFactory.CreateText("Label", card, $"{label}  [{distance:0}m]", 13, ModernGuiTheme.TextColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                return;
            }

            targetCamera = Camera.main;
            if (targetCamera != null)
            {
                return;
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].isActiveAndEnabled)
                {
                    targetCamera = cameras[i];
                    return;
                }
            }
        }

        private void DisableLegacy()
        {
            var legacy = Object.FindFirstObjectByType<WorldMarkerHud>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }
        }
    }
}
