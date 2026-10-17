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
            [Tooltip("Nhóm sơ chế (tiếng dao và vụn khi cắt); Auto = dùng mặc định theo loại nguyên liệu.")]
            public FoodCategory category = FoodCategory.Auto;
            [Tooltip("Âm thanh dao chặt riêng của nguyên liệu này; để trống sẽ dùng âm thanh theo nhóm.")]
            public AudioClip chopSound;
            [Tooltip("Các biến thể tiếng chặt thêm (tùy chọn); mỗi nhát chọn ngẫu nhiên giữa tiếng chính và các biến thể để đỡ lặp lại.")]
            public AudioClip[] chopSoundVariants;
            [Tooltip("Prefab vụn riêng của nguyên liệu này bắn ra mỗi nhát chặt; để trống sẽ dùng vụn theo nhóm.")]
            public ParticleSystem chopDebris;
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

        // EN: Returns the category set for a food type in this table, or Auto when none is set.
        // VI: Trả về nhóm sơ chế đã đặt cho loại nguyên liệu trong bảng, hoặc Auto nếu chưa đặt.
        public FoodCategory GetCategoryOverride(FoodType type)
        {
            foreach (FoodIcon icon in icons)
            {
                if (icon != null && icon.foodType == type)
                    return icon.category;
            }
            return FoodCategory.Auto;
        }

        // EN: Returns a chop sound for a food type, picked at random among its main sound and variants; null when it should fall back to its group's sound.
        // VI: Trả về một tiếng chặt cho loại nguyên liệu, chọn ngẫu nhiên giữa tiếng chính và các biến thể; null khi cần dùng âm thanh theo nhóm.
        public AudioClip GetChopSound(FoodType type)
        {
            FoodIcon entry = Find(type);
            if (entry == null)
                return null;
            int variants = entry.chopSoundVariants != null ? entry.chopSoundVariants.Length : 0;
            int pick = UnityEngine.Random.Range(0, variants + 1);
            AudioClip clip = pick == 0 ? entry.chopSound : entry.chopSoundVariants[pick - 1];
            return clip != null ? clip : entry.chopSound;
        }

        // EN: Returns the chop debris prefab set for a food type, or null when it should fall back to its group's debris.
        // VI: Trả về prefab vụn riêng của loại nguyên liệu, hoặc null khi cần dùng vụn theo nhóm.
        public ParticleSystem GetChopDebris(FoodType type)
        {
            FoodIcon entry = Find(type);
            return entry != null ? entry.chopDebris : null;
        }

        // EN: Returns the table entry for a food type, or null when it is missing.
        // VI: Trả về dòng của loại nguyên liệu trong bảng, hoặc null nếu chưa có.
        private FoodIcon Find(FoodType type)
        {
            foreach (FoodIcon icon in icons)
            {
                if (icon != null && icon.foodType == type)
                    return icon;
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
