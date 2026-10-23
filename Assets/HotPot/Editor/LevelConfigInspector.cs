using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Inspector for LevelConfig assets with a button that opens the visual Level Editor.
    // VI: Inspector cho asset LevelConfig, có nút mở công cụ thiết kế Level Editor.
    [CustomEditor(typeof(LevelConfig))]
    public class LevelConfigInspector : Editor
    {
        // EN: Opens the draft editor and previews asset fields read-only to prevent accidental direct asset edits.
        // VI: Mở công cụ chỉnh bản nháp và xem field asset ở chế độ chỉ đọc để tránh vô tình sửa trực tiếp asset.
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Mở Level Editor", GUILayout.Height(28f)))
                LevelConfigEditorWindow.Open((LevelConfig)target);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Chỉnh dữ liệu trong Level Editor và bấm Lưu để ghi vào asset. Các field bên dưới chỉ để xem.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                DrawDefaultInspector();
        }
    }
}
