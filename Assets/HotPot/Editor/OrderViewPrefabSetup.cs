using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that turns the order cards into one shared prefab: the left card (smallest x) is saved as Assets/HotPot/Prefabs/OrderView/OrderView.prefab (or the existing prefab is reused), every other card is replaced by an instance at the same place with the same name, and every scene reference to the old card (CustomerManager lanes, CustomerView.orderCard, ...) is moved to the new instance.
    // VI: Công cụ editor biến các thẻ order thành một prefab dùng chung: thẻ bên trái (x nhỏ nhất) được lưu thành Assets/HotPot/Prefabs/OrderView/OrderView.prefab (hoặc dùng lại prefab đã có), các thẻ còn lại được thay bằng instance cùng vị trí, cùng tên, và mọi tham chiếu trong scene tới thẻ cũ (làn của CustomerManager, CustomerView.orderCard, ...) được chuyển sang instance mới.
    public static class OrderViewPrefabSetup
    {
        public const string PrefabPath = "Assets/HotPot/Prefabs/OrderView/OrderView.prefab";

        // EN: Runs on the active scene and saves it when it had no other unsaved changes.
        // VI: Chạy trên scene đang mở và lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Make Order View Prefab")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            List<OrderView> views = Object.FindObjectsByType<OrderView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(v => v.gameObject.scene == scene)
                .OrderBy(v => v.transform.position.x).ToList();
            if (views.Count == 0)
            {
                Debug.LogWarning("[OrderViewPrefabSetup] Không tìm thấy OrderView nào trong scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                OrderView template = views[0];
                OrderViewSetup.Setup(template);
                if (!AssetDatabase.IsValidFolder("Assets/HotPot/Prefabs/OrderView"))
                    AssetDatabase.CreateFolder("Assets/HotPot/Prefabs", "OrderView");
                prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(template.gameObject, PrefabPath, InteractionMode.UserAction);
                if (prefab == null)
                {
                    Debug.LogError("[OrderViewPrefabSetup] Không lưu được prefab OrderView.");
                    return;
                }
                Debug.Log($"[OrderViewPrefabSetup] Đã tạo prefab {PrefabPath} từ {template.name}.", prefab);
            }

            int replaced = 0;
            foreach (OrderView view in views)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(view.gameObject) == prefab)
                    continue;
                ReplaceWithInstance(view.gameObject, prefab);
                replaced++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log($"[OrderViewPrefabSetup] Xong: {views.Count} thẻ dùng prefab OrderView ({replaced} thẻ được thay bằng instance).");
        }

        // EN: Creates a prefab instance in place of a card (same parent, sibling index, transform and name), remaps scene references from the old card's objects to their counterparts, then deletes the old card (with Undo).
        // VI: Tạo instance prefab thay cho một thẻ (cùng cha, thứ tự, transform và tên), chuyển tham chiếu trong scene từ object của thẻ cũ sang object tương ứng, rồi xoá thẻ cũ (có Undo).
        private static void ReplaceWithInstance(GameObject old, GameObject prefab)
        {
            Transform oldT = old.transform;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, oldT.parent);
            Undo.RegisterCreatedObjectUndo(instance, "Replace Order View With Prefab");
            Transform newT = instance.transform;
            newT.SetSiblingIndex(oldT.GetSiblingIndex());
            newT.SetLocalPositionAndRotation(oldT.localPosition, oldT.localRotation);
            newT.localScale = oldT.localScale;
            instance.name = old.name;
            instance.SetActive(old.activeSelf);

            Dictionary<Object, Object> map = BuildMap(old, instance);
            RemapSceneReferences(old.scene, map, old);
            Undo.DestroyObjectImmediate(old);
            OrderView view = instance.GetComponent<OrderView>();
            if (view != null)
                view.RefreshPreview();
        }

        // EN: Pairs every GameObject and component of the old card with the one at the same hierarchy path, type and index in the new instance.
        // VI: Ghép từng GameObject và component của thẻ cũ với object cùng đường dẫn, cùng kiểu và cùng thứ tự trong instance mới.
        private static Dictionary<Object, Object> BuildMap(GameObject old, GameObject instance)
        {
            Dictionary<Object, Object> map = new Dictionary<Object, Object>();
            foreach (Transform source in old.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(source, old.transform);
                Transform target = path.Length == 0 ? instance.transform : instance.transform.Find(path);
                if (target == null)
                    continue;
                map[source.gameObject] = target.gameObject;
                foreach (IGrouping<System.Type, Component> group in source.GetComponents<Component>().Where(c => c != null).GroupBy(c => c.GetType()))
                {
                    Component[] targets = target.GetComponents(group.Key);
                    int i = 0;
                    foreach (Component component in group)
                    {
                        if (i < targets.Length)
                            map[component] = targets[i];
                        i++;
                    }
                }
            }
            return map;
        }

        // EN: Rewrites object references in every component of the scene (outside the old card) that point into the old card.
        // VI: Ghi lại tham chiếu trong mọi component của scene (ngoài thẻ cũ) đang trỏ vào thẻ cũ.
        private static void RemapSceneReferences(Scene scene, Dictionary<Object, Object> map, GameObject old)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.transform.IsChildOf(old.transform))
                        continue;
                    SerializedObject so = new SerializedObject(component);
                    SerializedProperty property = so.GetIterator();
                    bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                            continue;
                        if (map.TryGetValue(property.objectReferenceValue, out Object replacement))
                        {
                            property.objectReferenceValue = replacement;
                            changed = true;
                        }
                    }
                    if (changed)
                        so.ApplyModifiedProperties();
                }
            }
        }
    }
}
