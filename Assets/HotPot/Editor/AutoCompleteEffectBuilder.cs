using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Editor tool that builds the AutoComplete booster particle prefabs: the looping star with its magic circle (soft star core, glow, turning ring, comet with a real particle trail, drifting bokeh, orbiting small stars and a world-space flight trail), the burst played at the tray and the small sparkle puff played when the star appears or absorbs an ingredient.
    // VI: Công cụ editor dựng các prefab particle của booster AutoComplete: ngôi sao lặp lại cùng vòng phép (lõi sao mềm, quầng sáng, vòng xoay, sao chổi có trail particle thật, bokeh trôi, sao nhỏ bay vòng và vệt bay trong không gian world), vụ nổ ở khay và chùm lấp lánh nhỏ khi sao xuất hiện hoặc hút nguyên liệu.
    public static class AutoCompleteEffectBuilder
    {
        private const string EffectFolder = "Assets/HotPot/Art/Sprites/Effects";
        private const string MaterialFolder = "Assets/HotPot/Art/Materials/Particles";
        private const string PrefabFolder = "Assets/HotPot/Prefabs/Effects";
        public const string StarPrefabPath = PrefabFolder + "/AutoCompleteStar.prefab";
        public const string BurstPrefabPath = PrefabFolder + "/AutoCompleteBurst.prefab";
        public const string SparklePrefabPath = PrefabFolder + "/AutoCompleteSparkle.prefab";

        private static readonly Color Gold = new Color(1f, 0.78f, 0.3f, 1f);
        private static readonly Color Cream = new Color(1f, 0.96f, 0.82f, 1f);

        private struct Materials
        {
            public Material star, glow, ring, bokeh, twinkle, trail;
        }

        // EN: Builds (or rebuilds) the three AutoComplete particle prefabs.
        // VI: Dựng (hoặc dựng lại) ba prefab particle của AutoComplete.
        [MenuItem("Tools/HotPot/Build AutoComplete Effects")]
        public static void Build()
        {
            FoodLandEffectBuilder.EnsureFolder(MaterialFolder);
            FoodLandEffectBuilder.EnsureFolder(PrefabFolder);
            Materials materials = LoadMaterials();
            if (materials.star == null)
                return;
            SavePrefab("AutoCompleteStar", StarPrefabPath, root => BuildStar(root, materials));
            SavePrefab("AutoCompleteBurst", BurstPrefabPath, root => BuildBurst(root, materials));
            SavePrefab("AutoCompleteSparkle", SparklePrefabPath, root => BuildSparkle(root, materials));
            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoCompleteEffectBuilder] Đã dựng {StarPrefabPath}, {BurstPrefabPath}, {SparklePrefabPath}.");
        }

        // EN: Imports the effect textures and creates their materials: the solid stars use the alpha-blended sprite material, every light layer uses the additive shader.
        // VI: Import texture hiệu ứng và tạo material: sao đặc dùng material sprite trong suốt thường, mọi lớp ánh sáng dùng shader cộng sáng.
        private static Materials LoadMaterials()
        {
            Texture2D star = Import("FxSoftStar.png");
            if (star == null)
                return default;
            return new Materials
            {
                star = FoodLandEffectBuilder.GetOrCreateMaterial("AutoCompleteStar", star),
                glow = Additive("AutoCompleteGlow", Import("FxSoftCircle.png")),
                ring = Additive("AutoCompleteRing", Import("FxMagicRing.png")),
                bokeh = Additive("AutoCompleteBokeh", Import("FxBokeh.png")),
                twinkle = Additive("AutoCompleteTwinkle", Import("FxTwinkleStar.png")),
                trail = Additive("AutoCompleteTrail", Import("FxTrail.png"))
            };
        }

        // EN: Imports one texture from the Effects sprite folder, warning when it is missing.
        // VI: Import một texture trong thư mục sprite Effects, cảnh báo khi không có.
        internal static Texture2D Import(string file)
        {
            Texture2D texture = FoodLandEffectBuilder.ImportEffectTexture($"{EffectFolder}/{file}");
            if (texture == null)
                Debug.LogWarning($"[AutoCompleteEffectBuilder] Thiếu {EffectFolder}/{file}.");
            return texture;
        }

        // EN: Loads or creates an additive material (shader FoodPop/SpriteAdditive) for the given texture.
        // VI: Tải hoặc tạo material cộng sáng (shader FoodPop/SpriteAdditive) cho texture đã cho.
        internal static Material Additive(string name, Texture2D texture)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Shader shader = Shader.Find("FoodPop/SpriteAdditive");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
                material.shader = shader;
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        // EN: Creates a temporary root, lets the callback add its layers, then saves it as a ParticleEffect prefab over the previous version.
        // VI: Tạo object gốc tạm, để hàm truyền vào thêm các lớp, rồi lưu thành prefab ParticleEffect ghi đè bản cũ.
        internal static void SavePrefab(string name, string path, System.Action<GameObject> build)
        {
            GameObject root = new GameObject(name);
            try
            {
                build(root);
                FoodLandEffectBuilder.SaveEffectPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- Star (looping)

        // EN: Looping star in local space so it follows the moving transform: soft star core, glow, turning ring, comet with a trail running round the ring, bokeh inside, small stars orbiting outside, plus a world-space trail left behind while it flies.
        // VI: Ngôi sao lặp lại trong không gian cục bộ để đi theo transform đang di chuyển: lõi sao mềm, quầng sáng, vòng xoay, sao chổi có trail chạy quanh vòng, bokeh bên trong, sao nhỏ bay vòng bên ngoài, cùng vệt bay để lại trong không gian world khi sao bay.
        private static void BuildStar(GameObject root, Materials m)
        {
            ParticleSystem glow = Layer(root, "Glow", true);
            Looping(glow, 1f, 3f, new ParticleSystem.MinMaxCurve(1f), 2.6f, 4);
            Fade(glow, Gold, 0.55f, 0.3f, 0.7f);
            FoodLandEffectBuilder.SetSizeOverLifetime(glow, Curve(0.85f, 1.1f, 0.9f));
            Render(glow, m.glow, 40);

            ParticleSystem ring = Layer(root, "Ring", true);
            Looping(ring, 1.6f, 1.2f, new ParticleSystem.MinMaxCurve(1.6f), 3.1f, 4);
            ParticleSystem.MainModule ringMain = ring.main;
            ringMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            Spin(ring, -60f, -40f);
            Fade(ring, Gold, 0.9f, 0.25f, 0.7f);
            FoodLandEffectBuilder.SetSizeOverLifetime(ring, Curve(0.9f, 1f, 1.05f));
            Render(ring, m.ring, 30);

            ParticleSystem comet = Layer(root, "Comet", true);
            Looping(comet, 0.9f, 2.2f, new ParticleSystem.MinMaxCurve(0.9f), 0.14f, 6);
            EdgeCircle(comet, 1.2f);
            Orbit(comet, -7f);
            Fade(comet, Cream, 1f, 0.1f, 0.8f);
            Trail(comet, m.trail, 0.4f, 1.4f, Cream, Gold);
            Render(comet, m.bokeh, 20);

            ParticleSystem bokeh = Layer(root, "Bokeh", true);
            Looping(bokeh, 1f, 12f, new ParticleSystem.MinMaxCurve(0.6f, 1.1f), 0f, 20);
            ParticleSystem.MainModule bokehMain = bokeh.main;
            bokehMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
            bokehMain.startColor = new ParticleSystem.MinMaxGradient(Cream, Gold);
            FilledCircle(bokeh, 1f);
            Orbit(bokeh, 1.2f);
            Twinkle(bokeh);
            Render(bokeh, m.bokeh, 10);

            ParticleSystem minis = Layer(root, "MiniStars", true);
            Looping(minis, 1f, 3.5f, new ParticleSystem.MinMaxCurve(1.2f, 1.6f), 0f, 8);
            ParticleSystem.MainModule minisMain = minis.main;
            minisMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.34f);
            minisMain.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            EdgeCircle(minis, 1.45f);
            Orbit(minis, 0.9f);
            Spin(minis, -60f, 60f);
            Fade(minis, Color.white, 1f, 0.2f, 0.75f);
            FoodLandEffectBuilder.SetSizeOverLifetime(minis, Curve(0f, 1f, 0.6f));
            Render(minis, m.star, 4);

            ParticleSystem core = root.AddComponent<ParticleSystem>();
            Looping(core, 0.9f, 3f, new ParticleSystem.MinMaxCurve(0.9f), 1f, 5);
            ParticleSystem.MainModule coreMain = core.main;
            coreMain.startRotation = new ParticleSystem.MinMaxCurve(-0.14f, 0.14f);
            Spin(core, -10f, 10f);
            Fade(core, Color.white, 1f, 0.25f, 0.75f);
            FoodLandEffectBuilder.SetSizeOverLifetime(core, Curve(0.92f, 1.06f, 0.92f));
            Render(core, m.star, 0);

            ParticleSystem trailDots = Layer(root, "FlightTrail", false);
            FlightTrail(trailDots, 18f, new ParticleSystem.MinMaxCurve(0.06f, 0.18f), new ParticleSystem.MinMaxGradient(Cream, Gold));
            Twinkle(trailDots);
            Render(trailDots, m.bokeh, 12);

            ParticleSystem trailStars = Layer(root, "FlightTrailStars", false);
            FlightTrail(trailStars, 3.5f, new ParticleSystem.MinMaxCurve(0.14f, 0.24f), new ParticleSystem.MinMaxGradient(Color.white));
            Spin(trailStars, -120f, 120f);
            Fade(trailStars, Color.white, 1f, 0.3f, 0.5f);
            Render(trailStars, m.star, 6);
        }

        // ---------------------------------------------------------------- Burst (one shot)

        // EN: One-shot burst at the tray: a bright flash, the magic ring expanding and turning as it fades, comets sweeping outward with trails, a shower of bokeh, small stars and twinkles.
        // VI: Vụ nổ một lần ở khay: chớp sáng mạnh, vòng phép lan rộng và xoay trong lúc mờ dần, sao chổi quét ra ngoài kèm trail, mưa bokeh, sao nhỏ và tia lấp lánh.
        private static void BuildBurst(GameObject root, Materials m)
        {
            ParticleSystem flash = root.AddComponent<ParticleSystem>();
            OneShot(flash, 1, new ParticleSystem.MinMaxCurve(0.4f), 6f, 0f);
            FoodLandEffectBuilder.SetSizeOverLifetime(flash, Curve(0.25f, 0.9f, 1f));
            Fade(flash, Gold, 1f, 0f, 0.3f);
            Render(flash, m.glow, 40);

            ParticleSystem ring = Layer(root, "Ring", false);
            OneShot(ring, 1, new ParticleSystem.MinMaxCurve(0.65f), 6f, 0f);
            ParticleSystem.MainModule ringMain = ring.main;
            ringMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            Spin(ring, -160f, -120f);
            FoodLandEffectBuilder.SetSizeOverLifetime(ring, Curve(0.45f, 0.85f, 1f));
            Fade(ring, Gold, 1f, 0f, 0.4f);
            Render(ring, m.ring, 30);

            ParticleSystem comets = Layer(root, "Comets", false);
            OneShot(comets, 3, new ParticleSystem.MinMaxCurve(0.55f), 0.16f, 0f);
            EdgeCircle(comets, 1.3f);
            Orbit(comets, -9f);
            ParticleSystem.VelocityOverLifetimeModule velocity = comets.velocityOverLifetime;
            velocity.radial = new ParticleSystem.MinMaxCurve(3f);
            Fade(comets, Cream, 1f, 0.5f, 1f);
            Trail(comets, m.trail, 0.5f, 1.6f, Cream, Gold);
            Render(comets, m.bokeh, 20);

            ParticleSystem bokeh = Layer(root, "Bokeh", false);
            Spray(bokeh, 26, 2f, 5f, new ParticleSystem.MinMaxCurve(0.06f, 0.24f), 0.4f);
            ParticleSystem.MainModule bokehMain = bokeh.main;
            bokehMain.startColor = new ParticleSystem.MinMaxGradient(Cream, Gold);
            Twinkle(bokeh);
            Render(bokeh, m.bokeh, 10);

            ParticleSystem minis = Layer(root, "MiniStars", false);
            Spray(minis, 10, 2f, 4.5f, new ParticleSystem.MinMaxCurve(0.18f, 0.32f), 0.6f);
            Spin(minis, -240f, 240f);
            Fade(minis, Color.white, 1f, 0.5f, 1f);
            Render(minis, m.star, 4);

            ParticleSystem twinkles = Layer(root, "Twinkles", false);
            OneShot(twinkles, 6, new ParticleSystem.MinMaxCurve(0.3f, 0.5f), 0f, 0f);
            ParticleSystem.MainModule twinkleMain = twinkles.main;
            twinkleMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            twinkleMain.startDelay = new ParticleSystem.MinMaxCurve(0f, 0.15f);
            FilledCircle(twinkles, 1.4f);
            FoodLandEffectBuilder.SetSizeOverLifetime(twinkles, Curve(0f, 1f, 0f));
            Spin(twinkles, 60f, 140f);
            Fade(twinkles, Cream, 1f, 0.4f, 1f);
            Render(twinkles, m.twinkle, 2);
        }

        // ---------------------------------------------------------------- Sparkle (one shot, small)

        // EN: Small one-shot puff used when the star appears or swallows an ingredient: a quick flash, a few bokeh dots and small stars.
        // VI: Chùm nhỏ chạy một lần khi sao xuất hiện hoặc nuốt một nguyên liệu: chớp sáng nhanh, vài chấm bokeh và sao nhỏ.
        private static void BuildSparkle(GameObject root, Materials m)
        {
            ParticleSystem flash = root.AddComponent<ParticleSystem>();
            OneShot(flash, 1, new ParticleSystem.MinMaxCurve(0.25f), 2.4f, 0f);
            FoodLandEffectBuilder.SetSizeOverLifetime(flash, Curve(0.4f, 1f, 1f));
            Fade(flash, Gold, 0.9f, 0f, 0.3f);
            Render(flash, m.glow, 20);

            ParticleSystem bokeh = Layer(root, "Bokeh", false);
            Spray(bokeh, 10, 1.5f, 3.5f, new ParticleSystem.MinMaxCurve(0.05f, 0.18f), 0.3f);
            ParticleSystem.MainModule bokehMain = bokeh.main;
            bokehMain.startColor = new ParticleSystem.MinMaxGradient(Cream, Gold);
            bokehMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            Twinkle(bokeh);
            Render(bokeh, m.bokeh, 10);

            ParticleSystem minis = Layer(root, "MiniStars", false);
            Spray(minis, 4, 1.5f, 3f, new ParticleSystem.MinMaxCurve(0.14f, 0.24f), 0.5f);
            ParticleSystem.MainModule minisMain = minis.main;
            minisMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            Spin(minis, -240f, 240f);
            Fade(minis, Color.white, 1f, 0.5f, 1f);
            Render(minis, m.star, 4);
        }

        // ---------------------------------------------------------------- Helpers

        // EN: Adds a child layer with a fresh ParticleSystem using the shared one-shot settings; local space when the layer must follow the effect.
        // VI: Thêm lớp con với ParticleSystem mới theo thiết lập chạy một lần dùng chung; dùng không gian cục bộ khi lớp phải đi theo hiệu ứng.
        internal static ParticleSystem Layer(GameObject root, string name, bool local)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            ParticleSystem system = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 1f);
            main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            return system;
        }

        // EN: Looping layer (keeps the simulation space chosen by Layer; the root layer is local): emits rate particles per second (plus one at the start) with the given lifetime and size, no speed, and no emission shape.
        // VI: Lớp lặp lại (giữ không gian mô phỏng đã chọn ở Layer; lớp gốc dùng không gian cục bộ): phát rate hạt mỗi giây (cộng một hạt lúc bắt đầu) với thời gian sống và kích thước đã cho, không có vận tốc và không có hình dạng phát.
        internal static void Looping(ParticleSystem system, float duration, float rate, ParticleSystem.MinMaxCurve lifetime, float size, int maxParticles)
        {
            ParticleSystemSimulationSpace space = system.transform.parent == null ? ParticleSystemSimulationSpace.Local : system.main.simulationSpace;
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, duration);
            main.loop = true;
            main.simulationSpace = space;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            if (size > 0f)
                main.startSize = size;
            main.maxParticles = maxParticles;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
        }

        // EN: World-space trail layer: emits by distance travelled, so dots are only left behind while the star moves; they drift, fall slightly and fade.
        // VI: Lớp vệt bay trong không gian world: phát theo quãng đường di chuyển nên chỉ để lại hạt khi sao đang bay; hạt trôi, rơi nhẹ và mờ dần.
        internal static void FlightTrail(ParticleSystem system, float perUnit, ParticleSystem.MinMaxCurve size, ParticleSystem.MinMaxGradient color)
        {
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 1f);
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = 0.3f;
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = perUnit;
            FilledCircle(system, 0.3f);
        }

        // EN: One-shot layer: a single burst of count particles with the given lifetime, size and speed, no emission shape.
        // VI: Lớp chạy một lần: một đợt bắn count hạt với thời gian sống, kích thước và vận tốc đã cho, không có hình dạng phát.
        internal static void OneShot(ParticleSystem system, short count, ParticleSystem.MinMaxCurve lifetime, float size, float speed)
        {
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 1f);
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            if (size > 0f)
                main.startSize = size;
            main.maxParticles = Mathf.Max(1, count);
            FoodLandEffectBuilder.SetBurst(system, count);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
        }

        // EN: One-shot spray: count particles thrown in every direction of the XY plane, slowed by drag and pulled down by gravity.
        // VI: Bắn tung một lần: count hạt bay theo mọi hướng trong mặt phẳng XY, bị lực cản làm chậm và trọng lực kéo xuống.
        internal static void Spray(ParticleSystem system, short count, float minSpeed, float maxSpeed, ParticleSystem.MinMaxCurve size, float gravity)
        {
            OneShot(system, count, new ParticleSystem.MinMaxCurve(0.5f, 1f), 0f, 0f);
            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = size;
            main.gravityModifier = gravity;
            FilledCircle(system, 0.15f);
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.8f;
            limit.dampen = 0.12f;
        }

        // EN: Emission shape: particles spawn on the edge of a circle of the given radius.
        // VI: Hình dạng phát: hạt xuất hiện trên viền vòng tròn có bán kính đã cho.
        internal static void EdgeCircle(ParticleSystem system, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0f;
        }

        // EN: Emission shape: particles spawn anywhere inside a circle of the given radius.
        // VI: Hình dạng phát: hạt xuất hiện bất kỳ đâu bên trong vòng tròn có bán kính đã cho.
        internal static void FilledCircle(ParticleSystem system, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
        }

        // EN: Makes particles circle the effect's centre at the given angular speed (radians per second; negative is clockwise).
        // VI: Cho hạt bay vòng quanh tâm hiệu ứng với tốc độ góc đã cho (radian mỗi giây; âm là chiều kim đồng hồ).
        internal static void Orbit(ParticleSystem system, float speed)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(speed);
        }

        // EN: Spins particles around Z at a random speed between the given degrees per second.
        // VI: Xoay hạt quanh trục Z với tốc độ ngẫu nhiên trong khoảng độ mỗi giây đã cho.
        internal static void Spin(ParticleSystem system, float minDegrees, float maxDegrees)
        {
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(minDegrees * Mathf.Deg2Rad, maxDegrees * Mathf.Deg2Rad);
        }

        // EN: Colour over lifetime: fades in until fadeIn, holds the given alpha, then fades out from fadeOut.
        // VI: Màu theo thời gian sống: hiện dần tới fadeIn, giữ alpha đã cho, rồi mờ dần từ fadeOut.
        internal static void Fade(ParticleSystem system, Color tint, float alpha, float fadeIn, float fadeOut)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(fadeIn > 0f ? 0f : alpha, 0f), new GradientAlphaKey(alpha, Mathf.Max(0.001f, fadeIn)), new GradientAlphaKey(alpha, Mathf.Clamp(fadeOut, fadeIn + 0.001f, 0.999f)), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // EN: Twinkling alpha: pops in, flickers twice and fades out.
        // VI: Alpha lấp lánh: hiện nhanh, chớp hai lần rồi mờ đi.
        internal static void Twinkle(ParticleSystem system)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.45f, 0.4f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // EN: Real particle trail behind each particle: lasts the given share of the particle's life, tapers to nothing and fades from head colour to tail colour.
        // VI: Trail particle thật phía sau mỗi hạt: tồn tại theo tỉ lệ thời gian sống đã cho, thon dần về 0 và chuyển màu từ đầu tới đuôi.
        internal static void Trail(ParticleSystem system, Material material, float lifetime, float width, Color head, Color tail)
        {
            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = lifetime;
            trails.minVertexDistance = 0.02f;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(width, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(gradient);
            system.GetComponent<ParticleSystemRenderer>().trailMaterial = material;
        }

        // EN: Three-key size curve (start, middle, end) for size over lifetime.
        // VI: Đường cong kích thước ba điểm (đầu, giữa, cuối) cho kích thước theo thời gian sống.
        internal static AnimationCurve Curve(float start, float middle, float end)
        {
            return new AnimationCurve(new Keyframe(0f, start), new Keyframe(0.5f, middle), new Keyframe(1f, end));
        }

        // EN: Sets the layer's material and its sorting fudge (a bigger fudge draws the layer further behind the others).
        // VI: Gán material của lớp và sorting fudge (fudge lớn hơn thì lớp được vẽ phía sau các lớp khác).
        internal static void Render(ParticleSystem system, Material material, float fudge)
        {
            FoodLandEffectBuilder.ApplyRenderer(system, material, 0);
            system.GetComponent<ParticleSystemRenderer>().sortingFudge = fudge;
        }
    }
}
