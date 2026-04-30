using UnityEngine;

namespace MobilOfl.Gameplay
{
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField] private string promptText = "Etkilesim";

        public string PromptText => promptText;
        public virtual bool RequiresHold => false;
        public virtual float HoldDuration => 0f;

        public virtual bool CanMaintainHold(GameObject interactor)
        {
            return true;
        }

        public abstract bool TryInteract(GameObject interactor);
    }
}
