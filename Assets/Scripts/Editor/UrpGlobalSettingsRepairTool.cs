using UnityEditor;
using UnityEngine;

namespace MobilOfl.EditorTools
{
    public static class UrpGlobalSettingsRepairTool
    {
        private static readonly string[] SettingsAssetPaths =
        {
            "Assets/map/Settings/UniversalRenderPipelineGlobalSettings.asset",
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"
        };

        private static readonly string[] MissingReferenceTypeNames =
        {
            "WorldRenderPipelineResources",
            "RayTracingRenderPipelineResources",
            "ScreenSpaceAmbientOcclusionPersistentResources",
            "ScreenSpaceAmbientOcclusionDynamicResources",
            "OnTilePostProcessResource",
            "URPReflectionProbeSettings",
            "VrsRenderPipelineRuntimeResources",
            "RenderingDebuggerRuntimeResources",
            "LightmapSamplingSettings",
            "UniversalRenderPipelineRuntimeTerrainShaders",
            "URPTerrainShaderSetting"
        };

        [MenuItem("Mobil OFL/Repair/Sanitize URP Global Settings")]
        public static void SanitizeActiveUrpGlobalSettings()
        {
            var changedAny = false;
            foreach (var assetPath in SettingsAssetPaths)
            {
                changedAny |= SanitizeAsset(assetPath);
            }

            if (changedAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log(changedAny
                ? "[Mobil OFL] URP global settings missing managed references were removed."
                : "[Mobil OFL] URP global settings did not need repair.");
        }

        private static bool SanitizeAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                return false;
            }

            var serializedObject = new SerializedObject(asset);
            var settingsList = serializedObject.FindProperty("m_Settings.m_SettingsList.m_List");
            if (settingsList == null || !settingsList.isArray)
            {
                return false;
            }

            var changed = false;
            for (var i = settingsList.arraySize - 1; i >= 0; i--)
            {
                var element = settingsList.GetArrayElementAtIndex(i);
                if (!ShouldRemove(element))
                {
                    continue;
                }

                settingsList.DeleteArrayElementAtIndex(i);
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return true;
        }

        private static bool ShouldRemove(SerializedProperty element)
        {
            if (element == null)
            {
                return false;
            }

            var typeName = element.managedReferenceFullTypename;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return true;
            }

            for (var i = 0; i < MissingReferenceTypeNames.Length; i++)
            {
                if (typeName.Contains(MissingReferenceTypeNames[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
