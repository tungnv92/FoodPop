using System.Collections.Generic;
using FoodPop;
using UnityEditor;
using UnityEngine;

// EN: Imports and registers green tea content without rebuilding existing sushi recipes or level orders.
// VI: Nhập và đăng ký nội dung trà xanh mà không dựng lại công thức sushi hoặc order level hiện có.
internal static class TeaContentSetup
{
    private const string Root = "Assets/HotPot/Art/Sprites/Foods/Drinks";

    // EN: Registers tea leaves, ice and both drinks using artwork in the dedicated Drinks folders.
    // VI: Đăng ký lá trà, đá và hai đồ uống bằng ảnh trong các thư mục Drinks riêng.
    [MenuItem("Tools/HotPot/Setup Green Tea")]
    public static void Setup()
    {
        Sprite leaves = ImportSprite("Raw/tea-leaves.png");
        Sprite ice = ImportSprite("Shared/ice.png");
        Sprite brewed = ImportSprite("Prepared/tea-leaves.png");
        Sprite hot = ImportSprite("Final/green-tea.png");
        Sprite cold = ImportSprite("Final/iced-green-tea.png");
        if (leaves == null || ice == null || brewed == null || hot == null || cold == null)
            throw new System.InvalidOperationException("Thiếu ảnh đồ uống trong " + Root);

        FoodIconConfig icons = AssetDatabase.LoadAssetAtPath<FoodIconConfig>("Assets/HotPot/Config/FoodIconConfig.asset");
        FinalFoodConfig recipes = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>("Assets/HotPot/Config/FinalFoodConfig.asset");
        Undo.RecordObjects(new Object[] { icons, recipes }, "Setup Green Tea");
        RegisterIngredient(icons, FoodType.TeaLeaves, leaves, brewed);
        RegisterIngredient(icons, FoodType.Ice, ice, ice);
        RegisterDrink(recipes, FinalFoodType.GreenTea, "Trà xanh nóng", hot, FoodType.TeaLeaves);
        RegisterDrink(recipes, FinalFoodType.IcedGreenTea, "Trà xanh đá", cold, FoodType.TeaLeaves, FoodType.Ice);
        EditorUtility.SetDirty(icons);
        EditorUtility.SetDirty(recipes);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã đăng ký lá trà, đá, trà xanh nóng và trà xanh đá; giữ nguyên order level.");
    }

    // EN: Imports a transparent drink texture as a single sprite with a consistent two-unit maximum size matching sushi artwork.
    // VI: Nhập ảnh đồ uống nền trong suốt thành sprite đơn với kích thước tối đa hai đơn vị nhất quán với ảnh sushi.
    private static Sprite ImportSprite(string relativePath)
    {
        string path = Root + "/" + relativePath;
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.spritePixelsPerUnit = Mathf.Max(width, height) / 2f;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // EN: Upserts a drink ingredient's raw and prepared icons while retaining existing custom preparation settings.
    // VI: Thêm hoặc cập nhật ảnh thô và đã chế biến của nguyên liệu đồ uống, giữ thiết lập sơ chế tùy chỉnh hiện có.
    private static void RegisterIngredient(FoodIconConfig config, FoodType type, Sprite raw, Sprite prepared)
    {
        FoodIconConfig.FoodIcon entry = config.icons.Find(icon => icon != null && icon.foodType == type);
        if (entry == null)
        {
            entry = new FoodIconConfig.FoodIcon { foodType = type };
            config.icons.Add(entry);
        }
        entry.sprite = raw;
        entry.preparedSprite = prepared;
    }

    // EN: Upserts one drink recipe with one portion per listed ingredient; water and cups are supplied by the game.
    // VI: Thêm hoặc cập nhật công thức đồ uống với một phần mỗi nguyên liệu; nước và cốc có sẵn trong game.
    private static void RegisterDrink(FinalFoodConfig config, FinalFoodType type, string label, Sprite sprite, params FoodType[] ingredients)
    {
        FinalFoodConfig.FinalFoodRecipe recipe = config.GetRecipe(type);
        if (recipe == null)
        {
            recipe = new FinalFoodConfig.FinalFoodRecipe { finalFood = type };
            config.recipes.Add(recipe);
        }
        recipe.displayName = label;
        recipe.sprite = sprite;
        recipe.ingredients = new List<FoodType>(ingredients);
    }
}
