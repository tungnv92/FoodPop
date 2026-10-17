using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FoodPop
{
    // EN: Editor tool that places a small prep table and a cutting board on it in front of the chef, splits the conveyor background into floor and belt layers, orders all kitchen layers, adds the food anchor on the board's top surface and wires board, anchor and food contact shadow into ChefManager.
    // VI: Công cụ editor đặt một chiếc bàn nhỏ và thớt trên bàn trước mặt đầu bếp, tách nền băng chuyền thành lớp sàn và lớp băng, xếp lại thứ tự các lớp bếp, thêm điểm đặt món trên mặt thớt và gán thớt, điểm đặt món cùng bóng đổ của món vào ChefManager.
    public static class CuttingBoardSetup
    {
        private const string BoardSpritePath = "Assets/HotPot/Art/Sprites/Kitchen/CuttingBoard.png";
        private const string TableSpritePath = "Assets/HotPot/Art/Sprites/Kitchen/PrepTable.png";
        private const string TableName = "PrepTable";
        private const string ConveyorSpritePath = "Assets/HotPot/Art/Sprites/Demo/ConveyorStandalone.png";
        private const string FloorSpritePath = "Assets/HotPot/Art/Sprites/Demo/ConveyorStandaloneFloor.png";
        private const string BeltSpritePath = "Assets/HotPot/Art/Sprites/Demo/ConveyorStandaloneBelt.png";
        private const string RearChildName = "Rear";
        private const string LegacyBeltChildName = "Belt";
        private const float TableWidthOfBoard = 1.3f;
        // EN: Fractions of the sprite heights measured from the artwork: the board's top-surface centre above its pivot, and how far below the table's pivot that surface should sit (so the board rests slightly towards the front of the table top).
        // VI: Tỉ lệ theo chiều cao sprite đo từ ảnh vẽ: tâm mặt thớt cao hơn pivot của thớt bao nhiêu, và mặt đó nằm cách pivot của bàn bao nhiêu (để thớt nằm hơi lệch về phía trước mặt bàn).
        private const float BoardSurfaceAbovePivot = 0.05f;
        private const float BoardSurfaceAboveTablePivot = 0.15f;
        private const string ShadowSpritePath = "Assets/HotPot/Art/Sprites/Foods/IngredientContactShadow.png";
        private const string BoardName = "CuttingBoard";
        private const string AnchorName = "FoodAnchor";
        private const float WidthOfChef = 0.57f;
        private const float HeightFromChefBottom = 0.17f;
        private const float AnchorLocalY = 0.2f;

        // EN: Menu entry: creates the board and the table under it (or re-places existing ones) and assigns the board to ChefManager; the scene is marked dirty for saving.
        // VI: Mục menu: tạo thớt và bàn đỡ bên dưới (hoặc đặt lại nếu đã có) và gán thớt vào ChefManager; scene được đánh dấu cần lưu.
        [MenuItem("Tools/HotPot/Create Cutting Board")]
        public static void Create()
        {
            ChefManager chef = Object.FindFirstObjectByType<ChefManager>(FindObjectsInactive.Include);
            if (chef == null)
            {
                Debug.LogWarning("[CuttingBoardSetup] Scene chưa có ChefManager.");
                return;
            }
            SerializedObject serialized = new SerializedObject(chef);
            Animator animator = serialized.FindProperty("chefAnimator").objectReferenceValue as Animator;
            SpriteRenderer chefRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
            if (chefRenderer == null)
            {
                Debug.LogWarning("[CuttingBoardSetup] Không tìm thấy SpriteRenderer của đầu bếp trên Chef Animator.");
                return;
            }
            Sprite boardSprite = ImportSprite(BoardSpritePath);
            Sprite tableSprite = ImportSprite(TableSpritePath);
            if (boardSprite == null)
            {
                Debug.LogWarning($"[CuttingBoardSetup] Thiếu sprite {BoardSpritePath}.");
                return;
            }

            Transform board = GetOrCreateBoard(serialized, chefRenderer, boardSprite);
            Transform anchor = GetOrCreateAnchor(board);
            if (tableSprite != null)
                PlaceTable(board, tableSprite);
            SpriteRenderer belt = SplitConveyorLayers();
            ApplySorting(chefRenderer, board, belt);
            serialized.FindProperty("cuttingBoard").objectReferenceValue = board;
            serialized.FindProperty("cuttingPoint").objectReferenceValue = anchor;
            serialized.FindProperty("foodShadowSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSpritePath);
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(chef.gameObject.scene);
            Selection.activeTransform = board;
            Debug.Log("[CuttingBoardSetup] Đã đặt bàn và thớt trước mặt đầu bếp; hãy lưu scene (Ctrl+S).");
        }

        // EN: Imports a kitchen picture as a centred 100-PPU sprite and returns it (null when the file is missing).
        // VI: Import ảnh đồ bếp thành sprite pivot giữa, 100 pixel mỗi đơn vị, rồi trả về sprite đó (null nếu thiếu file).
        private static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // EN: Returns the board already assigned to ChefManager or a new one next to the chef; sizes it to the chef's width, puts it at waist height and draws it just above the chef.
        // VI: Trả về thớt đã gán trong ChefManager hoặc tạo thớt mới cạnh đầu bếp; chỉnh kích thước theo bề ngang đầu bếp, đặt ngang thắt lưng và vẽ ngay trên đầu bếp.
        private static Transform GetOrCreateBoard(SerializedObject chefManager, SpriteRenderer chefRenderer, Sprite boardSprite)
        {
            Transform board = chefManager.FindProperty("cuttingBoard").objectReferenceValue as Transform;
            if (board == null)
            {
                GameObject go = new GameObject(BoardName);
                Undo.RegisterCreatedObjectUndo(go, "Create Cutting Board");
                go.transform.SetParent(chefRenderer.transform.parent, false);
                board = go.transform;
            }
            SpriteRenderer renderer = board.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = Undo.AddComponent<SpriteRenderer>(board.gameObject);
            Undo.RecordObject(renderer, "Setup Cutting Board");
            Undo.RecordObject(board, "Setup Cutting Board");
            renderer.sprite = boardSprite;
            renderer.sortingLayerID = chefRenderer.sortingLayerID;
            renderer.sortingOrder = chefRenderer.sortingOrder + 2;
            board.gameObject.layer = chefRenderer.gameObject.layer;

            Bounds chefBounds = chefRenderer.bounds;
            float targetWidth = chefBounds.size.x * WidthOfChef;
            float spriteWidth = Mathf.Max(0.01f, boardSprite.bounds.size.x);
            Vector3 parentScale = board.parent != null ? board.parent.lossyScale : Vector3.one;
            float scale = targetWidth / spriteWidth / Mathf.Max(0.0001f, parentScale.x);
            board.localScale = new Vector3(scale, scale, 1f);
            board.position = new Vector3(chefBounds.center.x, chefBounds.min.y + chefBounds.size.y * HeightFromChefBottom, chefRenderer.transform.position.z);
            board.rotation = Quaternion.identity;
            return board;
        }

        // EN: Creates or re-places the prep table under the board: 1.3x the board's width, with the board resting on the middle of its top surface.
        // VI: Tạo hoặc đặt lại bàn dưới thớt: rộng gấp 1.3 lần thớt, thớt nằm giữa mặt bàn.
        private static void PlaceTable(Transform board, Sprite tableSprite)
        {
            SpriteRenderer boardRenderer = board.GetComponent<SpriteRenderer>();
            Transform parent = board.parent;
            Transform table = parent != null ? parent.Find(TableName) : null;
            if (table == null)
            {
                GameObject go = new GameObject(TableName);
                Undo.RegisterCreatedObjectUndo(go, "Create Prep Table");
                go.transform.SetParent(parent, false);
                table = go.transform;
            }
            SpriteRenderer renderer = table.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = Undo.AddComponent<SpriteRenderer>(table.gameObject);
            Undo.RecordObject(renderer, "Setup Prep Table");
            Undo.RecordObject(table, "Setup Prep Table");
            renderer.sprite = tableSprite;
            table.gameObject.layer = board.gameObject.layer;

            Bounds boardBounds = boardRenderer.bounds;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            float scale = boardBounds.size.x * TableWidthOfBoard / Mathf.Max(0.01f, tableSprite.bounds.size.x) / Mathf.Max(0.0001f, parentScale.x);
            table.localScale = new Vector3(scale, scale, 1f);
            table.rotation = Quaternion.identity;
            float tableHeight = tableSprite.bounds.size.y * scale * parentScale.y;
            float boardSurface = board.position.y + boardBounds.size.y * BoardSurfaceAbovePivot;
            table.position = new Vector3(boardBounds.center.x, boardSurface - tableHeight * BoardSurfaceAboveTablePivot, board.position.z);
        }

        // EN: Orders the layers as floor < rear conveyor < customers/chef < table < board < front lip, keeping the rear rim behind people while only the foreground lip overlaps their lower edge; lowers the chef's order only when needed.
        // VI: Xếp lớp theo thứ tự sàn < băng chuyền phía sau < khách/đầu bếp < bàn < thớt < mép trước, để viền sau nằm sau nhân vật và chỉ mép trước che phần dưới; chỉ hạ thứ tự của đầu bếp khi cần.
        private static void ApplySorting(SpriteRenderer chefRenderer, Transform board, SpriteRenderer belt)
        {
            SpriteRenderer boardRenderer = board.GetComponent<SpriteRenderer>();
            Transform tableTransform = board.parent != null ? board.parent.Find(TableName) : null;
            SpriteRenderer tableRenderer = tableTransform != null ? tableTransform.GetComponent<SpriteRenderer>() : null;
            SpriteRenderer front = FindConveyorFront();
            int frontOrder = front != null ? front.sortingOrder : chefRenderer.sortingOrder + 4;
            int sortingLayer = front != null ? front.sortingLayerID : chefRenderer.sortingLayerID;
            if (belt != null)
            {
                int lowestCustomerOrder = FindLowestCustomerOrder(sortingLayer);
                int rearOrder = lowestCustomerOrder == int.MaxValue
                    ? frontOrder - 60
                    : Mathf.Min(frontOrder - 1, lowestCustomerOrder - 1);
                SetSorting(belt, sortingLayer, rearOrder);
            }
            SetSorting(boardRenderer, sortingLayer, frontOrder - 2);
            if (tableRenderer != null)
                SetSorting(tableRenderer, sortingLayer, frontOrder - 3);
            if (chefRenderer.sortingLayerID == sortingLayer && chefRenderer.sortingOrder > frontOrder - 4)
                SetSorting(chefRenderer, sortingLayer, frontOrder - 4);
        }

        // EN: Sets a renderer's sorting layer and order with Undo support.
        // VI: Đặt sorting layer và thứ tự vẽ của renderer, có hỗ trợ Undo.
        private static void SetSorting(SpriteRenderer renderer, int sortingLayer, int order)
        {
            Undo.RecordObject(renderer, "Sort Kitchen Layers");
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
        }

        // EN: Splits the conveyor background into layers: the original renderer shows the island floor, a "Rear" child shows the ring behind customers, and the separate front lip stays in front. Returns the rear renderer (null when required assets are missing).
        // VI: Tách nền băng chuyền thành nhiều lớp: renderer gốc hiện sàn bên trong, object con "Rear" hiện vòng băng phía sau khách, còn mép trước được giữ riêng ở phía trước. Trả về renderer lớp sau (null nếu thiếu asset cần thiết).
        private static SpriteRenderer SplitConveyorLayers()
        {
            SpriteRenderer front = FindConveyorFront();
            SpriteRenderer conveyor = front != null && front.transform.parent != null ? front.transform.parent.GetComponent<SpriteRenderer>() : null;
            Sprite floor = ImportLike(FloorSpritePath, ConveyorSpritePath);
            Sprite beltSprite = ImportLike(BeltSpritePath, ConveyorSpritePath);
            if (conveyor == null || floor == null || beltSprite == null)
            {
                Debug.LogWarning("[CuttingBoardSetup] Không tách được lớp băng chuyền (thiếu ConveyorStandalone hoặc sprite Floor/Belt).");
                return null;
            }
            Undo.RecordObject(conveyor, "Split Conveyor Layers");
            conveyor.sprite = floor;

            Transform beltTransform = conveyor.transform.Find(RearChildName);
            if (beltTransform == null)
            {
                beltTransform = conveyor.transform.Find(LegacyBeltChildName);
                if (beltTransform != null)
                    beltTransform.name = RearChildName;
            }
            if (beltTransform == null)
            {
                GameObject go = new GameObject(RearChildName);
                Undo.RegisterCreatedObjectUndo(go, "Create Conveyor Belt Layer");
                go.transform.SetParent(conveyor.transform, false);
                beltTransform = go.transform;
            }
            beltTransform.localPosition = Vector3.zero;
            beltTransform.localRotation = Quaternion.identity;
            beltTransform.localScale = Vector3.one;
            beltTransform.gameObject.layer = conveyor.gameObject.layer;
            SpriteRenderer belt = beltTransform.GetComponent<SpriteRenderer>();
            if (belt == null)
                belt = Undo.AddComponent<SpriteRenderer>(beltTransform.gameObject);
            Undo.RecordObject(belt, "Setup Conveyor Belt Layer");
            belt.sprite = beltSprite;
            belt.sharedMaterial = conveyor.sharedMaterial;
            belt.color = conveyor.color;
            return belt;
        }

        // EN: Finds the lowest sorting order used by customer sprites on the target layer so the rear conveyor can be placed just behind them.
        // VI: Tìm sorting order thấp nhất của các sprite khách hàng trên layer đích để đặt băng chuyền phía sau ngay bên dưới họ.
        private static int FindLowestCustomerOrder(int sortingLayer)
        {
            int lowestOrder = int.MaxValue;
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!renderer.gameObject.scene.IsValid() || renderer.sortingLayerID != sortingLayer || renderer.sprite == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(renderer.sprite);
                if (!path.Contains("/Customers/"))
                    continue;
                lowestOrder = Mathf.Min(lowestOrder, renderer.sortingOrder);
            }
            return lowestOrder;
        }

        // EN: Imports a sprite with exactly the same texture settings as a reference sprite, so split layers line up pixel for pixel.
        // VI: Import sprite với đúng thiết lập texture của sprite tham chiếu, để các lớp đã tách khớp nhau từng pixel.
        private static Sprite ImportLike(string path, string referencePath)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporter reference = AssetImporter.GetAtPath(referencePath) as TextureImporter;
            if (importer == null || reference == null)
                return null;
            TextureImporterSettings settings = new TextureImporterSettings();
            reference.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = reference.maxTextureSize;
            importer.textureCompression = reference.textureCompression;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // EN: Finds the conveyor's front-lip renderer (sprite named ConveyorStandaloneFront...) in the open scenes.
        // VI: Tìm renderer mép trước của băng chuyền (sprite tên ConveyorStandaloneFront...) trong các scene đang mở.
        private static SpriteRenderer FindConveyorFront()
        {
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.sprite != null && renderer.sprite.name.StartsWith("ConveyorStandaloneFront"))
                    return renderer;
            }
            return null;
        }

        // EN: Returns the board's food anchor child, creating it on the board's top surface when missing.
        // VI: Trả về object con điểm đặt món của thớt, tạo mới trên mặt thớt nếu chưa có.
        private static Transform GetOrCreateAnchor(Transform board)
        {
            Transform anchor = board.Find(AnchorName);
            if (anchor == null)
            {
                GameObject go = new GameObject(AnchorName);
                Undo.RegisterCreatedObjectUndo(go, "Create Food Anchor");
                go.transform.SetParent(board, false);
                anchor = go.transform;
            }
            anchor.localPosition = new Vector3(0f, AnchorLocalY, 0f);
            return anchor;
        }
    }
}
