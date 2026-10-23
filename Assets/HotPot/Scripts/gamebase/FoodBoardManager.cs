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
    public partial class FoodBoardManager : MonoBehaviour
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
        [Tooltip("Hình đĩa đôi ngang chữ nhật bo nhẹ bốn góc.")]
        [SerializeField] private Sprite horizontalDoublePlate;
        [Tooltip("Hình đĩa đôi dọc chữ nhật bo nhẹ bốn góc.")]
        [SerializeField] private Sprite verticalDoublePlate;
        [Tooltip("Viền chọn trắng cho đĩa đôi ngang.")]
        [SerializeField] private Sprite horizontalDoubleOutline;
        [Tooltip("Viền chọn trắng cho đĩa đôi dọc.")]
        [SerializeField] private Sprite verticalDoubleOutline;
        [Tooltip("Nắp chữ nhật cho đĩa đôi ngang, dùng khi ẩn cả đĩa.")]
        [SerializeField] private Sprite horizontalDoubleLid;
        [Tooltip("Nắp chữ nhật cho đĩa đôi dọc, dùng khi ẩn cả đĩa.")]
        [SerializeField] private Sprite verticalDoubleLid;
        [Tooltip("Vật liệu viền chọn bám theo hình chữ nhật của đĩa đôi.")]
        [SerializeField] private Material doublePlateOutlineMaterial;
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

        // EN: Releases link visuals, pooled dishes and this manager's singleton registration.
        // VI: Giải phóng hình liên kết, đĩa trong pool và đăng ký singleton của manager.
        private void OnDestroy()
        {
            ClearDishLinks();
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

        // EN: Updates offscreen visibility and straight links after all dish and camera movement for the frame.
        // VI: Cập nhật ẩn ngoài màn hình và dây thẳng sau mọi chuyển động đĩa và camera trong frame.
        private void LateUpdate()
        {
            RefreshOffscreenDishes();
            UpdateDishLinkViews();
        }

        // EN: When true every dish stays active regardless of its position (BoosterController sets it while Shuffle flies dishes between rows).
        // VI: Khi true mọi đĩa luôn được bật bất kể vị trí (BoosterController bật trong lúc Shuffle cho đĩa bay giữa các hàng).
        public bool OffscreenHidingSuspended { get; set; }

        // EN: Only the first MaxVisibleRows rows of each column may be shown; deeper dishes stay off even when they are on screen (BoosterController limits it to 7 while PickupHand selects). int.MaxValue means no limit.
        // VI: Chỉ MaxVisibleRows hàng đầu của mỗi cột được hiện; đĩa ở hàng sâu hơn luôn bị tắt dù nằm trong màn hình (BoosterController giới hạn 7 hàng khi PickupHand đang chọn). int.MaxValue là không giới hạn.
        public int MaxVisibleRows { get; set; } = int.MaxValue;

        // EN: World Y of the bottom edge of the input camera view, or negative infinity when there is no camera (nothing gets hidden).
        // VI: Toạ độ Y world của mép dưới khung camera input, hoặc âm vô cùng khi không có camera (không đĩa nào bị tắt).
        private float GetCameraBottomY()
        {
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
                return float.NegativeInfinity;
            return cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Abs(cam.transform.position.z))).y;
        }

        // EN: Updates visibility once per anchor so double-dish placeholder cells never override it.
        // VI: Cập nhật hiển thị một lần tại ô gốc để ô giữ chỗ đĩa đôi không ghi đè trạng thái.
        private void RefreshOffscreenDishes()
        {
            float hideBelowY = GetCameraBottomY() - offscreenHideMargin;
            for (int c = 0; c < columnDishes.Count && c < ingredientColumns.Count; c++)
                for (int r = 0; r < columnDishes[c].Count; r++)
                {
                    if (!IsDishAnchor(c, r))
                        continue;
                    Food dish = columnDishes[c][r];
                    bool show = r == 0 || (r < MaxVisibleRows && (OffscreenHidingSuspended ||
                        ingredientColumns[c].TransformPoint(GetRowLocalPosition(r)).y >= hideBelowY));
                    if (dish.gameObject.activeSelf != show)
                        dish.gameObject.SetActive(show);
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

        // EN: Only a front-row dish may trigger a normal pick; tapping a deeper dish has no rejection or pick side effect.
        // VI: Chỉ đĩa hàng đầu được kích hoạt lượt chọn thường; chạm đĩa sâu không lấy hay phát hiệu ứng từ chối.
        public bool TryPickAt(Vector2 worldPoint)
        {
            for (int c = 0; c < columnDishes.Count; c++)
                for (int r = 0; r < columnDishes[c].Count; r++)
                    if (IsDishAnchor(c, r) && ContainsPoint(columnDishes[c][r], worldPoint))
                        return IsDishAtHead(columnDishes[c][r]) && PickGroup(columnDishes[c][r], false);
            return false;
        }

        // EN: Picks a column head and its complete linked group when all normal exposure and state conditions hold.
        // VI: Chọn đĩa đầu cột và cả nhóm nối khi đủ điều kiện lộ nhóm và trạng thái thông thường.
        public bool TryPickColumn(int columnIndex)
        {
            Food dish = GetColumnHead(columnIndex);
            return dish != null && PickGroup(dish, false);
        }

        // EN: PickupHand takes the whole group while bypassing depth, but still requires every lid open and every member unfrozen.
        // VI: PickupHand lấy cả nhóm không xét độ sâu, nhưng vẫn yêu cầu mọi nắp đã mở và mọi thành viên không đóng băng.
        public bool TryPickDishAt(int columnIndex, int row)
        {
            if (columnIndex < 0 || columnIndex >= columnDishes.Count || row < 0 || row >= columnDishes[columnIndex].Count)
                return false;
            Food dish = columnDishes[columnIndex][row];
            return dish != null && PickGroup(dish, true);
        }


        // EN: Shared icon table (lets boosters look up raw/prepared sprites).
        // VI: Bảng icon dùng chung (để booster tra sprite thô/đã sơ chế).
        public FoodIconConfig IconConfig => foodIconConfig;

        // EN: Takes one portion from either half for a booster; clears the footprint only after both halves are empty.
        // VI: Lấy một phần từ một trong hai nửa cho booster; chỉ xóa các ô đĩa chiếm khi cả hai nửa hết nguyên liệu.
        public bool TryTakePortion(FoodType type, out Vector3 position, out Sprite sprite)
        {
            position = default;
            sprite = null;
            if (!FindPortion(type, false, out int column, out int row) && !FindPortion(type, true, out column, out row))
                return false;
            Food dish = columnDishes[column][row];
            SpriteRenderer source = dish.foodType == type && dish.amount > 0 ? dish.IconRenderer : dish.SecondIconRenderer;
            position = source != null ? source.transform.position : dish.transform.position;
            sprite = GetIcon(type);
            if (dish.IsFrozen)
                ShatterIce(dish);
            if (dish.isHidden)
                dish.SetHidden(false);
            dish.TakePortion(type);
            if (dish.TotalAmount == 0)
                RemoveDishAndSettle(dish);
            return true;
        }

        private readonly List<int> pendingReturns = new List<int>();

        // EN: Books the bottom place of the active column that will hold the fewest dishes (counting returns still on their way) for a dish coming back from the conveyor; gives the column and the world position where that dish will sit.
        // VI: Giữ chỗ cuối của cột đang dùng sẽ có ít đĩa nhất (tính cả các đĩa đang trên đường trả về) cho một đĩa trả về từ băng chuyền; trả về cột và vị trí world nơi đĩa đó sẽ nằm.
        public bool TryReserveReturnSpot(out int column, out Vector3 position)
        {
            column = -1;
            position = default;
            while (pendingReturns.Count < columnDishes.Count)
                pendingReturns.Add(0);
            int best = int.MaxValue;
            for (int c = 0; c < columnDishes.Count && c < ingredientColumns.Count; c++)
            {
                if (!ingredientColumns[c].gameObject.activeSelf)
                    continue;
                int count = columnDishes[c].Count + pendingReturns[c];
                if (count < best)
                {
                    best = count;
                    column = c;
                }
            }
            if (column < 0)
                return false;
            pendingReturns[column]++;
            position = ingredientColumns[column].TransformPoint(GetRowLocalPosition(best));
            return true;
        }

        // EN: Places a returned raw dish (one portion, open, not frozen) at the bottom of a column booked with TryReserveReturnSpot and pops it in.
        // VI: Đặt một đĩa thô trả về (một phần, đang mở, không đóng băng) vào cuối cột đã giữ chỗ bằng TryReserveReturnSpot và cho đĩa nảy lên.
        public void AddReturnedDish(int column, FoodType type)
        {
            if (column < 0 || column >= columnDishes.Count || !EnsureDishPool())
                return;
            if (column < pendingReturns.Count)
                pendingReturns[column] = Mathf.Max(0, pendingReturns[column] - 1);
            Food dish = dishPool.Get(ingredientColumns[column]);
            if (dish == null)
                return;
            dish.transform.localRotation = dishLocalRotation;
            dish.transform.localPosition = GetRowLocalPosition(columnDishes[column].Count);
            dish.SetIceBlockSprites(iceBlockSprites);
            dish.Setup(type, 1, GetIcon(type));
            dish.SetIconConfig(foodIconConfig);
            dish.name = $"Ingredient dish · {type}";
            ApplyDishVisuals(dish);
            dish.gameObject.SetActive(true);
            columnDishes[column].Add(dish);
            RefreshColumnSelection(column);
            StartCoroutine(PopInDish(dish.transform));
        }

        // EN: Pops a dish in from nothing with a small overshoot to its normal board scale.
        // VI: Cho đĩa nảy lên từ cỡ 0, vượt nhẹ rồi về cỡ bình thường trên board.
        private IEnumerator PopInDish(Transform dish)
        {
            const float Duration = 0.3f;
            for (float time = 0f; time < Duration && dish != null; time += Time.deltaTime)
            {
                float t = time / Duration;
                float scale = t < 0.6f ? 1.18f * Mathf.SmoothStep(0f, 1f, t / 0.6f) : Mathf.Lerp(1.18f, 1f, (t - 0.6f) / 0.4f);
                dish.localScale = dishLocalScale * scale;
                yield return null;
            }
            if (dish != null)
                dish.localScale = dishLocalScale;
        }

        // EN: Finds matching portions in either half, nearest the head first and filtered by frozen state.
        // VI: Tìm phần nguyên liệu trong cả hai nửa, ưu tiên gần đầu cột và lọc theo trạng thái đóng băng.
        private bool FindPortion(FoodType type, bool frozen, out int column, out int row)
        {
            int maxRows = 0;
            foreach (List<Food> dishes in columnDishes)
                maxRows = Mathf.Max(maxRows, dishes.Count);
            for (row = 0; row < maxRows; row++)
                for (column = 0; column < columnDishes.Count; column++)
                {
                    if (!IsDishAnchor(column, row))
                        continue;
                    Food dish = columnDishes[column][row];
                    if (dish.IsFrozen == frozen && ((dish.foodType == type && dish.amount > 0) ||
                        (dish.SecondFoodType == type && dish.SecondAmount > 0)))
                        return true;
                }
            column = -1;
            row = -1;
            return false;
        }

        // EN: Breaks a dish's ice at once with the ice-break burst (shown only when the dish is on screen).
        // VI: Phá băng của đĩa ngay lập tức kèm hiệu ứng vỡ băng (chỉ hiện khi đĩa nằm trong màn hình).
        private void ShatterIce(Food dish)
        {
            SpriteRenderer ice = dish.IceRenderer;
            if (IsDishOnScreen(dish))
                OneShotEffects.PlayParticle(iceBreakEffect, ice != null ? ice.bounds.center : dish.transform.position,
                    ice != null ? ice.sortingLayerID : 0, ice != null ? ice.sortingOrder + 2 : 0);
            dish.SetFrozen(false, 0);
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

        // EN: Re-applies ordinary and whole-group readiness after every column has refreshed its lids.
        // VI: Áp lại trạng thái chọn đĩa thường và cả nhóm sau khi mọi cột cập nhật nắp.
        public void RefreshSelection()
        {
            for (int c = 0; c < columnDishes.Count; c++)
                RefreshColumnSelection(c);
            UpdateDishLinkViews();
        }

        // EN: Damages each distinct frozen dish once, including double dishes, then refreshes selection.
        // VI: Trừ băng mỗi đĩa riêng biệt đúng một lần, kể cả đĩa đôi, rồi cập nhật trạng thái chọn.
        private void ReduceFrozenBlocks()
        {
            HashSet<Food> visited = new HashSet<Food>();
            foreach (List<Food> dishes in columnDishes)
                foreach (Food dish in dishes)
                    if (dish != null && visited.Add(dish))
                        DamageIce(dish);
            RefreshSelection();
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


        // EN: Launches each member's ingredient halves to destinations reserved by the atomic group batch.
        // VI: Cho từng nửa nguyên liệu của mỗi thành viên bay tới đích đã giữ bởi lượt lấy nhóm.
        private void LaunchPickAnimation(DishPickBatch.Entry entry)
        {
            if (flyAnimator == null)
                return;
            Food dish = entry.dish;
            SpriteRenderer source = dish.IconRenderer;
            Vector3 start = source != null ? source.transform.position : dish.transform.position;
            flyAnimator.Launch(source, start, GetIcon(dish.foodType), entry.first);
            source = dish.SecondIconRenderer;
            start = source != null ? source.transform.position : dish.transform.position;
            flyAnimator.Launch(source, start, GetIcon(dish.SecondFoodType), entry.second);
        }

        // EN: Detaches live links before returning a dish to its pool or hiding a nonpooled dish.
        // VI: Gỡ liên kết đang chơi trước khi trả đĩa vào pool hoặc ẩn đĩa ngoài pool.
        private void RemoveFromBoard(Food dish)
        {
            DetachDishLink(dish);
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

        // EN: Opens exposed lids and keeps front linked dishes bright even while another member prevents picking the group.
        // VI: Mở nắp đã lộ và giữ đĩa nối ở đầu hàng sáng kể cả khi thành viên khác đang chặn việc lấy nhóm.
        private void RefreshColumnSelection(int columnIndex, bool animateReveal = true)
        {
            List<Food> dishes = columnDishes[columnIndex];
            for (int r = 0; r < dishes.Count; r++)
            {
                if (!IsDishAnchor(columnIndex, r))
                    continue;
                Food dish = dishes[r];
                bool head = IsDishExposedForReveal(dish);
                if (head && dish.isHidden)
                {
                    if (!animateReveal || Config.lidReveal.fadeDuration <= 0f)
                        dish.SetHidden(false);
                    else if (!lidReveals.ContainsKey(dish))
                        lidReveals[dish] = StartCoroutine(RevealHeadDish(columnIndex, dish));
                }
                dish.Selection?.SetSelectable(CanPickGroup(dish, false) || (IsLinked(dish) && IsDishAtHead(dish)));
            }
        }

        // EN: Waits for sliding and lid reveal, then refreshes every column so linked members become selectable together.
        // VI: Chờ dồn và mở nắp, rồi cập nhật mọi cột để các thành viên nối cùng trở nên chọn được.
        private IEnumerator RevealHeadDish(int columnIndex, Food dish)
        {
            Transform column = ingredientColumns[columnIndex];
            while (columnShifts.ContainsKey(column))
                yield return null;
            IngameConfig.LidRevealSettings lid = Config.lidReveal;
            yield return dish.FadeOutLid(lid.fadeDuration, lid.liftHeight, lid.scaleUp);
            lidReveals.Remove(dish);
            RefreshSelection();
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

        // EN: Rebuilds footprints and ordered links from saved coordinates before settling gaps and opening exposed dishes.
        // VI: Dựng lại vùng chiếm và liên kết theo tọa độ đã lưu trước khi dồn ô trống và mở đĩa đã lộ.
        public void LoadIngredientBoard(List<BoardColumn> board, IReadOnlyList<DishLinkConfig> links = null)
        {
            List<Transform> columns = GetColumnObjects();
            if (columns.Count == 0 || board == null || !EnsureDishPool())
                return;
            int upperLimit = Mathf.Min(MaxColumns, columns.Count);
            int columnCount = Mathf.Clamp(board.Count, Mathf.Min(MinColumns, upperLimit), upperLimit);
            if (board.Count > columnCount)
                Debug.LogWarning($"[FoodBoardManager] Board vượt {columnCount} cột đang hỗ trợ.", this);
            foreach (Transform column in columns)
                FinishColumnShift(column);
            StopLidReveals();
            ClearDishLinks();
            ReturnBoardDishes();
            pendingReturns.Clear();
            ApplyColumnSpacing(columnCount);
            UpdateIngredientTableVisual(columnCount);
            while (columnDishes.Count < columns.Count)
                columnDishes.Add(new List<Food>());
            for (int c = 0; c < columns.Count; c++)
                columns[c].gameObject.SetActive(c < columnCount);
            FillBoard(board, columnCount);
            BuildDishLinks(links);
            SettleBoard(false);
            for (int c = 0; c < columnDishes.Count; c++)
                RefreshColumnSelection(c, false);
            UpdateDishLinkViews();
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

        // EN: Loads rows in row-major order so each double reserves its second cell before that cell is visited.
        // VI: Nạp theo từng hàng để mỗi đĩa đôi giữ ô thứ hai trước khi ô đó được duyệt.
        private void FillBoard(List<BoardColumn> board, int columnCount)
        {
            int rows = 0;
            for (int c = 0; c < columnCount && c < board.Count; c++)
            {
                int count = board[c]?.foods?.Count ?? 0;
                rows = Mathf.Max(rows, count);
                for (int r = 0; r < count; r++)
                    columnDishes[c].Add(null);
            }
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columnCount && c < board.Count; c++)
                {
                    if (r >= columnDishes[c].Count || columnDishes[c][r] != null)
                        continue;
                    OrderFood cell = board[c]?.foods?[r];
                    if (cell == null || cell.foodType == FoodType.None || cell.requiredCount <= 0)
                        continue;
                    int width = cell.dishShape == DishShape.Horizontal ? 2 : 1;
                    int height = cell.dishShape == DishShape.Vertical ? 2 : 1;
                    if ((cell.IsDouble && (cell.secondFoodType == FoodType.None || cell.secondCount <= 0)) ||
                        (cell.dishShape != DishShape.Single && cell.dishShape != DishShape.Vertical && cell.dishShape != DishShape.Horizontal) ||
                        c + width > columnCount || r + height > columnDishes[c].Count ||
                        (width == 2 && (r >= columnDishes[c + 1].Count || columnDishes[c + 1][r] != null)))
                    {
                        Debug.LogError($"[FoodBoardManager] Đĩa không hợp lệ tại cột {c + 1}, hàng {r + 1}; bỏ qua.", this);
                        continue;
                    }
                    Food dish = CreateBoardDish(cell, c, r);
                    if (dish == null)
                        continue;
                    for (int x = c; x < c + width; x++)
                        for (int y = r; y < r + height; y++)
                            columnDishes[x][y] = dish;
                    dish.gameObject.SetActive(true);
                }
        }

        // EN: Configures a pooled dish with matching plate, ring and lid sprites plus dish-local ingredient spacing.
        // VI: Cấu hình đĩa từ pool với sprite đĩa, vòng viền và nắp tương ứng cùng khoảng cách nguyên liệu trong tọa độ đĩa.
        private Food CreateBoardDish(OrderFood cell, int column, int row)
        {
            Transform parent = ingredientColumns[column];
            Food dish = dishPool.Get(parent);
            if (dish == null)
                return null;
            dish.transform.localScale = dishLocalScale;
            dish.transform.localRotation = dishLocalRotation;
            dish.transform.localPosition = GetRowLocalPosition(row);
            dish.SetIceBlockSprites(iceBlockSprites);
            dish.Setup(cell.foodType, cell.requiredCount, GetIcon(cell.foodType), cell.isHidden);
            dish.SetIconConfig(foodIconConfig);
            Vector3 worldOffset = cell.dishShape == DishShape.Vertical ? parent.TransformVector(Vector3.down * rowSpacing) :
                cell.dishShape == DishShape.Horizontal ? ingredientColumns[column + 1].position - parent.position : Vector3.zero;
            Sprite plate = cell.dishShape == DishShape.Horizontal ? horizontalDoublePlate : verticalDoublePlate;
            Sprite outline = cell.dishShape == DishShape.Horizontal ? horizontalDoubleOutline : verticalDoubleOutline;
            Sprite lid = cell.dishShape == DishShape.Horizontal ? horizontalDoubleLid : verticalDoubleLid;
            dish.ConfigureShape(cell.dishShape, cell.secondFoodType, cell.secondCount, dish.transform.InverseTransformVector(worldOffset), plate, doublePlateOutlineMaterial, outline, lid);
            dish.SetFrozen(cell.isFrozen, cell.blockValue);
            dish.name = $"Ingredient dish · {cell.foodType} · {cell.dishShape}";
            ApplyDishVisuals(dish);
            return dish;
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

        // EN: Returns each distinct dish once and clears all footprint references.
        // VI: Trả mỗi đĩa riêng biệt một lần và xóa mọi tham chiếu ô chiếm.
        private void ReturnBoardDishes()
        {
            HashSet<Food> returned = new HashSet<Food>();
            foreach (List<Food> dishes in columnDishes)
            {
                foreach (Food dish in dishes)
                    if (dish != null && returned.Add(dish))
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

        // EN: Identifies the top-left cell of a dish, skipping empty cells and duplicate footprint entries.
        // VI: Xác định ô trên trái của đĩa, bỏ qua ô trống và các ô chiếm trùng tham chiếu.
        private bool IsDishAnchor(int column, int row)
        {
            if (column < 0 || column >= columnDishes.Count || row < 0 || row >= columnDishes[column].Count)
                return false;
            Food dish = columnDishes[column][row];
            return dish != null && (row == 0 || columnDishes[column][row - 1] != dish) &&
                (column == 0 || row >= columnDishes[column - 1].Count || columnDishes[column - 1][row] != dish);
        }

        // EN: Requires a dish to occupy row zero in every column of its footprint for a normal pick.
        // VI: Yêu cầu đĩa chiếm hàng đầu ở mọi cột trong vùng chiếm để chọn thông thường.
        private bool IsDishAtHead(Food dish)
        {
            for (int c = 0; c < columnDishes.Count; c++)
                if (GetColumnHead(c) == dish)
                    return dish.Shape != DishShape.Horizontal || GetColumnHead(c + 1) == dish;
            return false;
        }

        // EN: Removes one exhausted booster dish, updates its links and settles remaining gaps.
        // VI: Gỡ một đĩa hết nguyên liệu do booster, cập nhật liên kết và dồn khoảng trống còn lại.
        private void RemoveDishAndSettle(Food dish)
        {
            foreach (Transform column in ingredientColumns)
                FinishColumnShift(column);
            ClearDishFootprint(dish);
            RemoveFromBoard(dish);
            SettleBoard();
            RefreshSelection();
        }

        // EN: Compacts each footprint upward only through free cells; horizontal gates wait for both columns.
        // VI: Dồn từng vùng chiếm lên qua ô trống; cổng đĩa ngang chờ cả hai cột cùng trống phía trên.
        private void SettleBoard(bool animate = true)
        {
            foreach (Transform column in ingredientColumns)
                FinishColumnShift(column);
            bool moved;
            do
            {
                moved = false;
                for (int c = 0; c < columnDishes.Count; c++)
                    for (int r = 1; r < columnDishes[c].Count; r++)
                    {
                        if (!IsDishAnchor(c, r))
                            continue;
                        Food dish = columnDishes[c][r];
                        int width = dish.Shape == DishShape.Horizontal ? 2 : 1;
                        int height = dish.Shape == DishShape.Vertical ? 2 : 1;
                        if (columnDishes[c][r - 1] != null || (width == 2 && columnDishes[c + 1][r - 1] != null))
                            continue;
                        for (int x = c; x < c + width; x++)
                        {
                            columnDishes[x][r + height - 1] = null;
                            columnDishes[x][r - 1] = dish;
                        }
                        moved = true;
                    }
            } while (moved);
            AnimateSettledColumns(animate);
        }

        // EN: Trims unused tail cells and moves each anchor once to the position computed by settling.
        // VI: Cắt ô trống cuối cột và di chuyển mỗi ô gốc một lần tới vị trí đã tính sau khi dồn.
        private void AnimateSettledColumns(bool animate)
        {
            for (int c = 0; c < columnDishes.Count; c++)
            {
                List<Food> dishes = columnDishes[c];
                while (dishes.Count > 0 && dishes[dishes.Count - 1] == null)
                    dishes.RemoveAt(dishes.Count - 1);
                List<KeyValuePair<Transform, Vector3>> targets = new List<KeyValuePair<Transform, Vector3>>();
                for (int r = 0; r < dishes.Count; r++)
                {
                    if (!IsDishAnchor(c, r))
                        continue;
                    Vector3 target = GetRowLocalPosition(r);
                    if ((dishes[r].transform.localPosition - target).sqrMagnitude > 0.000001f)
                        targets.Add(new KeyValuePair<Transform, Vector3>(dishes[r].transform, target));
                }
                if (targets.Count == 0)
                    continue;
                Transform column = ingredientColumns[c];
                shiftTargets[column] = targets;
                if (!animate || Config.boardMotion.shiftUpDuration <= 0f)
                    FinishColumnShift(column);
                else
                    columnShifts[column] = StartCoroutine(AnimateColumnShift(column, targets));
            }
        }

#if UNITY_EDITOR
        // EN: Assigns default board assets including double plates, lids, dedicated rings and fallback outline material.
        // VI: Gán asset board mặc định gồm đĩa đôi, nắp, vòng viền riêng và vật liệu viền dự phòng.
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (horizontalDoublePlate == null)
                horizontalDoublePlate = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/CeramicPlateHorizontal.png");
            if (verticalDoublePlate == null)
                verticalDoublePlate = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/CeramicPlateVertical.png");
            if (horizontalDoubleOutline == null)
                horizontalDoubleOutline = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/PlateSelectionOutlineHorizontal.png");
            if (verticalDoubleOutline == null)
                verticalDoubleOutline = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/PlateSelectionOutlineVertical.png");
            if (horizontalDoubleLid == null)
                horizontalDoubleLid = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/PlateLidHorizontal.png");
            if (verticalDoubleLid == null)
                verticalDoubleLid = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/HotPot/Art/Sprites/UI/Dishes/PlateLidVertical.png");
            if (doublePlateOutlineMaterial == null)
                doublePlateOutlineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/HotPot/Art/Materials/Dishes/PlateSilhouette.mat");
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

