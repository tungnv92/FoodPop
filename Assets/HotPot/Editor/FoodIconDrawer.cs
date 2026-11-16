using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    [CustomPropertyDrawer(typeof(FoodIconConfig.FoodIcon))]
    public class FoodIconDrawer : PropertyDrawer
    {
        private const float PreviewSize = 64f;
        private const float Padding = 4f;

        // EN: Reserves sprite previews, sounds, debris and the skip-preparation toggle.
        // VI: Dành chỗ xem ảnh, âm thanh, vụn và ô "không cần sơ chế" riêng từng nguyên liệu.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + Padding;
            float variants = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("chopSoundVariants"), true) + Padding;
            return EditorGUIUtility.singleLineHeight + (PreviewSize + Padding) * 2f + Padding + line * 3f + variants;
        }

        // EN: Draws per-ingredient images, sound, debris and the skip-preparation toggle with serialized Undo support.
        // VI: Vẽ ảnh, âm thanh, vụn và ô "không cần sơ chế" riêng từng nguyên liệu với hỗ trợ Undo.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float typeWidth = position.width * 0.6f;
            Rect typeRect = new Rect(position.x, position.y, typeWidth - Padding, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("foodType"), GUIContent.none);
            Rect categoryRect = new Rect(position.x + typeWidth, position.y, position.width - typeWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(categoryRect, property.FindPropertyRelative("category"), GUIContent.none);
            Rect row = new Rect(position.x, typeRect.yMax + Padding, position.width, PreviewSize);
            DrawStateRow(row, property.FindPropertyRelative("sprite"), "Raw · Thô");
            row.y += PreviewSize + Padding;
            DrawStateRow(row, property.FindPropertyRelative("preparedSprite"), "Prepared · Sơ chế");
            Rect line = new Rect(position.x, row.yMax + Padding, position.width, EditorGUIUtility.singleLineHeight);
            DrawLabeledField(line, property.FindPropertyRelative("chopSound"), "Tiếng chặt");
            line.y += EditorGUIUtility.singleLineHeight + Padding;
            SerializedProperty variants = property.FindPropertyRelative("chopSoundVariants");
            Rect variantsRect = new Rect(line.x, line.y, line.width, EditorGUI.GetPropertyHeight(variants, true));
            EditorGUI.PropertyField(variantsRect, variants, new GUIContent("Biến thể tiếng chặt"), true);
            line.y = variantsRect.yMax + Padding;
            DrawLabeledField(line, property.FindPropertyRelative("chopDebris"), "Vụn khi chặt");
            line.y += EditorGUIUtility.singleLineHeight + Padding;
            DrawLabeledField(line, property.FindPropertyRelative("skipPreparation"), "Không cần sơ chế");

            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }

        // EN: Draws a short label followed by an object field on one line.
        // VI: Vẽ nhãn ngắn và ô chọn object trên cùng một dòng.
        private static void DrawLabeledField(Rect line, SerializedProperty field, string label)
        {
            Rect labelRect = new Rect(line.x, line.y, PreviewSize + Padding + 40f, line.height);
            EditorGUI.LabelField(labelRect, label);
            Rect fieldRect = new Rect(labelRect.xMax, line.y, Mathf.Max(0f, line.width - labelRect.width), line.height);
            EditorGUI.PropertyField(fieldRect, field, GUIContent.none);
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
