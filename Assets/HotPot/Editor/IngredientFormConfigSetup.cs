using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that moves the ingredient grid forms out of the layout components into shared ScriptableObject assets (one per layout type: IngredientForms_1Dish / _2Dish / _3Dish under Assets/HotPot/Config/OrderView) and assigns them to the OrderView prefab and every order card in the scene. The values are copied from the card whose forms were edited (a scene override), otherwise from the prefab.
    // VI: Công cụ editor chuyển bộ mẫu lưới nguyên liệu từ component bố cục sang asset ScriptableObject dùng chung (mỗi loại bố cục một asset: IngredientForms_1Dish / _2Dish / _3Dish trong Assets/HotPot/Config/OrderView) rồi gán cho prefab OrderView và mọi thẻ order trong scene. Giá trị lấy từ thẻ đã được chỉnh mẫu (có override trong scene), không có thì lấy từ prefab.
    public static class IngredientFormConfigSetup
    {
        public const string Folder = "Assets/HotPot/Config/OrderView";

        private static readonly (System.Type type, string name)[] LayoutTypes =
        {
            (typeof(OrderLayout1Dish), "IngredientForms_1Dish"),
            (typeof(OrderLayout2Dish), "IngredientForms_2Dish"),
            (typeof(OrderLayout3Dish), "IngredientForms_3Dish"),
        };

        // EN: Creates the missing assets from the current forms, assigns them everywhere and saves the scene when it had no other unsaved changes.
        // VI: Tạo asset còn thiếu từ bộ mẫu hiện tại, gán ở mọi nơi và lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Create Ingredient Form Configs")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            EnsureFolder();
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(OrderViewPrefabSetup.PrefabPath);
            List<OrderLayout> sceneLayouts = Object.FindObjectsByType<OrderLayout>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l.gameObject.scene == scene).ToList();

            foreach ((System.Type type, string name) in LayoutTypes)
            {
                string path = $"{Folder}/{name}.asset";
                IngredientGridFormConfig config = AssetDatabase.LoadAssetAtPath<IngredientGridFormConfig>(path);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<IngredientGridFormConfig>();
                    OrderLayout source = PickSource(type, sceneLayouts, prefabRoot);
                    if (source != null && source.LegacyIngredientForms != null && source.LegacyIngredientForms.Length > 0)
                        config.forms = (IngredientGridForm[])source.LegacyIngredientForms.Clone();
                    AssetDatabase.CreateAsset(config, path);
                    Debug.Log($"[IngredientFormConfigSetup] Đã tạo {path}" + (source != null ? $" từ {source.name} ({type.Name})." : " với mẫu mặc định."), config);
                }
                AssignToPrefab(type, config);
                foreach (OrderLayout layout in sceneLayouts.Where(l => l.GetType() == type))
                    Assign(layout, config);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log("[IngredientFormConfigSetup] Xong: mọi bố cục đã dùng asset bộ mẫu nguyên liệu dùng chung.");
        }

        // EN: Assigns an existing config asset of the matching type to a layout that has none (used by Setup Order Views).
        // VI: Gán asset bộ mẫu có sẵn đúng loại cho bố cục chưa có (dùng trong Setup Order Views).
        public static void AssignExisting(OrderLayout layout)
        {
            if (layout == null || layout.IngredientFormConfig != null)
                return;
            foreach ((System.Type type, string name) in LayoutTypes)
            {
                if (layout.GetType() != type)
                    continue;
                IngredientGridFormConfig config = AssetDatabase.LoadAssetAtPath<IngredientGridFormConfig>($"{Folder}/{name}.asset");
                if (config != null)
                    Assign(layout, config);
            }
        }

        // EN: Source of the initial values: a scene layout whose forms override the prefab, else the prefab's layout, else any scene layout of that type.
        // VI: Nguồn giá trị ban đầu: bố cục trong scene có override bộ mẫu so với prefab, không có thì bố cục trong prefab, không có nữa thì bố cục bất kỳ cùng loại trong scene.
        private static OrderLayout PickSource(System.Type type, List<OrderLayout> sceneLayouts, GameObject prefabRoot)
        {
            foreach (OrderLayout layout in sceneLayouts.Where(l => l.GetType() == type))
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromSource(layout);
                PropertyModification[] mods = PrefabUtility.GetPropertyModifications(layout.gameObject);
                if (source != null && mods != null && mods.Any(m => m.target == source && m.propertyPath.StartsWith("ingredientForms")))
                    return layout;
            }
            if (prefabRoot != null && prefabRoot.GetComponent(type) is OrderLayout fromPrefab)
                return fromPrefab;
            return sceneLayouts.FirstOrDefault(l => l.GetType() == type);
        }

        // EN: Writes the config into the OrderView prefab's layout of the given type (added if missing).
        // VI: Ghi asset vào bố cục cùng loại trong prefab OrderView (thêm component nếu thiếu).
        private static void AssignToPrefab(System.Type type, IngredientGridFormConfig config)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OrderViewPrefabSetup.PrefabPath) == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(OrderViewPrefabSetup.PrefabPath);
            try
            {
                OrderLayout layout = root.GetComponent(type) as OrderLayout;
                if (layout == null)
                    layout = root.AddComponent(type) as OrderLayout;
                SerializedObject so = new SerializedObject(layout);
                so.FindProperty("ingredientFormConfig").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, OrderViewPrefabSetup.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Assign(OrderLayout layout, IngredientGridFormConfig config)
        {
            SerializedObject so = new SerializedObject(layout);
            SerializedProperty property = so.FindProperty("ingredientFormConfig");
            if (property.objectReferenceValue == config)
                return;
            property.objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HotPot/Config"))
                AssetDatabase.CreateFolder("Assets/HotPot", "Config");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/HotPot/Config", "OrderView");
        }
    }
}
