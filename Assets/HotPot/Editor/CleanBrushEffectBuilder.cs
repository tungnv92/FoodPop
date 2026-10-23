using UnityEditor;
using UnityEngine;
using B = FoodPop.AutoCompleteEffectBuilder;

namespace FoodPop
{
    // EN: Editor tool that builds the CleanBrush booster particle prefabs: the soap foam trail left behind the broom, the foam puff played when the broom appears/vanishes or picks up a dish, and the bubble pop played when a dish lands back on the board.
    // VI: Công cụ editor dựng các prefab particle của booster CleanBrush: vệt bọt xà phòng phía sau chổi, chùm bọt khi chổi xuất hiện/biến mất hoặc nhặt một món, và bong bóng vỡ khi món rơi về board.
    public static class CleanBrushEffectBuilder
    {
        private const string PrefabFolder = "Assets/HotPot/Prefabs/Effects";
        public const string TrailPrefabPath = PrefabFolder + "/CleanFoamTrail.prefab";
        public const string PuffPrefabPath = PrefabFolder + "/CleanFoamPuff.prefab";
        public const string PopPrefabPath = PrefabFolder + "/CleanBubblePop.prefab";

        private static readonly Color Foam = new Color(1f, 1f, 1f, 1f);
        private static readonly Color Aqua = new Color(0.72f, 0.92f, 1f, 1f);

        // EN: Builds (or rebuilds) the three CleanBrush particle prefabs.
        // VI: Dựng (hoặc dựng lại) ba prefab particle của CleanBrush.
        [MenuItem("Tools/HotPot/Build CleanBrush Effects")]
        public static void Build()
        {
            FoodLandEffectBuilder.EnsureFolder("Assets/HotPot/Art/Materials/Particles");
            FoodLandEffectBuilder.EnsureFolder(PrefabFolder);
            Texture2D foamTexture = B.Import("FxFoam.png");
            Texture2D dropTexture = B.Import("FxSoftCircle.png");
            if (foamTexture == null || dropTexture == null)
                return;
            Material foam = FoodLandEffectBuilder.GetOrCreateMaterial("CleanFoam", foamTexture);
            Material drop = FoodLandEffectBuilder.GetOrCreateMaterial("CleanDrop", dropTexture);
            Material sparkle = B.Additive("CleanSparkle", B.Import("FxTwinkleStar.png"));
            B.SavePrefab("CleanFoamTrail", TrailPrefabPath, root => BuildTrail(root, foam, sparkle));
            B.SavePrefab("CleanFoamPuff", PuffPrefabPath, root => BuildPuff(root, foam, sparkle));
            B.SavePrefab("CleanBubblePop", PopPrefabPath, root => BuildPop(root, drop, sparkle));
            AssetDatabase.SaveAssets();
            Debug.Log($"[CleanBrushEffectBuilder] Đã dựng {TrailPrefabPath}, {PuffPrefabPath}, {PopPrefabPath}.");
        }

        // EN: Looping world-space trail that follows the broom: foam blobs dropped by distance travelled that float up a little, grow and fade, plus a few twinkles.
        // VI: Vệt lặp lại trong không gian world đi theo chổi: bọt rơi lại theo quãng đường di chuyển, nổi lên nhẹ, lớn dần rồi mờ đi, kèm vài tia lấp lánh.
        private static void BuildTrail(GameObject root, Material foam, Material sparkle)
        {
            ParticleSystem bubbles = root.AddComponent<ParticleSystem>();
            B.FlightTrail(bubbles, 22f, new ParticleSystem.MinMaxCurve(0.08f, 0.26f), new ParticleSystem.MinMaxGradient(Foam, Aqua));
            ParticleSystem.MainModule main = bubbles.main;
            main.gravityModifier = -0.15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            FoodLandEffectBuilder.SetSizeOverLifetime(bubbles, B.Curve(0.6f, 1f, 0.7f));
            B.Fade(bubbles, Color.white, 0.9f, 0.1f, 0.55f);
            B.Render(bubbles, foam, 4);

            ParticleSystem twinkles = B.Layer(root, "Twinkles", false);
            B.FlightTrail(twinkles, 4f, new ParticleSystem.MinMaxCurve(0.12f, 0.22f), new ParticleSystem.MinMaxGradient(Color.white, Aqua));
            B.Spin(twinkles, 60f, 160f);
            B.Twinkle(twinkles);
            B.Render(twinkles, sparkle, 0);
        }

        // EN: One-shot foam puff: a cloud of foam blobs bursting out and floating up, with a few twinkles.
        // VI: Chùm bọt một lần: đám bọt bung ra rồi nổi lên, kèm vài tia lấp lánh.
        private static void BuildPuff(GameObject root, Material foam, Material sparkle)
        {
            ParticleSystem bubbles = root.AddComponent<ParticleSystem>();
            B.Spray(bubbles, 14, 0.8f, 2.4f, new ParticleSystem.MinMaxCurve(0.1f, 0.32f), -0.2f);
            ParticleSystem.MainModule main = bubbles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Foam, Aqua);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            FoodLandEffectBuilder.SetSizeOverLifetime(bubbles, B.Curve(0.5f, 1f, 0.8f));
            B.Fade(bubbles, Color.white, 0.95f, 0.05f, 0.6f);
            B.Render(bubbles, foam, 4);

            ParticleSystem twinkles = B.Layer(root, "Twinkles", false);
            B.Spray(twinkles, 6, 1.2f, 2.8f, new ParticleSystem.MinMaxCurve(0.14f, 0.26f), 0.2f);
            B.Spin(twinkles, -200f, 200f);
            B.Twinkle(twinkles);
            B.Render(twinkles, sparkle, 0);
        }

        // EN: One-shot bubble pop: water droplets flung outward and falling, plus a couple of twinkles.
        // VI: Bong bóng vỡ một lần: giọt nước văng ra rồi rơi xuống, kèm vài tia lấp lánh.
        private static void BuildPop(GameObject root, Material drop, Material sparkle)
        {
            ParticleSystem drops = root.AddComponent<ParticleSystem>();
            B.Spray(drops, 12, 1.8f, 3.6f, new ParticleSystem.MinMaxCurve(0.05f, 0.11f), 0.9f);
            ParticleSystem.MainModule main = drops.main;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Aqua);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            B.Fade(drops, Color.white, 1f, 0f, 0.6f);
            B.Render(drops, drop, 2);

            ParticleSystem twinkles = B.Layer(root, "Twinkles", false);
            B.Spray(twinkles, 4, 1f, 2.2f, new ParticleSystem.MinMaxCurve(0.14f, 0.24f), 0f);
            B.Spin(twinkles, -200f, 200f);
            B.Twinkle(twinkles);
            B.Render(twinkles, sparkle, 0);
        }
    }
}
