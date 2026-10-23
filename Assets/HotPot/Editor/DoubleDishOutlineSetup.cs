using System.IO;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Builds simple rectangular selection-ring sprites using native geometry and the plate's proportions.
    // VI: Tạo sprite vòng viền chọn chữ nhật bằng hình học thuần và tỷ lệ của đĩa.
    public static class DoubleDishOutlineSetup
    {
        private const string Folder = "Assets/HotPot/Art/Sprites/UI/Dishes";

        // EN: Generates both transparent white outline sprites, preserving existing asset GUIDs when rebuilt.
        // VI: Tạo hai sprite viền trắng nền trong suốt, giữ GUID asset hiện có khi tạo lại.
        [MenuItem("Tools/FoodPop/Rebuild Double Dish Outlines")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(Folder);
            BuildOutline("Horizontal");
            BuildOutline("Vertical");
            AssetDatabase.Refresh();
        }

        // EN: Reads the plate from the shared dish-art folder and builds a uniformly thick ring at its size.
        // VI: Đọc đĩa từ thư mục hình đĩa dùng chung và tạo vòng viền dày đều theo kích thước đĩa.
        private static void BuildOutline(string orientation)
        {
            Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/HotPot/Art/Sprites/UI/Dishes/CeramicPlate{orientation}.png");
            if (plate == null)
                throw new System.InvalidOperationException($"Chưa có sprite đĩa {orientation}.");
            Vector2 plateSize = plate.bounds.size;
            float resolution = 1024f / Mathf.Max(plateSize.x, plateSize.y);
            int width = Mathf.RoundToInt(plateSize.x * resolution);
            int height = Mathf.RoundToInt(plateSize.y * resolution);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Color32[] pixels = new Color32[width * height];
                Vector2 halfSize = new Vector2(width, height) * 0.5f - Vector2.one * 2f;
                float radius = Mathf.Min(width, height) * 0.18f;
                float thickness = Mathf.Min(width, height) * 0.07f;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        Vector2 point = new Vector2(x + 0.5f - width * 0.5f, y + 0.5f - height * 0.5f);
                        float outer = RoundedRectDistance(point, halfSize, radius);
                        float inner = RoundedRectDistance(point, halfSize - Vector2.one * thickness, radius - thickness);
                        float alpha = Mathf.Clamp01(0.5f - outer) * Mathf.Clamp01(0.5f + inner);
                        pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                string path = $"{Folder}/PlateSelectionOutline{orientation}.png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = resolution;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // EN: Computes signed distance to a rounded rectangle for smooth outer and inner ring edges.
        // VI: Tính khoảng cách có dấu tới chữ nhật bo góc để làm mượt mép ngoài và trong của vòng viền.
        private static float RoundedRectDistance(Vector2 point, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfSize + Vector2.one * radius;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }
    }
}
