using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class FixMagentaCubesTool
    {
        private static readonly Regex CubeNameRegex = new Regex(@"^Cube(\s\(\d+\))?$", RegexOptions.Compiled);
        private const string NeutralMaterialPath = "Assets/map/Materials/OFL_Neutral_URP_Lit.mat";
        private const string GroundMaterialPath = "Assets/map/Materials/OFL_Ground_URP_Lit.mat";
        private const string GlassMaterialPath = "Assets/map/Materials/OFL_WindowGlass_URP_Lit.mat";
        private const string MagentaReportPath = "Assets/map/Materials/OFL_MagentaFixReport.txt";
        private const string RendererAnalysisPath = "Assets/map/Materials/OFL_RendererMaterialAnalysis.txt";
        private const string SceneViewProbePath = "Assets/map/Materials/OFL_SceneViewProbe.txt";
        private const string MainCameraProbePath = "Assets/map/Materials/OFL_MainCameraProbe.txt";

        [MenuItem("Tools/Map/Fix Magenta Cubes")]
        public static void FixMagentaCubes()
        {
            var shader = Shader.Find("Standard")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");

            if (shader == null)
            {
                Debug.LogError("Fix Magenta Cubes: Uygun shader bulunamadi.");
                return;
            }

            var neutralMaterial = GetOrCreateMaterial(NeutralMaterialPath, shader, "OFL_Neutral_URP_Lit", new Color(0.47f, 0.51f, 0.54f, 1f), 0f);
            var groundMaterial = GetOrCreateMaterial(GroundMaterialPath, shader, "OFL_Ground_URP_Lit", new Color(0.36f, 0.34f, 0.32f, 1f), 0f);
            var glassMaterial = GetOrCreateMaterial(GlassMaterialPath, shader, "OFL_WindowGlass_URP_Lit", new Color(0.10f, 0.17f, 0.28f, 0.92f), 0.25f);

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Fix Magenta Cubes: Acik bir scene bulunamadi.");
                return;
            }

            var changedRendererCount = 0;
            var changedSlotCount = 0;
            var convertedMaterialCount = 0;
            var report = "OFL Magenta Fix Report\n";
            foreach (var root in scene.GetRootGameObjects())
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    var rendererChanged = false;
                    if (materials == null || materials.Length == 0)
                    {
                        renderer.sharedMaterial = PickReplacement(renderer, neutralMaterial, groundMaterial, glassMaterial);
                        rendererChanged = true;
                        changedSlotCount++;
                    }
                    else
                    {
                        for (var i = 0; i < materials.Length; i++)
                        {
                            var mat = materials[i];
                            if (ConvertPipelineMaterialToCompatibleShader(mat, shader))
                            {
                                report += $"{renderer.transform.GetHierarchyPath()} slot {i}: converted {mat.name} to {shader.name}\n";
                                convertedMaterialCount++;
                            }

                            if (!ShouldReplaceRendererSlot(renderer, mat))
                            {
                                continue;
                            }

                            var replacement = PickReplacement(renderer, neutralMaterial, groundMaterial, glassMaterial);
                            report += $"{renderer.transform.GetHierarchyPath()} slot {i}: {(mat == null ? "<null>" : mat.name)} -> {replacement.name}\n";
                            materials[i] = replacement;
                            rendererChanged = true;
                            changedSlotCount++;
                        }

                        if (rendererChanged)
                        {
                            renderer.sharedMaterials = materials;
                        }
                    }

                    if (!rendererChanged)
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(renderer);
                    changedRendererCount++;
                }
            }

            System.IO.File.WriteAllText(MagentaReportPath, report);
            AssetDatabase.ImportAsset(MagentaReportPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Fix Magenta Cubes: {changedRendererCount} renderer, {changedSlotCount} material slotu guncellendi, {convertedMaterialCount} materyal shader'i donusturuldu.");
        }

        [MenuItem("Tools/Map/Analyze Renderer Materials")]
        public static void AnalyzeRendererMaterials()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Analyze Renderer Materials: Acik bir scene bulunamadi.");
                return;
            }

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            System.Array.Sort(renderers, (a, b) => b.bounds.size.sqrMagnitude.CompareTo(a.bounds.size.sqrMagnitude));

            var sb = new StringBuilder();
            sb.AppendLine("OFL Renderer Material Analysis");
            sb.AppendLine($"Scene: {scene.path}");
            sb.AppendLine($"Renderer count: {renderers.Length}");
            sb.AppendLine();

            var count = Mathf.Min(renderers.Length, 250);
            for (var i = 0; i < count; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                sb.AppendLine($"[{i}] {renderer.transform.GetHierarchyPath()}");
                sb.AppendLine($"    bounds.size={renderer.bounds.size} scale={renderer.transform.lossyScale}");
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    sb.AppendLine("    material: <none>");
                    continue;
                }

                for (var slot = 0; slot < materials.Length; slot++)
                {
                    var mat = materials[slot];
                    if (mat == null)
                    {
                        sb.AppendLine($"    slot {slot}: <null>");
                        continue;
                    }

                    var color = GetMaterialColor(mat);
                    var tex = mat.mainTexture != null ? mat.mainTexture.name : "<none>";
                    sb.AppendLine($"    slot {slot}: mat='{mat.name}' shader='{mat.shader?.name ?? "<null>"}' supported={(mat.shader != null && mat.shader.isSupported)} color={ColorUtility.ToHtmlStringRGBA(color)} tex='{tex}'");
                }
            }

            System.IO.File.WriteAllText(RendererAnalysisPath, sb.ToString());
            AssetDatabase.ImportAsset(RendererAnalysisPath);
            Debug.Log($"Analyze Renderer Materials: {count} renderer yazildi -> {RendererAnalysisPath}");
        }

        [MenuItem("Tools/Map/Probe Scene View Picks")]
        public static void ProbeSceneViewPicks()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogError("Probe Scene View Picks: Aktif Scene View bulunamadi.");
                return;
            }

            var camera = sceneView.camera;
            var samples = new[]
            {
                new Vector2(520f, 610f),
                new Vector2(820f, 590f),
                new Vector2(1050f, 350f),
                new Vector2(185f, 335f),
                new Vector2(300f, 295f),
                new Vector2(450f, 300f),
                new Vector2(980f, 260f),
            };

            var sb = new StringBuilder();
            sb.AppendLine("OFL Scene View Probe");
            sb.AppendLine($"camera pixel=({camera.pixelWidth}, {camera.pixelHeight}) pos={camera.transform.position} rot={camera.transform.rotation.eulerAngles}");
            sb.AppendLine();

            foreach (var guiPoint in samples)
            {
                sb.AppendLine($"Sample GUI {guiPoint}");
                try
                {
                    var picked = HandleUtility.PickGameObject(guiPoint, false);
                    AppendObjectInfo(sb, "pick", picked);
                }
                catch (System.Exception exception)
                {
                    sb.AppendLine($"  pick: <failed> {exception.GetType().Name}: {exception.Message}");
                }

                var screenPoint = new Vector3(guiPoint.x, camera.pixelHeight - guiPoint.y, 0f);
                var ray = camera.ScreenPointToRay(screenPoint);
                var hits = Physics.RaycastAll(ray, 10000f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                if (hits.Length == 0)
                {
                    sb.AppendLine("  raycast: <none>");
                }
                else
                {
                    var limit = Mathf.Min(hits.Length, 5);
                    for (var i = 0; i < limit; i++)
                    {
                        var hit = hits[i];
                        AppendObjectInfo(sb, $"raycast[{i}] dist={hit.distance:0.00} point={hit.point}", hit.collider != null ? hit.collider.gameObject : null);
                    }
                }

                sb.AppendLine();
            }

            System.IO.File.WriteAllText(SceneViewProbePath, sb.ToString());
            AssetDatabase.ImportAsset(SceneViewProbePath);
            Debug.Log($"Probe Scene View Picks: rapor yazildi -> {SceneViewProbePath}");
        }

        [MenuItem("Tools/Map/Probe Main Camera Rays")]
        public static void ProbeMainCameraRays()
        {
            var camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("Probe Main Camera Rays: Kamera bulunamadi.");
                return;
            }

            var samples = new[]
            {
                new Vector2(camera.pixelWidth * 0.50f, camera.pixelHeight * 0.75f),
                new Vector2(camera.pixelWidth * 0.20f, camera.pixelHeight * 0.55f),
                new Vector2(camera.pixelWidth * 0.80f, camera.pixelHeight * 0.55f),
                new Vector2(camera.pixelWidth * 0.50f, camera.pixelHeight * 0.35f),
                new Vector2(camera.pixelWidth * 0.50f, camera.pixelHeight * 0.50f),
            };

            var sb = new StringBuilder();
            sb.AppendLine("OFL Main Camera Probe");
            sb.AppendLine($"camera={camera.name} pixel=({camera.pixelWidth}, {camera.pixelHeight}) pos={camera.transform.position} rot={camera.transform.rotation.eulerAngles}");
            sb.AppendLine();

            foreach (var screenPoint in samples)
            {
                sb.AppendLine($"Sample Screen {screenPoint}");
                var ray = camera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
                var hits = Physics.RaycastAll(ray, 10000f);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                if (hits.Length == 0)
                {
                    sb.AppendLine("  raycast: <none>");
                    sb.AppendLine();
                    continue;
                }

                var limit = Mathf.Min(hits.Length, 8);
                for (var i = 0; i < limit; i++)
                {
                    var hit = hits[i];
                    AppendObjectInfo(sb, $"raycast[{i}] dist={hit.distance:0.00} point={hit.point}", hit.collider != null ? hit.collider.gameObject : null);
                }

                sb.AppendLine();
            }

            System.IO.File.WriteAllText(MainCameraProbePath, sb.ToString());
            AssetDatabase.ImportAsset(MainCameraProbePath);
            Debug.Log($"Probe Main Camera Rays: rapor yazildi -> {MainCameraProbePath}");
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, string materialName, Color color, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };

                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            SetColor(material, color);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool ShouldReplaceRendererSlot(Renderer renderer, Material material)
        {
            if (material == null || material.shader == null || !material.shader.isSupported)
            {
                return true;
            }

            if (material.shader.name.Contains("InternalErrorShader"))
            {
                return true;
            }

            if (CubeNameRegex.IsMatch(renderer.gameObject.name))
            {
                return true;
            }

            return IsMagentaLike(GetMaterialColor(material));
        }

        private static bool ConvertPipelineMaterialToCompatibleShader(Material material, Shader compatibleShader)
        {
            if (material == null || material.shader == null || compatibleShader == null)
            {
                return false;
            }

            if (!material.shader.name.Contains("Universal Render Pipeline"))
            {
                return false;
            }

            var color = GetMaterialColor(material);
            var mainTexture = material.mainTexture;
            material.shader = compatibleShader;
            SetColor(material, color);
            if (mainTexture != null && material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", mainTexture);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            EditorUtility.SetDirty(material);
            return true;
        }

        private static Material PickReplacement(Renderer renderer, Material neutral, Material ground, Material glass)
        {
            var lower = renderer.transform.GetHierarchyPath().ToLowerInvariant();
            if (lower.Contains("window") || lower.Contains("glass") || lower.Contains("cam") || lower.Contains("pencere"))
            {
                return glass;
            }

            if (lower.Contains("ground") || lower.Contains("floor") || lower.Contains("road") || lower.Contains("terrain") || lower.Contains("plane"))
            {
                return ground;
            }

            return neutral;
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.black;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static bool IsMagentaLike(Color color)
        {
            return color.r > 0.85f && color.b > 0.85f && color.g < 0.25f;
        }

        private static void AppendObjectInfo(StringBuilder sb, string label, GameObject gameObject)
        {
            if (gameObject == null)
            {
                sb.AppendLine($"  {label}: <none>");
                return;
            }

            sb.AppendLine($"  {label}: {gameObject.transform.GetHierarchyPath()} active={gameObject.activeInHierarchy} layer={LayerMask.LayerToName(gameObject.layer)}");
            sb.AppendLine($"    transform pos={gameObject.transform.position} scale={gameObject.transform.lossyScale}");
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = gameObject.GetComponentInChildren<Renderer>(true);
            }

            if (renderer == null)
            {
                sb.AppendLine("    renderer: <none>");
                return;
            }

            sb.AppendLine($"    renderer={renderer.GetType().Name} bounds.center={renderer.bounds.center} bounds.size={renderer.bounds.size}");
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                sb.AppendLine("    material: <none>");
                return;
            }

            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null)
                {
                    sb.AppendLine($"    slot {i}: <null>");
                    continue;
                }

                var color = GetMaterialColor(mat);
                var tex = mat.mainTexture != null ? mat.mainTexture.name : "<none>";
                sb.AppendLine($"    slot {i}: mat='{mat.name}' shader='{mat.shader?.name ?? "<null>"}' supported={(mat.shader != null && mat.shader.isSupported)} color={ColorUtility.ToHtmlStringRGBA(color)} tex='{tex}'");
            }
        }

        private static string GetHierarchyPath(this Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
