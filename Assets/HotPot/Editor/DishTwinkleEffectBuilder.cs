using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Editor tool that builds the twinkling-star particle prefab, nests it in the DishSlot prefab (wired through DishSlotView) and links that prefab to the serving tray.
    // VI: Công cụ editor dựng prefab particle sao lấp lánh, lồng vào prefab DishSlot (nối qua DishSlotView) và nối prefab đó với khay phục vụ.
    public static class DishTwinkleEffectBuilder
    {
        private const string StarTexturePath = "Assets/HotPot/Art/Sprites/Effects/FxTwinkleStar.png";
        private const string PrefabFolder = "Assets/HotPot/Prefabs/Effects";
        private const string PrefabPath = PrefabFolder + "/DishTwinkle.prefab";
        private const string DishSlotPrefabPath = "Assets/HotPot/Prefabs/Ingame/DishSlot.prefab";
        private const string ServingTrayPrefabPath = "Assets/HotPot/Prefabs/Ingame/ServingHotPot.prefab";

        // EN: Builds (or rebuilds) the twinkle prefab, nests it in the dish slot prefab and links the slot prefab to the serving tray.
        // VI: Dựng (hoặc dựng lại) prefab lấp lánh, lồng vào prefab ô món và nối prefab ô với khay phục vụ.
        [MenuItem("Tools/HotPot/Build Dish Twinkle Effect")]
        public static void Build()
        {
            Texture2D texture = FoodLandEffectBuilder.ImportEffectTexture(StarTexturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[DishTwinkleEffectBuilder] Thiếu {StarTexturePath}.");
                return;
            }
            Material material = FoodLandEffectBuilder.GetOrCreateMaterial("DishTwinkleStar", texture);
            GameObject root = new GameObject("DishTwinkle");
            ParticleEffect prefab;
            try
            {
                Configure(root.AddComponent<ParticleSystem>(), material);
                FoodLandEffectBuilder.EnsureFolder(PrefabFolder);
                prefab = FoodLandEffectBuilder.SaveEffectPrefab(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            const string obsoleteRuntime = "Assets/HotPot/Scripts/Customer/DishTwinkle.cs";
            if (AssetDatabase.LoadMainAssetAtPath(obsoleteRuntime) != null)
                AssetDatabase.MoveAssetToTrash(obsoleteRuntime);
            DishSlotView slotView = InstallInDishSlot(prefab);
            LinkServingTray(slotView);
            Debug.Log($"[DishTwinkleEffectBuilder] Đã dựng {PrefabPath}, lồng vào {DishSlotPrefabPath} và nối với {ServingTrayPrefabPath}.");
        }

        // EN: Adds DishSlotView to the DishSlot prefab root and nests one DishTwinkle instance (named "Twinkle") at the slot centre, replacing an older one.
        // VI: Thêm DishSlotView vào gốc prefab DishSlot và lồng một bản DishTwinkle (tên "Twinkle") tại tâm ô, thay thế bản cũ nếu có.
        private static DishSlotView InstallInDishSlot(ParticleEffect twinklePrefab)
        {
            if (twinklePrefab == null)
                return null;
            GameObject root = PrefabUtility.LoadPrefabContents(DishSlotPrefabPath);
            try
            {
                Transform old = root.transform.Find("Twinkle");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(twinklePrefab.gameObject, root.transform);
                instance.name = "Twinkle";
                instance.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                instance.layer = root.layer;
                DishSlotView view = root.GetComponent<DishSlotView>();
                if (view == null)
                    view = root.AddComponent<DishSlotView>();
                view.SetTwinkle(instance.GetComponent<ParticleEffect>());
                AssignOutline(root, view);
                PrefabUtility.SaveAsPrefabAsset(root, DishSlotPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<DishSlotView>(DishSlotPrefabPath);
        }

        // EN: Editor-time wiring: stores the "Slot Outline" SpriteRenderer on DishSlotView so runtime code uses the reference instead of searching children.
        // VI: Nối tham chiếu lúc dựng trong editor: lưu SpriteRenderer "Slot Outline" vào DishSlotView để code runtime dùng reference thay vì tìm child.
        private static void AssignOutline(GameObject root, DishSlotView view)
        {
            Transform outline = root.transform.Find("Slot Outline");
            SpriteRenderer renderer = outline != null ? outline.GetComponent<SpriteRenderer>() : null;
            if (renderer == null)
                Debug.LogWarning($"[DishTwinkleEffectBuilder] {DishSlotPrefabPath} thiếu child \"Slot Outline\" có SpriteRenderer.");
            view.SetOutline(renderer);
        }

        // EN: Assigns the DishSlotView prefab to ServingTrayView.dishSlotViewPrefab on the serving tray prefab (scene instances inherit it).
        // VI: Gán prefab DishSlotView vào ServingTrayView.dishSlotViewPrefab trên prefab khay phục vụ (các bản trong scene kế thừa).
        private static void LinkServingTray(DishSlotView slotView)
        {
            if (slotView == null)
                return;
            GameObject root = PrefabUtility.LoadPrefabContents(ServingTrayPrefabPath);
            try
            {
                foreach (ServingTrayView trayView in root.GetComponentsInChildren<ServingTrayView>(true))
                {
                    SerializedObject serialized = new SerializedObject(trayView);
                    serialized.FindProperty("dishSlotViewPrefab").objectReferenceValue = slotView;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, ServingTrayPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // EN: Looping, local-space sparkle: a few small stars per second appear at random spots in a flat ellipse, grow, spin a little and fade out.
        // VI: Lấp lánh lặp lại trong không gian cục bộ: mỗi giây vài ngôi sao nhỏ hiện ở vị trí ngẫu nhiên trong một elip dẹt, to dần, xoay nhẹ rồi mờ đi.
        private static void Configure(ParticleSystem system, Material material)
        {
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 1f);
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.65f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 45f * Mathf.Deg2Rad);
            main.startColor = new Color(1f, 0.97f, 0.82f, 1f);
            main.maxParticles = 20;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 7f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.45f;
            shape.radiusThickness = 1f;
            shape.scale = new Vector3(1f, 0.75f, 1f);

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(60f * Mathf.Deg2Rad, 140f * Mathf.Deg2Rad);

            FoodLandEffectBuilder.SetSizeOverLifetime(system, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f)));

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            FoodLandEffectBuilder.ApplyRenderer(system, material, 0);
        }
    }
}
