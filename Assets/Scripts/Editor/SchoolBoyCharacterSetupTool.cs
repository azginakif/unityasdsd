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
        public const string CharacterPrefabPath = "Assets/Prefabs/Characters/SchoolBoyCharacter.prefab";
        public const string ResourceCharacterPrefabPath = "Assets/Resources/Characters/SchoolBoyCharacter.prefab";
        public const string AnimatorControllerPath = "Assets/Animations/SchoolBoy/SchoolBoy.controller";
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

            ConfigureModelImporter(ActiveModelPath, true);

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
            return CreateOrUpdatePrefab(controller);
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

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
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
