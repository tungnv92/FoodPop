using UnityEditor;
using UnityEngine;

// EN: Exposes renderer sorting through an editor window when Unity hides it in the MeshRenderer inspector.
// VI: Cho phép chỉnh thứ tự vẽ bằng cửa sổ editor khi Inspector MeshRenderer không hiển thị.
public class RendererSortingWindow : EditorWindow
{
    private Renderer targetRenderer;

    // EN: Opens the sorting window with the selected object's renderer.
    // VI: Mở cửa sổ thứ tự vẽ với renderer của object đang chọn.
    [MenuItem("Tools/HotPot/Renderer Sorting")]
    public static void Open()
    {
        GetWindow<RendererSortingWindow>("Renderer Sorting").ResolveSelection();
    }

    // EN: Follows selection changes and redraws the controls.
    // VI: Theo dõi thay đổi lựa chọn và vẽ lại các nút chỉnh.
    private void OnSelectionChange()
    {
        ResolveSelection();
        Repaint();
    }

    // EN: Resolves a renderer directly on the selected object without choosing an unrelated child.
    // VI: Lấy renderer ngay trên object đang chọn, không tự chọn object con khác.
    private void ResolveSelection()
    {
        targetRenderer = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Renderer>() : null;
    }

    // EN: Edits the renderer's real sorting layer and order with Undo and prefab override support.
    // VI: Chỉnh sorting layer và thứ tự vẽ thực của renderer, hỗ trợ Undo và ghi đè prefab.
    private void OnGUI()
    {
        targetRenderer = (Renderer)EditorGUILayout.ObjectField("Renderer", targetRenderer, typeof(Renderer), true);
        if (targetRenderer == null)
        {
            EditorGUILayout.HelpBox("Chọn object phần smoke có MeshRenderer hoặc kéo Renderer vào ô trên.", MessageType.Info);
            return;
        }
        SortingLayer[] layers = SortingLayer.layers;
        string[] names = new string[layers.Length];
        int selected = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            names[i] = layers[i].name;
            if (layers[i].id == targetRenderer.sortingLayerID)
                selected = i;
        }
        EditorGUI.BeginChangeCheck();
        int layer = EditorGUILayout.Popup("Sorting Layer", selected, names);
        int order = EditorGUILayout.IntField("Order in Layer", targetRenderer.sortingOrder);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetRenderer, "Change Renderer Sorting");
            targetRenderer.sortingLayerID = layers[layer].id;
            targetRenderer.sortingOrder = Mathf.Clamp(order, -32768, 32767);
            EditorUtility.SetDirty(targetRenderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetRenderer);
            if (!EditorUtility.IsPersistent(targetRenderer) && targetRenderer.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetRenderer.gameObject.scene);
            SceneView.RepaintAll();
        }
        EditorGUILayout.HelpBox("Đặt cùng Sorting Layer với thức ăn và Order in Layer cao hơn để vẽ smoke phía trước. Lưu scene sau khi chỉnh.", MessageType.Info);
    }
}
