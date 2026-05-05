using System.Linq;
using MobilOfl.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MobilOfl.EditorTools
{
    [InitializeOnLoad]
    public static class SchoolBoyCharacterSetupTool
    {
        public const string ModelPath = "Assets/karakter/source/Final_SchoolBoy.fbx";
        public const string ProcessedModelPath = "Assets/karakter/processed/SchoolBoy_Animated.fbx";
        public const string IdlePath = "Assets/karakter/source/anims/Idle (9).fbx";
        public const string WalkPath = "Assets/karakter/source/anims/Walking (11).fbx";
        public const string RunPath = "Assets/karakter/source/anims/Running (5).fbx";
        public const string PaletteTexturePath = "Assets/karakter/textures/colors_10.png";
        public const string PaletteMaterialPath = "Assets/karakter/Materials/colors_10.mat";
        public const string FaceDetailMaterialPath = "Assets/karakter/Materials/schoolboy_face_details.mat";
        public const string SkinPatchMaterialPath = "Assets/karakter/Materials/schoolboy_skin_patch.mat";
        public const string HairPatchMaterialPath = "Assets/karakter/Materials/schoolboy_hair_patch.mat";
        public const string CharacterPrefabPath = "Assets/Prefabs/Characters/SchoolBoyCharacter.prefab";
        public const string ResourceCharacterPrefabPath = "Assets/Resources/Characters/SchoolBoyCharacter.prefab";
        public const string AnimatorControllerPath = "Assets/Animations/SchoolBoy/SchoolBoy.controller";
        public const string FloreswaNpcAnimatorControllerPath = "Assets/Animations/Floreswa/FloreswaNpc.controller";
        public const string FloreswaMixamoAnimatorControllerPath = "Assets/Animations/Floreswa/FloreswaMixamo.controller";
        public const string FloreswaMixamoAnimationFolder = "Assets/Floreswa/MixamoAnimations";
        public const string UniversalAnimationLibraryPath = "Assets/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
        private const float CharacterVisualHeight = 1.72f;

        static SchoolBoyCharacterSetupTool()
        {
            QueueProcessedCharacterSetup();
            EditorApplication.delayCall += AutoRepairCharacterAssets;
        }

        private static bool HasProcessedModel => System.IO.File.Exists(ProcessedModelPath);

        private static string ActiveModelPath => HasProcessedModel ? ProcessedModelPath : ModelPath;

        public static void QueueProcessedCharacterSetup()
        {
            EditorApplication.delayCall += ForceImportProcessedCharacterAssets;
        }

        private static void ForceImportProcessedCharacterAssets()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueProcessedCharacterSetup();
                return;
            }

            if (!HasProcessedModel)
            {
                return;
            }

            SetupSchoolBoyCharacterAssets();
        }

        [MenuItem("Mobil OFL/Characters/Setup School Boy Character")]
        public static void SetupSchoolBoyCharacterMenu()
        {
            var prefab = SetupSchoolBoyCharacterAssets();
            EditorUtility.DisplayDialog(
                "Mobil OFL Character Setup",
                prefab == null
                    ? "School boy model dosyasi bulunamadi. Assets/karakter/source klasorunu kontrol et."
                    : "School boy prefab ve animator hazirlandi.",
                "Tamam");
        }

        public static GameObject SetupSchoolBoyCharacterAssets()
        {
            if (!System.IO.File.Exists(ActiveModelPath))
            {
                return null;
            }

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Characters");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Characters");
            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/SchoolBoy");
            EnsureFolder("Assets/Animations/Floreswa");
            EnsureFolder(FloreswaMixamoAnimationFolder);
            EnsureFolder("Assets/karakter/Materials");

            ConfigurePaletteTexture();
            ConfigurePaletteMaterial();
            ConfigureModelImporter(ActiveModelPath, true);
            ConfigureUniversalAnimationLibraryImporter();
            ConfigureMixamoAnimationImporters();
            CreateOrUpdateFloreswaMixamoAnimatorController();

            if (!HasProcessedModel)
            {
                ConfigureModelImporter(IdlePath, false);
                ConfigureModelImporter(WalkPath, false);
                ConfigureModelImporter(RunPath, false);
                AssetDatabase.ImportAsset(IdlePath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(WalkPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(RunPath, ImportAssetOptions.ForceUpdate);
            }

            var controller = CreateOrUpdateAnimatorController();
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (existingPrefab != null)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourceCharacterPrefabPath) == null)
                {
                    AssetDatabase.CopyAsset(CharacterPrefabPath, ResourceCharacterPrefabPath);
                }

                return existingPrefab;
            }

            return CreateOrUpdatePrefab(controller);
        }

        public static RuntimeAnimatorController LoadOrCreateFloreswaNpcAnimatorController()
        {
            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/Floreswa");
            EnsureFolder(FloreswaMixamoAnimationFolder);
            ConfigureMixamoAnimationImporters();
            return CreateOrUpdateFloreswaMixamoAnimatorController();
        }

        [MenuItem("Mobil OFL/Characters/Build Floreswa Mixamo Animator")]
        public static void BuildFloreswaMixamoAnimatorMenu()
        {
            var controller = LoadOrCreateFloreswaNpcAnimatorController();
            EditorUtility.DisplayDialog(
                "Mobil OFL Mixamo",
                controller == null
                    ? "Assets/Floreswa/MixamoAnimations klasorune Mixamo FBX animasyonlarini ekle. Ornek: Idle, Walking, Running."
                    : "Floreswa Mixamo animator hazirlandi.",
                "Tamam");
        }

        private static void AutoRepairCharacterAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            if (!System.IO.File.Exists(ActiveModelPath))
            {
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            var resourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourceCharacterPrefabPath);
            if (controller != null &&
                ControllerHasPlayableBlendChildren(controller) &&
                ControllerUsesActiveModelClips(controller) &&
                resourcePrefab != null &&
                AnimationImportersAreGeneric())
            {
                return;
            }

            SetupSchoolBoyCharacterAssets();
        }

        public static GameObject LoadCharacterPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            return SetupSchoolBoyCharacterAssets();
        }

        private static bool AnimationImportersAreGeneric()
        {
            if (HasProcessedModel)
            {
                return AnimationImporterIsGeneric(ActiveModelPath);
            }

            return AnimationImporterIsGeneric(ModelPath) &&
                   AnimationImporterIsGeneric(IdlePath) &&
                   AnimationImporterIsGeneric(WalkPath) &&
                   AnimationImporterIsGeneric(RunPath);
        }

        private static bool AnimationImporterIsGeneric(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            return importer != null &&
                   importer.animationType == ModelImporterAnimationType.Generic;
        }

        private static void ConfigureModelImporter(string path, bool isMainModel)
        {
            if (!System.IO.File.Exists(path))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            changed |= SetImporterValue(importer.animationType, ModelImporterAnimationType.Generic, value => importer.animationType = value);
            changed |= SetImporterValue(
                importer.avatarSetup,
                isMainModel ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.NoAvatar,
                value => importer.avatarSetup = value);
            changed |= SetImporterValue(
                importer.materialImportMode,
                isMainModel ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None,
                value => importer.materialImportMode = value);
            changed |= SetImporterValue(importer.importCameras, false, value => importer.importCameras = value);
            changed |= SetImporterValue(importer.importLights, false, value => importer.importLights = value);
            changed |= SetImporterValue(importer.importBlendShapes, false, value => importer.importBlendShapes = value);
            changed |= SetImporterValue(importer.importAnimation, true, value => importer.importAnimation = value);
            changed |= SetImporterValue(importer.animationCompression, ModelImporterAnimationCompression.Optimal, value => importer.animationCompression = value);
            changed |= SetImporterValue(importer.importNormals, ModelImporterNormals.Calculate, value => importer.importNormals = value);
            changed |= SetImporterValue(importer.importTangents, ModelImporterTangents.None, value => importer.importTangents = value);

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                var clipName = GetClipName(path, isMainModel, clips[i].name, i);
                var loopClip = !isMainModel || path == ProcessedModelPath;
                changed |= SetClipValue(clips, i, clips[i].name, clipName, (clip, value) => clip.name = value);
                changed |= SetClipValue(clips, i, clips[i].loopTime, loopClip, (clip, value) => clip.loopTime = value);
                changed |= SetClipValue(clips, i, clips[i].loopPose, loopClip, (clip, value) => clip.loopPose = value);
                changed |= SetClipValue(clips, i, clips[i].lockRootRotation, true, (clip, value) => clip.lockRootRotation = value);
                changed |= SetClipValue(clips, i, clips[i].lockRootHeightY, true, (clip, value) => clip.lockRootHeightY = value);
                changed |= SetClipValue(clips, i, clips[i].lockRootPositionXZ, true, (clip, value) => clip.lockRootPositionXZ = value);
            }

            if (!changed)
            {
                return;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigurePaletteTexture()
        {
            var importer = AssetImporter.GetAtPath(PaletteTexturePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            changed |= SetImporterValue(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetImporterValue(importer.filterMode, FilterMode.Point, value => importer.filterMode = value);
            changed |= SetImporterValue(importer.npotScale, TextureImporterNPOTScale.None, value => importer.npotScale = value);
            changed |= SetImporterValue(importer.textureCompression, TextureImporterCompression.Uncompressed, value => importer.textureCompression = value);

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePaletteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
            if (material == null)
            {
                return;
            }

            material.doubleSidedGI = true;
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.25f);
            }

            EditorUtility.SetDirty(material);
        }

        private static bool SetImporterValue<T>(T currentValue, T nextValue, System.Action<T> setter)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            {
                return false;
            }

            setter(nextValue);
            return true;
        }

        private static bool SetClipValue<T>(
            ModelImporterClipAnimation[] clips,
            int index,
            T currentValue,
            T nextValue,
            System.Action<ModelImporterClipAnimation, T> setter)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            {
                return false;
            }

            var clip = clips[index];
            setter(clip, nextValue);
            clips[index] = clip;
            return true;
        }

        private static AnimatorController CreateOrUpdateAnimatorController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            EnsureFloatParameter(controller, "Speed");

            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            for (var i = stateMachine.states.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveState(stateMachine.states[i].state);
            }

            var oldBlendTrees = AssetDatabase.LoadAllAssetsAtPath(AnimatorControllerPath)
                .OfType<BlendTree>()
                .ToArray();
            for (var i = 0; i < oldBlendTrees.Length; i++)
            {
                Object.DestroyImmediate(oldBlendTrees[i], true);
            }

            var blendTree = new BlendTree
            {
                name = "LocomotionBlend",
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            if (HasProcessedModel)
            {
                AddBlendChild(blendTree, ActiveModelPath, 0f, "Idle");
                AddBlendChild(blendTree, ActiveModelPath, 0.45f, "Walk");
                AddBlendChild(blendTree, ActiveModelPath, 1f, "Run");
            }
            else
            {
                AddBlendChild(blendTree, IdlePath, 0f, "Idle");
                AddBlendChild(blendTree, WalkPath, 0.45f, "Walk");
                AddBlendChild(blendTree, RunPath, 1f, "Run");
            }

            if (blendTree.children == null || blendTree.children.Length < 3)
            {
                Debug.LogWarning("School boy locomotion blend tree eksik kliple olustu. Animasyon FBX import durumunu kontrol et.");
            }

            var locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = blendTree;
            stateMachine.defaultState = locomotion;
            AddUniversalAnimationLibraryStates(stateMachine);

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureUniversalAnimationLibraryImporter()
        {
            if (!System.IO.File.Exists(UniversalAnimationLibraryPath))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(UniversalAnimationLibraryPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            changed |= SetImporterValue(importer.animationType, ModelImporterAnimationType.Human, value => importer.animationType = value);
            changed |= SetImporterValue(importer.avatarSetup, ModelImporterAvatarSetup.CreateFromThisModel, value => importer.avatarSetup = value);
            changed |= SetImporterValue(importer.materialImportMode, ModelImporterMaterialImportMode.None, value => importer.materialImportMode = value);
            changed |= SetImporterValue(importer.importCameras, false, value => importer.importCameras = value);
            changed |= SetImporterValue(importer.importLights, false, value => importer.importLights = value);
            changed |= SetImporterValue(importer.importAnimation, true, value => importer.importAnimation = value);
            changed |= SetImporterValue(importer.animationCompression, ModelImporterAnimationCompression.Optimal, value => importer.animationCompression = value);

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                for (var i = 0; i < clips.Length; i++)
                {
                    var clipName = string.IsNullOrWhiteSpace(clips[i].name)
                        ? "UAL_Clip_" + i
                        : clips[i].name;
                    changed |= SetClipValue(clips, i, clips[i].name, clipName, (clip, value) => clip.name = value);
                    changed |= SetClipValue(clips, i, clips[i].loopTime, ShouldLoopLibraryClip(clipName), (clip, value) => clip.loopTime = value);
                    changed |= SetClipValue(clips, i, clips[i].loopPose, ShouldLoopLibraryClip(clipName), (clip, value) => clip.loopPose = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootRotation, true, (clip, value) => clip.lockRootRotation = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootHeightY, true, (clip, value) => clip.lockRootHeightY = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootPositionXZ, true, (clip, value) => clip.lockRootPositionXZ = value);
                }

                importer.clipAnimations = clips;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool ShouldLoopLibraryClip(string clipName)
        {
            return clipName.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   clipName.IndexOf("walk", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   clipName.IndexOf("run", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   clipName.IndexOf("loop", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddUniversalAnimationLibraryStates(AnimatorStateMachine stateMachine)
        {
            if (!System.IO.File.Exists(UniversalAnimationLibraryPath))
            {
                return;
            }

            var clips = LoadAnimationClips(UniversalAnimationLibraryPath)
                .Where(IsUsableClip)
                .Take(36)
                .ToArray();
            for (var i = 0; i < clips.Length; i++)
            {
                var state = stateMachine.AddState("UAL_" + SanitizeStateName(clips[i].name), new Vector3(520f, i * 44f, 0f));
                state.motion = clips[i];
                state.speed = 1f;
            }
        }

        private static AnimatorController CreateOrUpdateFloreswaNpcAnimatorController()
        {
            if (!System.IO.File.Exists(UniversalAnimationLibraryPath))
            {
                return null;
            }

            var clips = LoadAnimationClips(UniversalAnimationLibraryPath)
                .Where(IsUsableClip)
                .Take(12)
                .ToArray();
            if (clips.Length == 0)
            {
                return null;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(FloreswaNpcAnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(FloreswaNpcAnimatorControllerPath);
            }

            EnsureFloatParameter(controller, "Speed");

            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            for (var i = stateMachine.states.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveState(stateMachine.states[i].state);
            }

            var oldBlendTrees = AssetDatabase.LoadAllAssetsAtPath(FloreswaNpcAnimatorControllerPath)
                .OfType<BlendTree>()
                .ToArray();
            for (var i = 0; i < oldBlendTrees.Length; i++)
            {
                Object.DestroyImmediate(oldBlendTrees[i], true);
            }

            var blendTree = new BlendTree
            {
                name = "FloreswaLocomotionBlend",
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var idleClip = FindLibraryClip(clips, "idle") ?? clips[0];
            var walkClip = FindLibraryClip(clips, "walk") ?? (clips.Length > 1 ? clips[1] : idleClip);
            var runClip = FindLibraryClip(clips, "run") ?? (clips.Length > 2 ? clips[2] : walkClip);
            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(walkClip, 0.45f);
            blendTree.AddChild(runClip, 1f);

            var locomotion = stateMachine.AddState("FloreswaLocomotion");
            locomotion.motion = blendTree;
            stateMachine.defaultState = locomotion;

            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == idleClip || clip == walkClip || clip == runClip)
                {
                    continue;
                }

                var state = stateMachine.AddState("UAL_" + SanitizeStateName(clip.name), new Vector3(520f, i * 44f, 0f));
                state.motion = clip;
            }

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureMixamoAnimationImporters()
        {
            if (!AssetDatabase.IsValidFolder(FloreswaMixamoAnimationFolder))
            {
                return;
            }

            var paths = AssetDatabase.FindAssets("t:Model", new[] { FloreswaMixamoAnimationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();
            for (var i = 0; i < paths.Length; i++)
            {
                ConfigureMixamoAnimationImporter(paths[i]);
            }
        }

        private static void ConfigureMixamoAnimationImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            changed |= SetImporterValue(importer.animationType, ModelImporterAnimationType.Human, value => importer.animationType = value);
            changed |= SetImporterValue(importer.avatarSetup, ModelImporterAvatarSetup.CreateFromThisModel, value => importer.avatarSetup = value);
            changed |= SetImporterValue(importer.materialImportMode, ModelImporterMaterialImportMode.None, value => importer.materialImportMode = value);
            changed |= SetImporterValue(importer.importCameras, false, value => importer.importCameras = value);
            changed |= SetImporterValue(importer.importLights, false, value => importer.importLights = value);
            changed |= SetImporterValue(importer.importAnimation, true, value => importer.importAnimation = value);
            changed |= SetImporterValue(importer.animationCompression, ModelImporterAnimationCompression.Optimal, value => importer.animationCompression = value);

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                var clipName = GetMixamoClipName(path, clips[0].name);
                for (var i = 0; i < clips.Length; i++)
                {
                    changed |= SetClipValue(clips, i, clips[i].name, clipName, (clip, value) => clip.name = value);
                    changed |= SetClipValue(clips, i, clips[i].loopTime, ShouldLoopLibraryClip(clipName), (clip, value) => clip.loopTime = value);
                    changed |= SetClipValue(clips, i, clips[i].loopPose, ShouldLoopLibraryClip(clipName), (clip, value) => clip.loopPose = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootRotation, true, (clip, value) => clip.lockRootRotation = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootHeightY, true, (clip, value) => clip.lockRootHeightY = value);
                    changed |= SetClipValue(clips, i, clips[i].lockRootPositionXZ, true, (clip, value) => clip.lockRootPositionXZ = value);
                }

                importer.clipAnimations = clips;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController CreateOrUpdateFloreswaMixamoAnimatorController()
        {
            var clips = LoadMixamoAnimationClips();
            if (clips.Length == 0)
            {
                return null;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(FloreswaMixamoAnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(FloreswaMixamoAnimatorControllerPath);
            }

            EnsureFloatParameter(controller, "Speed");

            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;
            for (var i = stateMachine.states.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveState(stateMachine.states[i].state);
            }

            var oldBlendTrees = AssetDatabase.LoadAllAssetsAtPath(FloreswaMixamoAnimatorControllerPath)
                .OfType<BlendTree>()
                .ToArray();
            for (var i = 0; i < oldBlendTrees.Length; i++)
            {
                Object.DestroyImmediate(oldBlendTrees[i], true);
            }

            var blendTree = new BlendTree
            {
                name = "FloreswaMixamoLocomotion",
                blendParameter = "Speed",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var idleClip = FindLibraryClip(clips, "idle") ?? clips[0];
            var walkClip = FindLibraryClip(clips, "walk") ?? FindLibraryClip(clips, "walking") ?? (clips.Length > 1 ? clips[1] : idleClip);
            var runClip = FindLibraryClip(clips, "run") ?? FindLibraryClip(clips, "running") ?? (clips.Length > 2 ? clips[2] : walkClip);
            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(walkClip, 0.45f);
            blendTree.AddChild(runClip, 1f);

            var locomotion = stateMachine.AddState("FloreswaMixamoLocomotion");
            locomotion.motion = blendTree;
            stateMachine.defaultState = locomotion;

            var stateY = 80f;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == idleClip || clip == walkClip || clip == runClip)
                {
                    continue;
                }

                var state = stateMachine.AddState("Mixamo_" + SanitizeStateName(clip.name), new Vector3(520f, stateY, 0f));
                state.motion = clip;
                stateY += 44f;
            }

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip[] LoadMixamoAnimationClips()
        {
            if (!AssetDatabase.IsValidFolder(FloreswaMixamoAnimationFolder))
            {
                return new AnimationClip[0];
            }

            return AssetDatabase.FindAssets("t:Model", new[] { FloreswaMixamoAnimationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .SelectMany(LoadAnimationClips)
                .Where(IsUsableClip)
                .Distinct()
                .ToArray();
        }

        private static string GetMixamoClipName(string path, string fallbackName)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrWhiteSpace(name)
                ? (string.IsNullOrWhiteSpace(fallbackName) ? "MixamoClip" : fallbackName)
                : name;
        }

        private static AnimationClip FindLibraryClip(AnimationClip[] clips, string token)
        {
            return clips.FirstOrDefault(clip => clip != null && clip.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static AnimationClip[] LoadAnimationClips(string path)
        {
            var directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var modelClips = modelAsset != null
                ? AnimationUtility.GetAnimationClips(modelAsset)
                : new AnimationClip[0];

            return modelClips
                .Concat(AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>())
                .Concat(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                .Concat(directClip != null ? new[] { directClip } : new AnimationClip[0])
                .Where(IsUsableClip)
                .Distinct()
                .ToArray();
        }

        private static string SanitizeStateName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Clip";
            }

            var chars = rawName
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray();
            return new string(chars);
        }

        private static bool ControllerHasPlayableBlendChildren(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length == 0)
            {
                return false;
            }

            var states = controller.layers[0].stateMachine.states;
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.motion is BlendTree blendTree &&
                    blendTree.children != null &&
                    blendTree.children.Length >= 3 &&
                    BlendTreeHasPlayableChildren(blendTree))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool BlendTreeHasPlayableChildren(BlendTree blendTree)
        {
            var requiredPlayableChildren = 0;
            var children = blendTree.children;
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].motion is AnimationClip clip && IsPlayableClip(clip))
                {
                    requiredPlayableChildren++;
                }
            }

            return requiredPlayableChildren >= 3;
        }

        private static bool ControllerUsesActiveModelClips(AnimatorController controller)
        {
            if (!HasProcessedModel)
            {
                return true;
            }

            if (controller == null || controller.layers.Length == 0)
            {
                return false;
            }

            var matchingChildren = 0;
            var states = controller.layers[0].stateMachine.states;
            for (var i = 0; i < states.Length; i++)
            {
                var blendTree = states[i].state?.motion as BlendTree;
                if (blendTree == null || blendTree.children == null)
                {
                    continue;
                }

                var children = blendTree.children;
                for (var childIndex = 0; childIndex < children.Length; childIndex++)
                {
                    if (children[childIndex].motion is AnimationClip clip &&
                        AssetDatabase.GetAssetPath(clip) == ActiveModelPath)
                    {
                        matchingChildren++;
                    }
                }
            }

            return matchingChildren >= 3;
        }

        private static string GetClipName(string path, bool isMainModel, string originalName, int index)
        {
            if (path == ProcessedModelPath)
            {
                if (originalName.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Idle";
                }

                if (originalName.IndexOf("Walk", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Walk";
                }

                if (originalName.IndexOf("Run", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Run";
                }

                if (index == 0)
                {
                    return "Idle";
                }

                if (index == 1)
                {
                    return "Walk";
                }

                if (index == 2)
                {
                    return "Run";
                }
            }

            if (isMainModel)
            {
                return "SchoolBoyPose";
            }

            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.StartsWith("Idle", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Idle";
            }

            if (fileName.StartsWith("Walking", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Walk";
            }

            if (fileName.StartsWith("Running", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Run";
            }

            return fileName;
        }

        private static GameObject CreateOrUpdatePrefab(RuntimeAnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ActiveModelPath);
            if (model == null)
            {
                return null;
            }

            var root = new GameObject("SchoolBoyCharacter");
            var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (modelInstance == null)
            {
                Object.DestroyImmediate(root);
                return null;
            }

            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;
            FitModelToCharacterRig(modelInstance.transform);
            AddHeadCosmeticPatches(modelInstance.transform);

            var animator = modelInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            var movementAnimator = root.AddComponent<CharacterMovementAnimator>();
            var serializedObject = new SerializedObject(movementAnimator);
            serializedObject.FindProperty("animator").objectReferenceValue = animator;
            serializedObject.FindProperty("proceduralFallback").boolValue = animator == null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CharacterPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, ResourceCharacterPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddHeadCosmeticPatches(Transform modelRoot)
        {
            var head = FindDeepChild(modelRoot, "mixamorig:Head") ?? FindDeepChild(modelRoot, "Head");
            if (head == null)
            {
                return;
            }

            var faceMaterial = CreateOrUpdateCosmeticMaterial(
                FaceDetailMaterialPath,
                "SchoolBoyFaceDetails",
                new Color(0.12f, 0.095f, 0.08f, 1f),
                -1);
            var skinMaterial = CreateOrUpdateCosmeticMaterial(
                SkinPatchMaterialPath,
                "SchoolBoySkinPatch",
                new Color(0.43f, 0.34f, 0.27f, 1f),
                -1,
                true);
            var hairMaterial = CreateOrUpdateCosmeticMaterial(
                HairPatchMaterialPath,
                "SchoolBoyHairPatch",
                new Color(0.54f, 0.63f, 0.74f, 1f),
                -1,
                true);

            DestroyExistingChild(head, "SchoolBoyCosmeticPatches");

            var cosmeticRoot = new GameObject("SchoolBoyCosmeticPatches");
            cosmeticRoot.transform.SetParent(head, false);
            cosmeticRoot.transform.localPosition = Vector3.zero;
            cosmeticRoot.transform.localRotation = Quaternion.identity;
            cosmeticRoot.transform.localScale = Vector3.one;

            AddHairGapPatch(cosmeticRoot.transform, hairMaterial, 1f);
            AddHairGapPatch(cosmeticRoot.transform, hairMaterial, -1f);
            AddEyeCovers(cosmeticRoot.transform, skinMaterial, 1f);
            AddEyeCovers(cosmeticRoot.transform, skinMaterial, -1f);
            AddFaceDetails(cosmeticRoot.transform, faceMaterial, 1f);
            AddFaceDetails(cosmeticRoot.transform, faceMaterial, -1f);
        }

        private static Material CreateOrUpdateCosmeticMaterial(string path, string name, Color color, int renderQueue, bool preferLit = false)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = preferLit
                    ? (Shader.Find("Universal Render Pipeline/Lit") ??
                       Shader.Find("Universal Render Pipeline/Unlit") ??
                       Shader.Find("Standard"))
                    : (Shader.Find("Universal Render Pipeline/Unlit") ??
                       Shader.Find("Universal Render Pipeline/Lit") ??
                       Shader.Find("Unlit/Color") ??
                       Shader.Find("Standard"));
                material = new Material(shader)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.doubleSidedGI = true;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.08f);
            }

            material.renderQueue = renderQueue;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddHairGapPatch(Transform parent, Material material, float forwardSign)
        {
            var patch = CreateMeshObject("HairCenterFill" + (forwardSign > 0f ? "Front" : "Back"), parent, material);
            patch.transform.localPosition = new Vector3(0f, 0.14f, 0.021f * forwardSign);
            patch.transform.localRotation = Quaternion.Euler(8f, forwardSign > 0f ? 0f : 180f, 0f);
            patch.transform.localScale = Vector3.one;

            var mesh = new Mesh
            {
                name = "HairCenterFillMesh"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.112f, -0.034f, 0f),
                new Vector3(0.112f, -0.034f, 0f),
                new Vector3(0.072f, 0.064f, 0.008f),
                new Vector3(0f, 0.122f, 0.014f),
                new Vector3(-0.072f, 0.064f, 0.008f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 4,
                4, 2, 3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private static void AddEyeCovers(Transform parent, Material material, float forwardSign)
        {
            var z = 0.121f * forwardSign;
            AddEllipse("LeftEyeSkinCover" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(-0.042f, 0.026f, z), 0.025f, 0.035f, forwardSign);
            AddEllipse("RightEyeSkinCover" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(0.042f, 0.026f, z), 0.025f, 0.035f, forwardSign);
        }

        private static void AddFaceDetails(Transform parent, Material material, float forwardSign)
        {
            var z = 0.126f * forwardSign;
            AddEllipse("LeftEyeDetail" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(-0.042f, 0.026f, z), 0.009f, 0.018f, forwardSign);
            AddEllipse("RightEyeDetail" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(0.042f, 0.026f, z), 0.009f, 0.018f, forwardSign);
            AddSoftLine("MouthDetail" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(0f, -0.055f, z + 0.002f * forwardSign), 0.07f, 0.006f, forwardSign);
            AddSoftLine("NoseDetail" + (forwardSign > 0f ? "Front" : "Back"), parent, material, new Vector3(0.005f, -0.012f, z + 0.003f * forwardSign), 0.038f, 0.004f, forwardSign, 88f);
        }

        private static void AddEllipse(string name, Transform parent, Material material, Vector3 localPosition, float width, float height, float forwardSign)
        {
            const int segments = 18;
            var eye = CreateMeshObject(name, parent, material);
            eye.transform.localPosition = localPosition;
            eye.transform.localRotation = Quaternion.Euler(0f, forwardSign > 0f ? 0f : 180f, 0f);
            eye.transform.localScale = Vector3.one;

            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i < segments; i++)
            {
                var angle = (Mathf.PI * 2f * i) / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * width, Mathf.Sin(angle) * height, 0f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i == segments - 1 ? 1 : i + 2;
            }

            var mesh = new Mesh
            {
                name = name + "Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            eye.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private static void AddSoftLine(string name, Transform parent, Material material, Vector3 localPosition, float width, float height, float forwardSign, float zRotation = 0f)
        {
            var line = CreateMeshObject(name, parent, material);
            line.transform.localPosition = localPosition;
            line.transform.localRotation = Quaternion.Euler(0f, forwardSign > 0f ? 0f : 180f, zRotation);
            line.transform.localScale = Vector3.one;

            var mesh = new Mesh
            {
                name = name + "Mesh",
                vertices = new[]
                {
                    new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(width * 0.5f, -height * 0.5f, 0f),
                    new Vector3(width * 0.5f, height * 0.5f, 0f),
                    new Vector3(-width * 0.5f, height * 0.5f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            line.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>();
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return child;
        }

        private static void DestroyExistingChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
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

        private static void FitModelToCharacterRig(Transform modelRoot)
        {
            if (modelRoot == null)
            {
                return;
            }

            var bounds = CalculateRendererBounds(modelRoot);
            if (!bounds.HasValue || bounds.Value.size.y <= 0.001f)
            {
                return;
            }

            var scale = CharacterVisualHeight / bounds.Value.size.y;
            modelRoot.localScale = Vector3.one * scale;

            var scaledMinY = bounds.Value.min.y * scale;
            modelRoot.localPosition = new Vector3(0f, -scaledMinY, 0f);
        }

        private static Bounds? CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds? bounds = null;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (bounds.HasValue)
                {
                    var next = bounds.Value;
                    next.Encapsulate(renderer.bounds);
                    bounds = next;
                }
                else
                {
                    bounds = renderer.bounds;
                }
            }

            return bounds;
        }

        private static void EnsureFloatParameter(AnimatorController controller, string parameterName)
        {
            if (controller.parameters.Any(parameter => parameter.name == parameterName))
            {
                return;
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
        }

        private static void AddBlendChild(BlendTree blendTree, string clipPath, float threshold, string expectedName)
        {
            var clip = LoadAnimationClip(clipPath, expectedName);
            if (clip != null)
            {
                blendTree.AddChild(clip, threshold);
            }
            else
            {
                Debug.LogWarning($"School boy anim clip bulunamadi: {clipPath}");
            }
        }

        private static AnimationClip LoadAnimationClip(string path, string expectedName)
        {
            var directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (IsUsableClip(directClip) &&
                directClip.name.Equals(expectedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return directClip;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var modelClips = modelAsset != null
                ? AnimationUtility.GetAnimationClips(modelAsset)
                : new AnimationClip[0];

            var clips = modelClips
                .Concat(AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>())
                .Concat(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                .Concat(directClip != null ? new[] { directClip } : new AnimationClip[0])
                .Where(IsUsableClip)
                .Distinct()
                .ToArray();

            if (clips.Length == 0)
            {
                Debug.LogWarning($"School boy animasyon assetinde okunabilir clip yok: {path}");
                return null;
            }

            return clips.FirstOrDefault(clip => clip.name.Equals(expectedName, System.StringComparison.OrdinalIgnoreCase)) ??
                   clips.FirstOrDefault(clip => clip.name.IndexOf(expectedName, System.StringComparison.OrdinalIgnoreCase) >= 0) ??
                   clips[0];
        }

        private static bool IsUsableClip(AnimationClip clip)
        {
            return clip != null &&
                   !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase) &&
                   !clip.name.StartsWith("__default__", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlayableClip(AnimationClip clip)
        {
            if (!IsUsableClip(clip))
            {
                return false;
            }

            return AnimationUtility.GetCurveBindings(clip).Length > 0 ||
                   AnimationUtility.GetObjectReferenceCurveBindings(clip).Length > 0;
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

    public sealed class SchoolBoyCharacterAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets == null)
            {
                return;
            }

            for (var i = 0; i < importedAssets.Length; i++)
            {
                if (importedAssets[i] == SchoolBoyCharacterSetupTool.ProcessedModelPath)
                {
                    SchoolBoyCharacterSetupTool.QueueProcessedCharacterSetup();
                    return;
                }
            }
        }
    }
}
