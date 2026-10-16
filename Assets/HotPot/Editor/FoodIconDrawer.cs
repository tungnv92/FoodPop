using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    [CustomPropertyDrawer(typeof(FoodIconConfig.FoodIcon))]
    public class FoodIconDrawer : PropertyDrawer
    {
        private const float PreviewSize = 64f;
        private const float Padding = 4f;

        // EN: Reserves a type selector and separate preview rows for raw and prepared sprites.
        // VI: Dành chỗ chọn loại và hai hàng xem trước cho sprite thô và đã sơ chế.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + (PreviewSize + Padding) * 2f + Padding;
        }

        // EN: Draws the ingredient type and both state previews using serialized fields with Undo support.
        // VI: Vẽ loại nguyên liệu và ảnh hai trạng thái bằng field serialized có hỗ trợ Undo.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("foodType"), GUIContent.none);
            Rect row = new Rect(position.x, typeRect.yMax + Padding, position.width, PreviewSize);
            DrawStateRow(row, property.FindPropertyRelative("sprite"), "Raw · Thô");
            row.y += PreviewSize + Padding;
            DrawStateRow(row, property.FindPropertyRelative("preparedSprite"), "Prepared · Sơ chế");

            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }

        // EN: Shows a processing-state label, sprite picker and matching preview on one row.
        // VI: Hiện nhãn trạng thái, ô chọn sprite và ảnh xem trước tương ứng trên một hàng.
        private static void DrawStateRow(Rect row, SerializedProperty sprite, string label)
        {
            Rect preview = new Rect(row.x, row.y, PreviewSize, PreviewSize);
            DrawPreview(preview, sprite.objectReferenceValue as Sprite);
            Rect field = new Rect(preview.xMax + Padding, row.y, Mathf.Max(0f, row.width - PreviewSize - Padding), EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(field, label);
            field.y += EditorGUIUtility.singleLineHeight + Padding;
            EditorGUI.PropertyField(field, sprite, GUIContent.none);
        }

        // EN: Shows a cached sprite preview with a thumbnail fallback while Unity generates the full preview.
        // VI: Hiện ảnh xem trước sprite đã lưu đệm, dùng ảnh thu nhỏ trong lúc Unity tạo bản xem trước đầy đủ.
        private static void DrawPreview(Rect rect, Sprite sprite)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            if (sprite == null)
            {
                GUI.Label(rect, "Chưa có ảnh", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(sprite);
            if (preview != null)
                GUI.DrawTexture(new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2f, rect.height - Padding * 2f), preview, ScaleMode.ScaleToFit, true);
            if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID()))
                HandleUtility.Repaint();
        }
    }
}
