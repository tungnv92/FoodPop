using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that builds the customer feedback assets (checkmark, sounds) and wires every direct reference the runtime scripts need, so no runtime code has to search children or call GetComponent.
    // VI: Công cụ editor dựng asset phản hồi khách (dấu tích, âm thanh) và gán mọi tham chiếu trực tiếp mà script runtime cần, để code runtime không phải tìm child hay gọi GetComponent.
    public static class CustomerSetup
    {
        private const string CheckmarkPath = "Assets/HotPot/Art/Sprites/Orders/OrderCheckmark.png";
        private const string SoundFolder = "Assets/HotPot/Audio/SFX/Customer";
        private const string DishPrefabPath = "Assets/HotPot/Prefabs/Ingame/IngredientDish.prefab";
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";
        private const string FinalFoodConfigPath = "Assets/HotPot/Config/FinalFoodConfig.asset";
        private const string DishCompleteEffectPath = "Assets/HotPot/Prefabs/Effects/FoodPreparedEffect.prefab";
        private const int OrderLinesPerCard = CustomerOrder.MaxDishes;
        private const float IceCoverScaleX = 1.07f;
        private const float IceCoverScaleY = 1.32f;
        private static readonly string[] IceStagePaths =
        {
            "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Frost.png",
            "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Cracked.png",
            "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Faceted.png",
            "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Clear.png"
        };
        private const string IceShardTexturePath = "Assets/HotPot/Art/Sprites/Effects/FxIceShard.png";
        private const string IceShaderName = "FoodPop/IceSurface";
        private const string IceMaterialPath = "Assets/HotPot/Art/Materials/IceSurface.mat";
        private const string IceHitPrefabPath = "Assets/HotPot/Prefabs/Effects/IceHit.prefab";
        private const string IceBreakPrefabPath = "Assets/HotPot/Prefabs/Effects/IceBreak.prefab";

        // EN: Runs every step: assets, ingredient dish prefab, conveyor slots, board, customers and the level controller; saves the scene when it had no other unsaved changes.
        // VI: Chạy mọi bước: asset, prefab đĩa nguyên liệu, slot băng chuyền, board, khách và level controller; tự lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Setup Customers And References")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            Sprite checkmark = ImportSprite(CheckmarkPath);
            BindDishPrefab();
            BindConveyorSlots();
            BindFoodBoard();
            CustomerManager manager = BindCustomers(checkmark);
            BindLevelController(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log("[CustomerSetup] Đã dựng hiệu ứng khách và gán toàn bộ tham chiếu trực tiếp.");
        }

        // EN: Imports a PNG as a single sprite (100 pixels per unit, no compression).
        // VI: Import file PNG thành một sprite (100 pixel mỗi đơn vị, không nén).
        private static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[CustomerSetup] Thiếu sprite {path}.");
            return sprite;
        }

        // EN: Wires the ingredient dish prefab: Food renderers, selection outline, count canvas, lid and shadow references.
        // VI: Gán tham chiếu cho prefab đĩa nguyên liệu: renderer của Food, viền chọn, canvas số lượng, nắp và bóng.
        private static void BindDishPrefab()
        {
            Sprite[] iceStages = System.Array.ConvertAll(IceStagePaths, AssetDatabase.LoadAssetAtPath<Sprite>);
            Sprite iceSprite = iceStages[0];
            Material iceMaterial = GetOrCreateIceMaterial();
            GameObject root = PrefabUtility.LoadPrefabContents(DishPrefabPath);
            try
            {
                Food food = root.GetComponent<Food>();
                SpriteRenderer plate = FindRenderer(root.transform, "Plate");
                SpriteRenderer lid = FindRenderer(root.transform, "Plate/Lid");
                SpriteRenderer ingredient = FindRenderer(root.transform, "Ingredient");
                SpriteRenderer ice = EnsureIce(root.transform, plate, iceSprite);
                if (iceMaterial != null)
                    ice.sharedMaterial = iceMaterial;
                TextMeshPro blockText = EnsureBlockText(root.transform, plate);
                DishSelectionOutline outline = root.GetComponent<DishSelectionOutline>();
                if (food != null)
                {
                    SetFields(food, ("iconRenderer", ingredient), ("plateRenderer", plate), ("lidRenderer", lid),
                        ("selection", outline), ("countCanvas", root.GetComponentInChildren<Canvas>(true)),
                        ("iceRenderer", ice), ("blockText", blockText));
                    SetArray(food, "visualRenderers", root.GetComponentsInChildren<SpriteRenderer>(true));
                }
                if (outline != null)
                    SetFields(outline, ("lid", lid));
                BindShadow(ingredient, plate);
                PrefabUtility.SaveAsPrefabAsset(root, DishPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // EN: Returns the ice surface material using the FoodPop/IceSurface shader, creating it when missing.
        // VI: Trả về material bề mặt băng dùng shader FoodPop/IceSurface, tạo mới khi chưa có.
        private static Material GetOrCreateIceMaterial()
        {
            Shader shader = Shader.Find(IceShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[CustomerSetup] Không tìm thấy shader {IceShaderName}.");
                return null;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(IceMaterialPath);
            if (material == null)
            {
                FoodLandEffectBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(IceMaterialPath).Replace('\\', '/'));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, IceMaterialPath);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        // EN: Builds an ice shard burst: crystal shards flung out in all directions, spinning, falling with gravity and fading; saved as a ParticleEffect prefab.
        // VI: Dựng hiệu ứng vụn băng: các mảnh tinh thể văng ra mọi hướng, xoay tròn, rơi theo trọng lực và mờ dần; lưu thành prefab ParticleEffect.
        private static ParticleEffect BuildIceShardPrefab(string name, string path, short count, float speed, float minSize, float maxSize)
        {
            Texture2D texture = FoodLandEffectBuilder.ImportEffectTexture(IceShardTexturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[CustomerSetup] Thiếu {IceShardTexturePath}.");
                return null;
            }
            Material material = FoodLandEffectBuilder.GetOrCreateMaterial("IceShard", texture);
            GameObject root = new GameObject(name);
            try
            {
                ParticleSystem system = root.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 0.6f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
                main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.8f, 0.93f, 1f));
                main.gravityModifier = 1.4f;
                main.maxParticles = 40;
                FoodLandEffectBuilder.SetBurst(system, count);
                ParticleSystem.ShapeModule shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.25f;
                shape.arc = 360f;
                shape.rotation = Vector3.zero;
                ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 4f, Mathf.PI * 4f);
                FoodLandEffectBuilder.SetSizeOverLifetime(system, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f)));
                FoodLandEffectBuilder.SetFadeOut(system, Color.white, 1f, 0.6f);
                FoodLandEffectBuilder.ApplyRenderer(system, material, 0);
                FoodLandEffectBuilder.EnsureFolder("Assets/HotPot/Prefabs/Effects");
                return FoodLandEffectBuilder.SaveEffectPrefab(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // EN: Returns the dish's "Ice" overlay (created when missing), sized to cover the plate and hidden by default.
        // VI: Trả về lớp băng "Ice" của đĩa (tạo mới nếu chưa có), kích thước phủ kín đĩa và mặc định ẩn.
        private static SpriteRenderer EnsureIce(Transform root, SpriteRenderer plate, Sprite sprite)
        {
            SpriteRenderer ice = FindRenderer(root, "Ice");
            if (ice == null)
            {
                ice = new GameObject("Ice").AddComponent<SpriteRenderer>();
                ice.transform.SetParent(root, false);
            }
            ice.sprite = sprite;
            if (plate != null && sprite != null)
            {
                // The ice art is flatter than the plate art (opaque area 241x174 px vs 248x222 px on the same 256 px canvas),
                // so stretch it a little wider and noticeably taller to cover the whole plate, rim and base included.
                ice.transform.position = plate.bounds.center;
                float baseScale = plate.bounds.size.x / Mathf.Max(0.01f, sprite.bounds.size.x) / Mathf.Max(0.0001f, root.lossyScale.x);
                ice.transform.localScale = new Vector3(baseScale * IceCoverScaleX, baseScale * IceCoverScaleY, 1f);
                ice.sortingLayerID = plate.sortingLayerID;
                ice.sortingOrder = plate.sortingOrder + 5;
            }
            ice.gameObject.SetActive(false);
            return ice;
        }

        // EN: Returns the dish's "BlockValue" text (created when missing): a bold dark-blue number at the plate centre, hidden by default.
        // VI: Trả về text "BlockValue" của đĩa (tạo mới nếu chưa có): số xanh đậm in đậm ở giữa đĩa, mặc định ẩn.
        private static TextMeshPro EnsureBlockText(Transform root, SpriteRenderer plate)
        {
            Transform existing = root.Find("BlockValue");
            TextMeshPro text = existing != null ? existing.GetComponent<TextMeshPro>() : null;
            if (text == null)
            {
                GameObject go = new GameObject("BlockValue");
                go.transform.SetParent(root, false);
                text = go.AddComponent<TextMeshPro>();
            }
            text.text = "1";
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color32(20, 70, 120, 255);
            if (text.font != null)
                text.fontSharedMaterial = text.font.material;
            if (plate != null)
            {
                text.transform.position = plate.bounds.center;
                float worldHeight = plate.bounds.size.y * 0.55f;
                text.fontSize = worldHeight / Mathf.Max(0.0001f, root.lossyScale.y) * 10f;
                text.rectTransform.sizeDelta = new Vector2(plate.bounds.size.x, plate.bounds.size.y) / Mathf.Max(0.0001f, root.lossyScale.x);
                text.sortingLayerID = plate.sortingLayerID;
                text.sortingOrder = plate.sortingOrder + 6;
            }
            text.gameObject.SetActive(false);
            return text;
        }

        // EN: Gives every conveyor slot in the scene its own Food component and wires the slot, food and shadow references.
        // VI: Thêm component Food riêng cho mọi slot băng chuyền trong scene và gán tham chiếu của slot, food và bóng.
        private static void BindConveyorSlots()
        {
            FoodIconConfig config = AssetDatabase.LoadAssetAtPath<FoodIconConfig>(FoodIconConfigPath);
            foreach (ConveyorSlot slot in Object.FindObjectsByType<ConveyorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Food food = GetOrAdd<Food>(slot.gameObject);
                SpriteRenderer ingredient = FindRenderer(slot.transform, "Ingredient");
                SpriteRenderer plate = FindRenderer(slot.transform, "Plate");
                SetFields(food, ("iconRenderer", ingredient), ("plateRenderer", plate), ("foodIconConfig", config));
                SetFields(slot, ("ingredientRenderer", ingredient), ("slotFood", food));
                BindShadow(ingredient, plate);
            }
        }

        // EN: Lists the dishes placed in the board columns and links the fly animator on the board manager.
        // VI: Liệt kê các đĩa đặt sẵn trong các cột board và gán animator bay cho board manager.
        private static void BindFoodBoard()
        {
            FoodBoardManager board = Object.FindFirstObjectByType<FoodBoardManager>(FindObjectsInactive.Include);
            if (board == null)
                return;
            SerializedObject serialized = new SerializedObject(board);
            SerializedProperty columns = serialized.FindProperty("ingredientColumns");
            List<Food> dishes = new List<Food>();
            for (int i = 0; i < columns.arraySize; i++)
            {
                if (columns.GetArrayElementAtIndex(i).objectReferenceValue is Transform column)
                    dishes.AddRange(column.GetComponentsInChildren<Food>(true));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetFields(board, ("flyAnimator", board.GetComponent<FoodFlyAnimator>()),
                ("iceHitEffect", BuildIceShardPrefab("IceHit", IceHitPrefabPath, 7, 2.2f, 0.12f, 0.22f)),
                ("iceBreakEffect", BuildIceShardPrefab("IceBreak", IceBreakPrefabPath, 18, 3.6f, 0.16f, 0.32f)));
            SetArray(board, "sceneDishes", dishes.ToArray());
        }

        // EN: Assigns customer configs to the manager and links each lane to its own serving component and order view.
        // VI: Gán cấu hình khách cho manager và nối từng làn với component phục vụ riêng cùng thẻ order.
        private static CustomerManager BindCustomers(Sprite checkmark)
        {
            Transform group = FindSceneRoot(name => name.Contains("Customers and orders"));
            if (group == null)
            {
                Debug.LogWarning("[CustomerSetup] Không tìm thấy nhóm 'Customers and orders' trong scene.");
                return null;
            }
            CustomerManager manager = Object.FindFirstObjectByType<CustomerManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                GameObject go = new GameObject("CustomerManager");
                Undo.RegisterCreatedObjectUndo(go, "Create CustomerManager");
                manager = go.AddComponent<CustomerManager>();
            }
            SetFields(manager,
                ("conveyor", Object.FindFirstObjectByType<ConveyorChefMovement>(FindObjectsInactive.Include)),
                ("finalFoodConfig", LoadFinalFoodConfig()));
            SetFields(FoodLandEffectBuilder.EnsureSoundManager(),
                ("serveSound", FoodLandEffectBuilder.ImportSound($"{SoundFolder}/CustomerServePop.wav")),
                ("customerHappySound", FoodLandEffectBuilder.ImportSound($"{SoundFolder}/CustomerHappy.wav")));
            List<Transform> customers = group.Cast<Transform>().Where(child => child.name.StartsWith("Customer")).OrderBy(child => child.position.x).ToList();
            Transform waitingGroup = group.Cast<Transform>().FirstOrDefault(child => child.name.StartsWith("Waiting"));
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty lanes = serialized.FindProperty("lanes");
            lanes.arraySize = 2;
            string[] sides = { "Left", "Right" };
            List<ServingTrayView> trays = Object.FindObjectsByType<ServingTrayView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(tray => tray.transform.position.x).ToList();
            for (int side = 0; side < 2; side++)
            {
                Transform customer = side < customers.Count ? customers[side] : null;
                Transform orderCard = group.Cast<Transform>().FirstOrDefault(child => child.name.StartsWith($"{sides[side]} order"));
                SerializedProperty lane = lanes.GetArrayElementAtIndex(side);
                BindLane(lane, customer, orderCard, waitingGroup, sides[side], side == 0 ? -1 : 1, checkmark);
                BindServingTray(lane, trays.Count == 2 ? trays[side] : null);
            }
            serialized.ApplyModifiedProperties();
            return manager;
        }

        // EN: Wires one lane: adds CustomerView to the customer, sets up the OrderView of its order card and collects the waiting figures nearest to the counter first (the serving tray is bound separately).
        // VI: Gán một làn: thêm CustomerView cho khách, dựng OrderView cho thẻ order và gom các khách đứng chờ theo thứ tự gần quầy trước (khay phục vụ được gán riêng).
        private static void BindLane(SerializedProperty lane, Transform customer, Transform orderCard, Transform waitingGroup, string side, int direction, Sprite checkmark)
        {
            CustomerView view = null;
            SpriteRenderer portrait = null;
            if (customer != null)
            {
                view = GetOrAdd<CustomerView>(customer.gameObject);
                portrait = FindPortrait(customer);
                SetFields(view, ("portrait", portrait), ("orderCard", orderCard));
            }
            lane.FindPropertyRelative("customer").objectReferenceValue = view;
            lane.FindPropertyRelative("slideDirection").intValue = direction;

            OrderView orderView = orderCard != null ? BindOrderView(orderCard, checkmark) : null;
            lane.FindPropertyRelative("orderView").objectReferenceValue = orderView;

            List<SpriteRenderer> waiting = new List<SpriteRenderer>();
            if (waitingGroup != null && customer != null)
            {
                waiting = waitingGroup.Cast<Transform>()
                    .Where(child => child.name.StartsWith($"{side} queue"))
                    .OrderBy(child => Vector3.Distance(child.position, customer.position))
                    .Select(FindPortrait)
                    .Where(renderer => renderer != null).ToList();
            }
            SetArrayProperty(lane.FindPropertyRelative("waitingFigures"), waiting.Cast<Object>().ToArray());

            List<Sprite> portraits = new List<Sprite>();
            if (portrait != null && portrait.sprite != null)
                portraits.Add(portrait.sprite);
            portraits.AddRange(waiting.Select(renderer => renderer.sprite).Where(sprite => sprite != null));
            SetArrayProperty(lane.FindPropertyRelative("portraits"), portraits.Cast<Object>().ToArray());
        }

        // EN: Loads FinalFoodConfig, building it from the built-in recipes when the asset is missing.
        // VI: Nạp FinalFoodConfig, tự dựng từ công thức có sẵn nếu chưa có asset.
        private static FinalFoodConfig LoadFinalFoodConfig()
        {
            FinalFoodConfig config = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>(FinalFoodConfigPath);
            if (config == null)
            {
                FinalFoodConfigMenu.Build();
                config = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>(FinalFoodConfigPath);
            }
            return config;
        }

        // EN: Returns exactly count order slots of the card (children named "Order…"): duplicates the last one when there are fewer (prefab instances stay linked) and deletes the extra ones (with Undo).
        // VI: Trả về đúng count ô order của thẻ (child tên "Order…"): nhân bản ô cuối khi thiếu (bản sao prefab vẫn giữ liên kết) và xoá các ô thừa (có Undo).
        private static List<Transform> EnsureOrderLines(Transform orderCard, int count)
        {
            List<Transform> lines = orderCard.Cast<Transform>().Where(child => child.name.StartsWith("Order")).ToList();
            if (lines.Count == 0)
            {
                Debug.LogWarning($"[CustomerSetup] Thẻ {orderCard.name} không có dòng order nào để nhân bản.");
                return lines;
            }
            while (lines.Count < count)
                lines.Add(DuplicateOrderLine(lines[lines.Count - 1], lines.Count + 1));
            while (lines.Count > count)
            {
                Undo.DestroyObjectImmediate(lines[lines.Count - 1].gameObject);
                lines.RemoveAt(lines.Count - 1);
            }
            return lines;
        }

        // EN: Creates a copy of an order line next to it (as a prefab instance when the source is one) and names it by its slot number.
        // VI: Tạo bản sao của một dòng order ngay cạnh nó (dạng prefab instance nếu dòng gốc là prefab) và đặt tên theo số thứ tự ô.
        private static Transform DuplicateOrderLine(Transform source, int number)
        {
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject);
            GameObject copy = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, source.parent)
                : Object.Instantiate(source.gameObject, source.parent);
            Undo.RegisterCreatedObjectUndo(copy, "Create Order Line");
            copy.name = $"Order · Slot {number}";
            copy.transform.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            copy.transform.localScale = source.localScale;
            copy.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
            return copy.transform;
        }

        // EN: Binds a lane to the tray's own ServingFood and ensures its stack and serving components exist on the prefab.
        // VI: Nối làn với ServingFood riêng của khay và bảo đảm component chồng cùng phục vụ có trên prefab.
        private static void BindServingTray(SerializedProperty lane, ServingTrayView tray)
        {
            SerializedProperty property = lane.FindPropertyRelative("servingFood");
            if (property == null)
                return;
            if (tray == null)
            {
                ServingFood existing = property.objectReferenceValue as ServingFood;
                tray = existing != null ? existing.GetComponent<ServingTrayView>() : null;
            }
            if (tray == null)
            {
                Debug.LogWarning("[CustomerSetup] Không ghép được khay phục vụ cho làn; hãy gán ServingFood tay trên CustomerManager.");
                return;
            }
            TrayDishStack stack = EnsureTrayDishStack(tray);
            if (stack != null && tray.DishStack != stack)
                SetFields(tray, ("dishStack", stack));
            property.objectReferenceValue = EnsureServingFood(tray);
        }

        // EN: Adds the serving component to the tray prefab when missing so both scene instances keep independent serving resources.
        // VI: Thêm component phục vụ vào prefab khay khi thiếu để hai instance trong scene giữ tài nguyên phục vụ độc lập.
        private static ServingFood EnsureServingFood(ServingTrayView tray)
        {
            ServingFood serving = tray.GetComponent<ServingFood>();
            if (serving != null)
                return serving;
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tray.gameObject);
            if (string.IsNullOrEmpty(path))
            {
                serving = Undo.AddComponent<ServingFood>(tray.gameObject);
                SetFields(serving, ("tray", tray),
                    ("ingameConfig", AssetDatabase.LoadAssetAtPath<IngameConfig>("Assets/HotPot/Config/Resources/IngameConfig.asset")),
                    ("dishCompleteEffect", AssetDatabase.LoadAssetAtPath<ParticleEffect>(DishCompleteEffectPath)));
                return serving;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (ServingTrayView prefabTray in root.GetComponentsInChildren<ServingTrayView>(true))
                {
                    ServingFood prefabServing = prefabTray.GetComponent<ServingFood>();
                    if (prefabServing == null)
                        prefabServing = prefabTray.gameObject.AddComponent<ServingFood>();
                    SetFields(prefabServing, ("tray", prefabTray),
                        ("ingameConfig", AssetDatabase.LoadAssetAtPath<IngameConfig>("Assets/HotPot/Config/Resources/IngameConfig.asset")),
                        ("dishCompleteEffect", AssetDatabase.LoadAssetAtPath<ParticleEffect>(DishCompleteEffectPath)));
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return tray.GetComponent<ServingFood>();
        }

        // EN: Returns the tray's TrayDishStack; when it is missing and the tray is a prefab instance, adds it to the prefab asset (both components linked to each other) so every instance gets it, otherwise to the scene object.
        // VI: Trả về TrayDishStack của khay; nếu chưa có và khay là prefab instance thì thêm vào prefab gốc (hai component nối với nhau) để mọi instance đều có, ngược lại thêm vào object trong scene.
        private static TrayDishStack EnsureTrayDishStack(ServingTrayView tray)
        {
            TrayDishStack stack = tray.GetComponent<TrayDishStack>();
            if (stack != null)
                return stack;
            ServingTrayView source = PrefabUtility.GetCorrespondingObjectFromSource(tray);
            string path = source != null ? AssetDatabase.GetAssetPath(source) : null;
            if (string.IsNullOrEmpty(path))
            {
                stack = Undo.AddComponent<TrayDishStack>(tray.gameObject);
                SetFields(stack, ("tray", tray));
                SetFields(tray, ("dishStack", stack));
                return stack;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (ServingTrayView prefabTray in root.GetComponentsInChildren<ServingTrayView>(true))
                {
                    TrayDishStack prefabStack = prefabTray.GetComponent<TrayDishStack>();
                    if (prefabStack == null)
                        prefabStack = prefabTray.gameObject.AddComponent<TrayDishStack>();
                    SetFields(prefabStack, ("tray", prefabTray));
                    SetFields(prefabTray, ("dishStack", prefabStack));
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[CustomerSetup] Đã thêm TrayDishStack vào prefab {path}.");
            return tray.GetComponent<TrayDishStack>();
        }

        // EN: Adds OrderView to an order card, removes leftovers of older card layouts, makes sure it has exactly CustomerOrder.MaxDishes slots (OrderItemView + checkmark each), wires them and the "Artwork" background, and previews the layout with every slot.
        // VI: Thêm OrderView cho thẻ order, xoá phần còn lại của bố cục thẻ cũ, bảo đảm thẻ có đúng CustomerOrder.MaxDishes ô (mỗi ô có OrderItemView và dấu tích), gán các ô và nền "Artwork", rồi xem trước bố cục với đủ ô.
        private static OrderView BindOrderView(Transform orderCard, Sprite checkmark)
        {
            OrderView orderView = GetOrAdd<OrderView>(orderCard.gameObject);
            RemoveChild(orderCard, "FinalFood");
            RemoveChild(orderCard, "DishCount");
            List<OrderItemView> items = new List<OrderItemView>();
            foreach (Transform line in EnsureOrderLines(orderCard, OrderLinesPerCard))
                items.Add(BindOrderItem(line, checkmark));
            SetArray(orderView, "items", items.Cast<Object>().ToArray());
            SpriteRenderer background = FindRenderer(orderCard, "Artwork");
            if (background != null)
                SetFields(orderView, ("background", background));
            List<Object> touched = items.SelectMany(item => new Object[] { item.transform, item.gameObject }).ToList();
            if (background != null)
                touched.AddRange(new Object[] { background, background.transform });
            Undo.RecordObjects(touched.ToArray(), "Layout order slots");
            OrderViewSetup.Setup(orderView);
            orderView.Layout(items.Count);
            return orderView;
        }

        // EN: Deletes (with Undo) a child left by an earlier card layout, such as the old single-dish picture or dish counter.
        // VI: Xoá (có Undo) child còn lại từ bố cục thẻ cũ, như ảnh món đơn hoặc nhãn đếm món trước đây.
        private static void RemoveChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        // EN: Adds OrderItemView to an order line, wires its icon and count label and creates a hidden checkmark at the icon's lower-right corner.
        // VI: Thêm OrderItemView cho một dòng order, gán icon và nhãn số lượng, và tạo dấu tích (ẩn) ở góc dưới bên phải của icon.
        private static OrderItemView BindOrderItem(Transform line, Sprite checkmarkSprite)
        {
            OrderItemView item = GetOrAdd<OrderItemView>(line.gameObject);
            SpriteRenderer ingredient = FindRenderer(line, "Ingredient");
            SpriteRenderer card = FindRenderer(line, "Card");
            TMP_Text text = line.GetComponentInChildren<TMP_Text>(true);
            GameObject countRoot = text != null ? TopChildOf(line, text.transform).gameObject : null;
            SpriteRenderer checkmark = EnsureCheckmark(line, ingredient, card, checkmarkSprite);
            SpriteRenderer dishShadow = EnsureDishShadow(ingredient);
            SetFields(item, ("ingredientRenderer", ingredient), ("countText", text), ("countRoot", countRoot), ("checkmark", checkmark), ("dishShadow", dishShadow));
            return item;
        }

        // EN: Returns the "DishShadow" child of an order slot's icon (creating it when missing), drawn just behind the icon; OrderItemView fills its sprite, colour and offset at runtime.
        // VI: Trả về child "DishShadow" của icon trong ô order (tạo mới nếu chưa có), vẽ ngay sau icon; OrderItemView gán sprite, màu và độ lệch khi chạy.
        private static SpriteRenderer EnsureDishShadow(SpriteRenderer ingredient)
        {
            if (ingredient == null)
                return null;
            SpriteRenderer shadow = FindRenderer(ingredient.transform, "DishShadow");
            if (shadow == null)
            {
                GameObject go = new GameObject("DishShadow");
                Undo.RegisterCreatedObjectUndo(go, "Create Dish Shadow");
                go.transform.SetParent(ingredient.transform, false);
                shadow = go.AddComponent<SpriteRenderer>();
            }
            shadow.sortingLayerID = ingredient.sortingLayerID;
            shadow.sortingOrder = ingredient.sortingOrder - 1;
            shadow.gameObject.layer = ingredient.gameObject.layer;
            shadow.color = new Color(0f, 0f, 0f, 0.35f);
            shadow.enabled = false;
            return shadow;
        }

        // EN: Returns the existing "Checkmark" child or creates one, sized to about half the card and drawn above the icon.
        // VI: Trả về child "Checkmark" có sẵn hoặc tạo mới, kích thước khoảng nửa thẻ và vẽ trên icon.
        private static SpriteRenderer EnsureCheckmark(Transform line, SpriteRenderer ingredient, SpriteRenderer card, Sprite sprite)
        {
            SpriteRenderer checkmark = FindRenderer(line, "Checkmark");
            if (checkmark == null)
            {
                GameObject go = new GameObject("Checkmark");
                Undo.RegisterCreatedObjectUndo(go, "Create Checkmark");
                go.transform.SetParent(line, false);
                checkmark = go.AddComponent<SpriteRenderer>();
            }
            checkmark.sprite = sprite;
            SpriteRenderer reference = ingredient != null ? ingredient : card;
            if (reference != null)
            {
                checkmark.sortingLayerID = reference.sortingLayerID;
                checkmark.sortingOrder = reference.sortingOrder + 5;
                checkmark.gameObject.layer = reference.gameObject.layer;
            }
            Bounds area = card != null ? card.bounds : reference != null ? reference.bounds : new Bounds(line.position, Vector3.one);
            if (sprite != null)
            {
                float worldSize = area.size.x * 0.5f;
                float scale = worldSize / Mathf.Max(0.01f, sprite.bounds.size.x) / Mathf.Max(0.0001f, line.lossyScale.x);
                checkmark.transform.localScale = new Vector3(scale, scale, 1f);
            }
            checkmark.transform.position = new Vector3(area.max.x - area.size.x * 0.22f, area.min.y + area.size.y * 0.22f, line.position.z);
            checkmark.gameObject.SetActive(false);
            return checkmark;
        }

        // EN: Links the level controller to the board and customer managers.
        // VI: Gán board manager và customer manager cho level controller.
        private static void BindLevelController(CustomerManager manager)
        {
            LevelController controller = Object.FindFirstObjectByType<LevelController>(FindObjectsInactive.Include);
            if (controller == null)
                return;
            SetFields(controller, ("boardManager", Object.FindFirstObjectByType<FoodBoardManager>(FindObjectsInactive.Include)), ("customerManager", manager));
        }

        // EN: Wires the IngredientShadow on an ingredient renderer: itself, its "IngredientShadow" child and the plate.
        // VI: Gán IngredientShadow trên renderer nguyên liệu: chính nó, child "IngredientShadow" và đĩa.
        private static void BindShadow(SpriteRenderer ingredient, SpriteRenderer plate)
        {
            if (ingredient == null)
                return;
            IngredientShadow shadow = ingredient.GetComponent<IngredientShadow>();
            if (shadow != null)
                SetFields(shadow, ("ingredient", ingredient), ("shadow", FindRenderer(ingredient.transform, "IngredientShadow")), ("plate", plate));
        }

        // EN: Returns the SpriteRenderer at a child path, or null.
        // VI: Trả về SpriteRenderer tại đường dẫn child, hoặc null.
        private static SpriteRenderer FindRenderer(Transform root, string path)
        {
            Transform child = root.Find(path);
            SpriteRenderer renderer = child != null ? child.GetComponent<SpriteRenderer>() : null;
            return renderer != null ? renderer : null;
        }

        // EN: Returns the customer's "Portrait" renderer, or the first sprite renderer under it.
        // VI: Trả về renderer "Portrait" của khách, hoặc sprite renderer đầu tiên bên dưới.
        private static SpriteRenderer FindPortrait(Transform customer)
        {
            SpriteRenderer portrait = FindRenderer(customer, "Portrait");
            return portrait != null ? portrait : customer.GetComponentInChildren<SpriteRenderer>(true);
        }

        // EN: Returns the component on the GameObject, adding it (with undo) when missing.
        // VI: Trả về component trên GameObject, thêm mới (có undo) nếu chưa có.
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        // EN: Returns the direct child of root that contains the given descendant.
        // VI: Trả về child trực tiếp của root chứa đối tượng con cháu đã cho.
        private static Transform TopChildOf(Transform root, Transform descendant)
        {
            Transform current = descendant;
            while (current.parent != null && current.parent != root)
                current = current.parent;
            return current;
        }

        // EN: Finds a root GameObject of the active scene (or a direct child of one) whose name matches.
        // VI: Tìm GameObject gốc của scene đang mở (hoặc con trực tiếp của nó) có tên phù hợp.
        private static Transform FindSceneRoot(System.Func<string, bool> match)
        {
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (match(root.name))
                    return root.transform;
                foreach (Transform child in root.transform)
                {
                    if (match(child.name))
                        return child;
                }
            }
            return null;
        }

        // EN: Sets object-reference fields on a component through SerializedObject (recorded for prefab overrides and undo).
        // VI: Gán các trường tham chiếu của component qua SerializedObject (ghi nhận override prefab và undo).
        private static void SetFields(Object target, params (string name, Object value)[] fields)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach ((string name, Object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null)
                    Debug.LogWarning($"[CustomerSetup] {target.GetType().Name} không có trường {name}.");
                else
                    property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedProperties();
        }

        // EN: Replaces an object-reference array field on a component.
        // VI: Thay mảng tham chiếu của một trường trên component.
        private static void SetArray(Object target, string name, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SetArrayProperty(serialized.FindProperty(name), values);
            serialized.ApplyModifiedProperties();
        }

        // EN: Fills a serialized array or list property with object references.
        // VI: Điền mảng hoặc list đã serialize bằng các tham chiếu.
        private static void SetArrayProperty(SerializedProperty property, Object[] values)
        {
            if (property == null)
                return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
