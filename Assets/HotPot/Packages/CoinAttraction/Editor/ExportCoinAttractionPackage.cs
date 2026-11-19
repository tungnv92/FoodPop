using UnityEditor;
using System.Linq;

namespace FoodPop.CoinAttractionTools
{
    public static class ExportCoinAttractionPackage
    {
        private const string PackageRoot = "Assets/HotPot/Packages/CoinAttraction";

        // EN: Includes the effect and complete ParticleImage runtime/editor assets, excluding installed Unity package sources and unrelated game code.
        // VI: Kèm hiệu ứng và đủ asset runtime/editor ParticleImage, loại mã nguồn package Unity đã cài và code game không liên quan.
        public static void Export(string destination)
        {
            string[] roots =
            {
                PackageRoot,
                "Assets/AssetKits/ParticleImage/Runtime",
                "Assets/AssetKits/ParticleImage/Editor"
            };
            string[] assets = AssetDatabase.FindAssets("", roots)
                .Select(AssetDatabase.GUIDToAssetPath).Concat(roots).ToArray();
            string[] dependencies = AssetDatabase.GetDependencies(assets, true)
                .Where(path => path.StartsWith("Assets/")).Distinct().ToArray();
            AssetDatabase.ExportPackage(dependencies, destination, ExportPackageOptions.Default);
            UnityEngine.Debug.Log("Exported Coin Attraction: " + destination);
        }

        // EN: Exports the reusable coin effect and ParticleImage dependencies to a user-selected Unity package.
        // VI: Xuất hiệu ứng xu tái sử dụng cùng phụ thuộc ParticleImage thành Unity package tại nơi người dùng chọn.
        [MenuItem("Tools/Coin Attraction/Export Unity Package")]
        private static void ExportFromMenu()
        {
            string destination = EditorUtility.SaveFilePanel("Export Coin Attraction", "", "CoinAttraction", "unitypackage");
            if (string.IsNullOrEmpty(destination)) return;
            Export(destination);
        }

    }
}
