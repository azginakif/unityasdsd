using System.Threading.Tasks;
using MobilOfl.UI;
using UnityEngine;

namespace MobilOfl.Online
{
    public class OnlineSessionHud : MonoBehaviour
    {
        [SerializeField] private RelayNetworkBootstrap bootstrap;
        [SerializeField] private bool startExpanded = true;

        private bool _expanded;
        private string _joinCodeInput = string.Empty;
        private string _status = "Offline hazir.";
        private GUIStyle _windowStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _mutedStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _metricStyle;
        private GUIStyle _readyStyle;

        private void Awake()
        {
            _expanded = startExpanded;
        }

        private void OnEnable()
        {
            EnsureBootstrap();

            if (bootstrap != null)
            {
                bootstrap.StatusChanged += HandleStatusChanged;
                bootstrap.JoinCodeChanged += HandleJoinCodeChanged;
                _status = bootstrap.CurrentStatus;
            }
        }

        private void OnDisable()
        {
            if (bootstrap != null)
            {
                bootstrap.StatusChanged -= HandleStatusChanged;
                bootstrap.JoinCodeChanged -= HandleJoinCodeChanged;
            }
        }

        private void OnGUI()
        {
            if (MainMenuHud.IsBlockingGameplay)
            {
                return;
            }

            EnsureStyles();
            DrawWindow();
        }

