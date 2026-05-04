using UnityEngine;

namespace MobilOfl.Visuals
{
    public class LocalFirstPersonAvatarHider : MonoBehaviour
    {
        private Renderer[] _renderers;

        private void Awake()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        private void OnEnable()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        private void LateUpdate()
        {
            ApplyVisibility();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void ApplyVisibility()
        {
            if (_renderers == null)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = true;
                }
            }
        }
    }
}
