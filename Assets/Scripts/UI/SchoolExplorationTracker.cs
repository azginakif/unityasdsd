using System.Collections.Generic;
using MobilOfl.Online;
using UnityEngine;

namespace MobilOfl.UI
{
    public class SchoolExplorationTracker : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;

        private readonly List<string> _visitedZones = new List<string>();
        private string _currentZone = string.Empty;

        public IReadOnlyList<string> VisitedZones => _visitedZones;
        public int VisitedZoneCount => _visitedZones.Count;
        public string CurrentZone => _currentZone;

        private void Update()
        {
            ResolvePlayerTarget();
            if (playerTarget == null)
            {
                return;
            }

            var zone = SchoolLocationUtility.GetZoneTitle(playerTarget.position);
            if (string.IsNullOrWhiteSpace(zone))
            {
                return;
            }

            _currentZone = zone;
            if (!_visitedZones.Contains(zone))
            {
                _visitedZones.Add(zone);
            }
        }

        private void ResolvePlayerTarget()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            var avatars = Object.FindObjectsByType<NetworkPlayerAvatar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null && avatars[i].IsOwner)
                {
                    playerTarget = avatars[i].transform;
                    return;
                }
            }

            var player = GameObject.Find("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }
}
