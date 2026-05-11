using UnityEngine;

namespace MobilOfl.Gameplay
{
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField] private string promptText = "Etkilesim";

        public string PromptText => promptText;
        public virtual bool RequiresHold => false;
        public virtual float HoldDuration => 0f;

        public virtual float GetHoldDuration(GameObject interactor)
        {
            return HoldDuration;
        }

        public void ConfigurePrompt(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                promptText = text.Trim();
            }
        }

        public virtual bool CanMaintainHold(GameObject interactor)
        {
            return true;
        }

        public abstract bool TryInteract(GameObject interactor);
    }
}
