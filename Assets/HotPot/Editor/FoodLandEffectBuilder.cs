using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that builds the raw-to-prepared poof, chop smoke and per-food debris prefabs from the effect sprites, configures the pop/whoosh/ding/chop sounds and assigns them to the scene's FoodFlyAnimator, ChefManager and FoodIconConfig. The landing particle on the conveyor is no longer used and is cleared.
    // VI: Công cụ editor dựng prefab phụt khói khi sơ chế xong, khói khi cắt và vụn riêng từng món từ sprite hiệu ứng, cấu hình âm thanh pop/whoosh/ting/tiếng dao và gán vào FoodFlyAnimator, ChefManager và FoodIconConfig. Particle khi món chạm băng chuyền không còn dùng nữa và được bỏ gán.
    public static class FoodLandEffectBuilder
    {
        private const string SpriteFolder = "Assets/HotPot/Art/Sprites/Effects";
        private const string MaterialFolder = "Assets/HotPot/Art/Materials/Particles";
        private const string PrefabFolder = "Assets/HotPot/Prefabs/Effects";
        private const string AudioFolder = "Assets/HotPot/Audio/SFX";

        private const string SparkleTexturePath = SpriteFolder + "/FxSparkle.png";
        private const string SoftCircleTexturePath = SpriteFolder + "/FxSoftCircle.png";
        private const string RingTexturePath = SpriteFolder + "/FxRing.png";
        private const string LandSoundPath = AudioFolder + "/FoodLandPop.wav";
        private const string LaunchSoundPath = AudioFolder + "/FoodLaunchWhoosh.wav";
        private const string PreparedSoundPath = AudioFolder + "/FoodPreparedDing.wav";
        private const string PreparedPrefabPath = PrefabFolder + "/FoodPreparedEffect.prefab";
        private const string ChunkTexturePath = SpriteFolder + "/FxChunk.png";
        private const string ShredTexturePath = SpriteFolder + "/FxShred.png";
        private const string SmokePrefabPath = PrefabFolder + "/ChefChopSmoke.prefab";
        private const string MeatDebrisPrefabPath = PrefabFolder + "/ChefMeatDebris.prefab";
        private const string VegetableDebrisPrefabPath = PrefabFolder + "/ChefVegetableDebris.prefab";
        private const string ChopPrefabFolder = PrefabFolder + "/Chop";
        private const string ChopSoundFolder = AudioFolder + "/Chop";
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";

        // EN: Look of one debris layer: sprite file name (in the Effects sprite folder), two random tint colours and a size range in world units.
        // VI: Hình dạng một lớp vụn: tên file sprite (trong thư mục sprite Effects), hai màu tô ngẫu nhiên và khoảng kích thước theo đơn vị world.
        private struct DebrisLayer
        {
            public string sprite;
            public Color colorA;
            public Color colorB;
            public float minSize;
            public float maxSize;

            public DebrisLayer(string sprite, Color colorA, Color colorB, float minSize, float maxSize)
            {
                this.sprite = sprite;
                this.colorA = colorA;
                this.colorB = colorB;
                this.minSize = minSize;
                this.maxSize = maxSize;
            }
        }

        // EN: Defines ingredient debris, including green seaweed shreds, while condiments use the category fallback.
        // VI: Định nghĩa vụn nguyên liệu, gồm vụn rong biển xanh; gia vị dùng hiệu ứng mặc định theo nhóm.
        private static (FoodType type, DebrisLayer main, DebrisLayer? extra)[] GetChopSpecs()
        {
            return new (FoodType, DebrisLayer, DebrisLayer?)[]
            {
                (FoodType.SushiRice, new DebrisLayer("FxGrain", new Color(1f, 1f, 0.97f), new Color(0.95f, 0.93f, 0.86f), 0.07f, 0.11f), null),
                (FoodType.Salmon, new DebrisLayer("FxChunk", new Color(1f, 0.55f, 0.35f), new Color(1f, 0.72f, 0.55f), 0.09f, 0.15f),
                    new DebrisLayer("FxShred", new Color(1f, 0.92f, 0.86f), new Color(1f, 0.85f, 0.78f), 0.07f, 0.1f)),
                (FoodType.Tuna, new DebrisLayer("FxChunk", new Color(0.75f, 0.12f, 0.15f), new Color(0.9f, 0.26f, 0.26f), 0.09f, 0.16f), null),
                (FoodType.Shrimp, new DebrisLayer("FxChunk", new Color(1f, 0.5f, 0.25f), new Color(1f, 0.66f, 0.4f), 0.06f, 0.1f),
                    new DebrisLayer("FxChunk", new Color(1f, 0.96f, 0.92f), new Color(1f, 0.88f, 0.82f), 0.08f, 0.12f)),
                (FoodType.CrabStick, new DebrisLayer("FxShred", new Color(0.95f, 0.2f, 0.2f), new Color(1f, 0.38f, 0.32f), 0.1f, 0.15f),
                    new DebrisLayer("FxShred", new Color(1f, 0.98f, 0.96f), new Color(0.97f, 0.93f, 0.9f), 0.09f, 0.13f)),
                (FoodType.Tamagoyaki, new DebrisLayer("FxChunk", new Color(1f, 0.85f, 0.25f), new Color(1f, 0.93f, 0.52f), 0.07f, 0.12f), null),
                (FoodType.Cucumber, new DebrisLayer("FxDisc", new Color(0.55f, 0.85f, 0.4f), new Color(0.75f, 0.92f, 0.52f), 0.1f, 0.16f),
                    new DebrisLayer("FxShred", new Color(0.18f, 0.48f, 0.18f), new Color(0.28f, 0.6f, 0.25f), 0.07f, 0.11f)),
                (FoodType.SalmonRoe, new DebrisLayer("FxBead", new Color(1f, 0.45f, 0.1f), new Color(1f, 0.62f, 0.22f), 0.06f, 0.1f), null),
                (FoodType.Unagi, new DebrisLayer("FxChunk", new Color(0.45f, 0.25f, 0.12f), new Color(0.62f, 0.36f, 0.18f), 0.09f, 0.15f),
                    new DebrisLayer("FxDrop", new Color(0.3f, 0.15f, 0.06f), new Color(0.42f, 0.22f, 0.08f), 0.06f, 0.09f)),
                (FoodType.Octopus, new DebrisLayer("FxTentacleRing", new Color(0.85f, 0.35f, 0.45f), new Color(0.95f, 0.55f, 0.62f), 0.08f, 0.13f),
                    new DebrisLayer("FxChunk", new Color(1f, 0.9f, 0.92f), new Color(0.98f, 0.82f, 0.85f), 0.06f, 0.1f)),
                (FoodType.Seaweed, new DebrisLayer("FxShred", new Color(0.16f, 0.24f, 0.04f), new Color(0.3f, 0.4f, 0.08f), 0.06f, 0.11f), null),
                (FoodType.Scallop, new DebrisLayer("FxChunk", new Color(1f, 0.96f, 0.88f), new Color(0.98f, 0.88f, 0.75f), 0.08f, 0.13f), null),
            };
        }

        private static readonly Color Gold = new Color(1f, 0.74f, 0.18f, 1f);

        // EN: Menu entry: builds or rebuilds all landing effect assets and wires them into the open scene.
        // VI: Mục menu: dựng hoặc dựng lại toàn bộ asset hiệu ứng chạm và gán vào scene đang mở.
        [MenuItem("Tools/HotPot/Build Food Land Effect")]
        public static void Build()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);
            Texture2D sparkle = ImportEffectTexture(SparkleTexturePath);
            Texture2D softCircle = ImportEffectTexture(SoftCircleTexturePath);
            Texture2D ring = ImportEffectTexture(RingTexturePath);
            if (sparkle == null || softCircle == null || ring == null)
            {
                Debug.LogError($"[FoodLandEffectBuilder] Thiếu sprite hiệu ứng trong {SpriteFolder}.");
                return;
            }
            AudioClip landSound = ImportSound(LandSoundPath);
            AudioClip launchSound = ImportSound(LaunchSoundPath);
            AudioClip preparedSound = ImportSound(PreparedSoundPath);
            Texture2D chunk = ImportEffectTexture(ChunkTexturePath);
            Texture2D shred = ImportEffectTexture(ShredTexturePath);

            Material sparkleMaterial = GetOrCreateMaterial("FoodLandSparkle", sparkle);
            Material puffMaterial = GetOrCreateMaterial("FoodLandPuff", softCircle);
            ParticleEffect preparedPrefab = BuildPreparedPrefab(sparkleMaterial, puffMaterial);
            ParticleEffect smokePrefab = BuildSingleSystemPrefab("ChefChopSmoke", SmokePrefabPath, system => ConfigureChopSmoke(system, puffMaterial));
            ParticleEffect meatDebrisPrefab = chunk == null ? null : BuildSingleSystemPrefab("ChefMeatDebris", MeatDebrisPrefabPath,
                system => ConfigureDebris(system, GetOrCreateMaterial("FoodDebrisChunk", chunk), new Color(0.95f, 0.45f, 0.38f), new Color(1f, 0.72f, 0.62f), 0.1f, 0.17f));
            ParticleEffect vegetableDebrisPrefab = shred == null ? null : BuildSingleSystemPrefab("ChefVegetableDebris", VegetableDebrisPrefabPath,
                system => ConfigureDebris(system, GetOrCreateMaterial("FoodDebrisShred", shred), new Color(0.4f, 0.78f, 0.3f), new Color(0.75f, 0.92f, 0.45f), 0.12f, 0.2f));

            AssignPreparedFeedback(preparedPrefab);
            AssignChopFeedback(smokePrefab, meatDebrisPrefab, vegetableDebrisPrefab);
            AssignSounds(landSound, launchSound, preparedSound);
            BuildPerFoodChopEffects();
            EnsureAudioListener();
            Selection.activeObject = preparedPrefab != null ? preparedPrefab.gameObject : null;
            Debug.Log($"[FoodLandEffectBuilder] Đã dựng {PreparedPrefabPath} cùng hiệu ứng khi cắt và gán vào FoodFlyAnimator, ChefManager.");
        }

        // EN: Builds the raw-to-prepared "poof" prefab: a white cloud burst with golden sparkles, saved over the previous version.
        // VI: Dựng prefab "phụt khói" khi món chuyển từ thô sang sơ chế: đám khói trắng bung ra kèm hạt sáng vàng, ghi đè bản cũ.
        private static ParticleEffect BuildPreparedPrefab(Material sparkleMaterial, Material puffMaterial)
        {
            GameObject root = new GameObject("FoodPreparedEffect");
            try
            {
                ConfigurePoof(root.AddComponent<ParticleSystem>(), puffMaterial);
                ConfigureSparkles(CreateChildSystem(root, "Sparkles"), sparkleMaterial);
                return SaveEffectPrefab(root, PreparedPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // EN: Builds one debris prefab per ingredient from GetChopSpecs, imports its chop sound and two extra variants (Audio/SFX/Chop/Chop_<Type>.wav, _2, _3) and stores them in that ingredient's row of FoodIconConfig.
        // VI: Dựng một prefab vụn cho từng nguyên liệu theo GetChopSpecs, import âm thanh chặt và hai biến thể (Audio/SFX/Chop/Chop_<Loại>.wav, _2, _3) rồi lưu vào dòng của nguyên liệu đó trong FoodIconConfig.
        private static void BuildPerFoodChopEffects()
        {
            FoodIconConfig config = AssetDatabase.LoadAssetAtPath<FoodIconConfig>(FoodIconConfigPath);
            if (config == null)
            {
                Debug.LogWarning($"[FoodLandEffectBuilder] Không tìm thấy {FoodIconConfigPath}; bỏ qua vụn/âm thanh riêng từng món.");
                return;
            }
            EnsureFolder(ChopPrefabFolder);
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty icons = serialized.FindProperty("icons");
            foreach ((FoodType type, DebrisLayer main, DebrisLayer? extra) in GetChopSpecs())
            {
                ParticleEffect prefab = BuildFoodDebrisPrefab(type, main, extra);
                AudioClip sound = ImportSound($"{ChopSoundFolder}/Chop_{type}.wav");
                AudioClip variant2 = ImportSound($"{ChopSoundFolder}/Chop_{type}_2.wav");
                AudioClip variant3 = ImportSound($"{ChopSoundFolder}/Chop_{type}_3.wav");
                SerializedProperty entry = FindIconEntry(icons, type);
                if (entry == null)
                {
                    Debug.LogWarning($"[FoodLandEffectBuilder] FoodIconConfig chưa có dòng cho {type}.");
                    continue;
                }
                entry.FindPropertyRelative("chopDebris").objectReferenceValue = prefab;
                entry.FindPropertyRelative("chopSound").objectReferenceValue = sound;
                SetClipArray(entry.FindPropertyRelative("chopSoundVariants"), variant2, variant3);
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // EN: Fills a serialized AudioClip array with the given clips, skipping missing ones.
        // VI: Điền mảng AudioClip đã serialize bằng các clip đã cho, bỏ qua clip bị thiếu.
        private static void SetClipArray(SerializedProperty array, params AudioClip[] clips)
        {
            if (array == null)
                return;
            array.ClearArray();
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                    continue;
                array.InsertArrayElementAtIndex(array.arraySize);
                array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = clip;
            }
        }

        // EN: Builds the debris prefab of one ingredient: the main layer on the root and, when given, an extra layer as a child.
        // VI: Dựng prefab vụn của một nguyên liệu: lớp chính ở gốc và, nếu có, lớp phụ ở object con.
        private static ParticleEffect BuildFoodDebrisPrefab(FoodType type, DebrisLayer main, DebrisLayer? extra)
        {
            GameObject root = new GameObject($"Debris_{type}");
            try
            {
                ConfigureDebrisLayer(root.AddComponent<ParticleSystem>(), main);
                if (extra.HasValue)
                    ConfigureDebrisLayer(CreateChildSystem(root, "Extra"), extra.Value);
                return SaveEffectPrefab(root, $"{ChopPrefabFolder}/Debris_{type}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // EN: Configures one debris layer from its description, creating a material for its sprite.
        // VI: Cấu hình một lớp vụn theo mô tả, tạo material cho sprite của lớp đó.
        private static void ConfigureDebrisLayer(ParticleSystem system, DebrisLayer layer)
        {
            Texture2D texture = ImportEffectTexture($"{SpriteFolder}/{layer.sprite}.png");
            Material material = GetOrCreateMaterial($"Debris_{layer.sprite}", texture);
            ConfigureDebris(system, material, layer.colorA, layer.colorB, layer.minSize, layer.maxSize);
        }

        // EN: Returns the element of the icons array whose foodType matches, or null.
        // VI: Trả về phần tử trong mảng icons có foodType trùng khớp, hoặc null.
        private static SerializedProperty FindIconEntry(SerializedProperty icons, FoodType type)
        {
            for (int i = 0; i < icons.arraySize; i++)
            {
                SerializedProperty entry = icons.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("foodType").intValue == (int)type)
                    return entry;
            }
            return null;
        }

        // EN: Adds a ParticleEffect component holding direct references to every layer and renderer, then saves the prefab over the previous version.
        // VI: Thêm component ParticleEffect giữ tham chiếu trực tiếp tới mọi lớp và renderer, rồi lưu prefab ghi đè bản cũ.
        internal static ParticleEffect SaveEffectPrefab(GameObject root, string path)
        {
            root.AddComponent<ParticleEffect>().CollectReferences();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved != null ? saved.GetComponent<ParticleEffect>() : null;
        }

        // EN: Builds a prefab made of one ParticleSystem configured by the given callback and saves it over the previous version.
        // VI: Dựng prefab gồm một ParticleSystem được cấu hình bởi hàm truyền vào và lưu ghi đè bản cũ.
        private static ParticleEffect BuildSingleSystemPrefab(string name, string path, System.Action<ParticleSystem> configure)
        {
            GameObject root = new GameObject(name);
            try
            {
                configure(root.AddComponent<ParticleSystem>());
                return SaveEffectPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // EN: Chop smoke: a looping trickle of light grey puffs rising and growing from the food while the chef cuts (started and stopped by ChefManager).
        // VI: Khói khi cắt: các đốm khói xám nhạt bốc lên và lớn dần từ món liên tục trong lúc đầu bếp cắt (ChefManager bật/tắt).
        private static void ConfigureChopSmoke(ParticleSystem system, Material material)
        {
            ParticleSystem.MainModule main = ApplyCommonMain(system, 1f);
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.95f, 0.95f, 0.5f), new Color(0.85f, 0.85f, 0.85f, 0.35f));
            main.gravityModifier = -0.12f;
            main.maxParticles = 30;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 16f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            shape.arc = 180f;
            shape.rotation = Vector3.zero;

            SetSizeOverLifetime(system, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f)));
            SetFadeOut(system, Color.white, 1f, 0.3f);
            ApplyRenderer(system, material, 1);
        }

        // EN: Debris: no automatic emission (ChefManager emits a few pieces per chop); pieces pop upward, spin, fall with gravity and fade.
        // VI: Vụn: không tự phát (ChefManager bắn vài mảnh mỗi nhát chặt); mảnh vụn bật lên, xoay, rơi theo trọng lực và mờ dần.
        private static void ConfigureDebris(ParticleSystem system, Material material, Color colorA, Color colorB, float minSize, float maxSize)
        {
            ParticleSystem.MainModule main = ApplyCommonMain(system, 1f);
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = 1.1f;
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[0]);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.arc = 180f;
            shape.rotation = Vector3.zero;

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 3f, Mathf.PI * 3f);

            SetFadeOut(system, Color.white, 1f, 0.65f);
            ApplyRenderer(system, material, 1);
        }

        // EN: Poof cloud: several bright white soft blobs bursting outward and growing, hiding the sprite swap for a moment.
        // VI: Đám khói: vài đốm trắng mềm bung ra và lớn dần, che khoảnh khắc đổi ảnh món trong chốc lát.
        private static void ConfigurePoof(ParticleSystem system, Material material)
        {
            ParticleSystem.MainModule main = ApplyCommonMain(system, 0.5f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.maxParticles = 12;
            SetBurst(system, 7);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            shape.arc = 360f;
            shape.rotation = Vector3.zero;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.3f;
            limit.dampen = 0.2f;

            SetSizeOverLifetime(system, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.5f)));
            SetFadeOut(system, Color.white, 0.85f, 0.25f);
            ApplyRenderer(system, material, 1);
        }

        // EN: Main burst: a handful of golden stars thrown outward in the XY plane that slow down, fall slightly, spin and shrink away.
        // VI: Lớp chính: vài ngôi sao vàng bắn ra xung quanh trên mặt phẳng XY, chậm dần, rơi nhẹ, xoay và nhỏ dần rồi biến mất.
        private static void ConfigureSparkles(ParticleSystem system, Material material)
        {
            ParticleSystem.MainModule main = ApplyCommonMain(system, 0.6f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(Gold, Color.white);
            main.gravityModifier = 0.45f;
            main.maxParticles = 16;
            SetBurst(system, 8);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;
            shape.arc = 360f;
            shape.rotation = Vector3.zero;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.4f;
            limit.dampen = 0.18f;

            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 1.5f, Mathf.PI * 1.5f);

            SetSizeOverLifetime(system, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f)));
            SetFadeOut(system, Color.white, 1f, 0.6f);
            ApplyRenderer(system, material, 0);
        }

        // EN: Applies the settings every layer shares: one-shot, no play on awake, world space and scaling that follows the prefab's transform.
        // VI: Áp dụng thiết lập chung cho mọi lớp: chạy một lần, không tự chạy khi bật, không gian world và scale theo transform của prefab.
        internal static ParticleSystem.MainModule ApplyCommonMain(ParticleSystem system, float duration)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            return main;
        }

        // EN: Replaces continuous emission with a single burst of the given count at time zero.
        // VI: Thay phát liên tục bằng một đợt bắn duy nhất với số lượng đã cho tại thời điểm 0.
        internal static void SetBurst(ParticleSystem system, short count)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        }

        // EN: Turns off the emission shape so particles spawn exactly at the effect's origin.
        // VI: Tắt hình dạng phát để hạt xuất hiện đúng tại gốc của hiệu ứng.
        private static void DisableShape(ParticleSystem system)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
        }

        // EN: Enables size over lifetime with the given multiplier curve.
        // VI: Bật thay đổi kích thước theo thời gian sống với đường cong hệ số đã cho.
        internal static void SetSizeOverLifetime(ParticleSystem system, AnimationCurve curve)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        // EN: Enables colour over lifetime that keeps full alpha until fadeStart, then fades to transparent.
        // VI: Bật màu theo thời gian sống: giữ độ đậm tới thời điểm fadeStart rồi mờ dần tới trong suốt.
        internal static void SetFadeOut(ParticleSystem system, Color tint, float startAlpha, float fadeStart)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(startAlpha, Mathf.Clamp01(fadeStart)), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // EN: Sets the billboard material and a relative sorting order; FoodFlyAnimator overrides sorting at runtime to sit above the belt.
        // VI: Gán material billboard và thứ tự vẽ tương đối; FoodFlyAnimator sẽ ghi đè thứ tự vẽ lúc chạy để hiện trên băng chuyền.
        internal static void ApplyRenderer(ParticleSystem system, Material material, int sortingOrder)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // EN: Creates a child GameObject holding a new ParticleSystem.
        // VI: Tạo GameObject con chứa một ParticleSystem mới.
        private static ParticleSystem CreateChildSystem(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<ParticleSystem>();
        }

        // EN: Loads the material with the given name or creates it, using an unlit alpha-blended sprite shader that works with URP.
        // VI: Tải material theo tên hoặc tạo mới, dùng shader sprite không chiếu sáng có trong suốt, chạy được với URP.
        internal static Material GetOrCreateMaterial(string name, Texture2D texture)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        // EN: Imports an effect texture as a clamped, mip-free texture with transparent alpha and returns it.
        // VI: Import texture hiệu ứng ở dạng clamp, không mipmap, alpha trong suốt và trả về texture đó.
        internal static Texture2D ImportEffectTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // EN: Imports a short sound effect as mono, decompressed on load so it plays instantly, and returns the clip.
        // VI: Import âm thanh ngắn ở dạng mono, giải nén khi tải để phát ngay lập tức, rồi trả về clip.
        internal static AudioClip ImportSound(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FoodLandEffectBuilder] Không tìm thấy âm thanh {path}.");
                return null;
            }
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // EN: Assigns every board/chef sound clip to the open scene's IngameSoundManager (created when missing); saves the scene only when it had no other unsaved changes.
        // VI: Gán mọi âm thanh của board/đầu bếp vào IngameSoundManager của scene đang mở (tạo mới nếu chưa có); chỉ tự lưu scene khi scene không có thay đổi chưa lưu nào khác.
        private static void AssignSounds(AudioClip landSound, AudioClip launchSound, AudioClip preparedSound)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            IngameSoundManager manager = EnsureSoundManager();
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("foodLandSound").objectReferenceValue = landSound;
            serialized.FindProperty("dishLaunchSound").objectReferenceValue = launchSound;
            serialized.FindProperty("preparedSound").objectReferenceValue = preparedSound;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            else
                Debug.Log("[FoodLandEffectBuilder] Scene đang có thay đổi khác chưa lưu; đã gán nhưng chưa tự lưu, hãy bấm Ctrl+S.");
        }

        // EN: Returns the scene's IngameSoundManager, creating a GameObject with the manager and its AudioSource (wired directly) when none exists.
        // VI: Trả về IngameSoundManager của scene, tạo GameObject chứa manager và AudioSource (gán trực tiếp) khi chưa có.
        internal static IngameSoundManager EnsureSoundManager()
        {
            IngameSoundManager manager = Object.FindFirstObjectByType<IngameSoundManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                GameObject go = new GameObject("IngameSoundManager");
                Undo.RegisterCreatedObjectUndo(go, "Create IngameSoundManager");
                manager = go.AddComponent<IngameSoundManager>();
            }
            AudioSource source = manager.GetComponent<AudioSource>();
            if (source == null)
                source = Undo.AddComponent<AudioSource>(manager.gameObject);
            source.playOnAwake = false;
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("source").objectReferenceValue = source;
            serialized.ApplyModifiedProperties();
            return manager;
        }

        // EN: Assigns the prepared "poof" particle to the open scene's ChefManager.
        // VI: Gán particle "phụt khói" khi sơ chế xong vào ChefManager của scene đang mở.
        private static void AssignPreparedFeedback(ParticleEffect preparedPrefab)
        {
            ChefManager chef = Object.FindFirstObjectByType<ChefManager>(FindObjectsInactive.Include);
            if (chef == null)
            {
                Debug.LogWarning("[FoodLandEffectBuilder] Scene chưa có ChefManager; bỏ qua gán hiệu ứng sơ chế.");
                return;
            }
            SerializedObject serialized = new SerializedObject(chef);
            serialized.FindProperty("preparedEffectPrefab").objectReferenceValue = preparedPrefab;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(chef.gameObject.scene);
        }

        // EN: Assigns the chop smoke and the meat/vegetable debris prefabs to the open scene's ChefManager.
        // VI: Gán khói khi cắt và prefab vụn thịt/rau vào ChefManager của scene đang mở.
        private static void AssignChopFeedback(ParticleEffect smoke, ParticleEffect meatDebris, ParticleEffect vegetableDebris)
        {
            ChefManager chef = Object.FindFirstObjectByType<ChefManager>(FindObjectsInactive.Include);
            if (chef == null)
                return;
            SerializedObject serialized = new SerializedObject(chef);
            serialized.FindProperty("chopSmokePrefab").objectReferenceValue = smoke;
            serialized.FindProperty("meatDebrisPrefab").objectReferenceValue = meatDebris;
            serialized.FindProperty("vegetableDebrisPrefab").objectReferenceValue = vegetableDebris;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(chef.gameObject.scene);
        }

        // EN: Adds an AudioListener to the main (or first) camera when the open scene has none, otherwise no sound can be heard.
        // VI: Thêm AudioListener vào camera chính (hoặc camera đầu tiên) khi scene đang mở chưa có, nếu không sẽ không nghe được âm thanh nào.
        private static void EnsureAudioListener()
        {
            if (Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include) != null)
                return;
            Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogWarning("[FoodLandEffectBuilder] Scene không có camera để gắn AudioListener.");
                return;
            }
            Undo.AddComponent<AudioListener>(camera.gameObject);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            Debug.Log($"[FoodLandEffectBuilder] Đã thêm AudioListener vào {camera.name}; hãy lưu scene.");
        }

        // EN: Creates every missing folder along an Assets-relative path.
        // VI: Tạo mọi thư mục còn thiếu theo đường dẫn tính từ Assets.
        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
