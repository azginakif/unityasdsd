using MobilOfl.Gameplay;
using UnityEngine;

namespace MobilOfl.UI
{
    public class WorldMarkerHud : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float maxDistance = 28f;

        private GUIStyle _markerStyle;

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            EnsureCamera();
            EnsureStyles();

            if (targetCamera == null)
            {
                return;
            }

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

            var screen = targetCamera.WorldToScreenPoint(worldPosition);
            var rect = new Rect(screen.x - 90f, Screen.height - screen.y - 16f, 180f, 30f);

            ModernGuiTheme.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(0.05f, 0.08f, 0.11f, 0.84f));
            ModernGuiTheme.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), color);
            GUI.Label(rect, $"{label}  [{distance:0}m]", _markerStyle);
        }

        private void EnsureCamera()
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

        private void EnsureStyles()
        {
            if (_markerStyle != null)
            {
                return;
            }

            _markerStyle = ModernGuiTheme.CreateLabelStyle(13, true, true, ModernGuiTheme.TextColor);
        }
    }
}