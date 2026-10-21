using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Table of final dishes customers can order: each entry lists the 2-6 distinct ingredients that make the dish and its sprite.
    // VI: Bảng các món hoàn chỉnh (final food) khách có thể order: mỗi dòng liệt kê 2-6 nguyên liệu khác nhau tạo nên món và ảnh của món.
    [CreateAssetMenu(fileName = "FinalFoodConfig", menuName = "Hot Pot/Final Food Config")]
    public class FinalFoodConfig : ScriptableObject
    {
        public const int MinIngredients = 2;
        public const int MaxIngredients = 6;

        // EN: One final dish: its ID, display name, sprite and the ingredients needed (one of each).
        // VI: Một món hoàn chỉnh: ID, tên hiển thị, ảnh và các nguyên liệu cần (mỗi loại một).
        [Serializable]
        public class FinalFoodRecipe
        {
            [Tooltip("Loại món hoàn chỉnh.")]
            public FinalFoodType finalFood;
            [Tooltip("Tên hiển thị của món.")]
            public string displayName;
            [Tooltip("Ảnh món hoàn chỉnh hiện trên thẻ order.")]
            public Sprite sprite;
            [Tooltip("Các nguyên liệu tạo nên món (2 đến 6 loại, mỗi loại một).")]
            public List<FoodType> ingredients = new List<FoodType>();

            // EN: Number of ingredients in the recipe.
            // VI: Số nguyên liệu trong công thức.
            public int IngredientCount => ingredients != null ? ingredients.Count : 0;
        }

        [Tooltip("Mỗi món hoàn chỉnh một dòng; dùng menu ⋮ \"Tự điền công thức mặc định\" để điền lại từ dữ liệu sushi hiện có.")]
        public List<FinalFoodRecipe> recipes = new List<FinalFoodRecipe>();

        // EN: Returns the recipe of a final dish, or null when it is not in the table.
        // VI: Trả về công thức của món hoàn chỉnh, hoặc null nếu bảng chưa có.
        public FinalFoodRecipe GetRecipe(FinalFoodType type)
        {
            if (type == FinalFoodType.None)
                return null;
            foreach (FinalFoodRecipe recipe in recipes)
            {
                if (recipe != null && recipe.finalFood == type)
                    return recipe;
            }
            return null;
        }

        // EN: Returns the ingredients of a final dish (empty when unknown).
        // VI: Trả về danh sách nguyên liệu của món hoàn chỉnh (rỗng nếu không có).
        public IReadOnlyList<FoodType> GetIngredients(FinalFoodType type)
        {
            FinalFoodRecipe recipe = GetRecipe(type);
            return recipe != null && recipe.ingredients != null ? recipe.ingredients : (IReadOnlyList<FoodType>)Array.Empty<FoodType>();
        }

        // EN: Returns the sprite of a final dish, or null when missing.
        // VI: Trả về ảnh của món hoàn chỉnh, hoặc null nếu chưa có.
        public Sprite GetSprite(FinalFoodType type)
        {
            FinalFoodRecipe recipe = GetRecipe(type);
            return recipe != null ? recipe.sprite : null;
        }

        // EN: Collects every recipe that uses exactly the given number of ingredients (2-6).
        // VI: Lấy mọi công thức có đúng số nguyên liệu cho trước (2-6).
        public List<FinalFoodRecipe> GetRecipesWithIngredientCount(int count)
        {
            List<FinalFoodRecipe> result = new List<FinalFoodRecipe>();
            foreach (FinalFoodRecipe recipe in recipes)
            {
                if (recipe != null && recipe.IngredientCount == count)
                    result.Add(recipe);
            }
            return result;
        }

#if UNITY_EDITOR
        private const string FinalSpriteFolder = "Assets/HotPot/Art/Sprites/Foods/Sushi/Final";

        // EN: Built-in recipe data matching the sprites in Art/Sprites/Foods/Sushi/Final/<count>/<slug>-nigiri.png.
        // VI: Dữ liệu công thức có sẵn, khớp với ảnh trong Art/Sprites/Foods/Sushi/Final/<số món>/<slug>-nigiri.png.
        private readonly struct DefaultRecipe
        {
            public readonly FinalFoodType type;
            public readonly string displayName;
            public readonly string slug;
            public readonly int tier;
            public readonly FoodType[] ingredients;

            // EN: Stores one built-in recipe.
            // VI: Lưu một công thức có sẵn.
            public DefaultRecipe(FinalFoodType type, string displayName, string slug, int tier, params FoodType[] ingredients)
            {
                this.type = type;
                this.displayName = displayName;
                this.slug = slug;
                this.tier = tier;
                this.ingredients = ingredients;
            }
        }

        private static readonly DefaultRecipe[] Defaults =
        {
            new DefaultRecipe(FinalFoodType.SakeNigiri2, "Nigiri cá hồi (2 món)", "sake", 2, FoodType.SushiRice, FoodType.Salmon),
            new DefaultRecipe(FinalFoodType.SakeNigiri3, "Nigiri cá hồi (3 món)", "sake", 3, FoodType.SushiRice, FoodType.Salmon, FoodType.SalmonRoe),
            new DefaultRecipe(FinalFoodType.SakeNigiri4, "Nigiri cá hồi (4 món)", "sake", 4, FoodType.SushiRice, FoodType.Salmon, FoodType.SalmonRoe, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.SakeNigiri5, "Nigiri cá hồi (5 món)", "sake", 5, FoodType.SushiRice, FoodType.Salmon, FoodType.SalmonRoe, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.SakeNigiri6, "Nigiri cá hồi (6 món)", "sake", 6, FoodType.SushiRice, FoodType.Salmon, FoodType.SalmonRoe, FoodType.Seaweed, FoodType.Cucumber, FoodType.Wasabi),
            new DefaultRecipe(FinalFoodType.MaguroNigiri2, "Nigiri cá ngừ (2 món)", "maguro", 2, FoodType.SushiRice, FoodType.Tuna),
            new DefaultRecipe(FinalFoodType.MaguroNigiri3, "Nigiri cá ngừ (3 món)", "maguro", 3, FoodType.SushiRice, FoodType.Tuna, FoodType.Wasabi),
            new DefaultRecipe(FinalFoodType.MaguroNigiri4, "Nigiri cá ngừ (4 món)", "maguro", 4, FoodType.SushiRice, FoodType.Tuna, FoodType.Wasabi, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.MaguroNigiri5, "Nigiri cá ngừ (5 món)", "maguro", 5, FoodType.SushiRice, FoodType.Tuna, FoodType.Wasabi, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.MaguroNigiri6, "Nigiri cá ngừ (6 món)", "maguro", 6, FoodType.SushiRice, FoodType.Tuna, FoodType.Wasabi, FoodType.Seaweed, FoodType.Cucumber, FoodType.SoySauce),
            new DefaultRecipe(FinalFoodType.EbiNigiri2, "Nigiri tôm (2 món)", "ebi", 2, FoodType.SushiRice, FoodType.Shrimp),
            new DefaultRecipe(FinalFoodType.EbiNigiri3, "Nigiri tôm (3 món)", "ebi", 3, FoodType.SushiRice, FoodType.Shrimp, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.EbiNigiri4, "Nigiri tôm (4 món)", "ebi", 4, FoodType.SushiRice, FoodType.Shrimp, FoodType.Cucumber, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.EbiNigiri5, "Nigiri tôm (5 món)", "ebi", 5, FoodType.SushiRice, FoodType.Shrimp, FoodType.Cucumber, FoodType.Seaweed, FoodType.SalmonRoe),
            new DefaultRecipe(FinalFoodType.EbiNigiri6, "Nigiri tôm (6 món)", "ebi", 6, FoodType.SushiRice, FoodType.Shrimp, FoodType.Cucumber, FoodType.Seaweed, FoodType.SalmonRoe, FoodType.SoySauce),
            new DefaultRecipe(FinalFoodType.TamagoNigiri2, "Nigiri trứng cuộn (2 món)", "tamago", 2, FoodType.SushiRice, FoodType.Tamagoyaki),
            new DefaultRecipe(FinalFoodType.TamagoNigiri3, "Nigiri trứng cuộn (3 món)", "tamago", 3, FoodType.SushiRice, FoodType.Tamagoyaki, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.TamagoNigiri4, "Nigiri trứng cuộn (4 món)", "tamago", 4, FoodType.SushiRice, FoodType.Tamagoyaki, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.TamagoNigiri5, "Nigiri trứng cuộn (5 món)", "tamago", 5, FoodType.SushiRice, FoodType.Tamagoyaki, FoodType.Seaweed, FoodType.Cucumber, FoodType.CrabStick),
            new DefaultRecipe(FinalFoodType.TamagoNigiri6, "Nigiri trứng cuộn (6 món)", "tamago", 6, FoodType.SushiRice, FoodType.Tamagoyaki, FoodType.Seaweed, FoodType.Cucumber, FoodType.CrabStick, FoodType.PickledGinger),
            new DefaultRecipe(FinalFoodType.UnagiNigiri2, "Nigiri lươn (2 món)", "unagi", 2, FoodType.SushiRice, FoodType.Unagi),
            new DefaultRecipe(FinalFoodType.UnagiNigiri3, "Nigiri lươn (3 món)", "unagi", 3, FoodType.SushiRice, FoodType.Unagi, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.UnagiNigiri4, "Nigiri lươn (4 món)", "unagi", 4, FoodType.SushiRice, FoodType.Unagi, FoodType.Cucumber, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.UnagiNigiri5, "Nigiri lươn (5 món)", "unagi", 5, FoodType.SushiRice, FoodType.Unagi, FoodType.Cucumber, FoodType.Seaweed, FoodType.Tamagoyaki),
            new DefaultRecipe(FinalFoodType.UnagiNigiri6, "Nigiri lươn (6 món)", "unagi", 6, FoodType.SushiRice, FoodType.Unagi, FoodType.Cucumber, FoodType.Seaweed, FoodType.Tamagoyaki, FoodType.SoySauce),
            new DefaultRecipe(FinalFoodType.TakoNigiri2, "Nigiri bạch tuộc (2 món)", "tako", 2, FoodType.SushiRice, FoodType.Octopus),
            new DefaultRecipe(FinalFoodType.TakoNigiri3, "Nigiri bạch tuộc (3 món)", "tako", 3, FoodType.SushiRice, FoodType.Octopus, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.TakoNigiri4, "Nigiri bạch tuộc (4 món)", "tako", 4, FoodType.SushiRice, FoodType.Octopus, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.TakoNigiri5, "Nigiri bạch tuộc (5 món)", "tako", 5, FoodType.SushiRice, FoodType.Octopus, FoodType.Seaweed, FoodType.Cucumber, FoodType.SalmonRoe),
            new DefaultRecipe(FinalFoodType.TakoNigiri6, "Nigiri bạch tuộc (6 món)", "tako", 6, FoodType.SushiRice, FoodType.Octopus, FoodType.Seaweed, FoodType.Cucumber, FoodType.SalmonRoe, FoodType.Wasabi),
            new DefaultRecipe(FinalFoodType.HotateNigiri2, "Nigiri sò điệp (2 món)", "hotate", 2, FoodType.SushiRice, FoodType.Scallop),
            new DefaultRecipe(FinalFoodType.HotateNigiri3, "Nigiri sò điệp (3 món)", "hotate", 3, FoodType.SushiRice, FoodType.Scallop, FoodType.SalmonRoe),
            new DefaultRecipe(FinalFoodType.HotateNigiri4, "Nigiri sò điệp (4 món)", "hotate", 4, FoodType.SushiRice, FoodType.Scallop, FoodType.SalmonRoe, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.HotateNigiri5, "Nigiri sò điệp (5 món)", "hotate", 5, FoodType.SushiRice, FoodType.Scallop, FoodType.SalmonRoe, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.HotateNigiri6, "Nigiri sò điệp (6 món)", "hotate", 6, FoodType.SushiRice, FoodType.Scallop, FoodType.SalmonRoe, FoodType.Seaweed, FoodType.Cucumber, FoodType.Wasabi),
            new DefaultRecipe(FinalFoodType.KanikamaNigiri2, "Nigiri thanh cua (2 món)", "kanikama", 2, FoodType.SushiRice, FoodType.CrabStick),
            new DefaultRecipe(FinalFoodType.KanikamaNigiri3, "Nigiri thanh cua (3 món)", "kanikama", 3, FoodType.SushiRice, FoodType.CrabStick, FoodType.Seaweed),
            new DefaultRecipe(FinalFoodType.KanikamaNigiri4, "Nigiri thanh cua (4 món)", "kanikama", 4, FoodType.SushiRice, FoodType.CrabStick, FoodType.Seaweed, FoodType.Cucumber),
            new DefaultRecipe(FinalFoodType.KanikamaNigiri5, "Nigiri thanh cua (5 món)", "kanikama", 5, FoodType.SushiRice, FoodType.CrabStick, FoodType.Seaweed, FoodType.Cucumber, FoodType.Tamagoyaki),
            new DefaultRecipe(FinalFoodType.KanikamaNigiri6, "Nigiri thanh cua (6 món)", "kanikama", 6, FoodType.SushiRice, FoodType.CrabStick, FoodType.Seaweed, FoodType.Cucumber, FoodType.Tamagoyaki, FoodType.SoySauce),
        };

        // EN: Rebuilds the table from the built-in sushi recipes and loads each dish sprite from the Final folder; keeps sprites already assigned.
        // VI: Dựng lại bảng từ công thức sushi có sẵn và nạp ảnh từng món từ thư mục Final; giữ ảnh đã gán sẵn.
        [ContextMenu("Tự điền công thức mặc định")]
        public void FillDefaults()
        {
            UnityEditor.Undo.RecordObject(this, "Fill Final Food Recipes");
            foreach (DefaultRecipe data in Defaults)
            {
                FinalFoodRecipe recipe = GetRecipe(data.type);
                if (recipe == null)
                {
                    recipe = new FinalFoodRecipe { finalFood = data.type };
                    recipes.Add(recipe);
                }
                recipe.displayName = data.displayName;
                recipe.ingredients = new List<FoodType>(data.ingredients);
                if (recipe.sprite == null)
                    recipe.sprite = LoadFinalSprite(data.tier, data.slug);
            }
            recipes.Sort((a, b) => ((int)a.finalFood).CompareTo((int)b.finalFood));
            UnityEditor.EditorUtility.SetDirty(this);
        }

        // EN: Loads the sprite of a final dish from Final/<tier>/<slug>-nigiri.png, accepting textures imported with several sprites.
        // VI: Nạp ảnh món hoàn chỉnh từ Final/<số món>/<slug>-nigiri.png, chấp nhận cả texture được import thành nhiều sprite.
        private static Sprite LoadFinalSprite(int tier, string slug)
        {
            string path = $"{FinalSpriteFolder}/{tier}/{slug}-nigiri.png";
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
            foreach (UnityEngine.Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite found)
                    return found;
            }
            Debug.LogWarning($"[FinalFoodConfig] Không tìm thấy sprite tại {path}.");
            return null;
        }

        // EN: Warns about recipes with a missing type, an ingredient count outside 2-6, empty or repeated ingredients, or a duplicated final dish.
        // VI: Cảnh báo công thức thiếu loại món, số nguyên liệu ngoài khoảng 2-6, nguyên liệu trống hoặc trùng, hoặc món hoàn chỉnh bị lặp.
        private void OnValidate()
        {
            HashSet<FinalFoodType> seen = new HashSet<FinalFoodType>();
            foreach (FinalFoodRecipe recipe in recipes)
            {
                if (recipe == null)
                    continue;
                string label = recipe.finalFood.ToString();
                if (recipe.finalFood == FinalFoodType.None)
                    Debug.LogWarning("[FinalFoodConfig] Có dòng chưa chọn loại món hoàn chỉnh.", this);
                else if (!seen.Add(recipe.finalFood))
                    Debug.LogWarning($"[FinalFoodConfig] {label} bị lặp.", this);
                if (recipe.IngredientCount < MinIngredients || recipe.IngredientCount > MaxIngredients)
                    Debug.LogWarning($"[FinalFoodConfig] {label} có {recipe.IngredientCount} nguyên liệu; cần từ {MinIngredients} đến {MaxIngredients}.", this);
                if (recipe.ingredients == null)
                    continue;
                HashSet<FoodType> used = new HashSet<FoodType>();
                foreach (FoodType ingredient in recipe.ingredients)
                {
                    if (ingredient == FoodType.None)
                        Debug.LogWarning($"[FinalFoodConfig] {label} có nguyên liệu trống.", this);
                    else if (!used.Add(ingredient))
                        Debug.LogWarning($"[FinalFoodConfig] {label} có nguyên liệu {ingredient} bị lặp.", this);
                }
            }
        }
#endif
    }
}
