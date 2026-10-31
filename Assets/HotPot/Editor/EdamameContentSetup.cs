using System.Collections.Generic;
using FoodPop;
using UnityEditor;
using UnityEngine;

// EN: Imports and registers edamame content without rebuilding existing sushi recipes or level orders.
// VI: Nhập và đăng ký nội dung edamame mà không dựng lại công thức sushi hoặc order level hiện có.
internal static class EdamameContentSetup
{
    private const string Root = "Assets/HotPot/Art/Sprites/Foods/Edamame";

    // EN: Registers edamame, a raw salt bag, prepared salt crystals and both side dishes.
    // VI: Đăng ký edamame, túi muối thô, hạt muối đã chuẩn bị và hai món phụ.
    [MenuItem("Tools/HotPot/Setup Edamame")]
    public static void Setup()
    {
        Sprite edamame = ImportSprite("Raw/edamame.png");
        Sprite salt = ImportSprite("Shared/salt.png");
        Sprite saltBag = ImportSprite("Raw/salt.png");
        Sprite boiled = ImportSprite("Prepared/edamame.png");
        Sprite plain = ImportSprite("Final/boiled-edamame.png");
        Sprite salted = ImportSprite("Final/salted-edamame.png");
        if (edamame == null || salt == null || saltBag == null || boiled == null || plain == null || salted == null)
            throw new System.InvalidOperationException("Thiếu ảnh món phụ trong " + Root);

        FoodIconConfig icons = AssetDatabase.LoadAssetAtPath<FoodIconConfig>("Assets/HotPot/Config/FoodIconConfig.asset");
        FinalFoodConfig recipes = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>("Assets/HotPot/Config/FinalFoodConfig.asset");
        Undo.RecordObjects(new Object[] { icons, recipes }, "Setup Edamame");
        RegisterIngredient(icons, FoodType.Edamame, edamame, boiled);
        RegisterIngredient(icons, FoodType.Salt, saltBag, salt);
        RegisterSideDish(recipes, FinalFoodType.BoiledEdamame, "Edamame luộc", plain, FoodType.Edamame);
        RegisterSideDish(recipes, FinalFoodType.SaltedEdamame, "Edamame luộc rắc muối", salted, FoodType.Edamame, FoodType.Salt);
        EditorUtility.SetDirty(icons);
        EditorUtility.SetDirty(recipes);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã đăng ký edamame, muối, edamame luộc và edamame rắc muối; giữ nguyên order level.");
    }

    // EN: Imports a transparent side-dish texture as a single sprite with a consistent two-unit maximum size matching sushi artwork.
    // VI: Nhập ảnh món phụ nền trong suốt thành sprite đơn với kích thước tối đa hai đơn vị nhất quán với ảnh sushi.
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

    // EN: Upserts a side-dish ingredient's raw and prepared icons while retaining existing custom preparation settings.
    // VI: Thêm hoặc cập nhật ảnh thô và đã chế biến của nguyên liệu món phụ, giữ thiết lập sơ chế tùy chỉnh hiện có.
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

    // EN: Upserts one side-dish recipe with one portion per listed ingredient; water and bowls are supplied by the game.
    // VI: Thêm hoặc cập nhật công thức món phụ với một phần mỗi nguyên liệu; nước và bát có sẵn trong game.
    private static void RegisterSideDish(FinalFoodConfig config, FinalFoodType type, string label, Sprite sprite, params FoodType[] ingredients)
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
