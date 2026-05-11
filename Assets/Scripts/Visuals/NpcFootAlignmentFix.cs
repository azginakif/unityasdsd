using UnityEngine;

namespace MobilOfl.Visuals
{
    /// <summary>
    /// Applies a tiny post-animation foot rotation offset for NPC rigs that
    /// import with outward-biased left foot pose.
    /// </summary>
    public sealed class NpcFootAlignmentFix : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float leftFootYawCorrection = -8f;
        [SerializeField] private float leftToeYawCorrection = -4f;

        private Transform _leftFoot;
        private Transform _leftToes;

        private void Awake()
        {
            ResolveRigReferences();
        }

        private void OnEnable()
        {
            ResolveRigReferences();
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.enabled)
            {
                return;
            }

            if (_leftFoot == null)
            {
                ResolveRigReferences();
                if (_leftFoot == null)
                {
                    return;
                }
            }

            if (Mathf.Abs(leftFootYawCorrection) > 0.001f)
            {
                _leftFoot.localRotation *= Quaternion.Euler(0f, leftFootYawCorrection, 0f);
            }

            if (_leftToes != null && Mathf.Abs(leftToeYawCorrection) > 0.001f)
            {
                _leftToes.localRotation *= Quaternion.Euler(0f, leftToeYawCorrection, 0f);
            }
        }

        private void ResolveRigReferences()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                return;
            }

            _leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);

            if (_leftFoot == null)
            {
                _leftFoot = FindDeepChild(animator.transform, "foot.L") ??
                            FindDeepChild(animator.transform, "LeftFoot") ??
                            FindDeepChild(animator.transform, "mixamorig:LeftFoot");
            }

            if (_leftToes == null)
            {
                _leftToes = FindDeepChild(animator.transform, "toe.L") ??
                            FindDeepChild(animator.transform, "LeftToeBase") ??
                            FindDeepChild(animator.transform, "mixamorig:LeftToeBase");
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == childName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var result = FindDeepChild(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
