using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that adds what every order card in the open scene is missing: OrderLayout1Dish / OrderLayout2Dish / OrderLayout3Dish on the OrderView object, a trigger BoxCollider2D on each dish slot (its size is fitted by the layout) as its tap area, and the FoodIconConfig reference.
    // VI: Công cụ editor thêm phần còn thiếu cho mọi thẻ order trong scene đang mở: OrderLayout1Dish / OrderLayout2Dish / OrderLayout3Dish trên object OrderView, BoxCollider2D (trigger, size do bố cục tự đặt) trên từng ô món làm vùng chạm, và tham chiếu FoodIconConfig.
    public static class OrderViewSetup
    {
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";

        // EN: Runs on the active scene and saves it when it had no other unsaved changes.
        // VI: Chạy trên scene đang mở và lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Setup Order Views")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            OrderView[] views = Object.FindObjectsByType<OrderView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (views.Length == 0)
            {
                Debug.LogWarning("[OrderViewSetup] Không tìm thấy OrderView nào trong scene; chạy Setup Customers And References trước.");
                return;
            }
            foreach (OrderView view in views)
                Setup(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log($"[OrderViewSetup] Đã bổ sung component cho {views.Length} thẻ order.");
        }

        // EN: Adds (with Undo) the layout components, slot colliders and references one card is missing.
        // VI: Thêm (có Undo) component bố cục, collider của ô món và tham chiếu mà một thẻ còn thiếu.
        public static void Setup(OrderView view)
        {
            if (view == null)
                return;
            if (view.GetComponent<OrderLayout1Dish>() == null)
                Undo.AddComponent<OrderLayout1Dish>(view.gameObject);
            if (view.GetComponent<OrderLayout2Dish>() == null)
                Undo.AddComponent<OrderLayout2Dish>(view.gameObject);
            if (view.GetComponent<OrderLayout3Dish>() == null)
                Undo.AddComponent<OrderLayout3Dish>(view.gameObject);
            foreach (OrderLayout layout in view.GetComponents<OrderLayout>())
                IngredientFormConfigSetup.AssignExisting(layout);

            SerializedObject so = new SerializedObject(view);
            SerializedProperty icons = so.FindProperty("foodIconConfig");
            if (icons.objectReferenceValue == null)
                icons.objectReferenceValue = AssetDatabase.LoadAssetAtPath<FoodIconConfig>(FoodIconConfigPath);
            so.ApplyModifiedProperties();

            foreach (OrderItemView item in view.Items)
            {
                if (item == null || item.GetComponent<BoxCollider2D>() != null)
                    continue;
                BoxCollider2D area = Undo.AddComponent<BoxCollider2D>(item.gameObject);
                area.isTrigger = true;
                area.size = Vector2.one;
                area.offset = Vector2.zero;
            }
        }
    }
}
