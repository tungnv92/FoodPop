using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Editor tool for the designed ingredient layouts in Assets/HotPot/Prefabs/OrderView: adds IngredientPreviewView to every "OrderPreview_{set}_{count}" prefab (collecting its "Card" and "Ingredient…" children), then assigns them as templates in the ingredient form configs. Set 1 = inside a column (1/2/3-dish cards and the collapsed view); set 2 = the first collapse phase (other dishes hidden), falling back to set 1 when a count has no set-2 prefab.
    // VI: Công cụ editor cho các layout nguyên liệu thiết kế sẵn trong Assets/HotPot/Prefabs/OrderView: thêm IngredientPreviewView cho mọi prefab "OrderPreview_{bộ}_{số}" (gom con "Card" và "Ingredient…"), rồi gán làm template trong các bộ mẫu nguyên liệu. Bộ 1 = trong cột (thẻ 1/2/3 món và trạng thái đã thu); bộ 2 = giai đoạn đầu của collapse (các món khác ẩn), số nào không có prefab bộ 2 thì dùng bộ 1.
    public static class IngredientPreviewSetup
    {
        public const string Folder = "Assets/HotPot/Prefabs/OrderView";
        private static readonly Regex NamePattern = new Regex(@"^OrderPreview_(\d+)_(\d+)$");

        // EN: Runs both steps.
        // VI: Chạy cả hai bước.
        [MenuItem("Tools/HotPot/Setup Ingredient Previews")]
        public static void Run()
        {
            Dictionary<(int set, int count), IngredientPreviewView> previews = PreparePrefabs();
            int assigned = AssignTemplates(previews);
            Debug.Log($"[IngredientPreviewSetup] {previews.Count} layout nguyên liệu; đã gán {assigned} template vào bộ mẫu.");
        }

        // EN: Adds/refreshes IngredientPreviewView on every OrderPreview prefab and returns them by (set, ingredient count).
        // VI: Thêm/cập nhật IngredientPreviewView cho mọi prefab OrderPreview và trả về theo (bộ, số nguyên liệu).
        private static Dictionary<(int, int), IngredientPreviewView> PreparePrefabs()
        {
            Dictionary<(int, int), IngredientPreviewView> result = new Dictionary<(int, int), IngredientPreviewView>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Match match = NamePattern.Match(name);
                if (!match.Success)
                    continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    IngredientPreviewView view = root.GetComponent<IngredientPreviewView>();
                    if (view == null)
                        view = root.AddComponent<IngredientPreviewView>();
                    view.Collect();
                    EditorUtility.SetDirty(view);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                IngredientPreviewView asset = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<IngredientPreviewView>();
                result[(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))] = asset;
            }
            return result;
        }

        // EN: Fills empty template fields of the form configs: the 1-dish config uses set 1; the 2/3-dish configs use set 2 for collapse forms (else set 1) and set 1 for the others.
        // VI: Điền trường template còn trống trong các bộ mẫu: bộ mẫu 1 món dùng bộ 1; bộ mẫu 2/3 món dùng bộ 2 cho mẫu bật collapse (không có thì bộ 1) và bộ 1 cho mẫu còn lại.
        private static int AssignTemplates(Dictionary<(int, int), IngredientPreviewView> previews)
        {
            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:IngredientGridFormConfig"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                IngredientGridFormConfig config = AssetDatabase.LoadAssetAtPath<IngredientGridFormConfig>(path);
                if (config == null || config.forms == null)
                    continue;
                bool single = path.Contains("1Dish");
                bool changed = false;
                for (int i = 0; i < config.forms.Length; i++)
                {
                    IngredientGridForm form = config.forms[i];
                    if (form.template != null)
                        continue;
                    int set = !single && form.collapseLayout ? 2 : 1;
                    if (!previews.TryGetValue((set, form.ingredientCount), out IngredientPreviewView template))
                        previews.TryGetValue((1, form.ingredientCount), out template);
                    if (template == null)
                        continue;
                    form.template = template;
                    config.forms[i] = form;
                    changed = true;
                    assigned++;
                }
                if (changed)
                {
                    EditorUtility.SetDirty(config);
                }
            }
            AssetDatabase.SaveAssets();
            return assigned;
        }
    }
}
