using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace FoodPop
{
    // EN: One-time editor automation that attaches ConveyorChefMovement to Conveyor_Chef and builds its spline and 12 slots.
    // VI: Tự động hóa một lần trong editor: gắn ConveyorChefMovement vào Conveyor_Chef, dựng spline và 12 slot.
    [InitializeOnLoad]
    public static class ConveyorChefAutoSetup
    {
        private const string DoneKey = "HotPot.ConveyorChefAutoSetup.v4";
        private const string TargetName = "Conveyor_Chef";
        private const string SlotPrefabPath = "Assets/HotPot/Prefabs/Ingame/Conveyor_Slot.prefab";

        // EN: Schedules the one-time setup after scripts reload, once the editor is idle.
        // VI: Lên lịch thiết lập một lần sau khi script reload, khi editor rảnh.
        static ConveyorChefAutoSetup()
        {
            if (EditorPrefs.GetBool(DoneKey, false))
                return;
            EditorApplication.delayCall += TryRunOnce;
        }

        // EN: Runs the setup unless the editor is playing or compiling, then remembers that it has completed.
        // VI: Chạy thiết lập nếu editor không đang Play hoặc compile, rồi ghi nhớ là đã hoàn tất.
        private static void TryRunOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (Run())
                EditorPrefs.SetBool(DoneKey, true);
        }

        // EN: Menu entry to run the Conveyor_Chef setup again at any time.
        // VI: Mục menu để chạy lại thiết lập Conveyor_Chef bất cứ lúc nào.
        [MenuItem("Tools/HotPot/Setup Conveyor_Chef Movement")]
        private static void RunFromMenu()
        {
            Run();
        }

        // EN: Finds Conveyor_Chef in the open scenes, configures the movement component and builds spline and slots.
        // VI: Tìm Conveyor_Chef trong các scene đang mở, cấu hình component di chuyển và dựng spline cùng slot.
        private static bool Run()
        {
            GameObject target = FindInOpenScenes(TargetName);
            if (target == null)
            {
                Debug.LogWarning($"[ConveyorChefAutoSetup] '{TargetName}' not found in open scenes.");
                return false;
            }
            ConveyorChefMovement movement = EnsureMovement(target);
            AssignSlotPrefab(movement);
            ConveyorChefMovementEditor.SetupAll(movement);
            EditorSceneManager.MarkSceneDirty(target.scene);
            Selection.activeGameObject = target;
            Debug.Log($"[ConveyorChefAutoSetup] Setup done: {movement.Slots.Count} slots on spline of '{TargetName}'.", target);
            return true;
        }

        // EN: Adds SplineContainer and ConveyorChefMovement to the target when missing, with undo.
        // VI: Thêm SplineContainer và ConveyorChefMovement vào đối tượng nếu chưa có, có hỗ trợ undo.
        private static ConveyorChefMovement EnsureMovement(GameObject target)
        {
            if (target.GetComponent<SplineContainer>() == null)
                Undo.AddComponent<SplineContainer>(target);
            ConveyorChefMovement movement = target.GetComponent<ConveyorChefMovement>();
            if (movement == null)
                movement = Undo.AddComponent<ConveyorChefMovement>(target);
            movement.ResolveReferences();
            return movement;
        }

        // EN: Assigns the Conveyor_Slot prefab to the private slotPrefab field when it is empty.
        // VI: Gán prefab Conveyor_Slot vào field slotPrefab khi field còn trống.
        private static void AssignSlotPrefab(ConveyorChefMovement movement)
        {
            SerializedObject serialized = new SerializedObject(movement);
            SerializedProperty property = serialized.FindProperty("slotPrefab");
            if (property.objectReferenceValue != null)
                return;
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConveyorSlot>(SlotPrefabPath);
            serialized.ApplyModifiedProperties();
        }

        // EN: Returns the first GameObject with the given name in any loaded scene, including inactive ones.
        // VI: Trả về GameObject đầu tiên có tên cho trước trong mọi scene đang mở, kể cả object bị tắt.
        private static GameObject FindInOpenScenes(string objectName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == objectName)
                            return child.gameObject;
                    }
                }
            }
            return null;
        }
    }
}