        private void DrawWindow()
        {
            var width = _expanded ? 450f : 156f;
            var height = _expanded ? 618f : 58f;
            var rect = new Rect(Screen.width - width - 18f, Screen.height - height - 18f, width, height);

            GUILayout.BeginArea(rect, _windowStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("CO-OP", _titleStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_expanded ? "_" : "A", _buttonStyle, GUILayout.Width(40f), GUILayout.Height(28f)))
            {
                _expanded = !_expanded;
            }

            GUILayout.EndHorizontal();

            if (!_expanded)
            {
                GUILayout.EndArea();
                return;
            }

            var networkCaseState = NetworkCaseState.Instance;

            GUILayout.Label("Online oturum hazirligi", _bodyStyle);
            GUILayout.Label(_status, _mutedStyle);
            if (bootstrap != null)
            {
                GUILayout.Label($"Ag katmani: {bootstrap.BackendLabel}", _mutedStyle);
            }

            GUILayout.Space(8f);
            DrawMetrics(networkCaseState);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Offline Host", _buttonStyle, GUILayout.Height(34f)))
            {
                bootstrap?.StartOfflineHost();
            }

            if (GUILayout.Button("Online Host", _buttonStyle, GUILayout.Height(34f)))
            {
                _ = RunBootstrapTask(bootstrap != null ? bootstrap.StartRelayHostAsync() : Task.FromResult(false));
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label("Join Code", _bodyStyle);
            _joinCodeInput = GUILayout.TextField(_joinCodeInput, _textFieldStyle);

            if (GUILayout.Button("Oturuma Katil", _buttonStyle, GUILayout.Height(34f)))
            {
                _ = RunBootstrapTask(bootstrap != null ? bootstrap.JoinRelaySessionAsync(_joinCodeInput) : Task.FromResult(false));
            }

            GUILayout.Space(8f);

            if (bootstrap != null && !string.IsNullOrWhiteSpace(bootstrap.CurrentJoinCode))
            {
                GUILayout.Label($"Aktif kod: {bootstrap.CurrentJoinCode}", _bodyStyle);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Kodu Kopyala", _buttonStyle, GUILayout.Height(28f)))
                {
                    GUIUtility.systemCopyBuffer = bootstrap.CurrentJoinCode;
                }

                if (GUILayout.Button("Yapistir", _buttonStyle, GUILayout.Height(28f)))
                {
                    _joinCodeInput = GUIUtility.systemCopyBuffer;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            if (bootstrap != null)
            {
                GUILayout.BeginVertical(_cardStyle);
                GUILayout.Label("Teknik Not", _bodyStyle);
                GUILayout.Label(bootstrap.BackendUpgradeHint, _mutedStyle);
                GUILayout.EndVertical();
                GUILayout.Space(8f);
            }

            DrawReadyRoster(networkCaseState);
            GUILayout.Space(8f);
            DrawReadyButtons(networkCaseState);
            GUILayout.Space(8f);

            if (GUILayout.Button("Oturumu Kapat", _buttonStyle, GUILayout.Height(30f)))
            {
                bootstrap?.ShutdownSession();
            }

            GUILayout.EndArea();
            ModernGuiTheme.DrawPanelChrome(rect, ModernGuiTheme.AccentColor);
        }

        private void DrawMetrics(NetworkCaseState networkCaseState)
        {
            if (bootstrap == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            DrawMetricCard("MOD", bootstrap.CurrentMode);
            DrawMetricCard("OYUNCU", bootstrap.ConnectedClientCount.ToString());
            DrawMetricCard(
                "HAZIR",
                networkCaseState == null ? "-" : $"{networkCaseState.ReadyPlayerCount}/{networkCaseState.RegisteredPlayerCount}");
            GUILayout.EndHorizontal();
        }

        private void DrawMetricCard(string title, string value)
        {
            GUILayout.BeginVertical(_cardStyle, GUILayout.Height(76f));
            GUILayout.Label(title, _mutedStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, _metricStyle);
            GUILayout.EndVertical();
        }

        private void DrawReadyRoster(NetworkCaseState networkCaseState)
        {
            GUILayout.Label("Takim Durumu", _bodyStyle);

            if (bootstrap == null || !bootstrap.IsOnlineSessionActive || networkCaseState == null)
            {
                GUILayout.Label("Aktif online oturum acildiginda oyuncu listesi burada gorunur.", _mutedStyle);
                return;
            }

            var roster = networkCaseState.GetReadyRoster();
            if (roster.Count == 0)
            {
                GUILayout.Label("Hazirlik bilgisi bekleniyor.", _mutedStyle);
                return;
            }

            for (var i = 0; i < roster.Count; i++)
            {
                var player = roster[i];
                GUILayout.BeginVertical(_cardStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label(player.DisplayName, _bodyStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(player.IsReady ? "HAZIR" : "BEKLIYOR", player.IsReady ? _readyStyle : _mutedStyle);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(4f);
            GUILayout.Label(
                networkCaseState.AreAllRegisteredPlayersReady
                    ? "Tum oyuncular hazir. Host oyunu ilerletebilir."
                    : "Tum ekip hazir olmadan oturum daginik kalir.",
                networkCaseState.AreAllRegisteredPlayersReady ? _readyStyle : _mutedStyle);
        }

        private void DrawReadyButtons(NetworkCaseState networkCaseState)
        {
            if (bootstrap == null || !bootstrap.IsOnlineSessionActive || networkCaseState == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();

            GUI.enabled = !networkCaseState.GetLocalReadyState();
            if (GUILayout.Button("Hazirim", _buttonStyle, GUILayout.Height(34f)))
            {
                networkCaseState.RequestSetReady(true);
            }

            GUI.enabled = networkCaseState.GetLocalReadyState();
            if (GUILayout.Button("Beklemeye Al", _buttonStyle, GUILayout.Height(34f)))
            {
                networkCaseState.RequestSetReady(false);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private async Task RunBootstrapTask(Task<bool> task)
        {
            await task;
        }

        private void HandleStatusChanged(string status)
        {
            _status = status;
        }

        private void HandleJoinCodeChanged(string joinCode)
        {
            if (!string.IsNullOrWhiteSpace(joinCode))
            {
                _joinCodeInput = joinCode;
            }
        }

        private void EnsureBootstrap()
        {
            if (bootstrap == null)
            {
                bootstrap = Object.FindFirstObjectByType<RelayNetworkBootstrap>();
            }
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            _windowStyle = ModernGuiTheme.CreatePanelStyle(new RectOffset(16, 16, 14, 14));
            _cardStyle = ModernGuiTheme.CreateSoftPanelStyle(new RectOffset(12, 12, 10, 10));
            _titleStyle = ModernGuiTheme.CreateLabelStyle(22, true, false);
            _bodyStyle = ModernGuiTheme.CreateLabelStyle(14, false, false);
            _mutedStyle = ModernGuiTheme.CreateLabelStyle(12, false, false, ModernGuiTheme.MutedTextColor);
            _metricStyle = ModernGuiTheme.CreateLabelStyle(24, true, true);
            _readyStyle = ModernGuiTheme.CreateLabelStyle(13, true, false, ModernGuiTheme.AccentColor);
            _buttonStyle = ModernGuiTheme.CreateButtonStyle(14);
            _textFieldStyle = ModernGuiTheme.CreateTextFieldStyle(15);
        }
    }
}


