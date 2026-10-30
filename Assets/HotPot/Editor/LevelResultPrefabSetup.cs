using System;
using System.IO;
using FoodPop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// EN: Wires concrete popup classes to their prefabs and the user's existing scene UIManager object.
// VI: Nối các lớp popup cụ thể với prefab và object UIManager đã có của người dùng trong scene.
internal static class LevelResultPrefabSetup
{
    private const string PrefabFolder = "Assets/HotPot/Prefabs/UI/LevelResult/";

    // EN: Schedules the explicitly requested scene migration after Unity imports the concrete canvas scripts.
    // VI: Lên lịch chuyển đổi scene đã được yêu cầu sau khi Unity nhập script canvas cụ thể.
    [InitializeOnLoadMethod]
    private static void ScheduleMigration()
    {
        if (File.Exists(GetRequestPath())) EditorApplication.update += CompleteMigration;
    }

    // EN: Keeps the one-time migration marker outside project assets.
    // VI: Đặt dấu chuyển đổi một lần ngoài asset dự án.
    private static string GetRequestPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../output/LevelResultUI/ui-manager.request"));
    }

    // EN: Migrates prefab and scene references once compilation finishes, then consumes the request.
    // VI: Chuyển tham chiếu prefab và scene khi biên dịch xong, rồi nhận xong yêu cầu.
    private static void CompleteMigration()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= CompleteMigration;
        ConfigurePrefabs();
        ConfigureSceneUIManager();
        File.Delete(GetRequestPath());
    }

    // EN: Repairs missing concrete canvas references without rebuilding layouts or replacing assigned fields.
    // VI: Sửa tham chiếu canvas cụ thể còn thiếu mà không dựng lại bố cục hoặc thay trường đã gán.
    [MenuItem("Tools/HotPot/Setup Result Popup References")]
    public static void ConfigurePrefabs()
    {
        ConfigurePrefab<CanvasWin>("WinPopup", "Next", null, "Đã hoàn thành tất cả màn hiện có");
        ConfigurePrefab<CanvasLose>("LosePopup", "Replay", null, string.Empty);
        ConfigurePrefab<CanvasRevive>("RevivePopup", "Revive", "Decline Revive", "Hiện chưa thể dọn băng chuyền");
        AssetDatabase.SaveAssets();
    }

    // EN: Reuses the existing UIManager scene object, assigns child prefab instances and links level controllers before saving the scene.
    // VI: Dùng lại object UIManager trong scene, gán các bản prefab con và nối controller màn trước khi lưu scene.
    [MenuItem("Tools/HotPot/Setup Scene UIManager")]
    public static void ConfigureSceneUIManager()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Hãy thoát Play Mode trước khi gán UIManager.");
        var scene = SceneManager.GetActiveScene();
        UIManager manager = FindOrAttachSceneManager(scene);
        ConfigureManager(manager, true);
        AssignLevelControllers(scene, manager);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Không lưu được scene đã gán UIManager.");
        Debug.Log("[LevelResultPrefabSetup] Đã gán UIManager vào object " + manager.name + " cùng ba canvas con và lưu scene.");
    }

    // EN: Finds the sole scene manager or attaches one to the user's existing named object without creating a replacement.
    // VI: Tìm manager duy nhất của scene hoặc gắn vào object đã đặt tên của người dùng mà không tạo object thay thế.
    private static UIManager FindOrAttachSceneManager(Scene scene)
    {
        UIManager manager = null;
        foreach (UIManager candidate in Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.gameObject.scene == scene)
            {
                if (manager != null) throw new InvalidOperationException("Scene có nhiều UIManager, cần chọn rõ manager.");
                manager = candidate;
            }
        if (manager == null)
        {
            GameObject existing = null;
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == scene && string.Equals(candidate.name, "UIManager", StringComparison.OrdinalIgnoreCase))
                {
                    if (existing != null) throw new InvalidOperationException("Scene có nhiều object tên UIManager.");
                    existing = candidate.gameObject;
                }
            if (existing == null) throw new InvalidOperationException("Không tìm thấy object UIManager đã tạo trong scene.");
            manager = Undo.AddComponent<UIManager>(existing);
        }
        return manager;
    }

    // EN: Assigns the scene's manager to its level controllers while preserving other loaded scenes.
    // VI: Gán manager của scene cho các controller màn trong cùng scene, giữ các scene khác đang mở.
    private static void AssignLevelControllers(Scene scene, UIManager manager)
    {
        foreach (LevelController level in Object.FindObjectsByType<LevelController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (level.gameObject.scene != scene) continue;
            Undo.RecordObject(level, "Assign scene UIManager");
            var serialized = new SerializedObject(level);
            serialized.FindProperty("uiManager").objectReferenceValue = manager;
            serialized.ApplyModifiedProperties();
        }
    }

    // EN: Wires concrete child canvases for a scene manager or an isolated preview without runtime prefab lookup.
    // VI: Nối canvas con cụ thể cho manager scene hoặc bản xem thử riêng, không tìm prefab lúc chạy.
    public static void ConfigureManager(UIManager manager, bool recordUndo = false)
    {
        if (recordUndo) Undo.RecordObject(manager, "Configure scene popups");
        var serialized = new SerializedObject(manager);
        AssignScenePopup<CanvasWin>(manager, serialized, "canvasWin", "WinPopup", "CanvasWin", recordUndo);
        AssignScenePopup<CanvasLose>(manager, serialized, "canvasLose", "LosePopup", "CanvasLose", recordUndo);
        AssignScenePopup<CanvasRevive>(manager, serialized, "canvasRevive", "RevivePopup", "CanvasRevive", recordUndo);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        manager.HideAll();
        EditorUtility.SetDirty(manager);
    }

    // EN: Keeps an assigned scene canvas or creates one linked prefab child when no matching canvas exists.
    // VI: Giữ canvas scene đã gán hoặc tạo một bản prefab con còn liên kết khi chưa có canvas tương ứng.
    private static void AssignScenePopup<T>(UIManager manager, SerializedObject serialized, string propertyName, string prefabName, string childName, bool recordUndo) where T : LevelPopupView
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        T popup = property.objectReferenceValue as T;
        if (popup != null && EditorUtility.IsPersistent(popup)) popup = null;
        if (popup == null) popup = manager.GetComponentInChildren<T>(true);
        if (popup == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefabName + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Thiếu prefab " + prefabName);
            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, manager.transform);
            if (recordUndo) Undo.RegisterCreatedObjectUndo(child, "Add scene popup");
            child.name = childName;
            popup = child.GetComponent<T>();
            if (popup == null) throw new InvalidOperationException("Prefab thiếu lớp " + typeof(T).Name);
        }
        if (recordUndo) Undo.RecordObject(popup.gameObject, "Hide scene popup");
        popup.Hide();
        property.objectReferenceValue = popup;
    }

    // EN: Fills only missing serialized presentation references on one concrete prefab, retaining its artwork and events.
    // VI: Chỉ gán tham chiếu hiển thị đã lưu còn thiếu trên một prefab cụ thể, giữ hình ảnh và sự kiện.
    private static void ConfigurePrefab<T>(string prefabName, string primaryName, string secondaryName, string unavailableStatus) where T : LevelPopupView
    {
        string path = PrefabFolder + prefabName + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            T view = root.GetComponent<T>();
            if (view == null) view = root.AddComponent<T>();
            var serialized = new SerializedObject(view);
            AssignMissing(serialized, "popupCanvas", root.GetComponent<Canvas>());
            AssignMissing(serialized, "canvasGroup", root.GetComponent<CanvasGroup>());
            AssignMissing(serialized, "card", root.transform.Find("Result Card"));
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == primaryName) AssignMissing(serialized, "primaryButton", button);
                if (button.name == secondaryName) AssignMissing(serialized, "secondaryButton", button);
            }
            foreach (Text label in root.GetComponentsInChildren<Text>(true))
                if (label.name == "Status") AssignMissing(serialized, "statusLabel", label);
            SerializedProperty unavailable = serialized.FindProperty("unavailableStatus");
            if (string.IsNullOrEmpty(unavailable.stringValue)) unavailable.stringValue = unavailableStatus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // EN: Fills an unassigned serialized reference without replacing a designer's existing assignment.
    // VI: Gán tham chiếu đã lưu còn trống mà không thay thế lựa chọn hiện có của người thiết kế.
    private static void AssignMissing(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property.objectReferenceValue == null) property.objectReferenceValue = value;
    }
}
