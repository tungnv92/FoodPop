using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoodPop
{
    // EN: Inspector for FoodBoardManager with an "Update View" button that re-lays out columns and dishes after rowSpacing/columnSpacing change.
    // VI: Inspector cho FoodBoardManager, có nút "Update View" để dàn lại cột và đĩa sau khi đổi rowSpacing/columnSpacing.
    [CustomEditor(typeof(FoodBoardManager))]
    public class FoodBoardManagerEditor : Editor
    {
        // EN: Draws the default fields, then the Update View button below them.
        // VI: Vẽ các field mặc định, rồi vẽ nút Update View bên dưới.
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Update View", GUILayout.Height(28f)))
                UpdateView((FoodBoardManager)target);
        }

        // EN: Records Undo for every column and its dishes (Edit Mode only), calls UpdateView, then marks the scene dirty so the new positions are saved.
        // VI: Ghi Undo cho mọi cột và đĩa con (chỉ khi chưa Play), gọi UpdateView, rồi đánh dấu scene đã đổi để lưu vị trí mới.
        private void UpdateView(FoodBoardManager board)
        {
            bool editing = !Application.isPlaying;
            if (editing)
                RecordColumnsUndo();
            board.UpdateView();
            if (editing)
                EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        // EN: Registers every transform under the assigned ingredient columns with Undo before they are moved.
        // VI: Đăng ký Undo cho mọi transform thuộc các cột nguyên liệu đã gán trước khi di chuyển.
        private void RecordColumnsUndo()
        {
            SerializedProperty columns = serializedObject.FindProperty("ingredientColumns");
            if (columns == null)
                return;
            for (int i = 0; i < columns.arraySize; i++)
            {
                Transform column = columns.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (column != null)
                    Undo.RecordObjects(column.GetComponentsInChildren<Transform>(true), "Update Board View");
            }
        }
    }
}
