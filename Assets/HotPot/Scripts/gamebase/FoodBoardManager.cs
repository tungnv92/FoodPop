using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FoodPop
{
    // EN: Owns the scene's ingredient board, including columns, dishes, layout and icon bindings.
    // VI: Quản lý board nguyên liệu của scene, gồm cột, đĩa, bố cục và gán icon.
    [DisallowMultipleComponent]
    public class FoodBoardManager : MonoBehaviour
    {
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        public const int MinColumns = 3;
        public const int MaxColumns = 5;

        private static FoodBoardManager instance;

        // EN: Returns the scene singleton, resolving it once when lifecycle registration has not run yet.
        // VI: Trả về singleton trong scene, tìm một lần nếu vòng đời chưa đăng ký instance.
        public static FoodBoardManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<FoodBoardManager>();
                return instance;
            }
        }

        [Tooltip("Danh sách cột nguyên liệu theo thứ tự từ trái sang phải; gán trực tiếp các cột trong scene.")]
        [SerializeField] private List<Transform> ingredientColumns = new List<Transform>();
        [Tooltip("Bộ hiển thị nền băng chuyền nguyên liệu, tự đổi hình khi nạp số cột của level.")]
        [SerializeField] private SpriteRenderer ingredientTableRenderer;
        [Tooltip("Hình nền băng chuyền dùng cho board ba cột.")]
        [SerializeField] private Sprite ingredientTableThreeLaneSprite;
        [Tooltip("Hình nền băng chuyền dùng cho board bốn cột; cũng dùng khi chưa có hình tương ứng cho số cột khác.")]
        [SerializeField] private Sprite ingredientTableFourLaneSprite;
        [Tooltip("Bảng icon của các loại nguyên liệu; để trống sẽ tự gán cấu hình mặc định trong editor.")]
        [SerializeField] private FoodIconConfig foodIconConfig;
        [Tooltip("Các sprite lớp băng theo thứ tự từ sắp tan đến dày nhất; tự gán trong editor.")]
        [SerializeField] private Sprite[] iceBlockSprites = new Sprite[4];
        [Tooltip("Prefab đĩa nguyên liệu được tạo khi pool hết đĩa tái sử dụng.")]
        [SerializeField] private Food ingredientDishPrefab;
        [Tooltip("Đối tượng chứa các đĩa đang nằm trong pool; tự tạo nếu chưa gán.")]
        [SerializeField] private Transform dishPoolRoot;
        [Tooltip("Camera dùng để đổi vị trí chạm/chuột sang toạ độ world; để trống sẽ dùng Camera.main.")]
        [SerializeField] private Camera inputCamera;

        [Tooltip("Khoảng cách theo trục Y (đơn vị world cục bộ của cột) giữa hai đĩa liền nhau trong cùng một cột.")]
        [Min(0f)] [SerializeField] private float rowSpacing = 1.31f;
        [Tooltip("Khoảng cách theo trục X (đơn vị world cục bộ của object cha) giữa hai cột liền nhau; các cột đang dùng được căn giữa tại x = 0 (ví dụ 1 với 3 cột: -1, 0, 1). Đặt 0 thì các cột chồng lên nhau.")]
        [Min(0f)] [SerializeField] private float columnSpacing;
        [Tooltip("Khoảng cách (đơn vị world) phía dưới mép dưới camera: đĩa có tâm (tính theo hàng hiện tại) thấp hơn mép dưới quá khoảng này sẽ bị tắt; tự bật lại khi dồn lên gần màn hình. Đĩa đầu cột không bao giờ bị tắt.")]
        [Min(0f)] [SerializeField] private float offscreenHideMargin = 1f;
        [Tooltip("Hoạt cảnh món nhảy vòng cung lên băng chuyền (component trên cùng GameObject).")]
        [SerializeField] private FoodFlyAnimator flyAnimator;
        [Tooltip("Hiệu ứng vụn băng bắn ra mỗi khi khối băng bị trừ giá trị.")]
        [SerializeField] private ParticleEffect iceHitEffect;
        [Tooltip("Hiệu ứng băng vỡ tung khi khối băng về 0 và tan.")]
        [SerializeField] private ParticleEffect iceBreakEffect;
        [Tooltip("Các đĩa đặt sẵn trong scene; được đưa vào pool khi nạp level đầu tiên.")]
        [SerializeField] private List<Food> sceneDishes = new List<Food>();

        // EN: Returns the configured ingredient flight duration.
        // VI: Trả về thời gian bay của nguyên liệu đã cấu hình.
        public float ServeFlyDuration => Config.ingredientFlight.serveFlyDuration;

        // EN: Returns the configured ingredient flight arc height.
        // VI: Trả về độ cao vòng cung bay của nguyên liệu đã cấu hình.
        public float ServeArcHeight => Config.ingredientFlight.serveArcHeight;

        // EN: Returns the ingredient scale multiplier at the flight peak.
        // VI: Trả về hệ số phóng to nguyên liệu tại đỉnh đường bay.
        public float ServePeakScale => Config.ingredientFlight.servePeakScale;

        // EN: Returns the fallback arrival scale when the lane has no tray stack.
        // VI: Trả về tỉ lệ khi đáp dự phòng nếu làn không có chồng trên khay.
        public float ServeEndScale => Config.ingredientFlight.serveEndScale;

        // EN: Returns the extra sorting order for flying ingredients.
        // VI: Trả về thứ tự vẽ cộng thêm cho nguyên liệu đang bay.
        public int ServeSortingBoost => Config.ingredientFlight.serveSortingBoost;

        // EN: Returns the duration of an ingredient's landing pop.
        // VI: Trả về thời gian nảy khi nguyên liệu đáp xuống.
        public float LandPopDuration => Config.ingredientFlight.landPopDuration;

        // EN: Returns the scale increase of an ingredient's landing pop.
        // VI: Trả về mức tăng tỉ lệ khi nguyên liệu nảy lúc đáp xuống.
        public float LandPopAmount => Config.ingredientFlight.landPopAmount;

        // EN: Raised after a head dish is picked: gives its food type and the conveyor slots that received its portions.
        // VI: Phát ra sau khi chọn đĩa đầu cột: cho biết loại nguyên liệu và các slot băng chuyền đã nhận từng phần của đĩa.
        public event Action<FoodType, IReadOnlyList<ConveyorSlot>> DishPicked;

        private readonly List<ConveyorSlot> targetSlots = new List<ConveyorSlot>();

        private readonly Dictionary<Transform, Coroutine> columnShifts = new Dictionary<Transform, Coroutine>();
        private readonly Dictionary<Food, Coroutine> lidReveals = new Dictionary<Food, Coroutine>();
        private readonly Dictionary<Transform, List<KeyValuePair<Transform, Vector3>>> shiftTargets = new Dictionary<Transform, List<KeyValuePair<Transform, Vector3>>>();

        private readonly List<List<Food>> columnDishes = new List<List<Food>>();

        private IngredientDishPool dishPool;
        private Vector3 dishLocalScale;
        private Quaternion dishLocalRotation;
        private int dishLayer;
        private int[] dishSortingOrders = new int[0];
        private int dishSortingLayer;
        private int quantitySortingOrder;

        // EN: Clears the static singleton when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa singleton tĩnh khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers this scene manager and removes only a duplicate component, preserving its GameObject.
        // VI: Đăng ký manager của scene và chỉ xóa component trùng, giữ nguyên GameObject chứa nó.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[FoodBoardManager] Scene đã có manager; bỏ component trùng.", this);
                enabled = false;
                Destroy(this);
                return;
            }
            instance = this;
        }

        // EN: Disposes owned pooled dishes and releases this manager's singleton registration.
        // VI: Dọn các đĩa thuộc pool và giải phóng đăng ký singleton của manager này.
        private void OnDestroy()
        {
            dishPool?.Dispose();
            if (instance == this)
                instance = null;
        }

        // EN: Reads the pointer each frame and picks the head dish under it.
        // VI: Đọc con trỏ/chạm mỗi frame và chọn đĩa đầu cột nằm dưới vị trí đó.
        private void Update()
        {
            if (InputLocked)
                return;
            if (TryGetPointerDown(out Vector2 worldPoint))
                TryPickAt(worldPoint);
        }

        // EN: After dishes and cameras have moved this frame, turns off dishes far below the screen and turns them back on as they approach it.
        // VI: Sau khi đĩa và camera đã di chuyển trong frame, tắt các đĩa nằm quá xa dưới màn hình và bật lại khi chúng tới gần.
        private void LateUpdate()
        {
            RefreshOffscreenDishes();
        }

        // EN: When true every dish stays active regardless of its position (BoosterController sets it while Shuffle flies dishes between rows).
        // VI: Khi true mọi đĩa luôn được bật bất kể vị trí (BoosterController bật trong lúc Shuffle cho đĩa bay giữa các hàng).
        public bool OffscreenHidingSuspended { get; set; }

        // EN: World Y of the bottom edge of the input camera view, or negative infinity when there is no camera (nothing gets hidden).
        // VI: Toạ độ Y world của mép dưới khung camera input, hoặc âm vô cùng khi không có camera (không đĩa nào bị tắt).
        private float GetCameraBottomY()
        {
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
                return float.NegativeInfinity;
            return cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Abs(cam.transform.position.z))).y;
        }

        // EN: Activates or deactivates every non-head dish by its row position: a dish whose row centre lies more than offscreenHideMargin below the camera bottom is turned off. Using the row position (not the current one) turns a dish back on as soon as a shift starts moving it into view. Data (ice value, hidden state) is untouched.
        // VI: Bật hoặc tắt mọi đĩa không phải đầu cột theo vị trí hàng: đĩa có tâm hàng thấp hơn mép dưới camera quá offscreenHideMargin sẽ bị tắt. Dùng vị trí hàng (không phải vị trí hiện tại) để đĩa được bật lại ngay khi bắt đầu trượt vào màn hình. Không đổi dữ liệu (giá trị băng, trạng thái ẩn).
        private void RefreshOffscreenDishes()
        {
            float hideBelowY = GetCameraBottomY() - offscreenHideMargin;
            for (int c = 0; c < columnDishes.Count && c < ingredientColumns.Count; c++)
            {
                Transform column = ingredientColumns[c];
                List<Food> dishes = columnDishes[c];
                for (int r = 0; r < dishes.Count; r++)
                {
                    Food dish = dishes[r];
                    if (dish == null)
                        continue;
                    bool show = r == 0 || OffscreenHidingSuspended || column.TransformPoint(GetRowLocalPosition(r)).y >= hideBelowY;
                    if (dish.gameObject.activeSelf != show)
                        dish.gameObject.SetActive(show);
                }
            }
        }

        // EN: True when the dish is active and part of its plate is above the bottom edge of the camera view.
        // VI: True khi đĩa đang bật và một phần đĩa nằm trên mép dưới khung camera.
        private bool IsDishOnScreen(Food dish)
        {
            if (!dish.gameObject.activeInHierarchy)
                return false;
            SpriteRenderer area = dish.PlateRenderer != null ? dish.PlateRenderer : dish.IconRenderer;
            float top = area != null ? area.bounds.max.y : dish.transform.position.y;
            return top >= GetCameraBottomY();
        }

        // EN: Returns true on the frame the mouse or touch is pressed, with its world position (Input System).
        // VI: Trả về true ở frame chuột hoặc ngón tay vừa nhấn, kèm vị trí world (Input System).
        private bool TryGetPointerDown(out Vector2 worldPoint)
        {
            worldPoint = default;
            Pointer pointer = Pointer.current;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (pointer == null || cam == null || !pointer.press.wasPressedThisFrame)
                return false;
            Vector2 screen = pointer.position.ReadValue();
            worldPoint = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
            return true;
        }

        // EN: Number of columns currently tracked on the board.
        // VI: Số cột đang được theo dõi trên board.
        public int ColumnCount => columnDishes.Count;

        // EN: When true the board ignores taps (set by BoosterController while a booster animates the board).
        // VI: Khi true, board bỏ qua thao tác chạm (BoosterController bật khi booster đang chạy hiệu ứng trên board).
        public bool InputLocked { get; set; }

        // EN: True while a column is still sliding or a lid is still opening.
        // VI: True khi còn cột đang trượt hoặc nắp đang mở.
        public bool IsAnimating => columnShifts.Count > 0 || lidReveals.Count > 0;

        // EN: Dishes of a column, top (head) first.
        // VI: Các đĩa của một cột, từ đầu cột xuống.
        public IReadOnlyList<Food> GetColumnDishes(int columnIndex)
        {
            return columnIndex >= 0 && columnIndex < columnDishes.Count ? columnDishes[columnIndex] : (IReadOnlyList<Food>)System.Array.Empty<Food>();
        }

        // EN: Column transform that parents the dishes of that column.
        // VI: Transform của cột, là cha của các đĩa trong cột.
        public Transform GetColumnTransform(int columnIndex)
        {
            return ingredientColumns[columnIndex];
        }

        // EN: Local position (inside its column) of the dish at the given row.
        // VI: Vị trí cục bộ (trong cột) của đĩa ở hàng đã cho.
        public Vector3 GetRowLocalPosition(int row)
        {
            return new Vector3(0f, -rowSpacing * row, 0f);
        }

        // EN: Replaces the dish order of every column (each dish must already be parented to its column) and refreshes heads, outlines and lids.
        // VI: Thay thứ tự đĩa của mọi cột (mỗi đĩa phải đã là con của cột mới) và cập nhật đĩa đầu, viền chọn và nắp.
        public void ApplyColumnDishes(List<List<Food>> columns)
        {
            for (int c = 0; c < columnDishes.Count && c < columns.Count; c++)
            {
                columnDishes[c].Clear();
                columnDishes[c].AddRange(columns[c]);
            }
            for (int c = 0; c < columnDishes.Count; c++)
                RefreshColumnSelection(c);
        }

        // EN: Food type of a column's head dish, or None when the column is empty.
        // VI: Loại món của đĩa đầu cột, hoặc None nếu cột trống.
        public FoodType GetColumnHeadType(int columnIndex)
        {
            Food head = GetColumnHead(columnIndex);
            return head != null ? head.foodType : FoodType.None;
        }

        // EN: Picks the selectable head dish whose plate contains the given world point, if any.
        // VI: Chọn đĩa đầu cột có vùng đĩa chứa điểm world đã cho, nếu có.
        public bool TryPickAt(Vector2 worldPoint)
        {
            for (int c = 0; c < columnDishes.Count; c++)
            {
                Food head = GetColumnHead(c);
                if (head != null && ContainsPoint(head, worldPoint))
                    return TryPickColumn(c);
            }
            return false;
        }

        // EN: Picks an unfrozen, open head dish (normal tap pick); see PickDish for the shared flow.
        // VI: Chọn đĩa đầu cột không đóng băng và đang mở (chạm chọn thông thường); luồng chung nằm ở PickDish.
        public bool TryPickColumn(int columnIndex)
        {
            Food dish = GetColumnHead(columnIndex);
            if (dish == null || dish.IsFrozen || dish.isHidden)
                return false;
            return PickDish(columnIndex, 0);
        }

        // EN: Picks the dish at any row of a column (PickupHand booster). The dish must be unfrozen and already open (the booster opens a hidden dish first); dishes below it shift up exactly like a normal pick.
        // VI: Chọn đĩa ở hàng bất kỳ của một cột (booster PickupHand). Đĩa phải không đóng băng và đã mở (booster mở nắp đĩa ẩn trước); các đĩa bên dưới dồn lên giống hệt chọn thông thường.
        public bool TryPickDishAt(int columnIndex, int row)
        {
            if (columnIndex < 0 || columnIndex >= columnDishes.Count || row < 0 || row >= columnDishes[columnIndex].Count)
                return false;
            Food dish = columnDishes[columnIndex][row];
            if (dish == null || dish.IsFrozen || dish.isHidden)
                return false;
            return PickDish(columnIndex, row);
        }

        // EN: Shared pick flow: reserves conveyor slots (flashing the dish outline red when the conveyor has no room), flies the portions to the conveyor, removes the dish, shifts the dishes below it up one row, then damages every remaining frozen block.
        // VI: Luồng chọn dùng chung: giữ slot băng chuyền (viền đĩa nháy đỏ khi băng chuyền hết chỗ), cho các phần bay lên băng, gỡ đĩa, dồn các đĩa bên dưới lên một hàng rồi gây sát thương mọi khối băng còn lại.
        private bool PickDish(int columnIndex, int row)
        {
            Food dish = columnDishes[columnIndex][row];
            FoodType type = dish.foodType;
            if (!TryFillConveyorSlots(type, dish.amount, dish.transform.position))
            {
                if (dish.Selection != null)
                    dish.Selection.PlayRejected();
                return false;
            }
            Transform column = ingredientColumns[columnIndex];
            LaunchPickAnimation(dish, type);
            FinishColumnShift(column);
            columnDishes[columnIndex].RemoveAt(row);
            RemoveFromBoard(dish);
            ShiftColumnUp(columnIndex, row);
            RefreshColumnSelection(columnIndex);
            ReduceFrozenBlocks();
            DishPicked?.Invoke(type, targetSlots);
            return true;
        }

        // EN: Finds the active dish (any row) whose plate contains the world point; dishes turned off below the screen are skipped. Returns false when there is none.
        // VI: Tìm đĩa đang bật (hàng bất kỳ) có vùng đĩa chứa điểm world; bỏ qua đĩa đã tắt vì nằm dưới màn hình. Trả về false khi không có.
        public bool TryGetDishAt(Vector2 worldPoint, out int columnIndex, out int row)
        {
            for (int c = 0; c < columnDishes.Count; c++)
            {
                List<Food> dishes = columnDishes[c];
                for (int r = 0; r < dishes.Count; r++)
                {
                    if (dishes[r] != null && dishes[r].gameObject.activeSelf && ContainsPoint(dishes[r], worldPoint))
                    {
                        columnIndex = c;
                        row = r;
                        return true;
                    }
                }
            }
            columnIndex = -1;
            row = -1;
            return false;
        }

        // EN: Re-applies the normal selection look (head outline, dimmed waiting dishes) on every column; used after a booster changed the highlights.
        // VI: Áp lại giao diện chọn bình thường (viền đầu cột, đĩa chờ bị làm tối) cho mọi cột; dùng sau khi booster đổi trạng thái sáng.
        public void RefreshSelection()
        {
            for (int c = 0; c < columnDishes.Count; c++)
                RefreshColumnSelection(c);
        }

        // EN: After a successful pick, lowers the block value of every frozen dish on the board by one; columns whose dishes thawed refresh their selection.
        // VI: Sau mỗi lần chọn thành công, giảm giá trị khối băng của mọi đĩa đang đóng băng trên board đi một; cột có đĩa vừa tan băng được cập nhật lại trạng thái chọn.
        private void ReduceFrozenBlocks()
        {
            for (int c = 0; c < columnDishes.Count; c++)
            {
                bool thawed = false;
                foreach (Food dish in columnDishes[c])
                    thawed |= DamageIce(dish);
                if (thawed)
                    RefreshColumnSelection(c);
            }
        }

        // EN: Lowers one frozen dish's block value; only when the dish is on screen does it burst ice shards (a bigger burst when it breaks). Dishes below the screen just lose the value silently. Returns true when the dish thawed.
        // VI: Giảm giá trị băng của một đĩa đóng băng; chỉ khi đĩa nằm trong màn hình mới bắn vụn băng (bắn mạnh hơn khi vỡ). Đĩa nằm dưới màn hình chỉ bị trừ giá trị, không có hiệu ứng. Trả về true khi đĩa tan băng.
        private bool DamageIce(Food dish)
        {
            if (!dish.IsFrozen)
                return false;
            if (!IsDishOnScreen(dish))
                return dish.ReduceBlock();
            SpriteRenderer ice = dish.IceRenderer;
            Vector3 position = ice != null ? ice.bounds.center : dish.transform.position;
            int sortingLayer = ice != null ? ice.sortingLayerID : 0;
            int sortingOrder = ice != null ? ice.sortingOrder + 2 : 0;
            bool thawed = dish.ReduceBlock();
            OneShotEffects.PlayParticle(thawed ? iceBreakEffect : iceHitEffect, position, sortingLayer, sortingOrder);
            return thawed;
        }

        // EN: Reserves the nearest empty conveyor slots for every portion and fills them with the food type; returns false when the conveyor lacks space.
        // VI: Lấy các slot rỗng gần nhất trên băng chuyền cho từng phần và đặt loại nguyên liệu vào; trả về false khi băng chuyền không đủ chỗ.
        private bool TryFillConveyorSlots(FoodType type, int amount, Vector3 dishPosition)
        {
            ChefManager chef = ChefManager.Instance;
            if (chef == null || !chef.TryGetNearestEmptySlots(dishPosition, Mathf.Max(1, amount), targetSlots))
            {
                Debug.Log("[FoodBoardManager] Băng chuyền không đủ slot trống cho đĩa này.", this);
                return false;
            }
            foreach (ConveyorSlot slot in targetSlots)
                slot.TryFillFood(type, foodIconConfig);
            return true;
        }

        // EN: Sends the picked dish's portions flying from its ingredient sprite to the reserved slots; must run before the dish is returned to the pool.
        // VI: Cho các phần của đĩa vừa chọn bay từ sprite nguyên liệu tới các slot đã giữ; phải gọi trước khi trả đĩa về pool.
        private void LaunchPickAnimation(Food dish, FoodType type)
        {
            if (flyAnimator == null)
                return;
            SpriteRenderer source = dish.IconRenderer;
            Vector3 start = source != null ? source.transform.position : dish.transform.position;
            Sprite icon = source != null && source.sprite != null ? source.sprite : GetIcon(type);
            flyAnimator.Launch(source, start, icon, targetSlots);
        }

        // EN: Takes a picked dish off the board, returning it to the pool (or just hiding it when it is not pooled).
        // VI: Gỡ đĩa đã chọn khỏi bảng, trả về pool (hoặc chỉ ẩn đi nếu đĩa không thuộc pool).
        private void RemoveFromBoard(Food dish)
        {
            if (dishPool == null || !dishPool.Release(dish))
                dish.gameObject.SetActive(false);
        }

        // EN: Returns the current head dish of a column (first entry of its top-to-bottom list), or null.
        // VI: Trả về đĩa đầu cột hiện tại (phần tử đầu của danh sách từ trên xuống), hoặc null.
        private Food GetColumnHead(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= columnDishes.Count)
                return null;
            List<Food> dishes = columnDishes[columnIndex];
            return dishes.Count > 0 ? dishes[0] : null;
        }

        // EN: Hit-tests a point against the dish's plate sprite bounds.
        // VI: Kiểm tra điểm có nằm trong vùng sprite đĩa không.
        private static bool ContainsPoint(Food dish, Vector2 worldPoint)
        {
            SpriteRenderer area = dish.PlateRenderer != null ? dish.PlateRenderer : dish.IconRenderer;
            if (area == null)
                return false;
            Bounds bounds = area.bounds;
            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x &&
                   worldPoint.y >= bounds.min.y && worldPoint.y <= bounds.max.y;
        }

        // EN: Moves the dishes of the column from fromRow down (the ones that were below the removed dish) up by one row, animated over Config.boardMotion.shiftUpDuration; dishes above fromRow stay put.
        // VI: Đẩy các đĩa của cột từ hàng fromRow trở xuống (các đĩa nằm dưới đĩa vừa gỡ) lên một hàng, có hiệu ứng trượt trong Config.boardMotion.shiftUpDuration; đĩa phía trên fromRow giữ nguyên.
        private void ShiftColumnUp(int columnIndex, int fromRow = 0)
        {
            Transform column = ingredientColumns[columnIndex];
            List<Food> dishes = columnDishes[columnIndex];
            if (fromRow >= dishes.Count)
                return;
            List<KeyValuePair<Transform, Vector3>> targets = new List<KeyValuePair<Transform, Vector3>>(dishes.Count - fromRow);
            for (int r = fromRow; r < dishes.Count; r++)
                targets.Add(new KeyValuePair<Transform, Vector3>(dishes[r].transform, dishes[r].transform.localPosition + Vector3.up * rowSpacing));
            shiftTargets[column] = targets;
            if (Config.boardMotion.shiftUpDuration <= 0f)
            {
                FinishColumnShift(column);
                return;
            }
            columnShifts[column] = StartCoroutine(AnimateColumnShift(column, targets));
        }

        // EN: Smoothly slides dishes from their current positions to their targets, then clears the column's shift state.
        // VI: Trượt mượt các đĩa từ vị trí hiện tại tới vị trí đích, sau đó xoá trạng thái trượt của cột.
        private IEnumerator AnimateColumnShift(Transform column, List<KeyValuePair<Transform, Vector3>> targets)
        {
            Vector3[] starts = new Vector3[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                starts[i] = targets[i].Key.localPosition;
            float time = 0f;
            while (time < Config.boardMotion.shiftUpDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / Config.boardMotion.shiftUpDuration));
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].Key != null)
                        targets[i].Key.localPosition = Vector3.LerpUnclamped(starts[i], targets[i].Value, t);
                }
                yield return null;
            }
            columnShifts.Remove(column);
            FinishColumnShift(column);
        }

        // EN: Stops a running shift and snaps its dishes to their final row, so a quick second pick starts from a clean layout.
        // VI: Dừng hiệu ứng trượt đang chạy và đặt các đĩa đúng hàng đích, để lần chọn nhanh tiếp theo bắt đầu từ bố cục chuẩn.
        private void FinishColumnShift(Transform column)
        {
            if (columnShifts.TryGetValue(column, out Coroutine running))
            {
                if (running != null)
                    StopCoroutine(running);
                columnShifts.Remove(column);
            }
            if (!shiftTargets.TryGetValue(column, out List<KeyValuePair<Transform, Vector3>> targets))
                return;
            foreach (KeyValuePair<Transform, Vector3> target in targets)
            {
                if (target.Key != null)
                    target.Key.localPosition = target.Value;
            }
            shiftTargets.Remove(column);
        }

        // EN: Highlights only the head dish of one column; the other dishes are dimmed and keep their lids. A hidden head dish opens instantly when animateReveal is false (level load); otherwise it waits until it has slid into the first cell, lifts, enlarges and fades its lid out, and only then becomes visible and selectable.
        // VI: Chỉ hiện viền đĩa đầu của một cột; các đĩa còn lại bị làm tối và giữ nắp. Đĩa đầu đang ẩn mở ngay khi animateReveal là false (lúc nạp level); ngược lại chờ đĩa trượt tới ô đầu tiên, nhấc nắp lên, phóng to và làm mờ dần, rồi mới hiện và cho phép chọn.
        private void RefreshColumnSelection(int columnIndex, bool animateReveal = true)
        {
            List<Food> dishes = columnDishes[columnIndex];
            for (int i = 0; i < dishes.Count; i++)
            {
                Food dish = dishes[i];
                if (i == 0 && dish.isHidden)
                {
                    if (!animateReveal || Config.lidReveal.fadeDuration <= 0f)
                        dish.SetHidden(false);
                    else if (!lidReveals.ContainsKey(dish))
                        lidReveals[dish] = StartCoroutine(RevealHeadDish(columnIndex, dish));
                }
                if (dish.Selection != null)
                    dish.Selection.SetSelectable(i == 0 && !dish.IsFrozen && !dish.isHidden);
            }
        }

        // EN: Waits for the column to finish sliding, fades the head dish's lid out, then refreshes the column so the dish becomes selectable.
        // VI: Chờ cột trượt xong, làm mờ dần nắp của đĩa đầu, rồi cập nhật lại cột để đĩa chọn được.
        private IEnumerator RevealHeadDish(int columnIndex, Food dish)
        {
            Transform column = ingredientColumns[columnIndex];
            while (columnShifts.ContainsKey(column))
                yield return null;
            IngameConfig.LidRevealSettings lid = Config.lidReveal;
            yield return dish.FadeOutLid(lid.fadeDuration, lid.liftHeight, lid.scaleUp);
            lidReveals.Remove(dish);
            RefreshColumnSelection(columnIndex);
        }

        // EN: Stops every running lid fade and opens those dishes instantly (used before the board is rebuilt).
        // VI: Dừng mọi hiệu ứng mờ nắp đang chạy và mở ngay các đĩa đó (dùng trước khi dựng lại board).
        private void StopLidReveals()
        {
            foreach (KeyValuePair<Food, Coroutine> reveal in lidReveals)
            {
                if (reveal.Value != null)
                    StopCoroutine(reveal.Value);
                if (reveal.Key != null)
                    reveal.Key.SetHidden(false);
            }
            lidReveals.Clear();
        }

        // EN: Recycles and fills the board, centres its active columns, updates the conveyor image and highlights each selectable head dish.
        // VI: Tái sử dụng và điền board, căn giữa các cột đang dùng, cập nhật hình băng chuyền và hiện viền đĩa đầu có thể chọn ở mỗi cột.
        public void LoadIngredientBoard(List<BoardColumn> board)
        {
            List<Transform> columns = GetColumnObjects();
            if (columns.Count == 0 || board == null)
            {
                Debug.LogWarning("[FoodBoardManager] Không tìm thấy Ingredients/Column hoặc bảng trống.", this);
                return;
            }
            if (!EnsureDishPool())
                return;
            int upperLimit = Mathf.Min(MaxColumns, columns.Count);
            int columnCount = Mathf.Clamp(board.Count, Mathf.Min(MinColumns, upperLimit), upperLimit);
            if (board.Count < MinColumns || board.Count > columnCount)
                Debug.LogWarning($"[FoodBoardManager] Bảng có {board.Count} cột, chỉ hỗ trợ {MinColumns}-{MaxColumns} cột (scene có {columns.Count}); dùng {columnCount} cột.", this);

            foreach (Transform column in columns)
                FinishColumnShift(column);
            StopLidReveals();
            ReturnBoardDishes();
            ApplyColumnSpacing(columnCount);
            UpdateIngredientTableVisual(columnCount);
            for (int c = 0; c < columns.Count; c++)
            {
                bool used = c < columnCount;
                columns[c].gameObject.SetActive(used);
                List<OrderFood> foods = used && c < board.Count ? board[c]?.foods : null;
                FillColumn(c, foods);
                RefreshColumnSelection(c, animateReveal: false);
            }
        }

        // EN: Selects the three-lane image for three active columns and the existing four-lane image otherwise, preserving the renderer's scene settings.
        // VI: Chọn hình ba làn cho ba cột đang dùng và hình bốn làn hiện có cho số cột khác, giữ nguyên thiết lập hiển thị trong scene.
        private void UpdateIngredientTableVisual(int activeCount)
        {
            if (ingredientTableRenderer == null)
                return;
            Sprite sprite = activeCount == 3 ? ingredientTableThreeLaneSprite : ingredientTableFourLaneSprite;
            if (sprite != null)
                ingredientTableRenderer.sprite = sprite;
        }

        // EN: Spreads the first activeCount columns evenly by columnSpacing, centred on local x = 0 (e.g. spacing 1 with 3 columns gives x = -1, 0, 1); spacing 0 stacks every column at x = 0.
        // VI: Dàn đều activeCount cột đầu tiên theo columnSpacing, căn giữa tại x cục bộ = 0 (ví dụ spacing 1 với 3 cột cho x = -1, 0, 1); spacing bằng 0 thì mọi cột chồng lên nhau tại x = 0.
        private void ApplyColumnSpacing(int activeCount)
        {
            if (activeCount <= 0)
                return;
            for (int c = 0; c < activeCount && c < ingredientColumns.Count; c++)
            {
                Transform column = ingredientColumns[c];
                Vector3 position = column.localPosition;
                position.x = (c - (activeCount - 1) * 0.5f) * columnSpacing;
                column.localPosition = position;
            }
        }

        // EN: Rents dishes at local row positions, assigns shared ice visuals and each cell's hidden/frozen state, then records them top to bottom.
        // VI: Lấy đĩa theo tọa độ hàng cục bộ, gán sprite băng dùng chung cùng trạng thái ẩn/đóng băng của ô rồi ghi theo thứ tự từ trên xuống.
        private void FillColumn(int columnIndex, List<OrderFood> foods)
        {
            while (columnDishes.Count <= columnIndex)
                columnDishes.Add(new List<Food>());
            if (foods == null)
                return;
            Transform column = ingredientColumns[columnIndex];
            for (int r = 0; r < foods.Count; r++)
            {
                OrderFood cell = foods[r];
                if (cell == null || cell.foodType == FoodType.None || cell.requiredCount <= 0)
                    continue;
                Food dish = dishPool.Get(column);
                if (dish == null)
                    continue;
                dish.transform.localScale = dishLocalScale;
                dish.transform.localRotation = dishLocalRotation;
                dish.transform.localPosition = new Vector3(0f, -rowSpacing * r, 0f);
                dish.SetIceBlockSprites(iceBlockSprites);
                dish.Setup(cell.foodType, cell.requiredCount, GetIcon(cell.foodType), cell.isHidden);
                dish.SetFrozen(cell.isFrozen, cell.blockValue);
                dish.SetIconConfig(foodIconConfig);
                dish.name = $"Ingredient dish · {cell.foodType}";
                ApplyDishVisuals(dish);
                dish.gameObject.SetActive(true);
                columnDishes[columnIndex].Add(dish);
            }
        }

        // EN: Captures dish visuals from the first scene dish (or the prefab), then adopts the scene dishes into the reusable pool; row spacing comes from the inspector.
        // VI: Ghi lại hình ảnh đĩa từ đĩa scene đầu tiên (hoặc prefab), rồi đưa các đĩa scene vào pool tái sử dụng; khoảng cách hàng lấy từ inspector.
        private bool EnsureDishPool()
        {
            if (dishPool != null)
                return true;
            if (ingredientDishPrefab == null)
            {
                Debug.LogWarning("[FoodBoardManager] Chưa gán prefab đĩa nguyên liệu cho pool.", this);
                return false;
            }
            Food template = sceneDishes.Count > 0 && sceneDishes[0] != null ? sceneDishes[0] : ingredientDishPrefab;
            CaptureDishVisuals(template);
            if (dishPoolRoot == null)
            {
                dishPoolRoot = new GameObject("IngredientDishPool").transform;
                dishPoolRoot.SetParent(transform, false);
            }
            dishPoolRoot.gameObject.SetActive(false);
            dishPool = new IngredientDishPool(ingredientDishPrefab, dishPoolRoot);
            foreach (Food dish in sceneDishes)
                dishPool.Adopt(dish);
            return true;
        }

        // EN: Remembers the template's transform, layer and per-renderer sorting so every pooled dish is drawn the same way.
        // VI: Ghi nhớ transform, layer và thứ tự vẽ từng renderer của đĩa mẫu để mọi đĩa trong pool được vẽ giống nhau.
        private void CaptureDishVisuals(Food template)
        {
            dishLocalScale = template.transform.localScale;
            dishLocalRotation = template.transform.localRotation;
            dishLayer = template.gameObject.layer;
            SpriteRenderer[] renderers = template.VisualRenderers;
            dishSortingOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                dishSortingOrders[i] = renderers[i].sortingOrder;
                dishSortingLayer = renderers[i].sortingLayerID;
            }
            quantitySortingOrder = template.CountCanvas != null ? template.CountCanvas.sortingOrder : 0;
        }

        // EN: Returns every dish still on the board to the pool and clears the column lists.
        // VI: Trả mọi đĩa còn trên board về pool và xóa danh sách các cột.
        private void ReturnBoardDishes()
        {
            foreach (List<Food> dishes in columnDishes)
            {
                foreach (Food dish in dishes)
                    RemoveFromBoard(dish);
                dishes.Clear();
            }
        }

        // EN: Restores the board's renderer layers and sorting so newly created prefab dishes match scene dishes.
        // VI: Khôi phục layer và thứ tự vẽ để đĩa prefab tạo mới giống đĩa hiện có trong scene.
        private void ApplyDishVisuals(Food dish)
        {
            dish.gameObject.layer = dishLayer;
            SpriteRenderer[] renderers = dish.VisualRenderers;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;
                renderer.gameObject.layer = dishLayer;
                renderer.sortingLayerID = dishSortingLayer;
                if (i < dishSortingOrders.Length)
                    renderer.sortingOrder = dishSortingOrders[i];
            }
            if (dish.CountCanvas != null)
            {
                dish.CountCanvas.gameObject.layer = dishLayer;
                dish.CountCanvas.sortingOrder = quantitySortingOrder;
            }
        }

        // EN: Returns the assigned column references in Inspector order, rejecting missing or duplicate entries.
        // VI: Trả về reference cột theo thứ tự Inspector, từ chối phần tử chưa gán hoặc bị trùng.
        private List<Transform> GetColumnObjects()
        {
            if (ingredientColumns == null)
                return new List<Transform>();
            for (int i = 0; i < ingredientColumns.Count; i++)
            {
                if (ingredientColumns[i] == null || ingredientColumns.IndexOf(ingredientColumns[i]) != i)
                {
                    Debug.LogWarning($"[FoodBoardManager] Reference cột {i + 1} chưa được gán hoặc bị trùng; kiểm tra danh sách cột trong Inspector.", this);
                    return new List<Transform>();
                }
            }
            return ingredientColumns;
        }

        // EN: Returns the icon of a food type from the FoodIconConfig, or null.
        // VI: Lấy icon của một loại nguyên liệu từ FoodIconConfig, hoặc null.
        private Sprite GetIcon(FoodType type)
        {
            return foodIconConfig != null ? foodIconConfig.GetIcon(type) : null;
        }

