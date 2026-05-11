using System.Text;
using MobilOfl.Gameplay;
using UnityEngine;

namespace MobilOfl.UI
{
    public class CaseDebugHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 420f, 520f);

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private Vector2 _scroll;

        private void OnGUI()
        {
            if (CaseSessionManager.Instance == null)
            {
                return;
            }

            EnsureStyles();

            GUILayout.BeginArea(panelRect, _panelStyle);
            GUILayout.Label("MOBIL OFL DEBUG", _titleStyle);

            var activeCase = CaseSessionManager.Instance.ActiveCase;
            GUILayout.Label(activeCase != null ? activeCase.CaseTitle : "Aktif vaka yok", _labelStyle);

            if (playerInteraction != null)
            {
                var prompt = playerInteraction.CurrentInteractable != null
                    ? $"Bakilan nesne: {playerInteraction.CurrentInteractable.PromptText}"
                    : "Bakilan nesne: yok";
                GUILayout.Label(prompt, _labelStyle);

                if (GUILayout.Button("Etkilesim"))
                {
                    playerInteraction.TryInteractFromUi();
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("Toplanan Deliller", _labelStyle);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220f));
            var builder = new StringBuilder();

            foreach (var evidenceId in CaseSessionManager.Instance.CollectedEvidenceIds)
            {
                var evidence = CaseSessionManager.Instance.GetEvidence(evidenceId);
                if (evidence == null)
                {
                    continue;
                }

                builder.Append("- ");
                builder.Append(evidence.Title);
                builder.Append(" [");
                builder.Append(evidence.Category);
                builder.AppendLine("]");
            }

            if (builder.Length == 0)
            {
                builder.Append("Henuz delil toplanmadi.");
            }

            GUILayout.TextArea(builder.ToString(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            if (activeCase != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Supheliyi Sucla", _labelStyle);

                foreach (var suspect in activeCase.Suspects)
                {
                    var canAccuse = CaseSessionManager.Instance.CanAccuse(suspect.Id);
                    GUI.enabled = canAccuse;

                    if (GUILayout.Button(suspect.DisplayName))
                    {
                        CaseSessionManager.Instance.TryResolveCase(
                            suspect.Id,
                            activeCase.CulpritMotive,
                            activeCase.CulpritTimeline,
                            out var result);
                        Debug.Log(result);
                    }
                }

                GUI.enabled = true;
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
        }
    }
}
