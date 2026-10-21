using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Editor menu that creates or refreshes Config/FinalFoodConfig.asset from the built-in sushi recipes and Final sprites.
    // VI: Menu editor tạo hoặc làm mới Config/FinalFoodConfig.asset từ công thức sushi có sẵn và ảnh trong thư mục Final.
    public static class FinalFoodConfigMenu
    {
        private const string AssetPath = "Assets/HotPot/Config/FinalFoodConfig.asset";

        // EN: Loads the config (creating it when missing), fills recipes and sprites, saves and selects it.
        // VI: Nạp config (tạo mới nếu chưa có), điền công thức và ảnh, lưu và chọn asset.
        [MenuItem("Tools/HotPot/Build Final Food Config")]
        public static void Build()
        {
            FinalFoodConfig config = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>(AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FinalFoodConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }
            config.FillDefaults();
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log($"[FinalFoodConfig] Đã điền {config.recipes.Count} món hoàn chỉnh.", config);
        }
    }
}
