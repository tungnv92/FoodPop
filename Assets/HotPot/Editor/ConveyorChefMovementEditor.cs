using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace FoodPop
{
    // EN: Inspector for ConveyorChefMovement with one-click setup of the belt spline and the 12 conveyor slots.
    // VI: Inspector cho ConveyorChefMovement, dựng spline băng chuyền và 12 slot chỉ với một nút bấm.
    [CustomEditor(typeof(ConveyorChefMovement))]
    public class ConveyorChefMovementEditor : Editor
    {
        // EN: Draws the default fields followed by the setup buttons.
        // VI: Vẽ các field mặc định rồi đến các nút thiết lập.
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            ConveyorChefMovement movement = (ConveyorChefMovement)target;
            if (GUILayout.Button("Setup: Build Spline + Create Slots + Snap"))
                SetupAll(movement);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Spline From Conveyor"))
                    BuildSpline(movement);
                if (GUILayout.Button("Create / Collect Slots"))
                    CreateSlots(movement);
                if (GUILayout.Button("Sort Slots Above Belt"))
                    SortSlots(movement);
                if (GUILayout.Button("Snap Slots To Path"))
                    SnapSlots(movement);
            }
        }

        // EN: Runs the full setup: spline, slots, then snapping them onto the path.
        // VI: Chạy toàn bộ thiết lập: spline, slot, rồi đặt slot lên đường đi.
        public static void SetupAll(ConveyorChefMovement movement)
        {
            BuildSpline(movement);
            CreateSlots(movement);
            SortSlots(movement);
            SnapSlots(movement);
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

        // EN: Collects child slots and instantiates missing ones as prefab instances with undo support.
        // VI: Gom slot con và tạo thêm slot thiếu dưới dạng prefab instance, có hỗ trợ undo.
        private static void CreateSlots(ConveyorChefMovement movement)
        {
            Undo.RecordObject(movement, "Create Conveyor Slots");
            movement.EnsureSlots(InstantiateSlotPrefab);
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