#if UNITY_EDITOR
        // EN: Assigns default HotPot conveyor images, icons, ice shells and dish prefab without discovering scene columns.
        // VI: Gán hình băng chuyền, icon, lớp băng và prefab đĩa mặc định trong HotPot, không tự tìm cột trong scene.
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (ingredientTableThreeLaneSprite == null)
                ingredientTableThreeLaneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/Demo/IngredientTableStandalone3Lane.png");
            if (ingredientTableFourLaneSprite == null)
                ingredientTableFourLaneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/Demo/IngredientTableStandalone.png");
            if (foodIconConfig == null)
                foodIconConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<FoodIconConfig>("Assets/HotPot/Config/FoodIconConfig.asset");
            string[] icePaths =
            {
                "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Frost.png",
                "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Cracked.png",
                "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Faceted.png",
                "Assets/HotPot/Art/Sprites/Blocks/Ice/IceBlock_Clear.png"
            };
            if (iceBlockSprites == null || iceBlockSprites.Length != icePaths.Length)
                iceBlockSprites = new Sprite[icePaths.Length];
            for (int i = 0; i < icePaths.Length; i++)
            {
                if (iceBlockSprites[i] == null)
                    iceBlockSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(icePaths[i]);
            }
            if (ingredientDishPrefab == null)
                ingredientDishPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Food>("Assets/HotPot/Prefabs/Ingame/IngredientDish.prefab");
        }
#endif
    }
}

