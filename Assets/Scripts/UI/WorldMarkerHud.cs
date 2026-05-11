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
            var markerText = BuildMarkerText(label, distance);
            var width = Mathf.Clamp(markerText.Length * 8.5f + 24f, 156f, 260f);
            var x = Mathf.Clamp(screen.x - width * 0.5f, 12f, Screen.width - width - 12f);
            var y = Mathf.Clamp(Screen.height - screen.y - 16f, 12f, Screen.height - 42f);
            var rect = new Rect(x, y, width, 30f);

            ModernGuiTheme.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(0.05f, 0.08f, 0.11f, 0.84f));
            ModernGuiTheme.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), color);
            GUI.Label(rect, markerText, _markerStyle);
        }

        private static string BuildMarkerText(string label, float distance)
        {
            var safeLabel = string.IsNullOrWhiteSpace(label) ? "Hedef" : label.Trim();
            if (safeLabel.Length > 22)
            {
                safeLabel = safeLabel.Substring(0, 19).TrimEnd() + "...";
            }

            return $"{safeLabel} [{distance:0}m]";
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
            _markerStyle.wordWrap = false;
            _markerStyle.clipping = TextClipping.Clip;
        }
    }
}
