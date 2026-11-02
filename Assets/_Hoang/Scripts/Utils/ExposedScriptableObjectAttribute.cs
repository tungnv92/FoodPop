using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExposedScriptableObjectAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ExposedScriptableObjectAttribute))]
public class ExposedScriptableObjectAttributeDrawer : PropertyDrawer
{
    private Editor editor = null;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        if (property.objectReferenceValue != null)
        {
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
        }

        if (property.objectReferenceValue != null && property.isExpanded)
        {
            EditorGUI.indentLevel++;

            if (!editor)
            {
                Editor.CreateCachedEditor(property.objectReferenceValue, null, ref editor);
            }

            editor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
    }
}
#endif
