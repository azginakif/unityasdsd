using MobilOfl.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace MobilOfl.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerAvatar : NetworkBehaviour
    {
        [SerializeField] private PrototypeFirstPersonController movementController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private Renderer[] localBodyRenderers;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ApplyOwnershipState();
        }

        private void Reset()
        {
            movementController = GetComponent<PrototypeFirstPersonController>();
            interactionController = GetComponent<PlayerInteractionController>();
            playerCamera = GetComponentInChildren<Camera>(true);

            if (playerCamera != null)
            {
                audioListener = playerCamera.GetComponent<AudioListener>();
            }
        }

        private void ApplyOwnershipState()
        {
            var ownerActive = IsOwner;

            if (movementController != null)
            {
                movementController.enabled = ownerActive;
            }

            if (interactionController != null)
            {
                interactionController.enabled = ownerActive;
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = ownerActive;
                playerCamera.tag = ownerActive ? "MainCamera" : "Untagged";
            }

            if (audioListener != null)
            {
                audioListener.enabled = ownerActive;
            }

            if (localBodyRenderers == null)
            {
                return;
            }

            foreach (var bodyRenderer in localBodyRenderers)
            {
                if (bodyRenderer == null)
                {
                    continue;
                }

                bodyRenderer.enabled = !ownerActive;
            }
        }
    }
}