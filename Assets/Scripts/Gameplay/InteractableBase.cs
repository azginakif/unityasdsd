using UnityEngine;

namespace MobilOfl.Gameplay
{
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField] private string promptText = "Etkilesim";

        public string PromptText => promptText;

        public abstract bool TryInteract(GameObject interactor);
    }
}
