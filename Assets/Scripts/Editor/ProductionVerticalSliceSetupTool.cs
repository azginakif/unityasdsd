using MobilOfl.Visuals;
using MobilOfl.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobilOfl.EditorTools
{
    public static class ProductionVerticalSliceSetupTool
    {
        private const string RootName = "VerticalSliceBlockout";

        [MenuItem("Mobil OFL/Production/Generate Vertical Slice Blockout")]
        public static void GenerateVerticalSliceBlockout()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var root = new GameObject(RootName);
            var materials = new BlockoutMaterials();

            CreateSchoolWing(root.transform, materials);
            CreateCharacterLineup(root.transform, materials);
            CreatePropReferences(root.transform, materials);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Mobil OFL",
                "Vertical slice blockout olusturuldu. Bu sahne objeleri final model degil; map, prop ve karakter uretimi icin yer tutucu referanslardir.",
                "Tamam");
        }

        private static void CreateSchoolWing(Transform parent, BlockoutMaterials materials)
        {
            var root = new GameObject("PlayableSchoolWing_Blockout");
            root.transform.SetParent(parent);

            CreateZone(root.transform, "Ana Koridor", new Vector3(0f, 0f, 0f), new Vector3(30f, 0.18f, 4f), materials.Floor);
            CreateWall(root.transform, "Koridor Sol Duvar", new Vector3(0f, 1.5f, -2.15f), new Vector3(30f, 3f, 0.18f), materials.Wall);
            CreateWall(root.transform, "Koridor Sag Duvar", new Vector3(0f, 1.5f, 2.15f), new Vector3(30f, 3f, 0.18f), materials.Wall);

            CreateRoom(root.transform, "Sinif A", new Vector3(-10f, 0f, -8f), new Vector3(8f, 0.18f, 8f), materials, string.Empty);
            CreateRoom(root.transform, "Kutuphane", new Vector3(0f, 0f, -8f), new Vector3(9f, 0.18f, 8f), materials, string.Empty);
            CreateRoom(root.transform, "Guvenlik Odasi", new Vector3(-10f, 0f, 8f), new Vector3(7f, 0.18f, 7f), materials, string.Empty);
            CreateRoom(root.transform, "Ogretmenler Odasi", new Vector3(0f, 0f, 8f), new Vector3(9f, 0.18f, 7f), materials, string.Empty);
            CreateRoom(root.transform, "Arsiv", new Vector3(10f, 0f, 8f), new Vector3(8f, 0.18f, 7f), materials, "tool.archive-pass");
            CreateRoom(root.transform, "Kantin", new Vector3(11f, 0f, -8f), new Vector3(8f, 0.18f, 8f), materials, string.Empty);

            CreateLabel(root.transform, "ANA KORIDOR", new Vector3(0f, 2.4f, 0f));
            CreateLabel(root.transform, "SINIF", new Vector3(-10f, 2.4f, -4.4f));
            CreateLabel(root.transform, "KUTUPHANE", new Vector3(0f, 2.4f, -4.4f));
            CreateLabel(root.transform, "GUVENLIK", new Vector3(-10f, 2.4f, 4.4f));
            CreateLabel(root.transform, "OGRETMENLER", new Vector3(0f, 2.4f, 4.4f));
            CreateLabel(root.transform, "ARSIV", new Vector3(10f, 2.4f, 4.4f));
            CreateLabel(root.transform, "KANTIN", new Vector3(11f, 2.4f, -4.4f));

            for (var i = 0; i < 10; i++)
            {
                var x = -13.5f + i * 3f;
                CreateBlock(root.transform, "Locker_" + i, new Vector3(x, 1f, 1.72f), new Vector3(0.8f, 1.9f, 0.25f), materials.Locker);
            }
        }

        private static void CreateRoom(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 floorScale,
            BlockoutMaterials materials,
            string requiredToolId)
        {
            var root = new GameObject(name + "_Blockout");
            root.transform.SetParent(parent);

            CreateZone(root.transform, name + "_Floor", center, floorScale, materials.RoomFloor);
            var width = floorScale.x;
            var depth = floorScale.z;
            var corridorSideSign = center.z < 0f ? 1f : -1f;
            var farSideSign = -corridorSideSign;

            CreateWall(root.transform, name + "_BackWall", center + new Vector3(0f, 1.5f, farSideSign * depth * 0.5f), new Vector3(width, 3f, 0.18f), materials.Wall);
            CreateWall(root.transform, name + "_LeftWall", center + new Vector3(-width * 0.5f, 1.5f, 0f), new Vector3(0.18f, 3f, depth), materials.Wall);
            CreateWall(root.transform, name + "_RightWall", center + new Vector3(width * 0.5f, 1.5f, 0f), new Vector3(0.18f, 3f, depth), materials.Wall);

            var corridorZ = center.z + corridorSideSign * depth * 0.5f;
            CreateWall(root.transform, name + "_FrontWall_L", new Vector3(center.x - width * 0.33f, 1.5f, corridorZ), new Vector3(width * 0.34f, 3f, 0.18f), materials.Wall);
            CreateWall(root.transform, name + "_FrontWall_R", new Vector3(center.x + width * 0.33f, 1.5f, corridorZ), new Vector3(width * 0.34f, 3f, 0.18f), materials.Wall);
            CreateDoor(root.transform, name + "_Door", new Vector3(center.x, 1.15f, corridorZ), corridorSideSign, requiredToolId, materials.Door);
        }

        private static void CreateDoor(Transform parent, string name, Vector3 position, float corridorSideSign, string requiredToolId, Material material)
        {
            var door = CreateBlock(parent, name, position, new Vector3(1.35f, 2.3f, 0.14f), material);
            door.transform.rotation = Quaternion.Euler(0f, corridorSideSign > 0f ? 0f : 180f, 0f);

            var interactable = door.AddComponent<DoorInteractable>();
            interactable.ConfigurePrompt("Kapiyi kullan");
            interactable.ConfigureAccess(
                requiredToolId,
                requiredToolId == "tool.archive-pass" ? "Arsiv kapisi icin once gecis karti gerekiyor." : string.Empty,
                false);
        }

        private static void CreateCharacterLineup(Transform parent, BlockoutMaterials materials)
        {
            var root = new GameObject("CharacterPlaceholderLineup");
            root.transform.SetParent(parent);

            CreateCharacter(root.transform, "Guvenlik Gorevlisi_ModelPlaceholder", new Vector3(-2f, 0f, 5.7f), materials.Security);
            CreateCharacter(root.transform, "Kutuphane Ogrencisi_ModelPlaceholder", new Vector3(1f, 0f, -5.8f), materials.Student);
            CreateCharacter(root.transform, "Ogretmen Yardimcisi_ModelPlaceholder", new Vector3(-1.2f, 0f, 5.9f), materials.Teacher);
            CreateCharacter(root.transform, "Arsiv Sorumlusu_ModelPlaceholder", new Vector3(9.4f, 0f, 5.7f), materials.Archive);
            CreateCharacter(root.transform, "Kantin Calisani_ModelPlaceholder", new Vector3(11f, 0f, -5.7f), materials.Canteen);
        }

        private static void CreatePropReferences(Transform parent, BlockoutMaterials materials)
        {
            var root = new GameObject("ReusablePropPlaceholders");
            root.transform.SetParent(parent);

            for (var i = 0; i < 8; i++)
            {
                CreateBlock(root.transform, "ClassroomDesk_" + i, new Vector3(-12.6f + (i % 4) * 1.7f, 0.42f, -8.9f + (i / 4) * 2.1f), new Vector3(1.1f, 0.35f, 0.75f), materials.Wood);
            }

            for (var i = 0; i < 5; i++)
            {
                CreateBlock(root.transform, "LibraryShelf_" + i, new Vector3(-3.2f + i * 1.6f, 1.1f, -9.4f), new Vector3(0.35f, 2.1f, 2.6f), materials.Shelf);
            }

            CreateBlock(root.transform, "SecurityTerminal", new Vector3(-10.2f, 0.86f, 8.2f), new Vector3(1.1f, 0.35f, 0.7f), materials.Tech);
            CreateBlock(root.transform, "ArchiveCabinet_A", new Vector3(8f, 1.1f, 9.6f), new Vector3(1.2f, 2.1f, 0.42f), materials.Archive);
            CreateBlock(root.transform, "ArchiveCabinet_B", new Vector3(11f, 1.1f, 9.6f), new Vector3(1.2f, 2.1f, 0.42f), materials.Archive);
            CreateBlock(root.transform, "CanteenCounter", new Vector3(11f, 0.8f, -9.5f), new Vector3(5f, 1.1f, 0.7f), materials.Canteen);
        }

        private static void CreateCharacter(Transform parent, string name, Vector3 position, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.82f, 0.55f);
            ApplyMaterial(body, material);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            ApplyMaterial(head, material);

            var leftArm = CreateLimb(root.transform, "LeftArm", new Vector3(-0.42f, 1.12f, 0f), material);
            var rightArm = CreateLimb(root.transform, "RightArm", new Vector3(0.42f, 1.12f, 0f), material);

            var visual = root.AddComponent<PlaceholderCharacterVisual>();
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("head").objectReferenceValue = head.transform;
            serialized.FindProperty("body").objectReferenceValue = body.transform;
            serialized.FindProperty("leftArm").objectReferenceValue = leftArm;
            serialized.FindProperty("rightArm").objectReferenceValue = rightArm;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateLimb(Transform parent, string name, Vector3 localPosition, Material material)
        {
            var limb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            limb.name = name;
            limb.transform.SetParent(parent, false);
            limb.transform.localPosition = localPosition;
            limb.transform.localScale = new Vector3(0.09f, 0.48f, 0.09f);
            ApplyMaterial(limb, material);
            return limb.transform;
        }

        private static void CreateZone(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            CreateBlock(parent, name, position, scale, material);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            CreateBlock(parent, name, position, scale, material);
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            ApplyMaterial(block, material);
            return block;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position)
        {
            var label = new GameObject(text + "_Label");
            label.transform.SetParent(parent);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = 0.36f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.92f, 0.88f, 0.72f, 1f);
        }

        private sealed class BlockoutMaterials
        {
            public readonly Material Floor = CreateMaterial("VS_Floor", new Color(0.24f, 0.26f, 0.28f, 1f));
            public readonly Material RoomFloor = CreateMaterial("VS_RoomFloor", new Color(0.18f, 0.22f, 0.23f, 1f));
            public readonly Material Wall = CreateMaterial("VS_Wall", new Color(0.64f, 0.66f, 0.62f, 1f));
            public readonly Material Locker = CreateMaterial("VS_Locker", new Color(0.2f, 0.38f, 0.58f, 1f));
            public readonly Material Wood = CreateMaterial("VS_Wood", new Color(0.46f, 0.32f, 0.19f, 1f));
            public readonly Material Shelf = CreateMaterial("VS_Shelf", new Color(0.32f, 0.24f, 0.18f, 1f));
            public readonly Material Tech = CreateMaterial("VS_Tech", new Color(0.08f, 0.36f, 0.4f, 1f));
            public readonly Material Door = CreateMaterial("VS_Door", new Color(0.3f, 0.2f, 0.14f, 1f));
            public readonly Material Security = CreateMaterial("VS_Security", new Color(0.18f, 0.32f, 0.72f, 1f));
            public readonly Material Student = CreateMaterial("VS_Student", new Color(0.22f, 0.58f, 0.34f, 1f));
            public readonly Material Teacher = CreateMaterial("VS_Teacher", new Color(0.62f, 0.38f, 0.24f, 1f));
            public readonly Material Archive = CreateMaterial("VS_Archive", new Color(0.42f, 0.46f, 0.66f, 1f));
            public readonly Material Canteen = CreateMaterial("VS_Canteen", new Color(0.78f, 0.5f, 0.2f, 1f));
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
