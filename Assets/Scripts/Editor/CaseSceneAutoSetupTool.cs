using MobilOfl.Case;
using MobilOfl.Gameplay;
using MobilOfl.Online;
using MobilOfl.UI;
using MobilOfl.Visuals;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MobilOfl.EditorTools
{
    public static class CaseSceneAutoSetupTool
    {
        private const string CaseAssetFolder = "Assets/Data/Cases";
        private const string CaseAssetPath = CaseAssetFolder + "/ExamTheftCase.asset";
        private const string EvidenceRootName = "SampleEvidenceRoot";
        private const string NpcRootName = "SampleNpcRoot";
        private const string PlayerRootName = "Player";
        private const string MobileControlsCanvasName = "MobileControlsCanvas";
        private const string SchoolBlockRootName = "SampleSchoolBlock";
        private const string AtmosphereRootName = "SampleAtmosphere";
        private const string SessionManagerName = "CaseSessionManager";
        private const string DebugHudName = "DebugHUD";
        private const string RecommendedCompanyName = "Mobil OFL";
        private const string RecommendedProductName = "MOBIL OFL";
        private const string RecommendedBundleVersion = "0.2.0";
        private const string RecommendedAndroidAppId = "com.mobilofl.prototype";
        private const string RecommendedIosAppId = "com.mobilofl.prototype";
        private const string RecommendedStandaloneAppId = "com.mobilofl.prototype";

        [MenuItem("Mobil OFL/Setup/Auto Setup Investigation Scene")]
        public static void AutoSetupInvestigationScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                EditorUtility.DisplayDialog("Mobil OFL", "Aktif sahne bulunamadi.", "Tamam");
                return;
            }

            ApplyRecommendedProjectSettingsInternal();
            EnsureSceneInBuildSettings(activeScene.path);
            EnsureFolder("Assets/Data");
            EnsureFolder(CaseAssetFolder);

            var caseDefinition = LoadOrCreateCaseDefinition();
            PopulateCaseDefinition(caseDefinition);

            var playerSetup = EnsurePlayer();

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
            EnsureSampleSchoolBlock();
            EnsureInvestigationDeskInteractable();
            EnsureAtmosphereLights();
            EnsureEvidenceObjects(caseDefinition);
            EnsureNpcObjects(caseDefinition);
            OnlinePrototypeSetupTool.ConfigureOnlinePrototypeInScene(false);
            EnsureMainMenuHud();

            EditorSceneManager.MarkSceneDirty(activeScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Mobil OFL",
                "Vaka asset'i olusturuldu, sahne kuruldu ve onerilen proje ayarlari uygulandi.",
                "Tamam");
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
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, RecommendedAndroidAppId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, RecommendedIosAppId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, RecommendedStandaloneAppId);
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

        private static PlayerSetup EnsurePlayer()
        {
            var playerRoot = GameObject.Find(PlayerRootName);
            if (playerRoot == null)
            {
                playerRoot = new GameObject(PlayerRootName);
            }

            playerRoot.transform.position = new Vector3(0f, 1f, -8f);
            playerRoot.transform.rotation = Quaternion.identity;

            var controller = playerRoot.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerRoot.AddComponent<CharacterController>();
            }

            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            var movement = playerRoot.GetComponent<PrototypeFirstPersonController>();
            if (movement == null)
            {
                movement = playerRoot.AddComponent<PrototypeFirstPersonController>();
            }

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.name = "Main Camera";
            camera.tag = "MainCamera";
            camera.transform.SetParent(playerRoot.transform);
            camera.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            camera.transform.localRotation = Quaternion.identity;

            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

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
            interactionSerializedObject.FindProperty("interactDistance").floatValue = 4f;
            interactionSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interaction);

            var movementSerializedObject = new SerializedObject(movement);
            movementSerializedObject.FindProperty("cameraPivot").objectReferenceValue = camera.transform;
            movementSerializedObject.FindProperty("lookSmoothing").floatValue = 24f;
            movementSerializedObject.FindProperty("maxLookDelta").floatValue = 34f;
            movementSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(movement);

            return new PlayerSetup
            {
                Root = playerRoot,
                Camera = camera,
                Interaction = interaction,
                Movement = movement
            };
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
            var joystick = EnsureJoystick(canvasRect);
            var lookArea = EnsureLookArea(canvasRect);
            var sprintButton = EnsureMobileButton(
                canvasRect,
                "SprintButton",
                "KOS",
                new Vector2(1f, 0f),
                new Vector2(-300f, 170f),
                new Vector2(145f, 145f),
                new Color(0.25f, 0.55f, 0.9f, 0.55f));
            var jumpButton = EnsureMobileButton(
                canvasRect,
                "JumpButton",
                "ZIPLA",
                new Vector2(1f, 0f),
                new Vector2(-150f, 300f),
                new Vector2(145f, 145f),
                new Color(0.9f, 0.7f, 0.25f, 0.6f));
            var interactButton = EnsureMobileButton(
                canvasRect,
                "InteractButton",
                "AL",
                new Vector2(1f, 0f),
                new Vector2(-150f, 150f),
                new Vector2(155f, 155f),
                new Color(0.25f, 0.85f, 0.45f, 0.65f));
            var notebookButton = EnsureMobileButton(
                canvasRect,
                "NotebookButton",
                "DOSYA",
                new Vector2(1f, 1f),
                new Vector2(-155f, -85f),
                new Vector2(180f, 88f),
                new Color(0.18f, 0.18f, 0.18f, 0.68f));

            var movementSerializedObject = new SerializedObject(playerSetup.Movement);
            movementSerializedObject.FindProperty("mobileMoveJoystick").objectReferenceValue = joystick;
            movementSerializedObject.FindProperty("mobileLookArea").objectReferenceValue = lookArea;
            movementSerializedObject.FindProperty("mobileSprintButton").objectReferenceValue = sprintButton;
            movementSerializedObject.FindProperty("mobileJumpButton").objectReferenceValue = jumpButton;
            movementSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSetup.Movement);

            var interactionSerializedObject = new SerializedObject(playerSetup.Interaction);
            interactionSerializedObject.FindProperty("mobileInteractButton").objectReferenceValue = interactButton;
            interactionSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerSetup.Interaction);

            return notebookButton;
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
            var background = EnsureUiRect(canvasRect, "MoveJoystick", new Vector2(0f, 0f), new Vector2(180f, 170f), new Vector2(230f, 230f));
            var backgroundImage = EnsureImage(background.gameObject, new Color(0.12f, 0.18f, 0.22f, 0.45f));
            backgroundImage.raycastTarget = true;

            var joystick = background.GetComponent<MobileJoystick>();
            if (joystick == null)
            {
                joystick = background.gameObject.AddComponent<MobileJoystick>();
            }

            var handle = EnsureUiRect(background, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(95f, 95f));
            EnsureImage(handle.gameObject, new Color(0.9f, 0.95f, 1f, 0.8f)).raycastTarget = false;

            var serializedObject = new SerializedObject(joystick);
            serializedObject.FindProperty("background").objectReferenceValue = background;
            serializedObject.FindProperty("handle").objectReferenceValue = handle;
            serializedObject.FindProperty("handleRange").floatValue = 72f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(joystick);

            return joystick;
        }

        private static MobileLookArea EnsureLookArea(RectTransform canvasRect)
        {
            var lookAreaRect = EnsureUiRect(canvasRect, "LookArea", new Vector2(1f, 0.5f), new Vector2(-480f, 0f), new Vector2(960f, 1080f));
            var image = EnsureImage(lookAreaRect.gameObject, new Color(0f, 0f, 0f, 0.01f));
            image.raycastTarget = true;

            var lookArea = lookAreaRect.GetComponent<MobileLookArea>();
            if (lookArea == null)
            {
                lookArea = lookAreaRect.gameObject.AddComponent<MobileLookArea>();
            }

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

            var mobileButton = buttonRect.GetComponent<MobileButton>();
            if (mobileButton == null)
            {
                mobileButton = buttonRect.gameObject.AddComponent<MobileButton>();
            }

            var labelRect = EnsureUiRect(buttonRect, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var text = labelRect.GetComponent<Text>();
            if (text == null)
            {
                text = labelRect.gameObject.AddComponent<Text>();
            }

            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 24;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;

            return mobileButton;
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

            var statusHud = Object.FindFirstObjectByType<CaseStatusHud>();
            if (statusHud != null)
            {
                statusHud.enabled = false;
                EditorUtility.SetDirty(statusHud);
            }

            var checklistHud = Object.FindFirstObjectByType<CaseChecklistHud>();
            if (checklistHud != null)
            {
                checklistHud.enabled = false;
                EditorUtility.SetDirty(checklistHud);
            }

            var waypointHud = Object.FindFirstObjectByType<InvestigationWaypointHud>();
            if (waypointHud != null)
            {
                waypointHud.enabled = false;
                EditorUtility.SetDirty(waypointHud);
            }

            var bannerHud = Object.FindFirstObjectByType<LocationBannerHud>();
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

                private static void EnsureInvestigationDeskInteractable()
        {
            var deskObject = GameObject.Find("InvestigationDesk");
            if (deskObject == null)
            {
                return;
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
            var light = Object.FindFirstObjectByType<Light>();
            if (light != null && light.type == LightType.Directional)
            {
                return;
            }

            var lightObject = GameObject.Find("Directional Light");
            if (lightObject == null)
            {
                lightObject = new GameObject("Directional Light");
            }

            light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 0.55f;
            light.color = new Color(0.78f, 0.86f, 1f);
        }

        private static void EnsureAtmosphereLights()
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

            CreateFluorescentLight(root.transform, "CorridorLightA", new Vector3(0f, 2.85f, -3f), new Color(0.7f, 0.95f, 1f), 2.2f);
            CreateFluorescentLight(root.transform, "CorridorLightB", new Vector3(0f, 2.85f, 5f), new Color(0.7f, 0.95f, 1f), 2.0f);
            CreateFluorescentLight(root.transform, "CorridorLightC", new Vector3(0f, 2.85f, 12f), new Color(0.7f, 0.95f, 1f), 1.8f);
            CreateRoomAccentLight(root.transform, "SecurityBlueGlow", new Vector3(-7f, 2.1f, 10f), new Color(0.15f, 0.45f, 1f), 2.2f);
            CreateRoomAccentLight(root.transform, "LibraryWarmGlow", new Vector3(7f, 2.1f, -2f), new Color(1f, 0.72f, 0.35f), 1.7f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.08f, 0.11f, 0.13f);
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.16f);
        }

        private static void CreateFluorescentLight(Transform parent, string name, Vector3 position, Color color, float intensity)
        {
            CreateBlock(parent, name + "_Fixture", position + Vector3.up * 0.07f, new Vector3(2.6f, 0.08f, 0.22f), new Color(0.8f, 0.9f, 0.95f));

            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = 6f;

            var flicker = lightObject.AddComponent<LightFlicker>();
            var serializedObject = new SerializedObject(flicker);
            serializedObject.FindProperty("targetLight").objectReferenceValue = light;
            serializedObject.FindProperty("baseIntensity").floatValue = intensity;
            serializedObject.FindProperty("flickerAmount").floatValue = 0.18f;
            serializedObject.FindProperty("flickerSpeed").floatValue = 5f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flicker);
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
        }

        private static void EnsureSampleSchoolBlock()
        {
            var root = GameObject.Find(SchoolBlockRootName);
            if (root == null)
            {
                root = new GameObject(SchoolBlockRootName);
            }

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
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            CreateBlock(parent, name, position, scale, new Color(0.72f, 0.74f, 0.68f));
        }

        private static void CreateDoorMarker(Transform parent, string name, Vector3 position)
        {
            CreateBlock(parent, name, position, new Vector3(1.4f, 0.08f, 0.7f), new Color(0.25f, 0.65f, 0.85f));
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

        private static void CreateInvestigationCorner(Transform parent)
        {
            CreateRoomLabel(parent, "VAKA MASASI", new Vector3(0f, 0.08f, -6.2f));
            CreateNoticeBoard(parent, "InvestigationBoard", new Vector3(-1.65f, 1.65f, -6.5f));
            CreateDesk(parent, "InvestigationDesk", new Vector3(0.15f, 0.45f, -5.6f));
            CreateBench(parent, "InvestigationBench", new Vector3(0.15f, 0.32f, -4.65f));
            CreateBlock(parent, "InvestigationLampBase", new Vector3(1.15f, 0.8f, -5.75f), new Vector3(0.14f, 0.7f, 0.14f), new Color(0.18f, 0.18f, 0.22f));
            CreateBlock(parent, "InvestigationLampHead", new Vector3(1.15f, 1.2f, -5.48f), new Vector3(0.42f, 0.12f, 0.24f), new Color(0.88f, 0.82f, 0.48f));
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

        private static void EnsureEvidenceObjects(CaseDefinition caseDefinition)
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
                new Vector3(-7f, 0.75f, 10f),
                PrimitiveType.Cube,
                new Vector3(1f, 1f, 1f));

            CreateEvidenceObject(
                root.transform,
                caseDefinition,
                "evidence.answer-key-note",
                "Etkilesim: Not Kagidi",
                new Vector3(7f, 0.75f, -2f),
                PrimitiveType.Cube,
                new Vector3(1.2f, 0.2f, 1.2f));

            CreateEvidenceObject(
                root.transform,
                caseDefinition,
                "evidence.locker-key",
                "Etkilesim: Yedek Anahtar",
                new Vector3(7f, 0.85f, 10f),
                PrimitiveType.Capsule,
                new Vector3(0.45f, 0.8f, 0.45f));

            CreateEvidenceObject(
                root.transform,
                caseDefinition,
                "evidence.archive-ledger",
                "Etkilesim: Arsiv Defteri",
                new Vector3(-15f, 0.82f, 9f),
                PrimitiveType.Cube,
                new Vector3(1.1f, 0.22f, 1.1f));
        }

        private static void EnsureNpcObjects(CaseDefinition caseDefinition)
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
                new Vector3(-2f, 0.95f, 9f),
                "Kayitlari gormeden kimseyi suclayamam.",
                "Kamera kaydini bulduysan soyleyebilirim: gece 22:15'te bilisim kulubu ogrencisi laboratuvar koridorundaydi.",
                "evidence.security-log",
                "evidence.guard-testimony",
                new Color(0.2f, 0.35f, 0.8f));

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.library-student",
                "Kutuphane Ogrencisi",
                "Etkilesim: Ogrenci ile konus",
                new Vector3(5f, 0.95f, -2f),
                "O notun kime ait oldugunu bilmiyorum.",
                "Cevap anahtari notunu gordum. Bilisim kulubu ogrencisinin defterinden dustu.",
                "evidence.answer-key-note",
                "evidence.student-testimony",
                new Color(0.25f, 0.65f, 0.35f));

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.teacher-assistant",
                "Ogretmen Yardimcisi",
                "Etkilesim: Ogretmen Yardimcisi ile konus",
                new Vector3(5f, 0.95f, 8f),
                "Dolap anahtari kayboldu ama bunu herkes biliyor olabilir.",
                "Yedek anahtar bende degildi. Dolabin yanina en son bilisim kulubu ogrencisi geldi.",
                "evidence.locker-key",
                string.Empty,
                new Color(0.7f, 0.45f, 0.25f));

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.archive-clerk",
                "Arsiv Sorumlusu",
                "Etkilesim: Arsiv Sorumlusu ile konus",
                new Vector3(-13.2f, 0.95f, 8.8f),
                "Defter olmadan arsiv odasi hakkinda resmi bir sey soyleyemem.",
                "Giris defterine gore bilisim kulubu ogrencisi sinavdan hemen once arsiv anahtarini sormustu.",
                "evidence.archive-ledger",
                string.Empty,
                new Color(0.48f, 0.58f, 0.82f));

            CreateNpcObject(
                root.transform,
                caseDefinition,
                "npc.canteen-worker",
                "Kantin Calisani",
                "Etkilesim: Kantin Calisani ile konus",
                new Vector3(13.2f, 0.95f, 8.8f),
                "Gec saatte kim geldigini hatirlamiyorum.",
                "Simdi hatirladim; o nottan sonra ayni ogrenci gece enerji icecegi alip laboratuvar tarafina kostu.",
                "evidence.answer-key-note",
                "evidence.canteen-testimony",
                new Color(0.86f, 0.62f, 0.28f));
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
            Color color)
        {
            var npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObject.name = displayName;
            npcObject.transform.SetParent(parent);
            npcObject.transform.position = worldPosition;
            npcObject.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

            var renderer = npcObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePreviewMaterial(displayName + "_Material", color);
            }

            var interactable = npcObject.GetComponent<NpcInteractable>();
            if (interactable == null)
            {
                interactable = npcObject.AddComponent<NpcInteractable>();
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
            glowLight.range = 3.5f;
            glowLight.intensity = 0.8f;

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






















