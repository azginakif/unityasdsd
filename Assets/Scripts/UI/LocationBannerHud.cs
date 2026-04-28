using MobilOfl.Gameplay;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.UI
{
    public class LocationBannerHud : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float visibleDuration = 2.4f;

        private string _currentTitle = string.Empty;
        private string _currentSubtitle = string.Empty;
        private float _visibleUntil;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;

        private void Update()
        {
            ResolvePlayerTarget();
            if (playerTarget == null)
            {
                return;
            }

            var title = SchoolLocationUtility.GetZoneTitle(playerTarget.position);
            if (title == _currentTitle)
            {
                return;
            }

            _currentTitle = title;
            _currentSubtitle = SchoolLocationUtility.GetZoneSubtitle(playerTarget.position);
            _visibleUntil = Time.time + visibleDuration;
        }

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen || string.IsNullOrWhiteSpace(_currentTitle))
            {
                return;
            }

            var timeLeft = _visibleUntil - Time.time;
            if (timeLeft <= 0f)
            {
                return;
            }

            EnsureStyles();

            var alpha = Mathf.Clamp01(Mathf.Min(1f, timeLeft / 0.4f));
            var width = Mathf.Min(520f, Screen.width - 60f);
            var rect = new Rect((Screen.width - width) * 0.5f, 134f, width, 86f);
            var shadowRect = new Rect(rect.x, rect.y + 5f, rect.width, rect.height);

            ModernGuiTheme.DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.18f * alpha));
            ModernGuiTheme.DrawRect(rect, new Color(ModernGuiTheme.PanelColor.r, ModernGuiTheme.PanelColor.g, ModernGuiTheme.PanelColor.b, 0.94f * alpha));
            ModernGuiTheme.DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), new Color(ModernGuiTheme.AccentWarmColor.r, ModernGuiTheme.AccentWarmColor.g, ModernGuiTheme.AccentWarmColor.b, alpha));

            GUILayout.BeginArea(rect, _panelStyle);
            GUILayout.Label(_currentTitle, _titleStyle);
            GUILayout.Label(_currentSubtitle, _subtitleStyle);
            GUILayout.EndArea();

            ModernGuiTheme.DrawPanelChrome(rect, new Color(ModernGuiTheme.AccentWarmColor.r, ModernGuiTheme.AccentWarmColor.g, ModernGuiTheme.AccentWarmColor.b, alpha));
        }

        private void ResolvePlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            var avatars = Object.FindObjectsByType<NetworkPlayerAvatar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

            _panelStyle = ModernGuiTheme.CreatePanelStyle(new RectOffset(16, 16, 10, 10));
            _titleStyle = ModernGuiTheme.CreateLabelStyle(24, true, true);
            _subtitleStyle = ModernGuiTheme.CreateLabelStyle(13, false, true, ModernGuiTheme.MutedTextColor);
        }
    }
}
