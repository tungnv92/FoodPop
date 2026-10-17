using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that builds the customer feedback assets (heart burst, checkmark, sounds) and wires every direct reference the runtime scripts need, so no runtime code has to search children or call GetComponent.
    // VI: Công cụ editor dựng asset phản hồi khách (tim, dấu tích, âm thanh) và gán mọi tham chiếu trực tiếp mà script runtime cần, để code runtime không phải tìm child hay gọi GetComponent.
    public static class CustomerSetup
    {
        private const string HeartTexturePath = "Assets/HotPot/Art/Sprites/Effects/FxHeart.png";
        private const string CheckmarkPath = "Assets/HotPot/Art/Sprites/Orders/OrderCheckmark.png";
        private const string HeartPrefabPath = "Assets/HotPot/Prefabs/Effects/CustomerHearts.prefab";
        private const string SoundFolder = "Assets/HotPot/Audio/SFX/Customer";
        private const string DishPrefabPath = "Assets/HotPot/Prefabs/Ingame/IngredientDish.prefab";
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";

        // EN: Runs every step: assets, ingredient dish prefab, conveyor slots, board, customers and the level controller; saves the scene when it had no other unsaved changes.
        // VI: Chạy mọi bước: asset, prefab đĩa nguyên liệu, slot băng chuyền, board, khách và level controller; tự lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Setup Customers And References")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            ParticleEffect hearts = BuildHeartPrefab();
            Sprite checkmark = ImportSprite(CheckmarkPath);
            BindDishPrefab();
            BindConveyorSlots();
            BindFoodBoard();
            CustomerManager manager = BindCustomers(hearts, checkmark);
            BindLevelController(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log("[CustomerSetup] Đã dựng hiệu ứng khách và gán toàn bộ tham chiếu trực tiếp.");
        }

        // EN: Builds the reward heart burst: a handful of hearts popping up and floating away, saved as a ParticleEffect prefab.
        // VI: Dựng hiệu ứng tim khen thưởng: vài trái tim bung lên rồi bay lơ lửng, lưu thành prefab ParticleEffect.
        private static ParticleEffect BuildHeartPrefab()
        {
            Texture2D texture = FoodLandEffectBuilder.ImportEffectTexture(HeartTexturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[CustomerSetup] Thiếu {HeartTexturePath}.");
                return null;
            }
            Material material = FoodLandEffectBuilder.GetOrCreateMaterial("CustomerHeart", texture);
            GameObject root = new GameObject("CustomerHearts");
            try
            {
                ConfigureHearts(root.AddComponent<ParticleSystem>(), material);
                FoodLandEffectBuilder.EnsureFolder("Assets/HotPot/Prefabs/Effects");
                return FoodLandEffectBuilder.SaveEffectPrefab(root, HeartPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // EN: Hearts: one burst of 10, launched upward in a cone, slowing down, swaying and fading while they grow then shrink.
        // VI: Tim: một đợt 10 trái bắn lên theo hình nón, chậm dần, đung đưa và mờ dần, phóng to rồi thu nhỏ.
        private static void ConfigureHearts(ParticleSystem system, Material material)
        {
            ParticleSystem.MainModule main = FoodLandEffectBuilder.ApplyCommonMain(system, 1f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            main.gravityModifier = -0.15f;
            FoodLandEffectBuilder.SetBurst(system, 10);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.6f;
            limit.dampen = 0.15f;
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.8f;
            FoodLandEffectBuilder.SetSizeOverLifetime(system, new AnimationCurve(
                new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1.15f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.6f)));
            FoodLandEffectBuilder.SetFadeOut(system, Color.white, 1f, 0.6f);
            FoodLandEffectBuilder.ApplyRenderer(system, material, 0);
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
            GameObject root = PrefabUtility.LoadPrefabContents(DishPrefabPath);
            try
            {
                Food food = root.GetComponent<Food>();
                SpriteRenderer plate = FindRenderer(root.transform, "Plate");
                SpriteRenderer lid = FindRenderer(root.transform, "Plate/Lid");
                SpriteRenderer ingredient = FindRenderer(root.transform, "Ingredient");
                DishSelectionOutline outline = root.GetComponent<DishSelectionOutline>();
                if (food != null)
                {
                    SetFields(food, ("iconRenderer", ingredient), ("plateRenderer", plate), ("lidRenderer", lid),
                        ("selection", outline), ("countCanvas", root.GetComponentInChildren<Canvas>(true)));
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
            SetFields(board, ("flyAnimator", board.GetComponent<FoodFlyAnimator>()));
            SetArray(board, "sceneDishes", dishes.ToArray());
        }

        // EN: Creates (or reuses) the CustomerManager and wires both lanes from the "Customers and orders" group: customer, order card lines with a checkmark each, waiting figures and portraits.
        // VI: Tạo (hoặc dùng lại) CustomerManager và gán hai làn từ nhóm "Customers and orders": khách, các dòng thẻ order kèm dấu tích, khách đứng chờ và ảnh khách.
        private static CustomerManager BindCustomers(ParticleEffect hearts, Sprite checkmark)
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
                ("foodIconConfig", AssetDatabase.LoadAssetAtPath<FoodIconConfig>(FoodIconConfigPath)),
                ("heartEffectPrefab", hearts),
                ("serveSound", FoodLandEffectBuilder.ImportSound($"{SoundFolder}/CustomerServePop.wav")),
                ("itemDoneSound", FoodLandEffectBuilder.ImportSound($"{SoundFolder}/OrderItemDone.wav")),
                ("customerHappySound", FoodLandEffectBuilder.ImportSound($"{SoundFolder}/CustomerHappy.wav")));
            List<Transform> customers = group.Cast<Transform>().Where(child => child.name.StartsWith("Customer")).OrderBy(child => child.position.x).ToList();
            Transform waitingGroup = group.Cast<Transform>().FirstOrDefault(child => child.name.StartsWith("Waiting"));
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty lanes = serialized.FindProperty("lanes");
            lanes.arraySize = 2;
            string[] sides = { "Left", "Right" };
            for (int side = 0; side < 2; side++)
            {
                Transform customer = side < customers.Count ? customers[side] : null;
                Transform orderCard = group.Cast<Transform>().FirstOrDefault(child => child.name.StartsWith($"{sides[side]} order"));
                BindLane(lanes.GetArrayElementAtIndex(side), customer, orderCard, waitingGroup, sides[side], side == 0 ? -1 : 1, checkmark);
            }
            serialized.ApplyModifiedProperties();
            return manager;
        }

        // EN: Wires one lane: adds CustomerView to the customer, OrderItemView (with a checkmark child) to each order line, and collects the waiting figures nearest to the counter first.
        // VI: Gán một làn: thêm CustomerView cho khách, OrderItemView (kèm child dấu tích) cho từng dòng order, và gom các khách đứng chờ theo thứ tự gần quầy trước.
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

            List<OrderItemView> items = new List<OrderItemView>();
            if (orderCard != null)
            {
                foreach (Transform line in orderCard)
                {
                    if (line.name.StartsWith("Order"))
                        items.Add(BindOrderItem(line, checkmark));
                }
            }
            SetArrayProperty(lane.FindPropertyRelative("orderItems"), items.Cast<Object>().ToArray());

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
            SetFields(item, ("ingredientRenderer", ingredient), ("countText", text), ("countRoot", countRoot), ("checkmark", checkmark));
            return item;
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
