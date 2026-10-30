using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace FoodPop
{
    // EN: Inspector for configuring the belt spline and matching its slots to Slot Count.
    // VI: Inspector cấu hình spline băng chuyền và đồng bộ số slot theo Slot Count.
    [CustomEditor(typeof(ConveyorChefMovement))]
    public class ConveyorChefMovementEditor : Editor
    {
        // EN: Draws the default fields and a single button for the complete conveyor setup.
        // VI: Vẽ các field mặc định và một nút duy nhất để thiết lập toàn bộ băng chuyền.
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            ConveyorChefMovement movement = (ConveyorChefMovement)target;
            if (GUILayout.Button("Setup: Build Spline + Create Slots + Snap"))
                SetupAll(movement);
        }

        // EN: Rebuilds the spline, matches Slot Count, and spaces the slots along the path as one undoable setup.
        // VI: Dựng lại spline, đồng bộ Slot Count và chia đều slot trên đường đi trong một bước có thể hoàn tác.
        public static void SetupAll(ConveyorChefMovement movement)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Conveyor Chef");
            BuildSpline(movement);
            CreateSlots(movement);
            SortSlots(movement);
            SnapSlots(movement);
            Undo.CollapseUndoOperations(undoGroup);
        }

        // EN: Applies the slot sorting orders and layers in edit mode with undo support.
        // VI: Áp dụng sorting order và layer cho slot trong edit mode, có hỗ trợ undo.
        private static void SortSlots(ConveyorChefMovement movement)
        {
            foreach (ConveyorSlot slot in movement.Slots)
            {
                if (slot == null)
                    continue;
                foreach (Transform t in slot.GetComponentsInChildren<Transform>(true))
                    Undo.RecordObject(t.gameObject, "Sort Conveyor Slots");
                foreach (SpriteRenderer renderer in slot.GetComponentsInChildren<SpriteRenderer>(true))
                    Undo.RecordObject(renderer, "Sort Conveyor Slots");
            }
            movement.ApplySlotSorting();
        }

        // EN: Rebuilds the spline from the conveyor sprite with undo support.
        // VI: Dựng lại spline từ sprite băng chuyền, có hỗ trợ undo.
        private static void BuildSpline(ConveyorChefMovement movement)
        {
            movement.ResolveReferences();
            SplineContainer container = movement.GetComponent<SplineContainer>();
            Undo.RecordObjects(new Object[] { movement, container }, "Build Conveyor Spline");
            movement.RebuildSplineFromConveyor();
            EditorUtility.SetDirty(container);
            EditorUtility.SetDirty(movement);
        }

        // EN: Creates missing prefab slots and removes surplus slots from the end to match Slot Count, with undo support.
        // VI: Tạo slot prefab còn thiếu và xóa slot dư từ cuối danh sách cho đúng Slot Count, có hỗ trợ hoàn tác.
        private static void CreateSlots(ConveyorChefMovement movement)
        {
            Undo.RecordObject(movement, "Create Conveyor Slots");
            movement.EnsureSlots(InstantiateSlotPrefab);
            SerializedObject serialized = new SerializedObject(movement);
            SerializedProperty slots = serialized.FindProperty("slots");
            int count = Mathf.Max(1, serialized.FindProperty("slotCount").intValue);
            var surplus = new System.Collections.Generic.List<GameObject>();
            for (int i = count; i < slots.arraySize; i++)
            {
                ConveyorSlot slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as ConveyorSlot;
                if (slot != null)
                    surplus.Add(slot.gameObject);
            }
            if (slots.arraySize > count)
            {
                slots.arraySize = count;
                serialized.ApplyModifiedProperties();
                foreach (GameObject slot in surplus)
                    Undo.DestroyObjectImmediate(slot);
            }
            EditorUtility.SetDirty(movement);
        }

        // EN: Creates one linked prefab instance of the slot under the given parent and registers it for undo.
        // VI: Tạo một prefab instance có liên kết của slot dưới parent và đăng ký undo.
        private static ConveyorSlot InstantiateSlotPrefab(ConveyorSlot prefab, Transform parent)
        {
            ConveyorSlot slot = (ConveyorSlot)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(slot.gameObject, "Create Conveyor Slot");
            return slot;
        }

        // EN: Places the slots at their starting spline positions.
        // VI: Đặt slot vào vị trí bắt đầu trên spline.
        private static void SnapSlots(ConveyorChefMovement movement)
        {
            foreach (ConveyorSlot slot in movement.Slots)
            {
                if (slot != null)
                    Undo.RecordObject(slot.transform, "Snap Conveyor Slots");
            }
            movement.RefreshPathMetrics();
            movement.PlaceSlots();
        }
    }
}
