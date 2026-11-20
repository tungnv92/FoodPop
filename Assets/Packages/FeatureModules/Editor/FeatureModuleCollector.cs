using System.Collections.Generic;
using GamePackages;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds every FeatureModule asset in the project to the ProfileManager module list of the open scenes.
// Thêm mọi asset FeatureModule trong project vào danh sách module của ProfileManager trong các scene đang mở.
public class FeatureModuleCollector : AssetPostprocessor
{
    private const string ModulesPath = "featureRegistry.modules";

    // EN: Collects modules automatically when a FeatureModule asset is imported or created.
    // VI: Tự gom module khi một asset FeatureModule được import hoặc tạo mới.
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (AssetDatabase.LoadAssetAtPath<FeatureModule>(path) == null) continue;
            EditorApplication.delayCall += CollectModules;
            return;
        }
    }

    // EN: Keeps the existing module order, appends modules missing from the list and drops empty slots on every ProfileManager in the open scenes.
    // VI: Giữ thứ tự module hiện có, thêm module còn thiếu và bỏ ô trống cho mọi ProfileManager trong các scene đang mở.
    [MenuItem("FoodPop/Feature Modules/Collect Modules")]
    public static void CollectModules()
    {
        List<FeatureModule> allModules = FindAllModules();
        ProfileManager[] managers = Object.FindObjectsByType<ProfileManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ProfileManager manager in managers)
        {
            CollectInto(manager, allModules);
        }
        Debug.Log($"[FeatureModuleCollector] {allModules.Count} module, {managers.Length} ProfileManager.");
    }

    // EN: Loads every FeatureModule asset in the project, sorted by asset path for a stable order.
    // VI: Nạp mọi asset FeatureModule trong project, sắp theo đường dẫn để thứ tự ổn định.
    private static List<FeatureModule> FindAllModules()
    {
        List<string> paths = new();
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FeatureModule)))
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        paths.Sort(System.StringComparer.Ordinal);

        List<FeatureModule> modules = new();
        foreach (string path in paths)
        {
            modules.Add(AssetDatabase.LoadAssetAtPath<FeatureModule>(path));
        }
        return modules;
    }

    // EN: Rewrites one ProfileManager's module list with undo support and marks its scene dirty when it changed.
    // VI: Ghi lại danh sách module của một ProfileManager có hỗ trợ undo và đánh dấu scene đã sửa khi có thay đổi.
    private static void CollectInto(ProfileManager manager, List<FeatureModule> allModules)
    {
        SerializedObject serialized = new SerializedObject(manager);
        SerializedProperty list = serialized.FindProperty(ModulesPath);

        List<FeatureModule> result = new();
        for (int i = 0; i < list.arraySize; i++)
        {
            FeatureModule module = list.GetArrayElementAtIndex(i).objectReferenceValue as FeatureModule;
            if (module != null && !result.Contains(module)) result.Add(module);
        }
        foreach (FeatureModule module in allModules)
        {
            if (!result.Contains(module)) result.Add(module);
        }

        bool changed = result.Count != list.arraySize;
        for (int i = 0; !changed && i < result.Count; i++)
        {
            changed = list.GetArrayElementAtIndex(i).objectReferenceValue != result[i];
        }
        if (!changed) return;

        list.arraySize = result.Count;
        for (int i = 0; i < result.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = result[i];
        }
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
