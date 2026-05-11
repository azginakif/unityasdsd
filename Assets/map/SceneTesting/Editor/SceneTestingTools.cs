using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneTestingTools
{
    private const string PlayerName = "First Person Test Player";

    [MenuItem("Tools/Scene Testing/Add First Person Test Player", priority = 10)]
    public static void AddFirstPersonTestPlayer()
    {
        GameObject player = new GameObject(PlayerName);
        Undo.RegisterCreatedObjectUndo(player, "Create First Person Test Player");
        player.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.35f;
        controller.slopeLimit = 55f;

        GameObject cameraObject = new GameObject("Player Camera");
        Undo.RegisterCreatedObjectUndo(cameraObject, "Create Player Camera");
        Undo.SetTransformParent(cameraObject.transform, player.transform, "Parent Player Camera");
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObject.tag = "MainCamera";

        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.nearClipPlane = 0.03f;
        cameraObject.AddComponent<AudioListener>();

        FirstPersonTestController testController = player.AddComponent<FirstPersonTestController>();
        Undo.RecordObject(testController, "Assign Test Camera");
        testController.SetCameraPivot(cameraObject.transform);
        EditorUtility.SetDirty(testController);
        EditorSceneManager.MarkSceneDirty(player.scene);

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log(
            "First person test player added. In Play mode use WASD to move, Space to jump, Shift to sprint, left click to lock the cursor, and Esc to release it.",
            player);
    }

    [MenuItem("Tools/Scene Testing/Report Renderers Without Colliders", priority = 11)]
    public static void ReportRenderersWithoutColliders()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int missingColliderCount = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (renderer.GetComponentInParent<Collider>() != null)
            {
                continue;
            }

            missingColliderCount++;
            Debug.LogWarning($"Collider may be missing: {GetHierarchyPath(renderer.transform)}", renderer.gameObject);
        }

        if (missingColliderCount == 0)
        {
            Debug.Log("No obvious collider gaps were found on active renderers.");
            return;
        }

        Debug.LogWarning($"Missing collider warning count: {missingColliderCount}. You can select the objects directly from the Console.");
    }

    private static Vector3 GetSpawnPosition()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            return SceneView.lastActiveSceneView.pivot + Vector3.up;
        }

        return Vector3.up * 2f;
    }

    private static Quaternion GetSpawnRotation()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 euler = SceneView.lastActiveSceneView.rotation.eulerAngles;
            return Quaternion.Euler(0f, euler.y, 0f);
        }

        return Quaternion.identity;
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = $"{target.name}/{path}";
        }

        return path;
    }
}
