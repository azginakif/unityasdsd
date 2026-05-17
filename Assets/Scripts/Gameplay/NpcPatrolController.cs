using MobilOfl.Online;
using MobilOfl.UI;
using MobilOfl.Visuals;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class NpcPatrolController : MonoBehaviour
    {
        [SerializeField] private NpcInteractable npcInteractable;
        [SerializeField] private bool patrolEnabled = true;
        [SerializeField] private Vector3[] patrolOffsets =
        {
            Vector3.zero,
            new Vector3(0f, 0f, 1.8f),
            new Vector3(0f, 0f, -1.8f)
        };
        [SerializeField] private float moveSpeed = 1.25f;
        [SerializeField] private float turnSpeed = 5.4f;
        [SerializeField] private float waitDuration = 1.15f;
        [SerializeField] private float arrivalDistance = 0.18f;
        [SerializeField] private float viewDistance = 6.8f;
        [SerializeField] private float viewAngle = 62f;
        [SerializeField] private float sightPressurePerSecond = 0.52f;
        [SerializeField] private float eyeHeight = 1.35f;
        [SerializeField] private float collisionRadius = 0.32f;
        [SerializeField] private float collisionHeight = 1.7f;
        [SerializeField] private float obstacleProbeDistance = 0.24f;
        [SerializeField] private LayerMask occlusionMask = ~0;
        [SerializeField] private LayerMask movementBlockMask = ~0;

        private PlayerStealthController _playerStealth;
        private bool _footFixEnsured;
        private Vector3 _anchorPosition;
        private int _currentPatrolIndex;
        private float _waitUntil;

        private void Awake()
        {
            _anchorPosition = transform.position;
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();

            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            if (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved)
            {
                return;
            }

            if (NetworkCaseState.Instance != null &&
                NetworkCaseState.Instance.IsOnlineSessionActive &&
                !NetworkCaseState.Instance.IsGameplayPhase)
            {
                return;
            }

            UpdatePatrol();
            UpdateSightPressure();
        }

        private void UpdatePatrol()
        {
            if (npcInteractable != null && npcInteractable.IsInteractionFocused)
            {
                FaceFocusTarget();
                _waitUntil = Time.time + waitDuration;
                return;
            }

            if (!patrolEnabled || patrolOffsets == null || patrolOffsets.Length <= 1)
            {
                return;
            }

            if (Time.time < _waitUntil)
            {
                return;
            }

            var targetPosition = _anchorPosition + patrolOffsets[_currentPatrolIndex];
            var toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arrivalDistance)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolOffsets.Length;
                _waitUntil = Time.time + waitDuration;
                return;
            }

            var moveStep = Mathf.Min(moveSpeed * Time.deltaTime, toTarget.magnitude);
            var moveDirection = toTarget.normalized;
            if (!CanMove(moveDirection, moveStep + obstacleProbeDistance))
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolOffsets.Length;
                _waitUntil = Time.time + waitDuration;
                return;
            }

            transform.position += moveDirection * moveStep;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }

        private void UpdateSightPressure()
        {
            if (_playerStealth == null || !_playerStealth.isActiveAndEnabled)
            {
                return;
            }

            var playerPosition = _playerStealth.transform.position + Vector3.up * 1f;
            var eyePosition = transform.position + Vector3.up * eyeHeight;
            var toPlayer = playerPosition - eyePosition;
            var distance = toPlayer.magnitude;
            if (distance > viewDistance || distance <= 0.05f)
            {
                return;
            }

            var direction = toPlayer / distance;
            if (Vector3.Angle(transform.forward, direction) > viewAngle * 0.5f)
            {
                return;
            }

            if (Physics.Raycast(eyePosition, direction, out var hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                if (!(hit.transform.IsChildOf(transform) || hit.transform == transform) &&
                    !hit.transform.IsChildOf(_playerStealth.transform) &&
                    hit.transform != _playerStealth.transform)
                {
                    return;
                }
            }

            _playerStealth.RegisterNpcSightPressure(sightPressurePerSecond * Time.deltaTime, npcInteractable != null ? npcInteractable.NpcDisplayName : name);
        }

        private void ResolveReferences()
        {
            if (npcInteractable == null || !npcInteractable.isActiveAndEnabled)
            {
                npcInteractable = GetComponent<NpcInteractable>();
            }

            if (_playerStealth == null || !_playerStealth.isActiveAndEnabled)
            {
                _playerStealth = Object.FindAnyObjectByType<PlayerStealthController>();
            }

            EnsureFootAlignmentFix();
        }

        private bool CanMove(Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return true;
            }

            var radius = Mathf.Max(0.12f, collisionRadius);
            var height = Mathf.Max(radius * 2f, collisionHeight);
            var center = transform.position + Vector3.up * (height * 0.5f);
            var halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            var bottom = center - Vector3.up * halfSegment;
            var top = center + Vector3.up * halfSegment;

            if (!Physics.CapsuleCast(bottom, top, radius, direction.normalized, out var hit, Mathf.Max(0.01f, distance), movementBlockMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        private void FaceFocusTarget()
        {
            var target = npcInteractable != null ? npcInteractable.FocusedInteractor : null;
            if (target == null)
            {
                return;
            }

            var direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        private void EnsureFootAlignmentFix()
        {
            if (_footFixEnsured)
            {
                return;
            }

            var visualAnimator = GetComponentInChildren<Animator>(true);
            if (visualAnimator == null)
            {
                return;
            }

            var fix = visualAnimator.GetComponent<NpcFootAlignmentFix>();
            if (fix == null)
            {
                fix = visualAnimator.gameObject.AddComponent<NpcFootAlignmentFix>();
            }

            _footFixEnsured = true;
        }
    }
}
