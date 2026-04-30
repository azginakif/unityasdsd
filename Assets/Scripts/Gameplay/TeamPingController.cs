using MobilOfl.Online;
using MobilOfl.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MobilOfl.Gameplay
{
    public class TeamPingController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInteractionController playerInteraction;
        [SerializeField] private Key pingKey = Key.R;
        [SerializeField] private float pingDistance = 32f;

        private void Update()
        {
            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            var networkCaseState = NetworkCaseState.Instance;
            if (networkCaseState == null || !networkCaseState.IsOnlineSessionActive || !networkCaseState.IsGameplayPhase)
            {
                return;
            }

            if (Keyboard.current == null || !Keyboard.current[pingKey].wasPressedThisFrame)
            {
                return;
            }

            ResolveReferences();
            SubmitPing(networkCaseState);
        }

        private void SubmitPing(NetworkCaseState networkCaseState)
        {
            if (playerInteraction != null && playerInteraction.CurrentInteractable != null)
            {
                networkCaseState.RequestSharedPing(
                    playerInteraction.CurrentInteractable.transform.position,
                    playerInteraction.CurrentInteractable.PromptText);
                return;
            }

            if (playerCamera == null)
            {
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, pingDistance, ~0, QueryTriggerInteraction.Collide))
            {
                var zone = SchoolLocationUtility.GetZoneTitle(hit.point);
                networkCaseState.RequestSharedPing(hit.point, zone);
            }
        }

        private void ResolveReferences()
        {
            if (playerInteraction == null || !playerInteraction.isActiveAndEnabled)
            {
                playerInteraction = GetComponent<PlayerInteractionController>();
            }

            if (playerCamera == null)
            {
                if (playerInteraction != null && playerInteraction.PlayerCamera != null)
                {
                    playerCamera = playerInteraction.PlayerCamera;
                }
                else
                {
                    playerCamera = GetComponentInChildren<Camera>();
                }
            }
        }
    }
}
