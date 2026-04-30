using MobilOfl.Gameplay;
using MobilOfl.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MobilOfl.Online
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayerAvatar : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString64Bytes> _displayName = new NetworkVariable<FixedString64Bytes>();

        [SerializeField] private PrototypeFirstPersonController movementController;
        [SerializeField] private PlayerInteractionController interactionController;
        [SerializeField] private InvestigationScanner scanner;
        [SerializeField] private PlayerStealthController stealthController;
        [SerializeField] private TeamPingController pingController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private Renderer[] localBodyRenderers;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName.Value.ToString())
            ? $"Dedektif {OwnerClientId + 1}"
            : _displayName.Value.ToString();
        public Vector3 MarkerWorldPosition => transform.position + Vector3.up * 2.25f;
        public bool ShouldShowWorldLabel => IsSpawned && !IsOwner;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ApplyOwnershipState();

            if (IsOwner)
            {
                SubmitDisplayName(PlayerProfileSettings.LoadPlayerName());
            }
        }

        private void Reset()
        {
            movementController = GetComponent<PrototypeFirstPersonController>();
            interactionController = GetComponent<PlayerInteractionController>();
            scanner = GetComponent<InvestigationScanner>();
            stealthController = GetComponent<PlayerStealthController>();
            pingController = GetComponent<TeamPingController>();
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

            if (scanner != null)
            {
                scanner.enabled = ownerActive;
            }

            if (stealthController != null)
            {
                stealthController.enabled = ownerActive;
            }

            if (pingController != null)
            {
                pingController.enabled = ownerActive;
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

        public void SubmitDisplayName(string playerName)
        {
            var sanitized = PlayerProfileSettings.Sanitize(playerName);
            if (IsServer)
            {
                _displayName.Value = new FixedString64Bytes(sanitized);
                return;
            }

            SubmitDisplayNameServerRpc(sanitized);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitDisplayNameServerRpc(string playerName)
        {
            _displayName.Value = new FixedString64Bytes(PlayerProfileSettings.Sanitize(playerName));
        }
    }
}
