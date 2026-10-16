using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Central table mapping every FoodType to its icon sprite; shared by gameplay and tools.
    // VI: Bảng trung tâm ánh xạ mỗi FoodType với sprite icon của nó; dùng chung cho gameplay và công cụ.
    [CreateAssetMenu(fileName = "FoodIconConfig", menuName = "Hot Pot/Food Icon Config")]
    public class FoodIconConfig : ScriptableObject
    {
        // EN: One entry: a food type and the sprite shown for it.
        // VI: Một dòng: loại nguyên liệu và sprite hiển thị cho nó.
        [Serializable]
        public class FoodIcon
        {
            public FoodType foodType;
            public Sprite sprite;
            public Sprite preparedSprite;
        }

        [Tooltip("Mỗi loại nguyên liệu một dòng; dùng nút \"Tự điền từ Art/Sprites/Foods\" trong menu ⋮ để điền nhanh.")]
        public List<FoodIcon> icons = new List<FoodIcon>();

        // EN: Returns the icon for the requested processing state, falling back to the raw icon if needed.
        // VI: Trả về icon theo trạng thái chế biến, dùng ảnh thô nếu chưa có ảnh sơ chế.
        public Sprite GetIcon(FoodType type, FoodState state = FoodState.Raw)
        {
            if (type == FoodType.None)
                return null;
            foreach (FoodIcon icon in icons)
            {
                if (icon != null && icon.foodType == type)
                    return state == FoodState.Prepared && icon.preparedSprite != null ? icon.preparedSprite : icon.sprite;
            }
            return null;
        }

#if UNITY_EDITOR
        // EN: Fills both sushi processing-state sprites while preserving existing valid assignments.
        // VI: Điền sprite sushi cho hai trạng thái và giữ các ảnh hợp lệ đã gán.
        [ContextMenu("Tự điền từ Art/Sprites/Foods")]
        private void FillFromFoodsFolder()
        {
            UnityEditor.Undo.RecordObject(this, "Fill Food Icons");
            foreach (FoodType type in Enum.GetValues(typeof(FoodType)))
            {
                if (type == FoodType.None)
                    continue;
                string file = System.Text.RegularExpressions.Regex.Replace(type.ToString(), "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
                FoodIcon entry = icons.Find(icon => icon != null && icon.foodType == type);
                if (entry == null)
                {
                    entry = new FoodIcon { foodType = type };
                    icons.Add(entry);
                }
                if (entry.sprite == null)
                    entry.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/HotPot/Art/Sprites/Foods/Sushi/Raw/{file}.png");
                if (entry.preparedSprite == null)
                    entry.preparedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/HotPot/Art/Sprites/Foods/Sushi/Prepared/{file}.png");
            }
            icons.Sort((a, b) => ((int)a.foodType).CompareTo((int)b.foodType));
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
