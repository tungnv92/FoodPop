using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoodPop
{
    // EN: Builds the larger, longer-lived gold collection trail and wires the open scene.
    // VI: Dựng vệt hạt vàng lớn hơn, sống lâu hơn cho lượt thu và gán vào scene đang mở.
    public static class CleanCollectionEffectBuilder
    {
        private const string Folder = "Assets/HotPot/Prefabs/Effects/Collection";
        private static readonly Color Gold = new Color(1f, 0.85f, 0.48f, 1f);
        private static readonly Color Cream = new Color(1f, 0.97f, 0.82f, 1f);

        // EN: Builds the trail prefab and assigns it to every scene CleanBrush booster.
        // VI: Dựng prefab vệt bay và gán cho mọi booster CleanBrush trong scene.
        [MenuItem("Tools/HotPot/Build Clean Collection Effects")]
        public static void Build()
        {
            FoodLandEffectBuilder.EnsureFolder(Folder);
            Material dots = AutoCompleteEffectBuilder.Additive("CleanCollectionDots", AutoCompleteEffectBuilder.Import("FxSoftCircle.png"));
            Material stars = AutoCompleteEffectBuilder.Additive("CleanCollectionStars", AutoCompleteEffectBuilder.Import("FxTwinkleStar.png"));
            ParticleEffect trail = BuildTrail(dots, stars);
            foreach (CleanBrushBooster settings in Object.FindObjectsByType<CleanBrushBooster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(settings, "Gán hạt sáng thu món");
                settings.collectionTrail = trail;
                PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
                EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
        }

        // EN: Configures a small bounded particle layer with game-time simulation and no automatic playback.
        // VI: Cấu hình lớp particle nhỏ có giới hạn, mô phỏng theo thời gian game và không tự chạy.
        private static ParticleSystem Layer(GameObject root, string name, Material material, bool loop, float lifetime, float minSize, float maxSize)
        {
            GameObject target = root.GetComponent<ParticleSystem>() == null ? root : new GameObject(name);
            if (target != root) target.transform.SetParent(root.transform, false);
            ParticleSystem system = target.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.duration = 0.5f;
            main.loop = loop;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new ParticleSystem.MinMaxGradient(Gold, Cream);
            main.maxParticles = 32;
            main.useUnscaledTime = false;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            FoodLandEffectBuilder.SetFadeOut(system, Cream, 0.8f, 0.12f);
            return system;
        }

        // EN: Builds larger dots and stars with longer world-space lifetimes behind flying food.
        // VI: Dựng chấm sáng và sao lớn hơn, sống lâu hơn trong world phía sau food đang bay.
        private static ParticleEffect BuildTrail(Material dots, Material stars)
        {
            GameObject root = new GameObject("CleanCollectionTrail");
            try
            {
                ParticleSystem main = Layer(root, "Dots", dots, true, 0.5f, 0.07f, 0.15f);
                var emission = main.emission;
                emission.rateOverTime = 24f;
                FoodLandEffectBuilder.SetSizeOverLifetime(main, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                ParticleSystem twinkles = Layer(root, "Stars", stars, true, 0.6f, 0.16f, 0.26f);
                var starEmission = twinkles.emission;
                starEmission.rateOverTime = 8f;
                FoodLandEffectBuilder.SetSizeOverLifetime(twinkles, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                return FoodLandEffectBuilder.SaveEffectPrefab(root, Folder + "/CleanCollectionTrail.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

    }
}
