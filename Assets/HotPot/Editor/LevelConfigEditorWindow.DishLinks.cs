using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    public partial class LevelConfigEditorWindow
    {
        // EN: Edits ordered link chains on the draft using one-based column/row coordinates.
        // VI: Chỉnh chuỗi nối có thứ tự trên bản nháp bằng tọa độ cột/hàng bắt đầu từ 1.
        private void DrawDishLinks()
        {
            DrawSectionTitle("Dish Link · dây nối thẳng");
            EditorGUILayout.HelpBox("Chọn 2–3 ô gốc theo thứ tự A–B–C. Các đoạn dây luôn thẳng, có thể đi qua đĩa ở giữa; chỉ các thành viên được chọn thuộc nhóm. Chạm một thành viên để lấy cả nhóm khi không bị đĩa ngoài nhóm chắn.", MessageType.Info);
            SerializedProperty chains = serialized.FindProperty("dishLinks");
            for (int i = 0; i < chains.arraySize; i++)
            {
                int index = i;
                SerializedProperty members = chains.GetArrayElementAtIndex(i).FindPropertyRelative("dishes");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Nhóm {i + 1} · {(members.arraySize == 3 ? "A–B–C" : "A–B")}", EditorStyles.boldLabel);
                if (GUILayout.Button("Xóa nhóm", GUILayout.Width(85f)))
                    pendingAction = () => RemoveDishLink(index);
                EditorGUILayout.EndHorizontal();
                float previousWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 30f;
                for (int j = 0; j < members.arraySize; j++)
                {
                    SerializedProperty position = members.GetArrayElementAtIndex(j);
                    Vector2Int value = position.vector2IntValue;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(((char)('A' + j)).ToString(), EditorStyles.boldLabel, GUILayout.Width(18f));
                    int column = EditorGUILayout.IntField("Cột", value.x + 1);
                    int row = EditorGUILayout.IntField("Hàng", value.y + 1);
                    position.vector2IntValue = new Vector2Int(column - 1, row - 1);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUIUtility.labelWidth = previousWidth;
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Nhóm 2 đĩa"))
                pendingAction = () => AddDishLink(2);
            if (GUILayout.Button("+ Nhóm 3 đĩa"))
                pendingAction = () => AddDishLink(3);
            EditorGUILayout.EndHorizontal();
        }

        // EN: Creates a draft chain from unclaimed anchors, preferring dishes already holding ingredients.
        // VI: Tạo chuỗi trên bản nháp từ ô gốc chưa thuộc nhóm, ưu tiên đĩa đã có nguyên liệu.
        private void AddDishLink(int count)
        {
            serialized.ApplyModifiedProperties();
            if (level.dishLinks == null)
                level.dishLinks = new List<DishLinkConfig>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();
            foreach (DishLinkConfig chain in level.dishLinks)
                if (chain?.dishes != null)
                    foreach (Vector2Int cell in chain.dishes)
                        used.Add(cell);
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int pass = 0; pass < 2; pass++)
                for (int r = 0; r < level.boardRows; r++)
                    for (int c = 0; c < level.boardColumns; c++)
                    {
                        OrderFood cell = level.GetCell(c, r);
                        Vector2Int position = new Vector2Int(c, r);
                        if (cell == null || level.IsCoveredCell(c, r) || used.Contains(position) || candidates.Contains(position))
                            continue;
                        if (pass == 0 && cell.foodType == FoodType.None)
                            continue;
                        candidates.Add(position);
                    }
            if (candidates.Count < count)
            {
                EditorUtility.DisplayDialog("Không đủ đĩa", $"Cần {count} ô gốc chưa thuộc nhóm khác. Hãy tăng kích thước bảng hoặc xóa bớt nhóm.", "OK");
                return;
            }
            Undo.RecordObject(level, "Thêm Dish Link");
            DishLinkConfig group = new DishLinkConfig();
            group.dishes.AddRange(candidates.GetRange(0, count));
            level.dishLinks.Add(group);
            EditorUtility.SetDirty(level);
        }

        // EN: Removes a complete chain with Undo without deleting any member dish.
        // VI: Xóa nguyên chuỗi có Undo mà không xóa đĩa thành viên.
        private void RemoveDishLink(int index)
        {
            serialized.ApplyModifiedProperties();
            if (level.dishLinks == null || index < 0 || index >= level.dishLinks.Count)
                return;
            Undo.RecordObject(level, "Xóa Dish Link");
            level.dishLinks.RemoveAt(index);
            EditorUtility.SetDirty(level);
        }

        // EN: Previews straight A–B–C segments without changing input hit areas or routing around intermediate cells.
        // VI: Xem trước đoạn thẳng A–B–C mà không đổi vùng nhận chạm hoặc né ô trung gian.
        private void DrawDishLinkPreview(Rect grid)
        {
            if (Event.current.type != EventType.Repaint || level.dishLinks == null)
                return;
            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = new Color(1f, 0.73f, 0.15f, 0.8f);
            foreach (DishLinkConfig group in level.dishLinks)
            {
                if (group?.dishes == null)
                    continue;
                for (int i = 1; i < group.dishes.Count; i++)
                {
                    Vector2Int a = group.dishes[i - 1];
                    Vector2Int b = group.dishes[i];
                    if (level.GetCell(a.x, a.y) == null || level.GetCell(b.x, b.y) == null)
                        continue;
                    Vector3 start = new Vector3(grid.x + a.x * (CellWidth + CellGap) + CellWidth * 0.5f, grid.y + a.y * (CellHeight + CellGap) + 26f);
                    Vector3 end = new Vector3(grid.x + b.x * (CellWidth + CellGap) + CellWidth * 0.5f, grid.y + b.y * (CellHeight + CellGap) + 26f);
                    Handles.DrawAAPolyLine(2f, start, end);
                }
            }
            Handles.color = previous;
            Handles.EndGUI();
        }
    }
}
