using MobilOfl.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobilOfl.UI
{
    public class CaseResultHud : MonoBehaviour
    {
        [SerializeField] private float wrongAccusationMessageDuration = 3f;

        private bool _hasResult;
        private bool _success;
        private string _resultMessage;
        private float _wrongMessageUntil;
        private CaseSessionManager _subscribedSession;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _warningStyle;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribedSession != null)
            {
                _subscribedSession.CaseResolved -= HandleCaseResolved;
                _subscribedSession = null;
            }
        }

        private void Update()
        {
            TrySubscribe();
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (_hasResult && _success)
            {
                DrawSuccessPanel();
                return;
            }

            if (_hasResult && Time.time <= _wrongMessageUntil)
            {
                DrawWrongAccusationWarning();
            }
        }

        private void TrySubscribe()
        {
            if (_subscribedSession != null || CaseSessionManager.Instance == null)
            {
                return;
            }

            _subscribedSession = CaseSessionManager.Instance;
            _subscribedSession.CaseResolved += HandleCaseResolved;
        }

        private void HandleCaseResolved(bool success, string resultMessage)
        {
            _hasResult = true;
            _success = success;
            _resultMessage = resultMessage;

            if (!success)
            {
                _wrongMessageUntil = Time.time + wrongAccusationMessageDuration;
            }
        }

        private void DrawSuccessPanel()
        {
            var session = CaseSessionManager.Instance;
            var width = Mathf.Min(760f, Screen.width - 40f);
            var height = Mathf.Min(470f, Screen.height - 40f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUILayout.BeginArea(rect, _windowStyle);
            GUILayout.Label("VAKA COZULDU", _titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(_resultMessage, _bodyStyle);
            GUILayout.Space(14f);

            if (session != null)
            {
                GUILayout.Label($"Toplanan delil: {session.CollectedEvidenceIds.Count}", _bodyStyle);
                GUILayout.Label($"Kritik delil: {session.CollectedCriticalEvidenceCount}/{session.TotalCriticalEvidenceCount}", _bodyStyle);
                GUILayout.Label($"Gorusulen NPC: {session.InterviewedNpcCount}", _bodyStyle);
                GUILayout.Label($"Gecen sure: {FormatElapsedTime(session.ElapsedCaseTimeSeconds)}", _bodyStyle);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Vakayi Yeniden Baslat", GUILayout.Width(220f), GUILayout.Height(42f)))
            {
                if (session != null)
                {
                    session.RestartCurrentCase();
                }

                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawWrongAccusationWarning()
        {
            var session = CaseSessionManager.Instance;
            var width = Mathf.Min(560f, Screen.width - 36f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.18f, width, 104f);
            var warningText = _resultMessage;

            if (session != null)
            {
                warningText += "\n" + session.GetRecommendedNextStep();
            }

            GUI.Label(rect, warningText, _warningStyle);
        }

        private static string FormatElapsedTime(float timeSeconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            _windowStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(24, 24, 22, 22)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
    }
}
