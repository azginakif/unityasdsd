using MobilOfl.Visuals;
using UnityEngine;

namespace MobilOfl.Gameplay
{
    public static class InvestigationSceneRuntimeRepair
    {
        private const string EvidenceRootName = "SampleEvidenceRoot";
        private const string PlayerRootName = "Player";
        private const string SchoolBoyResourcePath = "Characters/SchoolBoyCharacter";
        private const int LocalPlayerVisualLayer = 8;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RepairLoadedInvestigationScene()
        {
            if (!LooksLikeInvestigationScene())
            {
                return;
            }

            EnsurePlayerCharacterVisual();
            EnsureNpcCharacterVisuals();
            EnsureKnownSearchSpotGates();
            EnsureToolPickup(
                "tool.archive-pass",
                "Arsiv Gecis Karti",
                "Arsiv Gecis Karti",
                "Etkilesim: Arsiv Gecis Karti",
                "Arsiv gecis karti alindi. Artik kisitli raf alanina girebilirsin.",
                new Vector3(7.8f, 0.86f, 9.5f),
                new Vector3(0.36f, 0.08f, 0.36f),
                new Color(0.22f, 0.88f, 0.82f, 1f));
            EnsureToolPickup(
                "tool.lockpick",
                "Maymuncuk Seti",
                "Maymuncuk Seti",
                "Etkilesim: Maymuncuk Seti",
                "Maymuncuk seti alindi. Kilitli cekmece ve kutulari artik acabilirsin.",
                new Vector3(-8.1f, 0.82f, 9.8f),
                new Vector3(0.34f, 0.12f, 0.34f),
                new Color(0.96f, 0.68f, 0.18f, 1f));
        }

        private static bool LooksLikeInvestigationScene()
        {
            if (Object.FindAnyObjectByType<CaseSessionManager>() != null)
            {
                return true;
            }

            return Object.FindObjectsByType<SearchSpotInteractable>(FindObjectsInactive.Include).Length > 0 ||
                   Object.FindObjectsByType<EvidenceInteractable>(FindObjectsInactive.Include).Length > 0;
        }

        private static void EnsurePlayerCharacterVisual()
        {
            var player = GameObject.Find(PlayerRootName);
            if (player == null || player.GetComponentInChildren<CharacterMovementAnimator>(true) != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(SchoolBoyResourcePath);
            if (prefab == null)
            {
                return;
            }

            var visual = Object.Instantiate(prefab, player.transform);
            visual.name = "PlayerAvatarVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SetLayerRecursively(visual.transform, LocalPlayerVisualLayer);

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.cullingMask &= ~(1 << LocalPlayerVisualLayer);
            }
        }

        private static void EnsureNpcCharacterVisuals()
        {
            var prefab = Resources.Load<GameObject>(SchoolBoyResourcePath);
            if (prefab == null)
            {
                return;
            }

            var npcs = Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include);
            for (var i = 0; i < npcs.Length; i++)
            {
                var npc = npcs[i];
                if (npc == null || npc.GetComponentInChildren<CharacterMovementAnimator>(true) != null)
                {
                    continue;
                }

                RemovePlaceholderVisuals(npc.transform);
                var rootRenderer = npc.GetComponent<Renderer>();
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = false;
                }

                var visual = Object.Instantiate(prefab, npc.transform);
                visual.name = npc.name + "_Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }
        }

        private static void RemovePlaceholderVisuals(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                var isGeneratedVisual =
                    child.name.EndsWith("_Visual", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetComponent<PlaceholderCharacterVisual>() != null;

                if (isGeneratedVisual)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private static void EnsureKnownSearchSpotGates()
        {
            var searchSpots = Object.FindObjectsByType<SearchSpotInteractable>(FindObjectsInactive.Include);
            foreach (var searchSpot in searchSpots)
            {
                if (searchSpot == null)
                {
                    continue;
                }

                switch (searchSpot.HiddenEvidenceId)
                {
                    case "evidence.locker-key":
                        searchSpot.ConfigureToolGate("tool.lockpick", "Bu cekmece icin once maymuncuk seti bulman gerekiyor.");
                        break;
                    case "evidence.archive-ledger":
                        searchSpot.ConfigureToolGate("tool.archive-pass", "Arsiv raf kutusu icin once gecis karti bulman gerekiyor.");
                        break;
                }
            }
        }

        private static void EnsureToolPickup(
            string toolId,
            string displayName,
            string label,
            string promptText,
            string pickupMessage,
            Vector3 worldPosition,
            Vector3 localScale,
            Color color)
        {
            if (HasToolPickup(toolId))
            {
                return;
            }

            var root = GameObject.Find(EvidenceRootName);
            if (root == null)
            {
                root = new GameObject(EvidenceRootName);
            }

            var toolObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            toolObject.name = displayName;
            toolObject.transform.SetParent(root.transform);
            toolObject.transform.position = worldPosition;
            toolObject.transform.localScale = localScale;

            var renderer = toolObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateMaterial(displayName + "_RuntimeMaterial", color);
            }

            var interactable = toolObject.AddComponent<ToolPickupInteractable>();
            interactable.Configure(toolId, displayName, label, pickupMessage, color);
            interactable.ConfigurePrompt(promptText);

            var glow = new GameObject("ToolGlow");
            glow.transform.SetParent(toolObject.transform, false);
            glow.transform.localPosition = Vector3.up * 0.65f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 2.4f;
            light.intensity = 0.55f;

            Debug.Log($"[Mobil OFL] Runtime scene repair added missing tool pickup: {toolId}");
        }

        private static bool HasToolPickup(string toolId)
        {
            var tools = Object.FindObjectsByType<ToolPickupInteractable>(FindObjectsInactive.Include);
            foreach (var tool in tools)
            {
                if (tool != null && tool.ToolId == toolId)
                {
                    return true;
                }
            }

            return false;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader);
            material.name = name;
            material.color = color;
            return material;
        }
    }
}
