using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using MobilOfl.Visuals;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MobilOfl.EditorTools
{
    public static class OnlinePrototypeSetupTool
    {
        private const string CaseAssetPath = "Assets/Data/Cases/ExamTheftCase.asset";
        private const string GeneratedPrefabsFolder = "Assets/Prefabs/Generated";
        private const string PlayerPrefabPath = GeneratedPrefabsFolder + "/NetworkPlayer.prefab";
        private const float PlayerEyeHeight = 1.62f;

        [MenuItem("Mobil OFL/Setup/Configure Online Prototype")]
        public static void ConfigureOnlinePrototypeMenu()
        {
            ConfigureOnlinePrototypeInScene(true);
        }

        public static void ConfigureOnlinePrototypeInScene(bool showDialog)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(GeneratedPrefabsFolder);

            var caseDefinition = AssetDatabase.LoadAssetAtPath<CaseDefinition>(CaseAssetPath);
            if (caseDefinition == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Mobil OFL",
                        "ExamTheftCase asset'i bulunamadi. Once ana setup aracini calistir.",
                        "Tamam");
                }

                return;
            }

            var playerPrefab = CreateOrUpdateNetworkPlayerPrefab();
            var caseState = EnsureNetworkCaseState(caseDefinition);
            var bootstrap = EnsureNetworkManager(playerPrefab, caseState);
            EnsureOnlineHud(bootstrap);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Mobil OFL",
                    "Online prototip kurulumu tamamlandi.",
                    "Tamam");
            }
        }

        private static GameObject CreateOrUpdateNetworkPlayerPrefab()
        {
            var root = new GameObject("NetworkPlayer");
            root.tag = "Untagged";

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            var movement = root.AddComponent<PrototypeFirstPersonController>();
            var interaction = root.AddComponent<PlayerInteractionController>();
            var scanner = root.AddComponent<InvestigationScanner>();
            var stealth = root.AddComponent<PlayerStealthController>();
            var ping = root.AddComponent<TeamPingController>();
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            var avatar = root.AddComponent<NetworkPlayerAvatar>();

            var localBodyRenderers = CreateNetworkPlayerVisual(root.transform);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, PlayerEyeHeight, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "Untagged";
            var listener = cameraObject.AddComponent<AudioListener>();

            var movementSerializedObject = new SerializedObject(movement);
            movementSerializedObject.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
            movementSerializedObject.FindProperty("lookSmoothing").floatValue = 24f;
            movementSerializedObject.FindProperty("maxLookDelta").floatValue = 34f;
            movementSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var interactionSerializedObject = new SerializedObject(interaction);
            interactionSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            interactionSerializedObject.FindProperty("interactDistance").floatValue = 4f;
            interactionSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var scannerSerializedObject = new SerializedObject(scanner);
            scannerSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            scannerSerializedObject.FindProperty("playerInteraction").objectReferenceValue = interaction;
            scannerSerializedObject.FindProperty("scanRadius").floatValue = 10.5f;
            scannerSerializedObject.FindProperty("scanDuration").floatValue = 2.35f;
            scannerSerializedObject.FindProperty("scanCooldown").floatValue = 6f;
            scannerSerializedObject.FindProperty("maxReportedSignals").intValue = 3;
            scannerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var stealthSerializedObject = new SerializedObject(stealth);
            stealthSerializedObject.FindProperty("movementController").objectReferenceValue = movement;
            stealthSerializedObject.FindProperty("scanner").objectReferenceValue = scanner;
            stealthSerializedObject.FindProperty("npcAwarenessRadius").floatValue = 7.2f;
            stealthSerializedObject.FindProperty("forcedCalmInteractionThreshold").floatValue = 0.9f;
            stealthSerializedObject.FindProperty("scanNoiseBoost").floatValue = 0.18f;
            stealthSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var pingSerializedObject = new SerializedObject(ping);
            pingSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            pingSerializedObject.FindProperty("playerInteraction").objectReferenceValue = interaction;
            pingSerializedObject.FindProperty("pingDistance").floatValue = 32f;
            pingSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var avatarSerializedObject = new SerializedObject(avatar);
            avatarSerializedObject.FindProperty("movementController").objectReferenceValue = movement;
            avatarSerializedObject.FindProperty("interactionController").objectReferenceValue = interaction;
            avatarSerializedObject.FindProperty("scanner").objectReferenceValue = scanner;
            avatarSerializedObject.FindProperty("stealthController").objectReferenceValue = stealth;
            avatarSerializedObject.FindProperty("pingController").objectReferenceValue = ping;
            avatarSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            avatarSerializedObject.FindProperty("audioListener").objectReferenceValue = listener;
            var bodyRenderersProperty = avatarSerializedObject.FindProperty("localBodyRenderers");
            bodyRenderersProperty.arraySize = localBodyRenderers.Length;
            for (var i = 0; i < localBodyRenderers.Length; i++)
            {
                bodyRenderersProperty.GetArrayElementAtIndex(i).objectReferenceValue = localBodyRenderers[i];
            }
            avatarSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Renderer[] CreateNetworkPlayerVisual(Transform parent)
        {
            var schoolBoyPrefab = SchoolBoyCharacterSetupTool.LoadCharacterPrefab();
            if (schoolBoyPrefab != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(schoolBoyPrefab, parent) as GameObject;
                if (instance != null)
                {
                    instance.name = "AvatarVisual";
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    return instance.GetComponentsInChildren<Renderer>(true);
                }
            }

            var visualRoot = new GameObject("AvatarVisual");
            visualRoot.transform.SetParent(parent, false);
            visualRoot.transform.localPosition = Vector3.zero;

            var material = CreatePreviewMaterial("NetworkPlayer_Material", new Color(0.22f, 0.62f, 0.82f));
            var body = CreateVisualPrimitive(visualRoot.transform, "AvatarBody", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.62f, 0.82f, 0.62f), material);
            var head = CreateVisualPrimitive(visualRoot.transform, "AvatarHead", PrimitiveType.Sphere, new Vector3(0f, 1.76f, 0f), new Vector3(0.4f, 0.4f, 0.4f), material);
            var leftArm = CreateVisualPrimitive(visualRoot.transform, "AvatarLeftArm", PrimitiveType.Cylinder, new Vector3(-0.43f, 1.08f, 0f), new Vector3(0.09f, 0.48f, 0.09f), material);
            var rightArm = CreateVisualPrimitive(visualRoot.transform, "AvatarRightArm", PrimitiveType.Cylinder, new Vector3(0.43f, 1.08f, 0f), new Vector3(0.09f, 0.48f, 0.09f), material);

            var visual = visualRoot.AddComponent<PlaceholderCharacterVisual>();
            var serializedObject = new SerializedObject(visual);
            serializedObject.FindProperty("head").objectReferenceValue = head.transform;
            serializedObject.FindProperty("body").objectReferenceValue = body.transform;
            serializedObject.FindProperty("leftArm").objectReferenceValue = leftArm.transform;
            serializedObject.FindProperty("rightArm").objectReferenceValue = rightArm.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return new[]
            {
                body.GetComponent<Renderer>(),
                head.GetComponent<Renderer>(),
                leftArm.GetComponent<Renderer>(),
                rightArm.GetComponent<Renderer>()
            };
        }

        private static GameObject CreateVisualPrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return primitive;
        }

        private static Material CreatePreviewMaterial(string name, Color color)
        {
            var material = new Material(FindCompatiblePreviewShader());
            material.name = name;
            SetPreviewMaterialColor(material, color);
            return material;
        }

        private static Shader FindCompatiblePreviewShader()
        {
            var shaderNames = new[]
            {
                "Standard",
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default"
            };

            for (var i = 0; i < shaderNames.Length; i++)
            {
                var shader = Shader.Find(shaderNames[i]);
                if (shader != null && shader.isSupported)
                {
                    return shader;
                }
            }

            return Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        }

        private static void SetPreviewMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0f);
            }
        }

        private static NetworkCaseState EnsureNetworkCaseState(CaseDefinition caseDefinition)
        {
            var gameObject = GameObject.Find("NetworkCaseState");
            if (gameObject == null)
            {
                gameObject = new GameObject("NetworkCaseState");
            }

            if (gameObject.GetComponent<NetworkObject>() == null)
            {
                gameObject.AddComponent<NetworkObject>();
            }

            var caseState = gameObject.GetComponent<NetworkCaseState>();
            if (caseState == null)
            {
                caseState = gameObject.AddComponent<NetworkCaseState>();
            }

            var serializedObject = new SerializedObject(caseState);
            serializedObject.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseState);

            return caseState;
        }

        private static RelayNetworkBootstrap EnsureNetworkManager(GameObject playerPrefab, NetworkCaseState caseState)
        {
            var gameObject = GameObject.Find("NetworkManager");
            if (gameObject == null)
            {
                gameObject = new GameObject("NetworkManager");
            }

            var networkManager = gameObject.GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }

            var transport = gameObject.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = gameObject.AddComponent<UnityTransport>();
            }

            if (networkManager.NetworkConfig != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                networkManager.NetworkConfig.NetworkTransport = transport;
                networkManager.NetworkConfig.EnableSceneManagement = true;
            }

            var bootstrap = gameObject.GetComponent<RelayNetworkBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = gameObject.AddComponent<RelayNetworkBootstrap>();
            }

            var serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty("networkManager").objectReferenceValue = networkManager;
            serializedObject.FindProperty("unityTransport").objectReferenceValue = transport;
            serializedObject.FindProperty("networkCaseState").objectReferenceValue = caseState;
            serializedObject.FindProperty("offlineScenePlayerRoot").objectReferenceValue = GameObject.Find("Player");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);

            return bootstrap;
        }

        private static void EnsureOnlineHud(RelayNetworkBootstrap bootstrap)
        {
            var gameObject = GameObject.Find("OnlineSessionHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("OnlineSessionHUD");
            }

            var hud = gameObject.GetComponent<OnlineSessionHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<OnlineSessionHud>();
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("bootstrap").objectReferenceValue = bootstrap;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            hud.enabled = false;
            EditorUtility.SetDirty(hud);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

