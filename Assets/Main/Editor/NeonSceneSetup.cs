using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NeonSceneSetup
{
    [MenuItem("Tools/Neon/Setup Prototype Scene")]
    private static void SetupPrototypeScene()
    {
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null) {
            GameObject cameraObject = new("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        Manager manager = Object.FindFirstObjectByType<Manager>();
        if (manager == null) {
            manager = new GameObject("NeonManager").AddComponent<Manager>();
        }

        AssignFirstAsset(manager, "gameDatas", FindFirstAssetGuid<GameDatas>());
        AssignFirstAsset(manager, "gamePlayDatas", FindFirstAssetGuid<GamePlayDatas>());
        AssignEnemyList(manager, FindAllAssetGuids<EnemyDatas>());

        Selection.activeObject = manager.gameObject;
        EditorUtility.DisplayDialog("Neon Setup", "Prototype scene objects were created. Check the Manager references and press Play.", "OK");
    }

    private static void AssignFirstAsset(Object target, string fieldName, string guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) {
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guid);
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null) {
            return;
        }

        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(path);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignEnemyList(Object target, List<string> guids)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty("enemyTypes");
        if (property == null) {
            return;
        }

        property.arraySize = guids.Count;
        for (int i = 0; i < guids.Count; i++) {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDatas>(path);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string FindFirstAssetGuid<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids.Length > 0 ? guids[0] : string.Empty;
    }

    private static List<string> FindAllAssetGuids<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return new List<string>(guids);
    }
}
