using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.UI
{
    public class SchoolMinimapHud : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Vector2 worldMin = new Vector2(-22f, -10f);
        [SerializeField] private Vector2 worldMax = new Vector2(22f, 30f);

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _mapLabelStyle;

        private void Update()
        {
            ResolvePlayerTarget();
        }

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            ResolvePlayerTarget();
            if (playerTarget == null)
            {
                return;
            }

            EnsureStyles();

            var rect = new Rect(Screen.width - 260f, 156f, 242f, 242f);
            GUILayout.BeginArea(rect, _panelStyle);
            GUILayout.Label("KAMPUS HARITASI", _titleStyle);
            GUILayout.Label(SchoolLocationUtility.GetZoneTitle(playerTarget.position), _subtitleStyle);
            GUILayout.EndArea();

            var mapRect = new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, rect.height - 68f);
            DrawMap(mapRect);
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentWarmColor);
        }

        private void DrawMap(Rect mapRect)
        {
            ModernGuiTheme.DrawRect(mapRect, new Color(0.06f, 0.09f, 0.12f, 0.96f));
            DrawGrid(mapRect);
            DrawZone(mapRect, new Vector2(0f, 4f), new Vector2(20f, 24f), new Color(0.2f, 0.26f, 0.3f, 0.85f), "ANA BLOK");
            DrawZone(mapRect, new Vector2(0f, 23f), new Vector2(18f, 10f), new Color(0.16f, 0.3f, 0.22f, 0.82f), "AVLU");
            DrawZone(mapRect, new Vector2(-15f, 4f), new Vector2(10f, 16f), new Color(0.18f, 0.22f, 0.34f, 0.82f), "LAB");
            DrawZone(mapRect, new Vector2(15f, 4f), new Vector2(10f, 16f), new Color(0.28f, 0.22f, 0.16f, 0.82f), "SPOR");
            DrawMarkers(mapRect);
            DrawPlayer(mapRect);
        }

        private void DrawGrid(Rect mapRect)
        {
            const int gridCount = 6;
            for (var i = 1; i < gridCount; i++)
            {
                var horizontalY = Mathf.Lerp(mapRect.yMin, mapRect.yMax, i / (float)gridCount);
                var verticalX = Mathf.Lerp(mapRect.xMin, mapRect.xMax, i / (float)gridCount);
                ModernGuiTheme.DrawRect(new Rect(mapRect.xMin, horizontalY, mapRect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
                ModernGuiTheme.DrawRect(new Rect(verticalX, mapRect.yMin, 1f, mapRect.height), new Color(1f, 1f, 1f, 0.05f));
            }
        }

        private void DrawZone(Rect mapRect, Vector2 center, Vector2 size, Color color, string label)
        {
            var min = WorldToMap(mapRect, new Vector3(center.x - size.x * 0.5f, 0f, center.y - size.y * 0.5f));
            var max = WorldToMap(mapRect, new Vector3(center.x + size.x * 0.5f, 0f, center.y + size.y * 0.5f));
            var rect = Rect.MinMaxRect(min.x, max.y, max.x, min.y);
            ModernGuiTheme.DrawRect(rect, color);
            GUI.Label(rect, label, _mapLabelStyle);
        }

        private void DrawMarkers(Rect mapRect)
        {
            var evidenceList = Object.FindObjectsByType<EvidenceInteractable>(FindObjectsInactive.Exclude);
            for (var i = 0; i < evidenceList.Length; i++)
            {
                var evidence = evidenceList[i];
                if (evidence == null || !evidence.IsMarkerVisible)
                {
                    continue;
                }

                DrawMarker(mapRect, evidence.transform.position, evidence.MarkerColor, 7f);
            }

            var npcList = Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Exclude);
            for (var i = 0; i < npcList.Length; i++)
            {
                var npc = npcList[i];
                if (npc == null || !npc.IsMarkerVisible)
                {
                    continue;
                }

                DrawMarker(mapRect, npc.transform.position, npc.MarkerColor, 8f);
            }
        }

        private void DrawPlayer(Rect mapRect)
        {
            var playerPoint = WorldToMap(mapRect, playerTarget.position);
            DrawPoint(playerPoint, ModernGuiTheme.AccentWarmColor, 10f);

            var forward = playerTarget.forward;
            var direction = new Vector2(forward.x, -forward.z).normalized;
            for (var i = 1; i <= 3; i++)
            {
                DrawPoint(playerPoint + direction * (6f * i), new Color(1f, 0.85f, 0.35f, 0.9f - i * 0.18f), 4f);
            }
        }

        private void DrawMarker(Rect mapRect, Vector3 worldPosition, Color color, float size)
        {
            if (worldPosition.x < worldMin.x || worldPosition.x > worldMax.x || worldPosition.z < worldMin.y || worldPosition.z > worldMax.y)
            {
                return;
            }

            DrawPoint(WorldToMap(mapRect, worldPosition), color, size);
        }

        private void DrawPoint(Vector2 point, Color color, float size)
        {
            ModernGuiTheme.DrawRect(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), color);
        }

        private Vector2 WorldToMap(Rect mapRect, Vector3 worldPosition)
        {
            var normalizedX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
            var normalizedY = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, normalizedX),
                Mathf.Lerp(mapRect.yMax, mapRect.yMin, normalizedY));
        }

                private string GetSectorLabel(Vector3 position)
        {
            return SchoolLocationUtility.GetZoneTitle(position);
        }
        private void ResolvePlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            var avatars = Object.FindObjectsByType<NetworkPlayerAvatar>(FindObjectsInactive.Exclude);
            for (var i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].IsOwner)
                {
                    playerTarget = avatars[i].transform;
                    return;
                }
            }

            var player = GameObject.Find("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = ModernGuiTheme.CreatePanelStyle(new RectOffset(12, 12, 10, 10));
            _titleStyle = ModernGuiTheme.CreateLabelStyle(16, true, false);
            _subtitleStyle = ModernGuiTheme.CreateLabelStyle(12, false, false, ModernGuiTheme.MutedTextColor);
            _mapLabelStyle = ModernGuiTheme.CreateLabelStyle(11, true, true, new Color(1f, 1f, 1f, 0.8f));
        }
    }
}

