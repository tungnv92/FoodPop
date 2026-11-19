using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Gives images newly added under Art/Sprites the project's sprite defaults (Single sprite, Full Rect mesh) on their first import only.
    // VI: Gán thiết lập sprite mặc định của project (Single, Full Rect) cho ảnh mới thêm vào Art/Sprites, chỉ ở lần import đầu tiên.
    public sealed class SpriteImportDefaults : AssetPostprocessor
    {
        private const string SpriteFolder = "Assets/HotPot/Art/Sprites/";

        // EN: Applies Sprite type, Single mode and Full Rect mesh to a texture that has no saved import settings yet, leaving every other setting and later manual edits untouched.
        // VI: Đặt kiểu Sprite, chế độ Single và mesh Full Rect cho texture chưa có thiết lập import đã lưu, giữ nguyên mọi thiết lập khác và các chỉnh tay về sau.
        private void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing || !assetPath.StartsWith(SpriteFolder))
                return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
