using MobilOfl.UI;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class InvestigationDeskInteractable : InteractableBase
    {
        [SerializeField] private string reviewMessage = "Vaka masasi acildi. Delilleri karsilastir ve suphelileri daralt.";
        [SerializeField] private string accuseMessage = "Yeterli delil var. Supheliler sekmesinden son suclamayi yapabilirsin.";

        public override bool TryInteract(GameObject interactor)
        {
            var notebook = Object.FindAnyObjectByType<CaseNotebookHud>();
            var session = CaseSessionManager.Instance;
            if (notebook == null || session == null)
            {
                return false;
            }

            if (session.HasAnyAccusableSuspect())
            {
                notebook.OpenSuspectsNotebook();
                session.PublishMessage(accuseMessage);
                return true;
            }

            notebook.OpenNotebook();
            session.PublishMessage(reviewMessage);
            return true;
        }
    }
}
