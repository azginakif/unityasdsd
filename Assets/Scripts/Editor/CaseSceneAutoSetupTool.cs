using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using MobilOfl.UI;
using MobilOfl.Visuals;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MobilOfl.EditorTools
{
    [InitializeOnLoad]
    public static class CaseSceneAutoSetupTool
    {
        private const string CaseAssetFolder = "Assets/Data/Cases";
        private const string CaseAssetPath = CaseAssetFolder + "/ExamTheftCase.asset";
        private const string EvidenceRootName = "SampleEvidenceRoot";
        private const string NpcRootName = "SampleNpcRoot";
        private const string PlayerRootName = "Player";
        private const string PlayerAvatarVisualName = "PlayerAvatarVisual";
        private const string LocalPlayerVisualLayerName = "LocalPlayerVisual";
        private const int LocalPlayerVisualLayer = 8;
        private const float ActorGroundY = 0.05f;
        private const float PlayerEyeHeight = 1.62f;
        private const float ActorHeight = 1.8f;
        private const float ActorRadius = 0.35f;
        private const float NpcActorHeight = 2.08f;
        private const float NpcActorRadius = 0.43f;
        private const float NpcPatrolMoveSpeed = 1.45f;
        private const string MobileControlsCanvasName = "MobileControlsCanvas";
        private const string SchoolBlockRootName = "SampleSchoolBlock";
        private const string AtmosphereRootName = "SampleAtmosphere";
        private const string CinematicVolumeProfilePath = "Assets/map/Scenes/SampleScene/OFL_CinematicLightingProfile.asset";
        private const string FloreswaPrefabFolder = "Assets/Floreswa/Prefabs";
        private const string SessionManagerName = "CaseSessionManager";
        private const string DebugHudName = "DebugHUD";
        private const string RecommendedCompanyName = "Mobil OFL";
        private const string RecommendedProductName = "MOBIL OFL";
        private const string RecommendedBundleVersion = "0.2.0";
        private const string RecommendedAndroidAppId = "com.mobilofl.prototype";
        private const string RecommendedIosAppId = "com.mobilofl.prototype";
        private const string RecommendedStandaloneAppId = "com.mobilofl.prototype";
        private static bool _sceneVisualRepairQueued;
        private static readonly string[] FloreswaNpcPrefabPaths =
        {
            FloreswaPrefabFolder + "/male01_1.prefab",
            FloreswaPrefabFolder + "/male01_2.prefab",
            FloreswaPrefabFolder + "/male01_3.prefab",
            FloreswaPrefabFolder + "/male02_1.prefab",
            FloreswaPrefabFolder + "/male02_2.prefab",
            FloreswaPrefabFolder + "/male02_3.prefab",
            FloreswaPrefabFolder + "/male03_1.prefab",
            FloreswaPrefabFolder + "/male03_2.prefab",
            FloreswaPrefabFolder + "/male03_3.prefab"
        };

        private readonly struct SceneLayoutProfile
        {
            public SceneLayoutProfile(bool usesImportedSchoolMap, Vector3 origin, Quaternion playerRotation)
            {
                UsesImportedSchoolMap = usesImportedSchoolMap;
                Origin = origin;
                PlayerRotation = playerRotation;
            }

            public bool UsesImportedSchoolMap { get; }
            public Vector3 Origin { get; }
            public Quaternion PlayerRotation { get; }
            public float ActorGroundY => UsesImportedSchoolMap ? Origin.y : CaseSceneAutoSetupTool.ActorGroundY;

            public Vector3 ToWorld(Vector3 prototypePosition)
            {
                if (!UsesImportedSchoolMap)
                {
                    return prototypePosition;
                }

                return new Vector3(
                    Origin.x + prototypePosition.z * 0.55f,
                    Origin.y + prototypePosition.y - CaseSceneAutoSetupTool.ActorGroundY,
                    Origin.z + prototypePosition.x * 4f);
            }

            public Vector3 ToActorWorld(Vector3 prototypePosition)
            {
                var world = ToWorld(prototypePosition);
                world.y = ActorGroundY;
                return world;
            }

            public Vector3 ToPatrolOffset(Vector3 prototypeOffset)
            {
                if (!UsesImportedSchoolMap)
                {
                    return prototypeOffset;
                }

                return new Vector3(
                    prototypeOffset.z * 0.55f,
                    prototypeOffset.y,
                    prototypeOffset.x * 4f);
            }
        }

        static CaseSceneAutoSetupTool()
        {
            QueueAutoRepairMissingSceneCharacterVisuals();
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static SceneLayoutProfile ResolveSceneLayoutProfile()
        {
            var hasImportedSchoolMap =
                GameObject.Find("COMPUTERdemoscene (2)") != null ||
                GameObject.Find("schoolbus") != null ||
                GameObject.Find("CHEMISTRYlabdemoscene") != null;

            if (!hasImportedSchoolMap)
            {
                return new SceneLayoutProfile(false, Vector3.zero, Quaternion.identity);
            }

            return new SceneLayoutProfile(
                true,
                new Vector3(920f, 5.08f, -585f),
                Quaternion.Euler(0f, 90f, 0f));
        }

        [MenuItem("Mobil OFL/Setup/Auto Setup Investigation Scene")]
        public static void AutoSetupInvestigationScene()
        {
            AutoSetupInvestigationScene(true);
        }

        [MenuItem("Mobil OFL/Setup/Auto Setup Investigation Scene Silent")]
        public static void AutoSetupInvestigationSceneSilent()
        {
            AutoSetupInvestigationScene(false);
        }

        [MenuItem("Mobil OFL/Setup/Auto Setup Map Sample Scene")]
        public static void AutoSetupMapSampleScene()
        {
            const string mapScenePath = "Assets/map/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(mapScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Mobil OFL] Map sample scene could not be opened: " + mapScenePath);
                return;
            }

            AutoSetupInvestigationScene(false);
        }

        private static void AutoSetupInvestigationScene(bool showDialog)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Mobil OFL", "Aktif sahne bulunamadi.", "Tamam");
                }

                return;
            }

            ApplyRecommendedProjectSettingsInternal();
            EnsureSceneInBuildSettings(activeScene.path);
            EnsureFolder("Assets/Data");
            EnsureFolder(CaseAssetFolder);
            SchoolBoyCharacterSetupTool.SetupSchoolBoyCharacterAssets();
            ConfigureFloreswaCharacterImports();

            var layout = ResolveSceneLayoutProfile();
            var caseDefinition = LoadOrCreateCaseDefinition();
            PopulateCaseDefinition(caseDefinition);

            var playerSetup = EnsurePlayer(layout);
            DisableImportedMapTestActors(layout);
            DisableExtraAudioListeners(playerSetup.Camera);

            EnsureSessionManager(caseDefinition);
            var progressTracker = EnsureProgressTracker();
            EnsureHintDirector();
            DisableLegacyDebugHud();
            EnsureStatusHud(playerSetup.Interaction);
            EnsureChecklistHud(progressTracker);
            EnsureReticleHud(playerSetup.Interaction);
            EnsureWorldMarkerHud(playerSetup.Camera);
            EnsureMinimapHud(playerSetup.Root.transform);
            EnsureLocationBannerHud();
            EnsureModernGameplayHud(playerSetup.Interaction, progressTracker);
            var notebookButton = EnsureMobileControls(playerSetup);
            EnsureNotebookHud(notebookButton);
            EnsureResultHud();
            EnsureDirectionalLight();
            EnsureSampleSchoolBlock(layout);
            EnsureHideSpots(layout);
            EnsureInvestigationDeskInteractable(layout);
            EnsureAtmosphereLights(layout);
            EnsureImportedMapDoorInteractables(layout);
            RepairNegativeScaleBoxColliders(layout);
            EnsureEvidenceObjects(caseDefinition, layout);
            EnsureNpcObjects(caseDefinition, layout);
            OnlinePrototypeSetupTool.ConfigureOnlinePrototypeInScene(false);
            EnsureMainMenuHud();

            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrWhiteSpace(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Mobil OFL",
                    "Vaka asset'i olusturuldu, sahne kuruldu ve onerilen proje ayarlari uygulandi.",
                    "Tamam");
            }
        }

        [MenuItem("Mobil OFL/Characters/Apply School Boy To Scene Actors")]
        public static void ApplySchoolBoyToSceneActors()
        {
            if (ApplySchoolBoyToSceneActorsInternal(true))
            {
                EditorUtility.DisplayDialog("Mobil OFL", "Ana karakter player'a, Floreswa karakterleri NPC'lere uygulandi.", "Tamam");
            }
        }

        [MenuItem("Mobil OFL/Production/Integrate Floreswa Characters")]
        public static void IntegrateFloreswaCharacters()
        {
            SchoolBoyCharacterSetupTool.SetupSchoolBoyCharacterAssets();
            EnsureSampleSchoolBlock(ResolveSceneLayoutProfile());
            ConfigureFloreswaCharacterImports();
            ApplySchoolBoyToSceneActorsInternal(false);

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (!string.IsNullOrWhiteSpace(activeScene.path))
                {
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Mobil OFL", "Floreswa NPC karakterleri ve UAL animasyon paketi oyuna baglandi.", "Tamam");
        }

        public static bool ApplySchoolBoyToSceneActorsInternal()
        {
            return ApplySchoolBoyToSceneActorsInternal(true);
        }

        private static bool ApplySchoolBoyToSceneActorsInternal(bool showDialogOnFailure)
        {
            var prefab = SchoolBoyCharacterSetupTool.SetupSchoolBoyCharacterAssets();
            if (prefab == null)
            {
                if (showDialogOnFailure)
                {
                    EditorUtility.DisplayDialog("Mobil OFL", "School boy prefab hazirlanamadi. Assets/karakter/source klasorunu kontrol et.", "Tamam");
                }

                return false;
            }

            EnsureLocalPlayerVisualLayer();

            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                NormalizePlayerActor(player.transform, Camera.main);
                EnsurePlayerCharacterVisual(player.transform, Camera.main);
            }

            var npcs = Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include);
            for (var i = 0; i < npcs.Length; i++)
            {
                var npc = npcs[i];
                if (npc == null)
                {
                    continue;
                }

                NormalizeNpcActor(npc.transform);
                ReplaceNpcCharacterVisual(npc.transform, npc.name + "_Visual", Color.white, npc.name);
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (!string.IsNullOrWhiteSpace(activeScene.path))
                {
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        private static void QueueAutoRepairMissingSceneCharacterVisuals()
        {
            if (_sceneVisualRepairQueued)
            {
                return;
            }

            _sceneVisualRepairQueued = true;
            EditorApplication.delayCall += () =>
            {
                _sceneVisualRepairQueued = false;
                AutoRepairMissingSceneCharacterVisuals();
            };
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                QueueAutoRepairMissingSceneCharacterVisuals();
            }
        }

        private static void AutoRepairMissingSceneCharacterVisuals()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.path))
            {
                return;
            }

            var hasBrokenVisual = false;
            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                hasBrokenVisual |= ActorNeedsNormalization(player.transform);
                hasBrokenVisual |= player.GetComponentInChildren<CharacterMovementAnimator>(true) == null;
            }

            var npcs = Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include);
            for (var i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] == null)
                {
                    continue;
                }

                hasBrokenVisual |= ActorNeedsNormalization(npcs[i].transform);
                hasBrokenVisual |= npcs[i].GetComponentInChildren<CharacterMovementAnimator>(true) == null;
                hasBrokenVisual |= NpcNeedsFloreswaVisual(npcs[i].transform);
            }

            if (!hasBrokenVisual)
            {
                return;
            }

            if (ApplySchoolBoyToSceneActorsInternal(false))
            {
                Debug.Log("[Mobil OFL] Aktif sahnede eksik karakter gorselleri otomatik uygulandi.");
            }
        }

        [MenuItem("Mobil OFL/Setup/Apply Recommended Mobile Project Settings")]
        public static void ApplyRecommendedProjectSettings()
        {
            ApplyRecommendedProjectSettingsInternal();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Mobil OFL",
                "Mobil prototip icin onerilen proje ayarlari uygulandi.",
                "Tamam");
        }

        private sealed class PlayerSetup
        {
            public GameObject Root;
            public Camera Camera;
            public PlayerInteractionController Interaction;
            public PrototypeFirstPersonController Movement;
            public InvestigationScanner Scanner;
            public PlayerStealthController Stealth;
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

        private static void ApplyRecommendedProjectSettingsInternal()
        {
            PlayerSettings.companyName = RecommendedCompanyName;
            PlayerSettings.productName = RecommendedProductName;
            PlayerSettings.bundleVersion = RecommendedBundleVersion;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, RecommendedAndroidAppId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, RecommendedIosAppId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, RecommendedStandaloneAppId);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != scenePath)
                {
                    continue;
                }

                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }

                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static CaseDefinition LoadOrCreateCaseDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CaseDefinition>(CaseAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<CaseDefinition>();
            AssetDatabase.CreateAsset(asset, CaseAssetPath);
            return asset;
        }

        private static void PopulateCaseDefinition(CaseDefinition caseDefinition)
        {
            var serializedObject = new SerializedObject(caseDefinition);

            serializedObject.FindProperty("caseId").stringValue = "case.exam-theft";
            serializedObject.FindProperty("caseTitle").stringValue = "Sinav Sorulari Calindi";
            serializedObject.FindProperty("openingBrief").stringValue =
                "Okulda sinav sorulari calindi. Takim delilleri toplayip gercegi ortaya cikarmali.";

            var evidenceItems = serializedObject.FindProperty("evidenceItems");
            evidenceItems.arraySize = 7;

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(0),
                "evidence.security-log",
                "Guvenlik Oda Kaydi",
                "Guvenlik terminalinde laboratuvar koridorunda gece hareket oldugunu gosteren kayit bulundu.",
                EvidenceCategory.Digital,
                true);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(1),
                "evidence.answer-key-note",
                "Cevap Anahtari Notu",
                "Kutuphane masasinda sinav cevaplariyla ilgili el yazili bir not bulundu.",
                EvidenceCategory.Document,
                true);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(2),
                "evidence.locker-key",
                "Yedek Dolap Anahtari",
                "Ogretmenler odasinda gizlenmis bir yedek dolap anahtari bulundu.",
                EvidenceCategory.Physical,
                false);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(3),
                "evidence.guard-testimony",
                "Guvenlik Ifadesi",
                "Guvenlik gorevlisi gece 22:15'te bilisim kulubu ogrencisini laboratuvar koridorunda gordugunu soyledi.",
                EvidenceCategory.Witness,
                true);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(4),
                "evidence.student-testimony",
                "Ogrenci Ifadesi",
                "Bir ogrenci cevap anahtari notunun kutuphanede bilisim kulubu ogrencisinin defterinden dustugunu soyledi.",
                EvidenceCategory.Witness,
                true);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(5),
                "evidence.archive-ledger",
                "Arsiv Giris Defteri",
                "Arsiv kanadindaki giris defterinde bilisim kulubu ogrencisinin sinavdan bir gun once odaya girdigi goruluyor.",
                EvidenceCategory.Document,
                true);

            SetEvidence(
                evidenceItems.GetArrayElementAtIndex(6),
                "evidence.canteen-testimony",
                "Kantin Ifadesi",
                "Kantin calisani, gece bilisim kulubu ogrencisinin enerji icecegi alip laboratuvar koridoruna kostugunu anlatti.",
                EvidenceCategory.Witness,
                false);

            var suspects = serializedObject.FindProperty("suspects");
            suspects.arraySize = 3;

            SetSuspect(
                suspects.GetArrayElementAtIndex(0),
                "suspect.tech-club",
                "Bilisim Kulubu Ogrencisi",
                "Okul sistemine erisimi olan ve guvenlik agini taniyan ogrenci.",
                new[]
                {
                    "evidence.security-log",
                    "evidence.answer-key-note",
                    "evidence.guard-testimony",
                    "evidence.student-testimony",
                    "evidence.archive-ledger",
                    "evidence.canteen-testimony"
                });

            SetSuspect(
                suspects.GetArrayElementAtIndex(1),
                "suspect.teacher-assistant",
                "Ogretmen Yardimcisi",
                "Sinav programina ve soru dolabina erisimi olan personel.",
                new[] { "evidence.locker-key", "evidence.archive-ledger" });

            SetSuspect(
                suspects.GetArrayElementAtIndex(2),
                "suspect.canteen-worker",
                "Kantin Calisani",
                "Okul icinde serbest dolasan ama dogrudan sistem erisimi olmayan calisan.",
                new[] { "evidence.locker-key", "evidence.canteen-testimony" });

            serializedObject.FindProperty("culpritSuspectId").stringValue = "suspect.tech-club";
            serializedObject.FindProperty("culpritMotive").stringValue =
                "Sinavdan once sorulari satip para kazanmak istedi.";
            serializedObject.FindProperty("culpritTimeline").stringValue =
                "Gece laboratuvar koridoruna girdi, notlari aldi, arsivde onceki kayitlari inceledi ve ogretmenler odasindaki dolabi acmak icin yedek anahtari kullandi.";

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(caseDefinition);
        }

        private static void SetEvidence(
            SerializedProperty property,
            string id,
            string title,
            string description,
            EvidenceCategory category,
            bool isCritical)
        {
            property.FindPropertyRelative("id").stringValue = id;
            property.FindPropertyRelative("title").stringValue = title;
            property.FindPropertyRelative("description").stringValue = description;
            property.FindPropertyRelative("category").enumValueIndex = (int)category;
            property.FindPropertyRelative("icon").objectReferenceValue = null;
            property.FindPropertyRelative("isCritical").boolValue = isCritical;
        }

        private static void SetSuspect(
            SerializedProperty property,
            string id,
            string displayName,
            string summary,
            string[] requiredEvidenceIds)
        {
            property.FindPropertyRelative("id").stringValue = id;
            property.FindPropertyRelative("displayName").stringValue = displayName;
            property.FindPropertyRelative("summary").stringValue = summary;

            var requiredEvidence = property.FindPropertyRelative("requiredEvidenceIds");
            requiredEvidence.arraySize = requiredEvidenceIds.Length;

            for (var i = 0; i < requiredEvidenceIds.Length; i++)
            {
                requiredEvidence.GetArrayElementAtIndex(i).stringValue = requiredEvidenceIds[i];
            }
        }

        private static PlayerSetup EnsurePlayer(SceneLayoutProfile layout)
        {
            var playerRoot = GameObject.Find(PlayerRootName);
            if (playerRoot == null)
            {
                playerRoot = new GameObject(PlayerRootName);
            }

            playerRoot.transform.position = layout.ToActorWorld(new Vector3(0f, ActorGroundY, -8f));
            playerRoot.transform.rotation = layout.PlayerRotation;

            var controller = playerRoot.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerRoot.AddComponent<CharacterController>();
            }

            controller.height = ActorHeight;
            controller.radius = ActorRadius;
            controller.center = new Vector3(0f, ActorHeight * 0.5f, 0f);
            controller.stepOffset = 0.3f;

            var movement = playerRoot.GetComponent<PrototypeFirstPersonController>();
            if (movement == null)
            {
                movement = playerRoot.AddComponent<PrototypeFirstPersonController>();
            }

            var scanner = playerRoot.GetComponent<InvestigationScanner>();
            if (scanner == null)
            {
                scanner = playerRoot.AddComponent<InvestigationScanner>();
            }

            var stealth = playerRoot.GetComponent<PlayerStealthController>();
            if (stealth == null)
            {
                stealth = playerRoot.AddComponent<PlayerStealthController>();
            }

            var camera = FindPreferredPlayerCamera(playerRoot.transform);
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.name = "Main Camera";
            camera.tag = "MainCamera";
            camera.transform.SetParent(playerRoot.transform);
            camera.transform.localPosition = new Vector3(0f, PlayerEyeHeight, 0f);
            camera.transform.localRotation = Quaternion.identity;
            camera.depth = 0f;
            camera.enabled = true;

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            RemoveDuplicatePlayerCameras(playerRoot.transform, camera);

            var oldInteractionOnCamera = camera.GetComponent<PlayerInteractionController>();
            if (oldInteractionOnCamera != null)
            {
                Object.DestroyImmediate(oldInteractionOnCamera);
            }

            var interaction = playerRoot.GetComponent<PlayerInteractionController>();
            if (interaction == null)
            {
                interaction = playerRoot.AddComponent<PlayerInteractionController>();
            }

            var interactionSerializedObject = new SerializedObject(interaction);
            interactionSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            interactionSerializedObject.FindProperty("interactDistance").floatValue = 5.6f;
            interactionSerializedObject.FindProperty("minimumInteractDistance").floatValue = 5.6f;
            interactionSerializedObject.FindProperty("aimAssistRadius").floatValue = 0.5f;
            interactionSerializedObject.FindProperty("nearbyButtonRadius").floatValue = 2.65f;
            interactionSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interaction);

            var movementSerializedObject = new SerializedObject(movement);
            movementSerializedObject.FindProperty("cameraPivot").objectReferenceValue = camera.transform;
            movementSerializedObject.FindProperty("lookSmoothing").floatValue = 24f;
            movementSerializedObject.FindProperty("maxLookDelta").floatValue = 34f;
            movementSerializedObject.FindProperty("sprintStaminaDrainPerSecond").floatValue = 0.28f;
            movementSerializedObject.FindProperty("sprintStaminaRecoverPerSecond").floatValue = 0.22f;
            movementSerializedObject.FindProperty("sprintRecoveryDelay").floatValue = 1f;
            movementSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(movement);

            var scannerSerializedObject = new SerializedObject(scanner);
            scannerSerializedObject.FindProperty("playerCamera").objectReferenceValue = camera;
            scannerSerializedObject.FindProperty("playerInteraction").objectReferenceValue = interaction;
            scannerSerializedObject.FindProperty("scanRadius").floatValue = 10.5f;
            scannerSerializedObject.FindProperty("scanDuration").floatValue = 2.35f;
            scannerSerializedObject.FindProperty("scanCooldown").floatValue = 6f;
            scannerSerializedObject.FindProperty("maxReportedSignals").intValue = 3;
            scannerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scanner);

            var stealthSerializedObject = new SerializedObject(stealth);
            stealthSerializedObject.FindProperty("movementController").objectReferenceValue = movement;
            stealthSerializedObject.FindProperty("scanner").objectReferenceValue = scanner;
            stealthSerializedObject.FindProperty("npcAwarenessRadius").floatValue = 7.2f;
            stealthSerializedObject.FindProperty("forcedCalmInteractionThreshold").floatValue = 0.9f;
            stealthSerializedObject.FindProperty("scanNoiseBoost").floatValue = 0.18f;
            stealthSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stealth);

            EnsurePlayerCharacterVisual(playerRoot.transform, camera);

            return new PlayerSetup
            {
                Root = playerRoot,
                Camera = camera,
                Interaction = interaction,
                Movement = movement,
                Scanner = scanner,
                Stealth = stealth
            };
        }

        private static Camera FindPreferredPlayerCamera(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            }

            var cameras = playerRoot.GetComponentsInChildren<Camera>(true);
            Camera preferred = null;
            var preferredScore = float.NegativeInfinity;
            for (var i = 0; i < cameras.Length; i++)
            {
                var candidate = cameras[i];
                if (candidate == null)
                {
                    continue;
                }

                var score = candidate.depth;
                if (candidate.enabled && candidate.gameObject.activeInHierarchy)
                {
                    score += 1000f;
                }

                if (candidate.CompareTag("MainCamera"))
                {
                    score += 100f;
                }

                if (candidate.GetComponent<AudioListener>() != null && candidate.GetComponent<AudioListener>().enabled)
                {
                    score += 2f;
                }

                if (preferred == null || score > preferredScore)
                {
                    preferred = candidate;
                    preferredScore = score;
                }
            }

            if (preferred != null)
            {
                return preferred;
            }

            var mainCamera = Camera.main;
            return mainCamera != null ? mainCamera : Object.FindAnyObjectByType<Camera>();
        }

        private static void RemoveDuplicatePlayerCameras(Transform playerRoot, Camera primaryCamera)
        {
            if (playerRoot == null || primaryCamera == null)
            {
                return;
            }

            var cameras = playerRoot.GetComponentsInChildren<Camera>(true);
            for (var i = 0; i < cameras.Length; i++)
            {
                var candidate = cameras[i];
                if (candidate == null || candidate == primaryCamera)
                {
                    continue;
                }

                Object.DestroyImmediate(candidate.gameObject);
            }
        }

        private static void DisableImportedMapTestActors(SceneLayoutProfile layout)
        {
            if (!layout.UsesImportedSchoolMap)
            {
                return;
            }

            var testPlayer = GameObject.Find("First Person Test Player");
            if (testPlayer == null)
            {
                return;
            }

            testPlayer.SetActive(false);
            EditorUtility.SetDirty(testPlayer);
        }

        private static MobileButton EnsureMobileControls(PlayerSetup playerSetup)
        {
            EnsureEventSystem();

            var canvasObject = GameObject.Find(MobileControlsCanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(MobileControlsCanvasName);
            }

            var canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            EnsureMobileHudChrome(canvasRect);
            DisableMobileChrome(canvasRect);
            var joystick = EnsureJoystick(canvasRect);
            var lookArea = EnsureLookArea(canvasRect);
            var sprintButton = EnsureMobileButton(
                canvasRect,
                "SprintButton",
                "KOS",
                new Vector2(1f, 0f),
                new Vector2(-214f, 118f),
                new Vector2(88f, 88f),
                new Color(0.12f, 0.2f, 0.25f, 0.42f));
            var interactButton = EnsureMobileButton(
                canvasRect,
                "InteractButton",
                "AL",
                new Vector2(1f, 0f),
                new Vector2(-100f, 154f),
                new Vector2(126f, 126f),
                new Color(0.08f, 0.3f, 0.25f, 0.5f));
            DisableMobileButton(canvasRect, "JumpButton");
            DisableMobileButton(canvasRect, "CrouchButton");
            var scanButton = EnsureMobileButton(
                canvasRect,
                "ScanButton",
                "TARA",
                new Vector2(1f, 0f),
                new Vector2(-214f, 228f),
                new Vector2(92f, 58f),
                new Color(0.08f, 0.25f, 0.29f, 0.4f));
            var notebookButton = EnsureMobileButton(
                canvasRect,
                "NotebookButton",
                "DOSYA",
                new Vector2(1f, 0f),
                new Vector2(-100f, 272f),
                new Vector2(100f, 58f),
                new Color(0.11f, 0.1f, 0.12f, 0.44f));
            EnsureMobileRuntimeOverlay(canvasObject, playerSetup.Interaction);

            var movementSerializedObject = new SerializedObject(playerSetup.Movement);
            movementSerializedObject.FindProperty("mobileMoveJoystick").objectReferenceValue = joystick;
            movementSerializedObject.FindProperty("mobileLookArea").objectReferenceValue = lookArea;
            movementSerializedObject.FindProperty("mobileSprintButton").objectReferenceValue = sprintButton;
            movementSerializedObject.FindProperty("mobileJumpButton").objectReferenceValue = null;
            movementSerializedObject.FindProperty("mobileCrouchButton").objectReferenceValue = null;
            movementSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSetup.Movement);

            var scannerSerializedObject = new SerializedObject(playerSetup.Scanner);
            scannerSerializedObject.FindProperty("mobileScanButton").objectReferenceValue = scanButton;
            scannerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSetup.Scanner);

            var interactionSerializedObject = new SerializedObject(playerSetup.Interaction);
            interactionSerializedObject.FindProperty("mobileInteractButton").objectReferenceValue = interactButton;
            interactionSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSetup.Interaction);

            return notebookButton;
        }

        private static void DisableMobileButton(RectTransform canvasRect, string buttonName)
        {
            var existing = canvasRect.Find(buttonName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        private static void DisableMobileChrome(RectTransform canvasRect)
        {
            DisableMobileButton(canvasRect, "MobileHudHeader");
            DisableMobileButton(canvasRect, "MobileObjectivePanel");
            DisableMobileButton(canvasRect, "MoveHintPanel");
            DisableMobileButton(canvasRect, "LookHintPanel");
            DisableMobileButton(canvasRect, "MobileContextPanel");
        }

        private static void EnsureMobileHudChrome(RectTransform canvasRect)
        {
            var header = EnsureUiRect(canvasRect, "MobileHudHeader", new Vector2(0f, 1f), new Vector2(230f, -86f), new Vector2(420f, 116f));
            var headerImage = EnsureImage(header.gameObject, new Color(0.06f, 0.08f, 0.11f, 0.82f));
            headerImage.raycastTarget = false;
            EnsureOutline(header.gameObject, new Color(0.42f, 0.37f, 0.26f, 0.9f), new Vector2(1f, -1f));
            EnsureShadow(header.gameObject, new Color(0f, 0f, 0f, 0.26f), new Vector2(0f, -4f));

            var accent = EnsureUiRect(header, "Accent", new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(388f, 5f));
            EnsureImage(accent.gameObject, new Color(0.88f, 0.71f, 0.31f, 0.9f)).raycastTarget = false;

            var title = EnsureText(header, "Title", "MOBIL OFL", 26, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.97f, 0.95f, 0.9f, 1f));
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(22f, -22f);
            title.rectTransform.sizeDelta = new Vector2(-44f, 30f);

            var subtitle = EnsureText(header, "Subtitle", "Mobil sorusturma arayuzu", 15, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.73f, 0.76f, 0.74f, 1f));
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(22f, -56f);
            subtitle.rectTransform.sizeDelta = new Vector2(-44f, 22f);

            var zonePill = EnsureUiRect(header, "ZonePill", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(132f, 28f));
            var zonePillImage = EnsureImage(zonePill.gameObject, new Color(0.12f, 0.16f, 0.18f, 0.92f));
            zonePillImage.raycastTarget = false;
            EnsureOutline(zonePill.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.4f), new Vector2(1f, -1f));
            var zoneText = EnsureText(zonePill, "ZoneText", "KORIDOR", 11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.97f, 0.95f, 0.9f, 1f));
            zoneText.rectTransform.anchorMin = Vector2.zero;
            zoneText.rectTransform.anchorMax = Vector2.one;
            zoneText.rectTransform.offsetMin = new Vector2(8f, 4f);
            zoneText.rectTransform.offsetMax = new Vector2(-8f, -4f);

            var objectivePanel = EnsureUiRect(canvasRect, "MobileObjectivePanel", new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(640f, 84f));
            var objectiveImage = EnsureImage(objectivePanel.gameObject, new Color(0.06f, 0.08f, 0.11f, 0.84f));
            objectiveImage.raycastTarget = false;
            EnsureOutline(objectivePanel.gameObject, new Color(0.42f, 0.37f, 0.26f, 0.86f), new Vector2(1f, -1f));
            EnsureShadow(objectivePanel.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -4f));
            var objectiveAccent = EnsureUiRect(objectivePanel, "Accent", new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(6f, 56f));
            EnsureImage(objectiveAccent.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.9f)).raycastTarget = false;
            var objectiveLabel = EnsureText(objectivePanel, "ObjectiveLabel", "ANLIK HEDEF", 11, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.73f, 0.76f, 0.74f, 1f));
            objectiveLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            objectiveLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            objectiveLabel.rectTransform.pivot = new Vector2(0f, 1f);
            objectiveLabel.rectTransform.anchoredPosition = new Vector2(22f, -12f);
            objectiveLabel.rectTransform.sizeDelta = new Vector2(-44f, 16f);
            var objectiveBody = EnsureText(objectivePanel, "ObjectiveText", "Ilk delili topla ve soru zincirini baslat.", 16, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.97f, 0.95f, 0.9f, 1f));
            objectiveBody.rectTransform.anchorMin = new Vector2(0f, 0f);
            objectiveBody.rectTransform.anchorMax = new Vector2(1f, 1f);
            objectiveBody.rectTransform.offsetMin = new Vector2(22f, 12f);
            objectiveBody.rectTransform.offsetMax = new Vector2(-16f, -28f);

            var moveHint = EnsureUiRect(canvasRect, "MoveHintPanel", new Vector2(0f, 0f), new Vector2(230f, 316f), new Vector2(240f, 64f));
            var moveHintImage = EnsureImage(moveHint.gameObject, new Color(0.07f, 0.1f, 0.12f, 0.68f));
            moveHintImage.raycastTarget = false;
            EnsureOutline(moveHint.gameObject, new Color(0.42f, 0.37f, 0.26f, 0.7f), new Vector2(1f, -1f));
            EnsureText(moveHint, "HintText", "Sol alan: hareket", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.97f, 0.95f, 0.9f, 1f));

            var lookHint = EnsureUiRect(canvasRect, "LookHintPanel", new Vector2(1f, 0f), new Vector2(-368f, 428f), new Vector2(286f, 64f));
            var lookHintImage = EnsureImage(lookHint.gameObject, new Color(0.07f, 0.1f, 0.12f, 0.64f));
            lookHintImage.raycastTarget = false;
            EnsureOutline(lookHint.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.38f), new Vector2(1f, -1f));
            EnsureText(lookHint, "HintText", "Sag alan: surukle ve bak", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.97f, 0.95f, 0.9f, 1f));

            var contextPanel = EnsureUiRect(canvasRect, "MobileContextPanel", new Vector2(1f, 0f), new Vector2(-286f, 148f), new Vector2(330f, 90f));
            var contextImage = EnsureImage(contextPanel.gameObject, new Color(0.06f, 0.08f, 0.11f, 0.82f));
            contextImage.raycastTarget = false;
            EnsureOutline(contextPanel.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.45f), new Vector2(1f, -1f));
            EnsureShadow(contextPanel.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -4f));
            var contextAccent = EnsureUiRect(contextPanel, "Accent", new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(6f, 56f));
            EnsureImage(contextAccent.gameObject, new Color(0.88f, 0.71f, 0.31f, 0.9f)).raycastTarget = false;
            var contextLabel = EnsureText(contextPanel, "ContextText", "Delil veya NPC hedefine yaklas.", 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.97f, 0.95f, 0.9f, 1f));
            contextLabel.rectTransform.anchorMin = Vector2.zero;
            contextLabel.rectTransform.anchorMax = Vector2.one;
            contextLabel.rectTransform.offsetMin = new Vector2(22f, 14f);
            contextLabel.rectTransform.offsetMax = new Vector2(-16f, -14f);
        }

        private static void EnsureMobileRuntimeOverlay(GameObject canvasObject, PlayerInteractionController playerInteraction)
        {
            var overlay = canvasObject.GetComponent<MobileInvestigationOverlay>();
            if (overlay == null)
            {
                overlay = canvasObject.AddComponent<MobileInvestigationOverlay>();
            }

            var canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            }

            var header = canvasObject.transform.Find("MobileHudHeader");
            var subtitle = header != null ? header.Find("Subtitle") : null;
            var zonePill = header != null ? header.Find("ZonePill/ZoneText") : null;
            var objectiveText = canvasObject.transform.Find("MobileObjectivePanel/ObjectiveText");
            var contextPanel = canvasObject.transform.Find("MobileContextPanel");
            var contextText = contextPanel != null ? contextPanel.Find("ContextText") : null;

            var contextCanvasGroup = contextPanel != null ? contextPanel.GetComponent<CanvasGroup>() : null;
            if (contextPanel != null && contextCanvasGroup == null)
            {
                contextCanvasGroup = contextPanel.gameObject.AddComponent<CanvasGroup>();
            }

            var serializedObject = new SerializedObject(overlay);
            serializedObject.FindProperty("playerInteraction").objectReferenceValue = playerInteraction;
            serializedObject.FindProperty("rootGroup").objectReferenceValue = canvasGroup;
            serializedObject.FindProperty("headerSubtitleText").objectReferenceValue = subtitle != null ? subtitle.GetComponent<Text>() : null;
            serializedObject.FindProperty("zoneText").objectReferenceValue = zonePill != null ? zonePill.GetComponent<Text>() : null;
            serializedObject.FindProperty("objectiveText").objectReferenceValue = objectiveText != null ? objectiveText.GetComponent<Text>() : null;
            serializedObject.FindProperty("contextText").objectReferenceValue = contextText != null ? contextText.GetComponent<Text>() : null;
            serializedObject.FindProperty("contextGroup").objectReferenceValue = contextCanvasGroup;
            serializedObject.FindProperty("showInEditor").boolValue = true;
            serializedObject.FindProperty("showOnDesktop").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(overlay);
        }

        private static void EnsureEventSystem()
        {
            var eventSystemObject = GameObject.Find("EventSystem");
            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
            }

            if (eventSystemObject.GetComponent<EventSystem>() == null)
            {
                eventSystemObject.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.DestroyImmediate(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static MobileJoystick EnsureJoystick(RectTransform canvasRect)
        {
            var background = EnsureUiRect(canvasRect, "MoveJoystick", new Vector2(0f, 0f), new Vector2(128f, 130f), new Vector2(150f, 150f));
            var backgroundImage = EnsureImage(background.gameObject, new Color(0.12f, 0.18f, 0.22f, 0.24f));
            backgroundImage.raycastTarget = true;
            EnsureOutline(background.gameObject, new Color(0.42f, 0.37f, 0.26f, 0.84f), new Vector2(1f, -1f));
            EnsureShadow(background.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -4f));

            var pulseRing = EnsureUiRect(background, "PulseRing", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124f, 124f));
            var pulseRingImage = EnsureImage(pulseRing.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.1f));
            pulseRingImage.raycastTarget = false;
            EnsureOutline(pulseRing.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.44f), new Vector2(1f, -1f));

            var innerPlate = EnsureUiRect(background, "InnerPlate", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            EnsureImage(innerPlate.gameObject, new Color(0.08f, 0.11f, 0.14f, 0.7f)).raycastTarget = false;

            var joystick = background.GetComponent<MobileJoystick>();
            if (joystick == null)
            {
                joystick = background.gameObject.AddComponent<MobileJoystick>();
            }

            var handle = EnsureUiRect(background, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
            var handleImage = EnsureImage(handle.gameObject, new Color(0.9f, 0.95f, 1f, 0.58f));
            handleImage.raycastTarget = false;
            EnsureOutline(handle.gameObject, new Color(0.05f, 0.06f, 0.08f, 0.7f), new Vector2(1f, -1f));
            EnsureShadow(handle.gameObject, new Color(0f, 0f, 0f, 0.26f), new Vector2(0f, -3f));

            var centerDot = EnsureUiRect(handle, "CenterDot", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
            EnsureImage(centerDot.gameObject, new Color(0.12f, 0.16f, 0.18f, 0.9f)).raycastTarget = false;

            var serializedObject = new SerializedObject(joystick);
            serializedObject.FindProperty("background").objectReferenceValue = background;
            serializedObject.FindProperty("handle").objectReferenceValue = handle;
            serializedObject.FindProperty("backgroundImage").objectReferenceValue = backgroundImage;
            serializedObject.FindProperty("handleImage").objectReferenceValue = handleImage;
            serializedObject.FindProperty("pulseRingImage").objectReferenceValue = pulseRingImage;
            serializedObject.FindProperty("handleRange").floatValue = 44f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(joystick);

            return joystick;
        }

        private static MobileLookArea EnsureLookArea(RectTransform canvasRect)
        {
            var lookAreaRect = EnsureUiRect(canvasRect, "LookArea", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var image = EnsureImage(lookAreaRect.gameObject, new Color(0.07f, 0.12f, 0.13f, 0.015f));
            image.raycastTarget = true;
            EnsureOutline(lookAreaRect.gameObject, new Color(0.26f, 0.76f, 0.72f, 0.12f), new Vector2(1f, -1f));
            lookAreaRect.SetAsFirstSibling();

            var lookArea = lookAreaRect.GetComponent<MobileLookArea>();
            if (lookArea == null)
            {
                lookArea = lookAreaRect.gameObject.AddComponent<MobileLookArea>();
            }

            var serializedObject = new SerializedObject(lookArea);
            serializedObject.FindProperty("overlayGraphic").objectReferenceValue = image;
            serializedObject.FindProperty("sensitivity").floatValue = 1.75f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lookArea);

            return lookArea;
        }

        private static MobileButton EnsureMobileButton(
            RectTransform canvasRect,
            string name,
            string label,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var buttonRect = EnsureUiRect(canvasRect, name, anchor, anchoredPosition, size);
            var image = EnsureImage(buttonRect.gameObject, color);
            image.raycastTarget = true;
            RuntimeUiFactory.ApplyOneUiRounding(buttonRect.gameObject, Mathf.Min(size.x, size.y) * 0.42f);
            EnsureOutline(buttonRect.gameObject, new Color(0.42f, 0.37f, 0.26f, 0.82f), new Vector2(1f, -1f));
            EnsureShadow(buttonRect.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -4f));
            SoftenGraphicEffects(buttonRect.gameObject, 0.14f);

            var accent = EnsureUiRect(buttonRect, "Accent", new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(size.x * 0.72f, 4f));
            EnsureImage(accent.gameObject, new Color(0.88f, 0.71f, 0.31f, 0.88f)).raycastTarget = false;
            accent.gameObject.SetActive(false);

            var contentRect = EnsureUiRect(buttonRect, "Content", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            contentRect.localScale = Vector3.one;

            var mobileButton = buttonRect.GetComponent<MobileButton>();
            if (mobileButton == null)
            {
                mobileButton = buttonRect.gameObject.AddComponent<MobileButton>();
            }

            var text = EnsureText(contentRect, "Label", label, size.y >= 100f ? 23 : 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            text.rectTransform.anchorMin = new Vector2(0f, 0f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.offsetMin = new Vector2(8f, 8f);
            text.rectTransform.offsetMax = new Vector2(-8f, -8f);

            var subLabel = EnsureText(contentRect, "SubLabel", "DOKUN", 11, FontStyle.Bold, TextAnchor.LowerCenter, new Color(0.73f, 0.76f, 0.74f, 0.92f));
            subLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            subLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            subLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            subLabel.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            subLabel.rectTransform.sizeDelta = new Vector2(-12f, 16f);
            subLabel.gameObject.SetActive(false);

            var serializedObject = new SerializedObject(mobileButton);
            serializedObject.FindProperty("background").objectReferenceValue = image;
            serializedObject.FindProperty("contentRoot").objectReferenceValue = contentRect;
            serializedObject.FindProperty("label").objectReferenceValue = text;
            serializedObject.FindProperty("idleColor").colorValue = color;
            serializedObject.FindProperty("pressedColor").colorValue = Color.Lerp(color, new Color(0.26f, 0.76f, 0.72f, 0.9f), 0.55f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mobileButton);

            return mobileButton;
        }

        private static void SoftenGraphicEffects(GameObject target, float maxAlpha)
        {
            var effects = target.GetComponents<Shadow>();
            for (var i = 0; i < effects.Length; i++)
            {
                var color = effects[i].effectColor;
                color.a = Mathf.Min(color.a, maxAlpha);
                effects[i].effectColor = color;
            }
        }

        private static RectTransform EnsureUiRect(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            GameObject gameObject;

            if (existing == null)
            {
                gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.transform.SetParent(parent, false);
            }
            else
            {
                gameObject = existing.gameObject;
            }

            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;

            return rectTransform;
        }

        private static Image EnsureImage(GameObject gameObject, Color color)
        {
            var image = gameObject.GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            image.color = color;
            return image;
        }

        private static Text EnsureText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor anchor,
            Color color)
        {
            var rect = EnsureUiRect(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = rect.GetComponent<Text>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<Text>();
            }

            text.text = value;
            text.alignment = anchor;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Outline EnsureOutline(GameObject gameObject, Color color, Vector2 distance)
        {
            var outline = gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        private static Shadow EnsureShadow(GameObject gameObject, Color color, Vector2 distance)
        {
            Shadow shadow = null;
            var shadows = gameObject.GetComponents<Shadow>();
            for (var i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] is Outline)
                {
                    continue;
                }

                shadow = shadows[i];
                break;
            }

            if (shadow == null)
            {
                shadow = gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            return shadow;
        }

        private static void EnsureSessionManager(CaseDefinition caseDefinition)
        {
            var managerObject = GameObject.Find(SessionManagerName);
            if (managerObject == null)
            {
                managerObject = new GameObject(SessionManagerName);
            }

            var manager = managerObject.GetComponent<CaseSessionManager>();
            if (manager == null)
            {
                manager = managerObject.AddComponent<CaseSessionManager>();
            }

            var serializedObject = new SerializedObject(manager);
            serializedObject.FindProperty("activeCase").objectReferenceValue = caseDefinition;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static CaseProgressTracker EnsureProgressTracker()
        {
            var gameObject = GameObject.Find("CaseProgressTracker");
            if (gameObject == null)
            {
                gameObject = new GameObject("CaseProgressTracker");
            }

            var tracker = gameObject.GetComponent<CaseProgressTracker>();
            if (tracker == null)
            {
                tracker = gameObject.AddComponent<CaseProgressTracker>();
            }

            return tracker;
        }

        private static void EnsureDebugHud(PlayerInteractionController playerInteraction)
        {
            var hudObject = GameObject.Find(DebugHudName);
            if (hudObject == null)
            {
                hudObject = new GameObject(DebugHudName);
            }

            var debugHud = hudObject.GetComponent<CaseDebugHud>();
            if (debugHud == null)
            {
                debugHud = hudObject.AddComponent<CaseDebugHud>();
            }

            var serializedObject = new SerializedObject(debugHud);
            serializedObject.FindProperty("playerInteraction").objectReferenceValue = playerInteraction;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(debugHud);
        }

        private static void DisableLegacyDebugHud()
        {
            var hudObject = GameObject.Find(DebugHudName);
            if (hudObject != null)
            {
                hudObject.SetActive(false);
            }
        }

        private static void EnsureHintDirector()
        {
            var gameObject = GameObject.Find("InvestigationHintDirector");
            if (gameObject == null)
            {
                gameObject = new GameObject("InvestigationHintDirector");
            }

            if (gameObject.GetComponent<InvestigationHintDirector>() == null)
            {
                gameObject.AddComponent<InvestigationHintDirector>();
            }
        }
        private static void EnsureStatusHud(PlayerInteractionController playerInteraction)
        {
            var hudObject = GameObject.Find("CaseStatusHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("CaseStatusHUD");
            }

            var statusHud = hudObject.GetComponent<CaseStatusHud>();
            if (statusHud == null)
            {
                statusHud = hudObject.AddComponent<CaseStatusHud>();
            }

            var serializedObject = new SerializedObject(statusHud);
            serializedObject.FindProperty("playerInteraction").objectReferenceValue = playerInteraction;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(statusHud);
        }

        private static void EnsureChecklistHud(CaseProgressTracker progressTracker)
        {
            var gameObject = GameObject.Find("CaseChecklistHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("CaseChecklistHUD");
            }

            var hud = gameObject.GetComponent<CaseChecklistHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<CaseChecklistHud>();
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("progressTracker").objectReferenceValue = progressTracker;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        private static void EnsureModernGameplayHud(PlayerInteractionController playerInteraction, CaseProgressTracker progressTracker)
        {
            var gameObject = GameObject.Find("ModernGameplayHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("ModernGameplayHUD");
            }

            var hud = gameObject.GetComponent<UguiGameplayHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<UguiGameplayHud>();
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("playerInteraction").objectReferenceValue = playerInteraction;
            serializedObject.FindProperty("progressTracker").objectReferenceValue = progressTracker;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            var statusHud = Object.FindAnyObjectByType<CaseStatusHud>();
            if (statusHud != null)
            {
                statusHud.enabled = false;
                EditorUtility.SetDirty(statusHud);
            }

            var checklistHud = Object.FindAnyObjectByType<CaseChecklistHud>();
            if (checklistHud != null)
            {
                checklistHud.enabled = false;
                EditorUtility.SetDirty(checklistHud);
            }

            var waypointHud = Object.FindAnyObjectByType<InvestigationWaypointHud>();
            if (waypointHud != null)
            {
                waypointHud.enabled = false;
                EditorUtility.SetDirty(waypointHud);
            }

            var bannerHud = Object.FindAnyObjectByType<LocationBannerHud>();
            if (bannerHud != null)
            {
                bannerHud.enabled = false;
                EditorUtility.SetDirty(bannerHud);
            }
        }
        private static void EnsureReticleHud(PlayerInteractionController playerInteraction)
        {
            var hudObject = GameObject.Find("FocusReticleHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("FocusReticleHUD");
            }

            var reticleHud = hudObject.GetComponent<FocusReticleHud>();
            if (reticleHud == null)
            {
                reticleHud = hudObject.AddComponent<FocusReticleHud>();
            }

            var serializedObject = new SerializedObject(reticleHud);
            serializedObject.FindProperty("playerInteraction").objectReferenceValue = playerInteraction;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            reticleHud.enabled = false;
            EditorUtility.SetDirty(reticleHud);

            if (hudObject.GetComponent<UguiReticleHud>() == null)
            {
                hudObject.AddComponent<UguiReticleHud>();
            }
        }

        private static void EnsureWorldMarkerHud(Camera targetCamera)
        {
            var gameObject = GameObject.Find("WorldMarkerHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("WorldMarkerHUD");
            }

            var hud = gameObject.GetComponent<WorldMarkerHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<WorldMarkerHud>();
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            hud.enabled = false;
            EditorUtility.SetDirty(hud);

            var modernHud = gameObject.GetComponent<UguiWorldMarkerHud>();
            if (modernHud == null)
            {
                modernHud = gameObject.AddComponent<UguiWorldMarkerHud>();
            }

            var modernSerialized = new SerializedObject(modernHud);
            modernSerialized.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            modernSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(modernHud);
        }

        private static void EnsureMinimapHud(Transform playerRoot)
        {
            var gameObject = GameObject.Find("SchoolMinimapHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("SchoolMinimapHUD");
            }

            var hud = gameObject.GetComponent<SchoolMinimapHud>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<SchoolMinimapHud>();
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("playerTarget").objectReferenceValue = playerRoot;
            serializedObject.FindProperty("worldMin").vector2Value = new Vector2(-22f, -10f);
            serializedObject.FindProperty("worldMax").vector2Value = new Vector2(22f, 30f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            hud.enabled = false;
            EditorUtility.SetDirty(hud);

            var modernHud = gameObject.GetComponent<UguiMinimapHud>();
            if (modernHud == null)
            {
                modernHud = gameObject.AddComponent<UguiMinimapHud>();
            }

            var modernSerialized = new SerializedObject(modernHud);
            modernSerialized.FindProperty("playerTarget").objectReferenceValue = playerRoot;
            modernSerialized.FindProperty("worldMin").vector2Value = new Vector2(-22f, -10f);
            modernSerialized.FindProperty("worldMax").vector2Value = new Vector2(22f, 30f);
            modernSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(modernHud);
        }
        private static void EnsureExplorationTracker()
        {
            var gameObject = GameObject.Find("SchoolExplorationTracker");
            if (gameObject == null)
            {
                gameObject = new GameObject("SchoolExplorationTracker");
            }

            if (gameObject.GetComponent<SchoolExplorationTracker>() == null)
            {
                gameObject.AddComponent<SchoolExplorationTracker>();
            }
        }
        private static void EnsureWaypointHud()
        {
            var gameObject = GameObject.Find("InvestigationWaypointHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("InvestigationWaypointHUD");
            }

            if (gameObject.GetComponent<InvestigationWaypointHud>() == null)
            {
                gameObject.AddComponent<InvestigationWaypointHud>();
            }
        }
        private static void EnsureLocationBannerHud()
        {
            var gameObject = GameObject.Find("LocationBannerHUD");
            if (gameObject == null)
            {
                gameObject = new GameObject("LocationBannerHUD");
            }

            if (gameObject.GetComponent<LocationBannerHud>() == null)
            {
                gameObject.AddComponent<LocationBannerHud>();
            }
        }

        private static void EnsureNotebookHud(MobileButton toggleButton)
        {
            var hudObject = GameObject.Find("CaseNotebookHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("CaseNotebookHUD");
            }

            var notebookHud = hudObject.GetComponent<CaseNotebookHud>();
            if (notebookHud == null)
            {
                notebookHud = hudObject.AddComponent<CaseNotebookHud>();
            }

            var serializedObject = new SerializedObject(notebookHud);
            serializedObject.FindProperty("toggleButton").objectReferenceValue = toggleButton;
            serializedObject.FindProperty("startOpen").boolValue = false;
            serializedObject.FindProperty("renderWithOnGui").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(notebookHud);

            if (hudObject.GetComponent<UguiNotebookHud>() == null)
            {
                hudObject.AddComponent<UguiNotebookHud>();
            }
        }

        private static void EnsureInvestigationDeskInteractable(SceneLayoutProfile layout)
        {
            var deskObject = GameObject.Find("InvestigationDesk");
            if (deskObject == null)
            {
                var root = GameObject.Find("SampleInvestigationRoot");
                if (root == null)
                {
                    root = new GameObject("SampleInvestigationRoot");
                }

                deskObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deskObject.name = "InvestigationDesk";
                deskObject.transform.SetParent(root.transform);
            }

            if (layout.UsesImportedSchoolMap)
            {
                deskObject.transform.position = layout.ToWorld(new Vector3(0.15f, 0.45f, -5.6f));
                deskObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                deskObject.transform.localScale = new Vector3(1.8f, 0.3f, 0.9f);

                var renderer = deskObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreatePreviewMaterial("InvestigationDesk_Material", new Color(0.45f, 0.28f, 0.14f, 1f));
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                EnsureInvestigationBoard(layout);
                EditorUtility.SetDirty(deskObject);
            }

            var interactable = deskObject.GetComponent<InvestigationDeskInteractable>();
            if (interactable == null)
            {
                interactable = deskObject.AddComponent<InvestigationDeskInteractable>();
            }

            var collider = deskObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = deskObject.AddComponent<BoxCollider>();
            }

            collider.isTrigger = false;
            collider.center = Vector3.zero;
            collider.size = Vector3.one;

            var serializedObject = new SerializedObject(interactable);
            var promptProperty = serializedObject.FindProperty("promptText");
            if (promptProperty != null)
            {
                promptProperty.stringValue = "Vaka masasini incele";
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
        }

        private static void EnsureInvestigationBoard(SceneLayoutProfile layout)
        {
            var root = GameObject.Find("SampleInvestigationRoot");
            if (root == null)
            {
                root = new GameObject("SampleInvestigationRoot");
            }

            var boardObject = GameObject.Find("InvestigationBoard");
            if (boardObject == null)
            {
                boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boardObject.name = "InvestigationBoard";
                boardObject.transform.SetParent(root.transform);
            }

            boardObject.transform.position = layout.ToWorld(new Vector3(-1.65f, 1.65f, -6.5f));
            boardObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            boardObject.transform.localScale = new Vector3(1.55f, 0.88f, 0.08f);

            var renderer = boardObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial("InvestigationBoard_Material", new Color(0.1f, 0.18f, 0.22f, 1f));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            EditorUtility.SetDirty(boardObject);
        }

        private static void EnsureImportedMapDoorInteractables(SceneLayoutProfile layout)
        {
            if (!layout.UsesImportedSchoolMap)
            {
                return;
            }

            RemoveInvalidImportedMapDoorInteractables();

            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = 0; i < transforms.Length; i++)
            {
                var doorTransform = transforms[i];
                if (!IsMovableImportedMapDoorMesh(doorTransform))
                {
                    continue;
                }

                var hinge = EnsureDoorHinge(doorTransform);
                if (hinge == null)
                {
                    continue;
                }

                var doorObject = doorTransform.gameObject;
                var hasNegativeScale = HasNegativeLossyScale(doorTransform);
                var collider = doorObject.GetComponent<Collider>();
                if (hasNegativeScale && collider is BoxCollider)
                {
                    Object.DestroyImmediate(collider);
                    collider = null;
                }

                if (collider == null)
                {
                    var meshFilter = doorObject.GetComponent<MeshFilter>();
                    collider = hasNegativeScale && meshFilter != null
                        ? (Collider)doorObject.AddComponent<MeshCollider>()
                        : doorObject.AddComponent<BoxCollider>();
                }

                collider.isTrigger = false;

                var childInteractable = doorObject.GetComponent<DoorInteractable>();
                if (childInteractable != null)
                {
                    Object.DestroyImmediate(childInteractable);
                }

                var interactable = hinge.GetComponent<DoorInteractable>();
                if (interactable == null)
                {
                    interactable = hinge.AddComponent<DoorInteractable>();
                }

                interactable.ConfigurePrompt("Kapiyi ac/kapat");
                interactable.ConfigureAccess(string.Empty, string.Empty, false);
                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(interactable);
                EditorUtility.SetDirty(doorObject);
                EditorUtility.SetDirty(hinge);
            }
        }

        private static void RemoveInvalidImportedMapDoorInteractables()
        {
            var interactables = Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include);
            for (var i = 0; i < interactables.Length; i++)
            {
                var interactable = interactables[i];
                if (interactable == null)
                {
                    continue;
                }

                if (interactable.name.StartsWith("DoorHinge_", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Object.DestroyImmediate(interactable);
            }
        }

        private static GameObject EnsureDoorHinge(Transform doorTransform)
        {
            if (doorTransform == null)
            {
                return null;
            }

            if (doorTransform.parent != null && doorTransform.parent.name.StartsWith("DoorHinge_", System.StringComparison.OrdinalIgnoreCase))
            {
                return doorTransform.parent.gameObject;
            }

            var hingeName = "DoorHinge_" + doorTransform.name.Replace(" ", "_").Replace("(", string.Empty).Replace(")", string.Empty);
            var existingHinge = doorTransform.parent != null ? doorTransform.parent.Find(hingeName) : null;
            var hinge = existingHinge != null ? existingHinge.gameObject : new GameObject(hingeName);
            var originalParent = doorTransform.parent;
            var originalSiblingIndex = doorTransform.GetSiblingIndex();
            var hingePosition = CalculateDoorHingePosition(doorTransform);

            hinge.transform.SetParent(originalParent, false);
            hinge.transform.position = hingePosition;
            hinge.transform.rotation = doorTransform.rotation;
            hinge.transform.localScale = Vector3.one;
            hinge.transform.SetSiblingIndex(originalSiblingIndex);
            doorTransform.SetParent(hinge.transform, true);
            EditorUtility.SetDirty(hinge);
            EditorUtility.SetDirty(doorTransform);
            return hinge;
        }

        private static Vector3 CalculateDoorHingePosition(Transform doorTransform)
        {
            var meshFilter = doorTransform.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var bounds = meshFilter.sharedMesh.bounds;
                var localHinge = bounds.center + Vector3.left * bounds.extents.x;
                return doorTransform.TransformPoint(localHinge);
            }

            var renderer = doorTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds.center - doorTransform.right * renderer.bounds.extents.x;
            }

            return doorTransform.position - doorTransform.right * 0.45f;
        }

        private static bool IsMovableImportedMapDoorMesh(Transform target)
        {
            if (target == null || !target.gameObject.scene.IsValid())
            {
                return false;
            }

            if (target.GetComponent<Renderer>() == null)
            {
                return false;
            }

            var root = target.root;
            if (root != null && root.name == SchoolBlockRootName)
            {
                return false;
            }

            var name = target.name.Trim();
            var lowerName = name.ToLowerInvariant();
            if (lowerName.Contains("wall") || lowerName.Contains("frame") || lowerName.Contains("marker"))
            {
                return false;
            }

            return lowerName == "door" ||
                   lowerName.StartsWith("door ", System.StringComparison.Ordinal) ||
                   lowerName.StartsWith("door(", System.StringComparison.Ordinal) ||
                   lowerName.StartsWith("door_", System.StringComparison.Ordinal) ||
                   lowerName.StartsWith("kapi", System.StringComparison.Ordinal);
        }

        private static void RepairNegativeScaleBoxColliders(SceneLayoutProfile layout)
        {
            if (!layout.UsesImportedSchoolMap)
            {
                return;
            }

            var boxColliders = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include);
            for (var i = 0; i < boxColliders.Length; i++)
            {
                var boxCollider = boxColliders[i];
                if (boxCollider == null || !HasNegativeLossyScale(boxCollider.transform))
                {
                    continue;
                }

                var meshFilter = boxCollider.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    boxCollider.enabled = false;
                    EditorUtility.SetDirty(boxCollider);
                    continue;
                }

                var gameObject = boxCollider.gameObject;
                var meshCollider = gameObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                Object.DestroyImmediate(boxCollider);
                EditorUtility.SetDirty(meshCollider);
                EditorUtility.SetDirty(gameObject);
            }
        }

        private static bool HasNegativeLossyScale(Transform target)
        {
            return target != null &&
                   (target.lossyScale.x < 0f || target.lossyScale.y < 0f || target.lossyScale.z < 0f);
        }

        private static void DisableExtraAudioListeners(Camera primaryCamera)
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null)
                {
                    continue;
                }

                var shouldStayEnabled = primaryCamera != null && listener.transform.IsChildOf(primaryCamera.transform);
                listener.enabled = shouldStayEnabled;
                EditorUtility.SetDirty(listener);
            }
        }

        private static void EnsureResultHud()
        {
            var hudObject = GameObject.Find("CaseResultHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("CaseResultHUD");
            }

            var legacyHud = hudObject.GetComponent<CaseResultHud>();
            if (legacyHud == null)
            {
                legacyHud = hudObject.AddComponent<CaseResultHud>();
            }

            legacyHud.enabled = false;
            EditorUtility.SetDirty(legacyHud);

            if (hudObject.GetComponent<UguiCaseResultHud>() == null)
            {
                hudObject.AddComponent<UguiCaseResultHud>();
            }
        }
        private static void EnsureMainMenuHud()
        {
            var hudObject = GameObject.Find("MainMenuHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("MainMenuHUD");
            }

            var hud = hudObject.GetComponent<MainMenuHud>();
            if (hud == null)
            {
                hud = hudObject.AddComponent<MainMenuHud>();
            }

            var networkManagerObject = GameObject.Find("NetworkManager");
            if (networkManagerObject == null)
            {
                return;
            }

            var relayBootstrap = networkManagerObject.GetComponent<RelayNetworkBootstrap>();
            if (relayBootstrap == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(hud);
            serializedObject.FindProperty("bootstrap").objectReferenceValue = relayBootstrap;
            serializedObject.FindProperty("startOpen").boolValue = true;
            serializedObject.FindProperty("restartCaseOnStart").boolValue = true;
            serializedObject.FindProperty("renderWithOnGui").boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            if (hudObject.GetComponent<UguiMainMenuHud>() == null)
            {
                hudObject.AddComponent<UguiMainMenuHud>();
            }
        }

        private static void EnsureDirectionalLight()
        {
            var light = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && candidate.type == LightType.Directional);

            var lightObject = GameObject.Find("Directional Light");
            if (lightObject == null)
            {
                lightObject = light != null ? light.gameObject : new GameObject("Directional Light");
            }

            light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(42f, -138f, 0f);
            light.intensity = 0.035f;
            light.color = new Color(0.86f, 0.9f, 0.95f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.25f;
            light.bounceIntensity = 0.18f;
            light.shadows = LightShadows.None;
            RenderSettings.sun = null;
        }

        private static void EnsureAtmosphereLights(SceneLayoutProfile layout)
        {
            var root = GameObject.Find(AtmosphereRootName);
            if (root == null)
            {
                root = new GameObject(AtmosphereRootName);
            }

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            if (layout.UsesImportedSchoolMap)
            {
                CreateImportedMapIndoorLighting(root.transform);
            }
            else
            {
                CreateFluorescentLight(root.transform, "CorridorLightEntrance", layout.ToWorld(new Vector3(0f, 2.72f, -6f)), new Color(0.88f, 0.95f, 1f), 1.05f, 4.6f, true);
                CreateFluorescentLight(root.transform, "CorridorLightMainA", layout.ToWorld(new Vector3(0f, 2.72f, -1f)), new Color(0.88f, 0.95f, 1f), 1f, 4.6f, true);
                CreateFluorescentLight(root.transform, "CorridorLightMainB", layout.ToWorld(new Vector3(0f, 2.72f, 4f)), new Color(0.88f, 0.95f, 1f), 0.96f, 4.6f, true);
                CreateFluorescentLight(root.transform, "CorridorLightMainC", layout.ToWorld(new Vector3(0f, 2.72f, 9f)), new Color(0.86f, 0.94f, 1f), 0.92f, 4.6f, true);
                CreateFluorescentLight(root.transform, "CorridorLightMainD", layout.ToWorld(new Vector3(0f, 2.72f, 14f)), new Color(0.86f, 0.94f, 1f), 0.88f, 4.6f, true);
            }

            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.fogColor = new Color(0.18f, 0.2f, 0.22f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.53f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.34f, 0.28f);
            RenderSettings.ambientIntensity = 1.35f;
            RenderSettings.reflectionIntensity = 0.22f;
            EnsureCinematicPostProcess();
        }

        private static void CreateImportedMapIndoorLighting(Transform parent)
        {
            var corridorColor = new Color(0.74f, 0.82f, 0.86f);
            var corridorZ = new[] { -642f, -632f, -622f, -612f, -602f, -592f, -582f, -572f, -562f, -552f, -542f, -532f, -522f };
            const float corridorCenterX = 922.15f;

            for (var i = 0; i < corridorZ.Length; i++)
            {
                var edge = i == 0 || i == corridorZ.Length - 1;
                var intensity = edge ? 0.105f : 0.15f;
                var range = edge ? 7.2f : 8.6f;
                CreateAreaFill(parent, $"CorridorCeilingFill_{i + 1:00}", new Vector3(corridorCenterX, 6.9f, corridorZ[i]), corridorColor, intensity, range);
            }

            CreateAreaFill(parent, "CorridorSoftBaseNorth", new Vector3(corridorCenterX, 5.75f, -622f), new Color(0.58f, 0.65f, 0.68f), 0.08f, 25f);
            CreateAreaFill(parent, "CorridorSoftBaseCenter", new Vector3(corridorCenterX, 5.75f, -584f), new Color(0.58f, 0.65f, 0.68f), 0.095f, 28f);
            CreateAreaFill(parent, "CorridorSoftBaseSouth", new Vector3(corridorCenterX, 5.75f, -546f), new Color(0.58f, 0.65f, 0.68f), 0.08f, 25f);

            CreateRoomCeilingLight(parent, "SecurityRoomSoftWash", new Vector3(925.5f, 6.55f, -613f), new Color(0.68f, 0.78f, 0.9f), 0.13f, 4.6f, false);
            CreateRoomCeilingLight(parent, "LibrarySoftWash", new Vector3(918.9f, 6.55f, -557f), new Color(0.9f, 0.82f, 0.68f), 0.12f, 4.8f, false);
            CreateRoomCeilingLight(parent, "TeachersRoomSoftWash", new Vector3(925.5f, 6.55f, -557f), new Color(0.9f, 0.8f, 0.66f), 0.11f, 4.6f, false);
        }

        private static void CreateFluorescentLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range, bool createFixture)
        {
            if (createFixture)
            {
                var fixture = CreateBlock(parent, name + "_Fixture", position + Vector3.up * 0.04f, new Vector3(2.1f, 0.045f, 0.18f), new Color(0.72f, 0.8f, 0.84f));
                ApplyEmission(fixture, new Color(0.68f, 0.86f, 1f), 0.22f);
            }

            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            lightObject.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = 104f;
            light.innerSpotAngle = 72f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;

            var flicker = lightObject.AddComponent<LightFlicker>();
            var serializedObject = new SerializedObject(flicker);
            serializedObject.FindProperty("targetLight").objectReferenceValue = light;
            serializedObject.FindProperty("baseIntensity").floatValue = intensity;
            serializedObject.FindProperty("flickerAmount").floatValue = 0.035f;
            serializedObject.FindProperty("flickerSpeed").floatValue = 2.2f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flicker);
        }

        private static void CreateRoomCeilingLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range, bool createFixture)
        {
            if (createFixture)
            {
                var fixture = CreateBlock(parent, name + "_Panel", position + Vector3.up * 0.04f, new Vector3(1.45f, 0.045f, 1.05f), new Color(0.78f, 0.78f, 0.72f));
                ApplyEmission(fixture, color, 0.12f);
            }

            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = 112f;
            light.innerSpotAngle = 74f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        private static void CreateAreaFill(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        private static void CreateRoomAccentLight(Transform parent, string name, Vector3 position, Color color, float intensity)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 5f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        private static void CreateWindowSunSpot(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 eulerAngles,
            float intensity,
            float range,
            float spotAngle)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.Euler(eulerAngles);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.78f, 0.48f);
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = spotAngle * 0.48f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.42f;
            light.shadowBias = 0.025f;
            light.shadowNormalBias = 0.18f;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        private static void ApplyEmission(GameObject target, Color color, float intensity)
        {
            if (target == null)
            {
                return;
            }

            var renderer = target.GetComponent<Renderer>();
            var material = renderer == null ? null : renderer.sharedMaterial;
            if (material == null || !material.HasProperty("_EmissionColor"))
            {
                return;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            EditorUtility.SetDirty(material);
        }

        private static void CreateSunPatch(Transform parent, string name, Vector3 position, Vector3 scale, float yaw)
        {
            var patch = CreateBlock(parent, name, position, scale, new Color(1f, 0.71f, 0.34f, 0.48f));
            patch.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var renderer = patch.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var material = renderer.sharedMaterial;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.48f, 0.14f) * 0.75f);
            }
        }

        private static void CreateReflectionProbe(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var probeObject = new GameObject(name);
            probeObject.transform.SetParent(parent);
            probeObject.transform.position = position;

            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.size = size;
            probe.intensity = 0.65f;
            probe.resolution = 64;
            probe.hdr = true;
        }

        private static void EnsureCinematicPostProcess()
        {
            var volumeObject = GameObject.Find("Global Volume") ?? GameObject.Find("Global Volume (1)") ?? new GameObject("Global Volume");
            var volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            EnsureFolder("Assets/map/Scenes/SampleScene");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(CinematicVolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, CinematicVolumeProfilePath);
            }

            volume.sharedProfile = profile;
            profile.components.RemoveAll(component => component == null);

            var tonemapping = EnsureVolumeComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = EnsureVolumeComponent<Bloom>(profile);
            bloom.threshold.Override(2.4f);
            bloom.intensity.Override(0f);
            bloom.scatter.Override(0.2f);
            bloom.highQualityFiltering.Override(true);

            var colorAdjustments = EnsureVolumeComponent<ColorAdjustments>(profile);
            colorAdjustments.postExposure.Override(0.05f);
            colorAdjustments.contrast.Override(-2f);
            colorAdjustments.saturation.Override(-2f);
            colorAdjustments.colorFilter.Override(Color.white);

            var shadowsMidtonesHighlights = EnsureVolumeComponent<ShadowsMidtonesHighlights>(profile);
            shadowsMidtonesHighlights.shadows.Override(new Vector4(1.03f, 1.05f, 1.07f, 0.08f));
            shadowsMidtonesHighlights.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
            shadowsMidtonesHighlights.highlights.Override(new Vector4(0.98f, 0.99f, 1f, -0.02f));
            shadowsMidtonesHighlights.shadowsStart.Override(0f);
            shadowsMidtonesHighlights.shadowsEnd.Override(0.42f);
            shadowsMidtonesHighlights.highlightsStart.Override(0.55f);
            shadowsMidtonesHighlights.highlightsEnd.Override(1f);

            var whiteBalance = EnsureVolumeComponent<WhiteBalance>(profile);
            whiteBalance.temperature.Override(11f);
            whiteBalance.tint.Override(-4f);

            var vignette = EnsureVolumeComponent<Vignette>(profile);
            vignette.intensity.Override(0.04f);
            vignette.smoothness.Override(0.35f);
            vignette.color.Override(new Color(0.02f, 0.028f, 0.035f));

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(volume);
            AssetDatabase.SaveAssets();
        }

        private static T EnsureVolumeComponent<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
            }

            if (!AssetDatabase.Contains(component))
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        private static void EnsureSampleSchoolBlock(SceneLayoutProfile layout)
        {
            var root = GameObject.Find(SchoolBlockRootName);
            if (layout.UsesImportedSchoolMap)
            {
                if (root != null)
                {
                    root.SetActive(false);
                    EditorUtility.SetDirty(root);
                }

                return;
            }

            if (root == null)
            {
                root = new GameObject(SchoolBlockRootName);
            }

            root.SetActive(true);

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            CreateBlock(root.transform, "MainFloor", new Vector3(0f, -0.05f, 4f), new Vector3(20f, 0.1f, 24f), new Color(0.45f, 0.48f, 0.46f));
            CreateBlock(root.transform, "CorridorFloor", new Vector3(0f, 0f, 0f), new Vector3(4f, 0.08f, 22f), new Color(0.55f, 0.58f, 0.52f));
            CreateBlock(root.transform, "CourtyardFloor", new Vector3(0f, -0.05f, 23f), new Vector3(18f, 0.1f, 10f), new Color(0.36f, 0.43f, 0.34f));
            CreateBlock(root.transform, "LeftAnnexFloor", new Vector3(-15f, -0.05f, 4f), new Vector3(10f, 0.1f, 16f), new Color(0.48f, 0.5f, 0.44f));
            CreateBlock(root.transform, "RightAnnexFloor", new Vector3(15f, -0.05f, 4f), new Vector3(10f, 0.1f, 16f), new Color(0.48f, 0.5f, 0.44f));

            CreateWall(root.transform, "LeftOuterWall", new Vector3(-10f, 1.5f, 4f), new Vector3(0.25f, 3f, 24f));
            CreateWall(root.transform, "RightOuterWall", new Vector3(10f, 1.5f, 4f), new Vector3(0.25f, 3f, 24f));
            CreateWall(root.transform, "BackWall", new Vector3(0f, 1.5f, 16f), new Vector3(20f, 3f, 0.25f));
            CreateWall(root.transform, "FrontWallLeft", new Vector3(-6f, 1.5f, -8f), new Vector3(8f, 3f, 0.25f));
            CreateWall(root.transform, "FrontWallRight", new Vector3(6f, 1.5f, -8f), new Vector3(8f, 3f, 0.25f));
            CreateWall(root.transform, "CourtyardBackWall", new Vector3(0f, 1.5f, 28f), new Vector3(18f, 3f, 0.25f));
            CreateWall(root.transform, "CourtyardLeftWall", new Vector3(-9f, 1.5f, 23f), new Vector3(0.25f, 3f, 10f));
            CreateWall(root.transform, "CourtyardRightWall", new Vector3(9f, 1.5f, 23f), new Vector3(0.25f, 3f, 10f));
            CreateWall(root.transform, "LeftAnnexOuterWall", new Vector3(-20f, 1.5f, 4f), new Vector3(0.25f, 3f, 16f));
            CreateWall(root.transform, "RightAnnexOuterWall", new Vector3(20f, 1.5f, 4f), new Vector3(0.25f, 3f, 16f));
            CreateWall(root.transform, "LeftAnnexBackWall", new Vector3(-15f, 1.5f, 12f), new Vector3(10f, 3f, 0.25f));
            CreateWall(root.transform, "RightAnnexBackWall", new Vector3(15f, 1.5f, 12f), new Vector3(10f, 3f, 0.25f));

            CreateWall(root.transform, "LeftRoomDividerA", new Vector3(-4f, 1.5f, -4f), new Vector3(0.2f, 3f, 7f));
            CreateWall(root.transform, "LeftRoomDividerB", new Vector3(-4f, 1.5f, 8f), new Vector3(0.2f, 3f, 7f));
            CreateWall(root.transform, "RightRoomDividerA", new Vector3(4f, 1.5f, -4f), new Vector3(0.2f, 3f, 7f));
            CreateWall(root.transform, "RightRoomDividerB", new Vector3(4f, 1.5f, 8f), new Vector3(0.2f, 3f, 7f));

            CreateWall(root.transform, "LeftMiddleWall", new Vector3(-7f, 1.5f, 4f), new Vector3(6f, 3f, 0.2f));
            CreateWall(root.transform, "RightMiddleWall", new Vector3(7f, 1.5f, 4f), new Vector3(6f, 3f, 0.2f));

            CreateDoorMarker(root.transform, "ClassroomDoor", new Vector3(-4f, 0.05f, -1f));
            CreateDoorMarker(root.transform, "LibraryDoor", new Vector3(4f, 0.05f, 2f));
            CreateDoorMarker(root.transform, "SecurityDoor", new Vector3(-4f, 0.05f, 10.5f));
            CreateDoorMarker(root.transform, "TeachersRoomDoor", new Vector3(4f, 0.05f, 10.5f));

            CreateRoomLabel(root.transform, "SINIF", new Vector3(-7f, 0.08f, -4f));
            CreateRoomLabel(root.transform, "KUTUPHANE", new Vector3(7f, 0.08f, -2f));
            CreateRoomLabel(root.transform, "GUVENLIK", new Vector3(-7f, 0.08f, 10f));
            CreateRoomLabel(root.transform, "OGRETMENLER", new Vector3(7f, 0.08f, 10f));
            CreateRoomLabel(root.transform, "KORIDOR", new Vector3(0f, 0.08f, 4f));
            CreateRoomLabel(root.transform, "AVLU", new Vector3(0f, 0.08f, 23f));
            CreateRoomLabel(root.transform, "LAB", new Vector3(-15f, 0.08f, 4f));
            CreateRoomLabel(root.transform, "SPOR", new Vector3(15f, 0.08f, 4f));

            CreateDesk(root.transform, "ClassroomDeskA", new Vector3(-7.5f, 0.45f, -5.5f));
            CreateDesk(root.transform, "ClassroomDeskB", new Vector3(-6f, 0.45f, -2.5f));
            CreateDesk(root.transform, "LibraryTable", new Vector3(7f, 0.45f, -2f));
            CreateDesk(root.transform, "SecurityConsole", new Vector3(-7f, 0.55f, 10f));
            CreateDesk(root.transform, "TeachersDesk", new Vector3(7f, 0.55f, 10f));
            CreateDesk(root.transform, "LabBenchA", new Vector3(-16.5f, 0.55f, 1f));
            CreateDesk(root.transform, "LabBenchB", new Vector3(-13.5f, 0.55f, 6f));
            CreateBench(root.transform, "CorridorBenchA", new Vector3(1.8f, 0.32f, -4.5f));
            CreateBench(root.transform, "CorridorBenchB", new Vector3(1.8f, 0.32f, 6.5f));
            CreateBench(root.transform, "CourtyardBenchA", new Vector3(-4f, 0.32f, 22f));
            CreateBench(root.transform, "CourtyardBenchB", new Vector3(4f, 0.32f, 24f));
            CreateLockerRow(root.transform, "LeftLockers", new Vector3(-9.15f, 1.1f, 1f), 5);
            CreateLockerRow(root.transform, "RightLockers", new Vector3(9.15f, 1.1f, 6f), 4);
            CreateNoticeBoard(root.transform, "HallNoticeBoard", new Vector3(3.85f, 1.65f, -6f));
            CreateNoticeBoard(root.transform, "TeachersNoticeBoard", new Vector3(3.85f, 1.65f, 12.5f));
            CreateBookshelf(root.transform, "LibraryShelfA", new Vector3(8.5f, 1.05f, -4.8f));
            CreateBookshelf(root.transform, "LibraryShelfB", new Vector3(8.5f, 1.05f, 0.2f));
            CreateStairBlock(root.transform, "LeftStairs", new Vector3(-10.6f, 0.1f, 2.5f), -1f);
            CreateStairBlock(root.transform, "RightStairs", new Vector3(10.6f, 0.1f, 2.5f), 1f);
            CreateInvestigationCorner(root.transform);
            CreateFloreswaDecorationSet(root.transform);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            CreateBlock(parent, name, position, scale, new Color(0.72f, 0.74f, 0.68f));
        }

        private static void EnsureHideSpots(SceneLayoutProfile layout)
        {
            CreateHideSpot(
                "HideSpot_CorridorBench",
                layout.ToWorld(new Vector3(1.8f, 0.65f, 6.5f)),
                new Vector3(1.85f, 0.42f, 0.72f),
                "Bankta sakinles",
                "Koridor bankinda oturup dikkat seviyeni dusurdun.");

            CreateHideSpot(
                "HideSpot_LibraryShelf",
                layout.ToWorld(new Vector3(8.45f, 1.1f, -4.8f)),
                new Vector3(0.9f, 1.8f, 1.4f),
                "Raf arkasinda bekle",
                "Raflarin arasinda bekleyip nefesini toparladin.");

            CreateHideSpot(
                "HideSpot_CourtyardBench",
                layout.ToWorld(new Vector3(-4f, 0.65f, 22f)),
                new Vector3(1.85f, 0.42f, 0.72f),
                "Avluda sakinles",
                "Avluya cekilip dikkat seviyeni dusurdun.");
        }

        private static void CreateDoorMarker(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name, position, new Vector3(1.4f, 0.08f, 0.7f), new Color(0.25f, 0.65f, 0.85f));
        }

        private static void CreateHideSpot(string name, Vector3 position, Vector3 scale, string promptText, string successMessage)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var hideSpot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hideSpot.name = name;
            hideSpot.transform.position = position;
            hideSpot.transform.localScale = scale;

            var renderer = hideSpot.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(name + "_Material", new Color(0.18f, 0.32f, 0.28f, 1f));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var collider = hideSpot.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }

            var interactable = hideSpot.GetComponent<HideSpotInteractable>();
            if (interactable == null)
            {
                interactable = hideSpot.AddComponent<HideSpotInteractable>();
            }

            var serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("promptText").stringValue = promptText;
            serializedObject.FindProperty("successMessage").stringValue = successMessage;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
        }

        private static void CreateDesk(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name, position, new Vector3(1.8f, 0.3f, 0.9f), new Color(0.45f, 0.28f, 0.14f));
        }

        private static void CreateBench(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name + "_Seat", position, new Vector3(1.9f, 0.18f, 0.55f), new Color(0.43f, 0.26f, 0.16f));
            CreateBlock(parent, name + "_Back", position + new Vector3(0f, 0.42f, -0.22f), new Vector3(1.9f, 0.7f, 0.12f), new Color(0.48f, 0.3f, 0.18f));
            CreateBlock(parent, name + "_LegA", position + new Vector3(-0.75f, -0.22f, 0f), new Vector3(0.12f, 0.44f, 0.12f), new Color(0.18f, 0.18f, 0.2f));
            CreateBlock(parent, name + "_LegB", position + new Vector3(0.75f, -0.22f, 0f), new Vector3(0.12f, 0.44f, 0.12f), new Color(0.18f, 0.18f, 0.2f));
        }

        private static void CreateLockerRow(Transform parent, string name, Vector3 startPosition, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var offset = new Vector3(0f, 0f, i * 1.12f);
                CreateBlock(parent, $"{name}_{i}", startPosition + offset, new Vector3(0.7f, 2.1f, 0.9f), new Color(0.32f, 0.44f, 0.58f));
                CreateBlock(parent, $"{name}_{i}_Handle", startPosition + offset + new Vector3(0.38f, 0f, 0f), new Vector3(0.04f, 0.18f, 0.18f), new Color(0.86f, 0.86f, 0.82f));
            }
        }

        private static void CreateNoticeBoard(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name + "_Frame", position, new Vector3(1.8f, 1.1f, 0.08f), new Color(0.46f, 0.3f, 0.15f));
            CreateBlock(parent, name + "_Board", position + new Vector3(0f, 0f, -0.02f), new Vector3(1.55f, 0.88f, 0.04f), new Color(0.64f, 0.53f, 0.28f));
            CreateBlock(parent, name + "_PinA", position + new Vector3(-0.42f, 0.18f, -0.04f), new Vector3(0.22f, 0.16f, 0.02f), new Color(0.92f, 0.88f, 0.7f));
            CreateBlock(parent, name + "_PinB", position + new Vector3(0.32f, -0.08f, -0.04f), new Vector3(0.28f, 0.18f, 0.02f), new Color(0.8f, 0.88f, 0.98f));
        }

        private static void ConfigureFloreswaCharacterImports()
        {
            for (var i = 0; i < FloreswaNpcPrefabPaths.Length; i++)
            {
                var modelPath = FloreswaNpcPrefabPaths[i]
                    .Replace("/Prefabs/", "/Models/")
                    .Replace(".prefab", ".fbx");
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                var changed = false;
                changed |= SetImporterValue(importer.animationType, ModelImporterAnimationType.Human, value => importer.animationType = value);
                changed |= SetImporterValue(importer.avatarSetup, ModelImporterAvatarSetup.CreateFromThisModel, value => importer.avatarSetup = value);
                changed |= SetImporterValue(importer.importCameras, false, value => importer.importCameras = value);
                changed |= SetImporterValue(importer.importLights, false, value => importer.importLights = value);
                changed |= SetImporterValue(importer.importAnimation, true, value => importer.importAnimation = value);
                changed |= SetImporterValue(importer.animationCompression, ModelImporterAnimationCompression.Optimal, value => importer.animationCompression = value);
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool SetImporterValue<T>(T currentValue, T nextValue, System.Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            {
                return false;
            }

            setter(nextValue);
            return true;
        }

        private static void CreateInvestigationCorner(Transform parent)
        {
            CreateRoomLabel(parent, "VAKA MASASI", new Vector3(0f, 0.08f, -6.2f));
            CreateNoticeBoard(parent, "InvestigationBoard", new Vector3(-1.65f, 1.65f, -6.5f));
            CreateDesk(parent, "InvestigationDesk", new Vector3(0.15f, 0.45f, -5.6f));
            CreateBench(parent, "InvestigationBench", new Vector3(0.15f, 0.32f, -4.65f));
            CreateBlock(parent, "InvestigationLampBase", new Vector3(1.15f, 0.8f, -5.75f), new Vector3(0.14f, 0.7f, 0.14f), new Color(0.18f, 0.18f, 0.22f));
            CreateBlock(parent, "InvestigationLampHead", new Vector3(1.15f, 1.2f, -5.48f), new Vector3(0.42f, 0.12f, 0.24f), new Color(0.88f, 0.82f, 0.48f));
        }

        private static void CreateFloreswaDecorationSet(Transform parent)
        {
            if (!AssetDatabase.IsValidFolder(FloreswaPrefabFolder))
            {
                return;
            }

            CreateImportedAsset(parent, FloreswaPrefabFolder + "/sofa.prefab", "Floreswa_Sofa_Corridor", new Vector3(1.8f, 0.05f, 6.5f), new Vector3(1.9f, 0.9f, 0.72f), new Vector3(0f, 180f, 0f), true);
            CreateImportedAsset(parent, FloreswaPrefabFolder + "/sofa.prefab", "Floreswa_Sofa_Courtyard", new Vector3(-4f, 0.05f, 22f), new Vector3(1.9f, 0.9f, 0.72f), new Vector3(0f, 12f, 0f), true);
            CreateImportedAsset(parent, FloreswaPrefabFolder + "/glasses01.prefab", "Floreswa_Glasses_TeacherDesk", new Vector3(7.25f, 0.86f, 10.08f), new Vector3(0.38f, 0.12f, 0.22f), new Vector3(0f, 28f, 0f), true);
            CreateImportedAsset(parent, FloreswaPrefabFolder + "/glasses02.prefab", "Floreswa_Glasses_LibraryTable", new Vector3(7.12f, 0.86f, -1.78f), new Vector3(0.38f, 0.12f, 0.22f), new Vector3(0f, -18f, 0f), true);
        }

        private static GameObject CreateImportedAsset(Transform parent, string prefabPath, string name, Vector3 bottomCenter, Vector3 approximateSize, Vector3 eulerAngles, bool markStatic)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.name = name;
            instance.transform.position = bottomCenter;
            instance.transform.rotation = Quaternion.Euler(eulerAngles);
            instance.transform.localScale = Vector3.one;
            FitImportedAssetToApproximateSize(instance.transform, approximateSize);
            AlignImportedAssetBottomCenter(instance.transform, bottomCenter);
            if (markStatic)
            {
                SetStaticFlags(instance);
            }

            return instance;
        }

        private static void FitImportedAssetToApproximateSize(Transform root, Vector3 approximateSize)
        {
            var bounds = CalculateRendererBounds(root);
            if (!bounds.HasValue)
            {
                return;
            }

            var size = bounds.Value.size;
            if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f)
            {
                return;
            }

            var scale = Mathf.Min(
                approximateSize.x / size.x,
                Mathf.Min(approximateSize.y / size.y, approximateSize.z / size.z));
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0.001f)
            {
                return;
            }

            root.localScale *= scale;
        }

        private static void AlignImportedAssetBottomCenter(Transform root, Vector3 bottomCenter)
        {
            var bounds = CalculateRendererBounds(root);
            if (!bounds.HasValue)
            {
                return;
            }

            var delta = new Vector3(
                bottomCenter.x - bounds.Value.center.x,
                bottomCenter.y - bounds.Value.min.y,
                bottomCenter.z - bounds.Value.center.z);
            root.position += delta;
        }

        private static Bounds? CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds? bounds = null;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (bounds.HasValue)
                {
                    var next = bounds.Value;
                    next.Encapsulate(renderers[i].bounds);
                    bounds = next;
                }
                else
                {
                    bounds = renderers[i].bounds;
                }
            }

            return bounds;
        }

        private static void SetStaticFlags(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            root.isStatic = true;
            for (var i = 0; i < root.transform.childCount; i++)
            {
                SetStaticFlags(root.transform.GetChild(i).gameObject);
            }
        }

        private static void CreateBookshelf(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name + "_Body", position, new Vector3(0.6f, 2.1f, 2.4f), new Color(0.34f, 0.21f, 0.12f));
            CreateBlock(parent, name + "_ShelfA", position + new Vector3(-0.02f, 0.55f, 0f), new Vector3(0.58f, 0.08f, 2.28f), new Color(0.45f, 0.28f, 0.16f));
            CreateBlock(parent, name + "_ShelfB", position + new Vector3(-0.02f, 0f, 0f), new Vector3(0.58f, 0.08f, 2.28f), new Color(0.45f, 0.28f, 0.16f));
            CreateBlock(parent, name + "_ShelfC", position + new Vector3(-0.02f, -0.55f, 0f), new Vector3(0.58f, 0.08f, 2.28f), new Color(0.45f, 0.28f, 0.16f));
        }

        private static void CreateStairBlock(Transform parent, string name, Vector3 basePosition, float direction)
        {
            for (var i = 0; i < 5; i++)
            {
                CreateBlock(
                    parent,
                    $"{name}_{i}",
                    basePosition + new Vector3(direction * i * 0.42f, i * 0.16f, 0f),
                    new Vector3(0.42f, 0.16f, 1.4f),
                    new Color(0.52f, 0.54f, 0.58f));
            }
        }

        private static void CreateRoomLabel(Transform parent, string text, Vector3 position)
        {
            var labelObject = new GameObject(text + "_Label");
            labelObject.transform.SetParent(parent);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.45f;
            textMesh.color = Color.black;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;

            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(name + "_Material", color);
            }

            return block;
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

        private static void EnsureEvidenceObjects(CaseDefinition caseDefinition, SceneLayoutProfile layout)
        {
            var root = GameObject.Find(EvidenceRootName);
            if (root == null)
            {
                root = new GameObject(EvidenceRootName);
            }

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            CreateEvidenceObject(
                root.transform,
                caseDefinition,
                "evidence.security-log",
                "Etkilesim: Guvenlik Kaydi",
                layout.ToWorld(new Vector3(-7.2f, 1.12f, 11.2f)),
                PrimitiveType.Cube,
                new Vector3(1.45f, 0.72f, 1.1f));

            CreateEvidenceObject(
                root.transform,
                caseDefinition,
                "evidence.answer-key-note",
                "Etkilesim: Not Kagidi",
                layout.ToWorld(new Vector3(6.8f, 0.75f, -2.6f)),
                PrimitiveType.Cube,
                new Vector3(1.2f, 0.2f, 1.2f));

            CreateToolPickupObject(
                root.transform,
                "tool.archive-pass",
                "Arsiv Gecis Karti",
                "Arsiv Gecis Karti",
                "Etkilesim: Arsiv Gecis Karti",
                "Arsiv gecis karti alindi. Artik kisitli raf alanina girebilirsin.",
                layout.ToWorld(new Vector3(7.1f, 0.86f, 9.3f)),
                PrimitiveType.Cylinder,
                new Vector3(0.36f, 0.08f, 0.36f),
                new Color(0.22f, 0.88f, 0.82f, 1f));

            CreateToolPickupObject(
                root.transform,
                "tool.lockpick",
                "Maymuncuk Seti",
                "Maymuncuk Seti",
                "Etkilesim: Maymuncuk Seti",
                "Maymuncuk seti alindi. Kilitli cekmece ve kutulari artik acabilirsin.",
                layout.ToWorld(new Vector3(-6.2f, 0.82f, 8.7f)),
                PrimitiveType.Cylinder,
                new Vector3(0.34f, 0.12f, 0.34f),
                new Color(0.96f, 0.68f, 0.18f, 1f));

            CreateSearchSpotObject(
                root.transform,
                caseDefinition,
                "evidence.locker-key",
                "Ogretmen Masasi Cekmecesi",
                "Cekmeceyi ara",
                "tool.lockpick",
                "Bu cekmece icin once maymuncuk seti bulman gerekiyor.",
                "Ogretmenler odasindaki cekmecede yedek anahtar bulundu.",
                layout.ToWorld(new Vector3(6.5f, 0.8f, 10.5f)),
                new Vector3(1.35f, 0.42f, 1.08f),
                new Color(0.74f, 0.62f, 0.28f, 1f));

            CreateSearchSpotObject(
                root.transform,
                caseDefinition,
                "evidence.archive-ledger",
                "Arsiv Raf Kutusu",
                "Kutuyu tara",
                "tool.archive-pass",
                "Arsiv raf kutusu icin once gecis karti bulman gerekiyor.",
                "Arsiv rafinda sakli defter bulundu.",
                layout.ToWorld(new Vector3(-14.5f, 0.86f, 9.6f)),
                new Vector3(1.7f, 0.82f, 1.35f),
                new Color(0.45f, 0.68f, 0.84f, 1f));
        }

        private static void EnsureNpcObjects(CaseDefinition caseDefinition, SceneLayoutProfile layout)
        {
            var root = GameObject.Find(NpcRootName);
            if (root == null)
            {
                root = new GameObject(NpcRootName);
            }

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.guard",
                "Guvenlik Gorevlisi",
                "Etkilesim: Guvenlik Gorevlisi ile konus",
                layout.ToActorWorld(new Vector3(-6.2f, 0.95f, 9.8f)),
                "Kayitlari gormeden kimseyi suclayamam.",
                "Kamera kaydini bulduysan soyleyebilirim: gece 22:15'te bilisim kulubu ogrencisi laboratuvar koridorundaydi.",
                "evidence.security-log",
                "evidence.guard-testimony",
                new Color(0.2f, 0.35f, 0.8f),
                new[]
                {
                    Vector3.zero,
                    layout.ToPatrolOffset(new Vector3(0.25f, 0f, 0.9f)),
                    layout.ToPatrolOffset(new Vector3(-0.25f, 0f, -0.8f))
                });

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.library-student",
                "Kutuphane Ogrencisi",
                "Etkilesim: Ogrenci ile konus",
                layout.ToActorWorld(new Vector3(6.2f, 0.95f, -2.4f)),
                "O notun kime ait oldugunu bilmiyorum.",
                "Cevap anahtari notunu gordum. Bilisim kulubu ogrencisinin defterinden dustu.",
                "evidence.answer-key-note",
                "evidence.student-testimony",
                new Color(0.25f, 0.65f, 0.35f),
                new[]
                {
                    Vector3.zero,
                    layout.ToPatrolOffset(new Vector3(0.25f, 0f, 0.7f)),
                    layout.ToPatrolOffset(new Vector3(-0.25f, 0f, -0.7f))
                });

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.teacher-assistant",
                "Ogretmen Yardimcisi",
                "Etkilesim: Ogretmen Yardimcisi ile konus",
                layout.ToActorWorld(new Vector3(6.3f, 0.95f, 8.8f)),
                "Dolap anahtari kayboldu ama bunu herkes biliyor olabilir.",
                "Yedek anahtar bende degildi. Dolabin yanina en son bilisim kulubu ogrencisi geldi.",
                "evidence.locker-key",
                string.Empty,
                new Color(0.7f, 0.45f, 0.25f),
                new[]
                {
                    Vector3.zero,
                    layout.ToPatrolOffset(new Vector3(-0.25f, 0f, 0.75f)),
                    layout.ToPatrolOffset(new Vector3(0.25f, 0f, -0.65f))
                });

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.archive-clerk",
                "Arsiv Sorumlusu",
                "Etkilesim: Arsiv Sorumlusu ile konus",
                layout.ToActorWorld(new Vector3(-13.8f, 0.95f, 8.2f)),
                "Defter olmadan arsiv odasi hakkinda resmi bir sey soyleyemem.",
                "Giris defterine gore bilisim kulubu ogrencisi sinavdan hemen once arsiv anahtarini sormustu.",
                "evidence.archive-ledger",
                string.Empty,
                new Color(0.48f, 0.58f, 0.82f),
                new[]
                {
                    Vector3.zero,
                    layout.ToPatrolOffset(new Vector3(0.22f, 0f, 0.8f)),
                    layout.ToPatrolOffset(new Vector3(-0.22f, 0f, -0.7f))
                });

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.canteen-worker",
                "Kantin Calisani",
                "Etkilesim: Kantin Calisani ile konus",
                layout.ToActorWorld(new Vector3(13.5f, 0.95f, 8.4f)),
                "Gec saatte kim geldigini hatirlamiyorum.",
                "Simdi hatirladim; o nottan sonra ayni ogrenci gece enerji icecegi alip laboratuvar tarafina kostu.",
                "evidence.answer-key-note",
                "evidence.canteen-testimony",
                new Color(0.86f, 0.62f, 0.28f),
                new[]
                {
                    Vector3.zero,
                    layout.ToPatrolOffset(new Vector3(-0.25f, 0f, 0.65f)),
                    layout.ToPatrolOffset(new Vector3(0.25f, 0f, -0.65f))
                });
        }

        private static void CreateNpcObject(
            Transform parent,
            CaseDefinition caseDefinition,
            string npcId,
            string displayName,
            string promptText,
            Vector3 worldPosition,
            string defaultLine,
            string evidenceLine,
            string requiredEvidenceId,
            string witnessEvidenceId,
            Color color,
            Vector3[] patrolOffsets)
        {
            var npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObject.name = displayName;
            npcObject.transform.SetParent(parent);
            npcObject.transform.position = worldPosition;
            npcObject.transform.localScale = Vector3.one;

            var capsule = npcObject.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = NpcActorHeight;
                capsule.radius = NpcActorRadius;
                capsule.center = new Vector3(0f, NpcActorHeight * 0.5f, 0f);
            }

            var renderer = npcObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(displayName + "_Material", color);
                renderer.enabled = false;
            }

            CreateNpcCharacterVisual(npcObject.transform, displayName + "_Visual", color, npcId);

            var interactable = npcObject.GetComponent<NpcInteractable>();
            if (interactable == null)
            {
                interactable = npcObject.AddComponent<NpcInteractable>();
            }

            var patrol = npcObject.GetComponent<NpcPatrolController>();
            if (patrol == null)
            {
                patrol = npcObject.AddComponent<NpcPatrolController>();
            }

            var serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            serializedObject.FindProperty("npcId").stringValue = npcId;
            serializedObject.FindProperty("npcDisplayName").stringValue = displayName;
            serializedObject.FindProperty("defaultLine").stringValue = defaultLine;
            serializedObject.FindProperty("requiredEvidenceId").stringValue = requiredEvidenceId;
            serializedObject.FindProperty("evidenceLine").stringValue = evidenceLine;
            serializedObject.FindProperty("witnessEvidenceId").stringValue = witnessEvidenceId;
            serializedObject.FindProperty("collectWitnessEvidenceOnce").boolValue = true;
            serializedObject.FindProperty("promptText").stringValue = promptText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);

            var patrolSerializedObject = new SerializedObject(patrol);
            patrolSerializedObject.FindProperty("npcInteractable").objectReferenceValue = interactable;
            patrolSerializedObject.FindProperty("patrolEnabled").boolValue = patrolOffsets != null && patrolOffsets.Length > 1;
            patrolSerializedObject.FindProperty("moveSpeed").floatValue = NpcPatrolMoveSpeed;
            patrolSerializedObject.FindProperty("turnSpeed").floatValue = 5.8f;
            patrolSerializedObject.FindProperty("waitDuration").floatValue = 0.85f;
            patrolSerializedObject.FindProperty("viewDistance").floatValue = 6.6f;
            patrolSerializedObject.FindProperty("viewAngle").floatValue = 62f;
            patrolSerializedObject.FindProperty("sightPressurePerSecond").floatValue = 0.58f;
            patrolSerializedObject.FindProperty("eyeHeight").floatValue = NpcActorHeight * 0.78f;
            var offsetsProperty = patrolSerializedObject.FindProperty("patrolOffsets");
            offsetsProperty.arraySize = patrolOffsets == null ? 0 : patrolOffsets.Length;
            for (var i = 0; i < offsetsProperty.arraySize; i++)
            {
                offsetsProperty.GetArrayElementAtIndex(i).vector3Value = patrolOffsets[i];
            }
            patrolSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(patrol);

            if (patrolOffsets != null && patrolOffsets.Length > 1)
            {
                var facing = patrolOffsets.FirstOrDefault(offset => offset.sqrMagnitude > 0.01f);
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.01f)
                {
                    npcObject.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                }
            }
        }

        private static Renderer[] CreatePlaceholderCharacterVisual(Transform parent, string name, Color color)
        {
            var visualRoot = new GameObject(name);
            visualRoot.transform.SetParent(parent, false);
            visualRoot.transform.localPosition = Vector3.zero;

            var material = CreatePreviewMaterial(name + "_Material", color);
            var body = CreateVisualPrimitive(visualRoot.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.58f, 0.82f, 0.58f), material);
            var head = CreateVisualPrimitive(visualRoot.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.82f, 0f), new Vector3(0.42f, 0.42f, 0.42f), material);
            var leftArm = CreateVisualPrimitive(visualRoot.transform, "LeftArm", PrimitiveType.Cylinder, new Vector3(-0.43f, 1.12f, 0f), new Vector3(0.09f, 0.48f, 0.09f), material);
            var rightArm = CreateVisualPrimitive(visualRoot.transform, "RightArm", PrimitiveType.Cylinder, new Vector3(0.43f, 1.12f, 0f), new Vector3(0.09f, 0.48f, 0.09f), material);

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

        private static void NormalizePlayerActor(Transform playerRoot, Camera camera)
        {
            if (playerRoot == null)
            {
                return;
            }

            var groundY = ResolveSceneLayoutProfile().ActorGroundY;
            playerRoot.position = new Vector3(playerRoot.position.x, groundY, playerRoot.position.z);
            playerRoot.localScale = Vector3.one;

            var controller = playerRoot.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.height = ActorHeight;
                controller.radius = ActorRadius;
                controller.center = new Vector3(0f, ActorHeight * 0.5f, 0f);
                controller.stepOffset = 0.3f;
                EditorUtility.SetDirty(controller);
            }

            if (camera != null && camera.transform.IsChildOf(playerRoot))
            {
                camera.transform.localPosition = new Vector3(0f, PlayerEyeHeight, 0f);
                EditorUtility.SetDirty(camera.transform);
            }

            EditorUtility.SetDirty(playerRoot);
        }

        private static void NormalizeNpcActor(Transform npcRoot)
        {
            if (npcRoot == null)
            {
                return;
            }

            var groundY = ResolveSceneLayoutProfile().ActorGroundY;
            npcRoot.position = new Vector3(npcRoot.position.x, groundY, npcRoot.position.z);
            npcRoot.localScale = Vector3.one;

            var capsule = npcRoot.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = NpcActorHeight;
                capsule.radius = NpcActorRadius;
                capsule.center = new Vector3(0f, NpcActorHeight * 0.5f, 0f);
                EditorUtility.SetDirty(capsule);
            }

            EditorUtility.SetDirty(npcRoot);
        }

        private static bool ActorNeedsNormalization(Transform actorRoot)
        {
            if (actorRoot == null)
            {
                return false;
            }

            var groundY = ResolveSceneLayoutProfile().ActorGroundY;
            if (Mathf.Abs(actorRoot.position.y - groundY) > 0.01f)
            {
                return true;
            }

            if ((actorRoot.localScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                return true;
            }

            var characterController = actorRoot.GetComponent<CharacterController>();
            if (characterController != null)
            {
                return Mathf.Abs(characterController.height - ActorHeight) > 0.01f ||
                       Mathf.Abs(characterController.radius - ActorRadius) > 0.01f ||
                       (characterController.center - new Vector3(0f, ActorHeight * 0.5f, 0f)).sqrMagnitude > 0.0001f;
            }

            var capsule = actorRoot.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                return false;
            }

            return Mathf.Abs(capsule.height - NpcActorHeight) > 0.01f ||
                   Mathf.Abs(capsule.radius - NpcActorRadius) > 0.01f ||
                   (capsule.center - new Vector3(0f, NpcActorHeight * 0.5f, 0f)).sqrMagnitude > 0.0001f;
        }

        private static bool NpcNeedsFloreswaVisual(Transform npcRoot)
        {
            if (npcRoot == null || !AssetDatabase.IsValidFolder(FloreswaPrefabFolder))
            {
                return false;
            }

            for (var i = 0; i < npcRoot.childCount; i++)
            {
                var child = npcRoot.GetChild(i);
                if (child == null || !child.name.EndsWith("_Visual", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                var sourcePath = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
                return !sourcePath.StartsWith(FloreswaPrefabFolder, System.StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private static Renderer[] CreateNpcCharacterVisual(Transform parent, string name, Color fallbackColor, string npcId)
        {
            return ReplaceNpcCharacterVisual(parent, name, fallbackColor, npcId);
        }

        private static Renderer[] ReplaceNpcCharacterVisual(Transform parent, string name, Color fallbackColor, string npcId)
        {
            RemoveGeneratedCharacterVisuals(parent, name);

            var prefabPath = GetFloreswaNpcPrefabPath(npcId);
            var prefab = string.IsNullOrWhiteSpace(prefabPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return CreatePlaceholderCharacterVisual(parent, name, fallbackColor);
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return CreatePlaceholderCharacterVisual(parent, name, fallbackColor);
            }

            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            FitCharacterVisualToActor(instance.transform, NpcActorHeight);
            AttachNpcAnimator(instance, prefabPath);
            return instance.GetComponentsInChildren<Renderer>(true);
        }

        private static string GetFloreswaNpcPrefabPath(string npcId)
        {
            if (!AssetDatabase.IsValidFolder(FloreswaPrefabFolder))
            {
                return string.Empty;
            }

            var index = Mathf.Abs(string.IsNullOrWhiteSpace(npcId) ? 0 : npcId.GetHashCode()) % FloreswaNpcPrefabPaths.Length;
            if (!string.IsNullOrWhiteSpace(npcId))
            {
                if (npcId.IndexOf("guard", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = 0;
                }
                else if (npcId.IndexOf("student", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = 3;
                }
                else if (npcId.IndexOf("teacher", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = 6;
                }
                else if (npcId.IndexOf("archive", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = 4;
                }
                else if (npcId.IndexOf("canteen", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    index = 7;
                }
            }

            return FloreswaNpcPrefabPaths[index];
        }

        private static void AttachNpcAnimator(GameObject instance, string prefabPath)
        {
            if (instance == null)
            {
                return;
            }

            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            var avatar = LoadFloreswaAvatar(prefabPath);
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.runtimeAnimatorController = SchoolBoyCharacterSetupTool.LoadOrCreateFloreswaNpcAnimatorController();
            animator.speed = 1f;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.enabled = animator.runtimeAnimatorController != null;

            var proceduralAnimator = instance.GetComponent<FloreswaProceduralAnimator>();
            if (proceduralAnimator == null)
            {
                proceduralAnimator = instance.AddComponent<FloreswaProceduralAnimator>();
            }

            proceduralAnimator.enabled = animator.runtimeAnimatorController == null;
            var proceduralSerializedObject = new SerializedObject(proceduralAnimator);
            proceduralSerializedObject.FindProperty("runSpeed").floatValue = NpcPatrolMoveSpeed;
            proceduralSerializedObject.FindProperty("armSwingDegrees").floatValue = 4.2f;
            proceduralSerializedObject.FindProperty("legSwingDegrees").floatValue = 1.8f;
            proceduralSerializedObject.FindProperty("moveBobAmount").floatValue = 0.018f;
            proceduralSerializedObject.FindProperty("bodySwayDegrees").floatValue = 0.6f;
            proceduralSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(proceduralAnimator);

            var movementAnimator = instance.GetComponent<CharacterMovementAnimator>();
            if (movementAnimator == null)
            {
                movementAnimator = instance.AddComponent<CharacterMovementAnimator>();
            }

            var serializedObject = new SerializedObject(movementAnimator);
            serializedObject.FindProperty("animator").objectReferenceValue = animator;
            serializedObject.FindProperty("proceduralFallback").boolValue = animator.runtimeAnimatorController == null;
            serializedObject.FindProperty("runSpeed").floatValue = 1.85f;
            serializedObject.FindProperty("movingSpeedThreshold").floatValue = 0.05f;
            serializedObject.FindProperty("movingBlendFloor").floatValue = 0.48f;
            serializedObject.FindProperty("moveBobAmount").floatValue = 0.018f;
            serializedObject.FindProperty("swayDegrees").floatValue = 0.6f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(movementAnimator);

            var footFix = instance.GetComponent<NpcFootAlignmentFix>();
            if (footFix == null)
            {
                footFix = instance.AddComponent<NpcFootAlignmentFix>();
            }

            var footFixSerializedObject = new SerializedObject(footFix);
            footFixSerializedObject.FindProperty("animator").objectReferenceValue = animator;
            footFixSerializedObject.FindProperty("leftFootYawCorrection").floatValue = -8f;
            footFixSerializedObject.FindProperty("leftToeYawCorrection").floatValue = -4f;
            footFixSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(footFix);
        }

        private static Avatar LoadFloreswaAvatar(string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return null;
            }

            var modelPath = prefabPath
                .Replace("/Prefabs/", "/Models/")
                .Replace(".prefab", ".fbx");
            return AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<Avatar>()
                .FirstOrDefault(avatar => avatar != null && avatar.isValid);
        }

        private static void FitCharacterVisualToActor(Transform visualRoot)
        {
            FitCharacterVisualToActor(visualRoot, ActorHeight);
        }

        private static void FitCharacterVisualToActor(Transform visualRoot, float targetHeight)
        {
            var bounds = CalculateRendererBounds(visualRoot);
            if (!bounds.HasValue || bounds.Value.size.y <= 0.001f)
            {
                return;
            }

            var scale = Mathf.Max(0.01f, targetHeight) / bounds.Value.size.y;
            visualRoot.localScale *= scale;
            bounds = CalculateRendererBounds(visualRoot);
            if (bounds.HasValue && visualRoot.parent != null)
            {
                visualRoot.position += Vector3.up * (visualRoot.parent.position.y - bounds.Value.min.y);
            }
        }

        private static void EnsurePlayerCharacterVisual(Transform playerRoot, Camera camera)
        {
            if (playerRoot == null)
            {
                return;
            }

            EnsureLocalPlayerVisualLayer();
            RemoveGeneratedCharacterVisuals(playerRoot, PlayerAvatarVisualName);

            var prefab = SchoolBoyCharacterSetupTool.LoadCharacterPrefab();
            if (prefab == null)
            {
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, playerRoot) as GameObject;
            if (instance == null)
            {
                return;
            }

            instance.name = PlayerAvatarVisualName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetLayerRecursively(instance.transform, LocalPlayerVisualLayer);

            if (camera != null)
            {
                camera.cullingMask &= ~(1 << LocalPlayerVisualLayer);
                EditorUtility.SetDirty(camera);
            }
        }

        private static void RemoveGeneratedCharacterVisuals(Transform parent, string expectedName)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                var shouldRemove =
                    child.name == expectedName ||
                    child.name == PlayerAvatarVisualName ||
                    child.name.EndsWith("_Visual", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetComponent<CharacterMovementAnimator>() != null ||
                    child.GetComponent<PlaceholderCharacterVisual>() != null;

                if (shouldRemove)
                {
                    Object.DestroyImmediate(child.gameObject);
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

        private static void EnsureLocalPlayerVisualLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || layers.arraySize <= LocalPlayerVisualLayer)
            {
                return;
            }

            var layerProperty = layers.GetArrayElementAtIndex(LocalPlayerVisualLayer);
            if (string.IsNullOrWhiteSpace(layerProperty.stringValue))
            {
                layerProperty.stringValue = LocalPlayerVisualLayerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
            }
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

        private static void CreateEvidenceObject(
            Transform parent,
            CaseDefinition caseDefinition,
            string evidenceId,
            string promptText,
            Vector3 worldPosition,
            PrimitiveType primitiveType,
            Vector3 localScale)
        {
            var evidenceObject = GameObject.CreatePrimitive(primitiveType);
            evidenceObject.name = evidenceId;
            evidenceObject.transform.SetParent(parent);
            evidenceObject.transform.position = worldPosition;
            evidenceObject.transform.localScale = localScale;

            var interactable = evidenceObject.GetComponent<EvidenceInteractable>();
            if (interactable == null)
            {
                interactable = evidenceObject.AddComponent<EvidenceInteractable>();
            }

            var serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            serializedObject.FindProperty("evidenceId").stringValue = evidenceId;
            serializedObject.FindProperty("collectedVisual").objectReferenceValue = null;
            serializedObject.FindProperty("disableObjectOnCollect").boolValue = true;
            serializedObject.FindProperty("promptText").stringValue = promptText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);

            ApplyEvidenceVisuals(evidenceObject);
        }

        private static void CreateSearchSpotObject(
            Transform parent,
            CaseDefinition caseDefinition,
            string evidenceId,
            string label,
            string promptText,
            string requiredToolId,
            string missingToolMessage,
            string searchMessage,
            Vector3 worldPosition,
            Vector3 localScale,
            Color color)
        {
            var searchObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            searchObject.name = label;
            searchObject.transform.SetParent(parent);
            searchObject.transform.position = worldPosition;
            searchObject.transform.localScale = localScale;

            var renderer = searchObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(label + "_Material", color);
            }

            var interactable = searchObject.GetComponent<SearchSpotInteractable>();
            if (interactable == null)
            {
                interactable = searchObject.AddComponent<SearchSpotInteractable>();
            }

            var serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("caseDefinition").objectReferenceValue = caseDefinition;
            serializedObject.FindProperty("hiddenEvidenceId").stringValue = evidenceId;
            serializedObject.FindProperty("markerLabel").stringValue = label;
            serializedObject.FindProperty("markerColor").colorValue = color;
            serializedObject.FindProperty("searchDuration").floatValue = 0.95f;
            serializedObject.FindProperty("useDistance").floatValue = 5.25f;
            serializedObject.FindProperty("requiredToolId").stringValue = requiredToolId;
            serializedObject.FindProperty("missingToolMessage").stringValue = missingToolMessage;
            serializedObject.FindProperty("searchCompleteMessage").stringValue = searchMessage;
            serializedObject.FindProperty("promptText").stringValue = promptText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);

            ApplyEvidenceVisuals(searchObject);
        }

        private static void CreateToolPickupObject(
            Transform parent,
            string toolId,
            string toolDisplayName,
            string label,
            string promptText,
            string pickupMessage,
            Vector3 worldPosition,
            PrimitiveType primitiveType,
            Vector3 localScale,
            Color color)
        {
            var toolObject = GameObject.CreatePrimitive(primitiveType);
            toolObject.name = toolDisplayName;
            toolObject.transform.SetParent(parent);
            toolObject.transform.position = worldPosition;
            toolObject.transform.localScale = localScale;

            var renderer = toolObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(toolDisplayName + "_Material", color);
            }

            var interactable = toolObject.GetComponent<ToolPickupInteractable>();
            if (interactable == null)
            {
                interactable = toolObject.AddComponent<ToolPickupInteractable>();
            }

            var serializedObject = new SerializedObject(interactable);
            serializedObject.FindProperty("toolId").stringValue = toolId;
            serializedObject.FindProperty("toolDisplayName").stringValue = toolDisplayName;
            serializedObject.FindProperty("markerLabel").stringValue = label;
            serializedObject.FindProperty("markerColor").colorValue = color;
            serializedObject.FindProperty("pickupMessage").stringValue = pickupMessage;
            serializedObject.FindProperty("promptText").stringValue = promptText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);

            ApplyEvidenceVisuals(toolObject);
        }

        private static void ApplyEvidenceVisuals(GameObject evidenceObject)
        {
            var renderer = evidenceObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(evidenceObject.name + "_GlowMaterial", new Color(0.15f, 0.85f, 1f));
            }

            var glowLightObject = new GameObject("EvidenceGlow");
            glowLightObject.transform.SetParent(evidenceObject.transform);
            glowLightObject.transform.localPosition = Vector3.up * 0.55f;

            var glowLight = glowLightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(0.2f, 0.9f, 1f);
            glowLight.range = 2.2f;
            glowLight.intensity = 0.28f;

            var pulse = evidenceObject.GetComponent<EvidenceVisualPulse>();
            if (pulse == null)
            {
                pulse = evidenceObject.AddComponent<EvidenceVisualPulse>();
            }

            var serializedObject = new SerializedObject(pulse);
            serializedObject.FindProperty("targetRenderer").objectReferenceValue = renderer;
            serializedObject.FindProperty("glowLight").objectReferenceValue = glowLight;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pulse);
        }
    }
}






















