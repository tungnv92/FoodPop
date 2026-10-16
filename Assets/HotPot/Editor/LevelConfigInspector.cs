using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Inspector for LevelConfig assets with a button that opens the visual Level Editor.
    // VI: Inspector cho asset LevelConfig, có nút mở công cụ thiết kế Level Editor.
    [CustomEditor(typeof(LevelConfig))]
    public class LevelConfigInspector : Editor
    {
        // EN: Draws the open button above the default fields.
        // VI: Vẽ nút mở công cụ phía trên các field mặc định.
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Mở Level Editor", GUILayout.Height(28f)))
                LevelConfigEditorWindow.Open((LevelConfig)target);
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
