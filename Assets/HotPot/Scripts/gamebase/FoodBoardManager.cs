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
        [Tooltip("Thời gian (giây) các đĩa còn lại trong cột trượt lên một hàng sau khi đĩa đầu được chọn.")]
        [Min(0f)] [SerializeField] private float shiftUpDuration = 0.18f;
        [Tooltip("Khoảng cách theo trục Y (đơn vị world cục bộ của cột) giữa hai đĩa liền nhau trong cùng một cột.")]
        [Min(0f)] [SerializeField] private float rowSpacing = 1.31f;
        [Tooltip("Khoảng cách theo trục X (đơn vị world cục bộ của object cha) giữa hai cột liền nhau; các cột đang dùng được căn giữa tại x = 0 (ví dụ 1 với 3 cột: -1, 0, 1). Đặt 0 thì các cột chồng lên nhau.")]
        [Min(0f)] [SerializeField] private float columnSpacing;
        [Tooltip("Hoạt cảnh món nhảy vòng cung lên băng chuyền (component trên cùng GameObject).")]
        [SerializeField] private FoodFlyAnimator flyAnimator;
        [Tooltip("Hiệu ứng vụn băng bắn ra mỗi khi khối băng bị trừ giá trị.")]
        [SerializeField] private ParticleEffect iceHitEffect;
        [Tooltip("Hiệu ứng băng vỡ tung khi khối băng về 0 và tan.")]
        [SerializeField] private ParticleEffect iceBreakEffect;
        [Tooltip("Các đĩa đặt sẵn trong scene; được đưa vào pool khi nạp level đầu tiên.")]
        [SerializeField] private List<Food> sceneDishes = new List<Food>();

        // EN: Raised after a head dish is picked: gives its food type and the conveyor slots that received its portions.
        // VI: Phát ra sau khi chọn đĩa đầu cột: cho biết loại nguyên liệu và các slot băng chuyền đã nhận từng phần của đĩa.
        public event Action<FoodType, IReadOnlyList<ConveyorSlot>> DishPicked;

        private readonly List<ConveyorSlot> targetSlots = new List<ConveyorSlot>();

        private readonly Dictionary<Transform, Coroutine> columnShifts = new Dictionary<Transform, Coroutine>();
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
            if (TryGetPointerDown(out Vector2 worldPoint))
                TryPickAt(worldPoint);
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

        // EN: Picks an unfrozen head dish, reserves conveyor slots, shifts the column, then damages every remaining frozen block after success.
        // VI: Chọn đĩa đầu cột không đóng băng, giữ slot băng chuyền, dồn cột rồi gây sát thương mọi khối băng còn lại sau khi chọn thành công.
        public bool TryPickColumn(int columnIndex)
        {
            Food dish = GetColumnHead(columnIndex);
            if (dish == null || dish.IsFrozen)
                return false;
            FoodType type = dish.foodType;
            if (!TryFillConveyorSlots(type, dish.amount, dish.transform.position))
                return false;
            Transform column = ingredientColumns[columnIndex];
            LaunchPickAnimation(dish, type);
            FinishColumnShift(column);
            columnDishes[columnIndex].RemoveAt(0);
            RemoveFromBoard(dish);
            ShiftColumnUp(columnIndex);
            RefreshColumnSelection(columnIndex);
            ReduceFrozenBlocks();
            DishPicked?.Invoke(type, targetSlots);
            return true;
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

        // EN: Lowers one frozen dish's block value and bursts ice shards from it (a bigger burst when it breaks); returns true when the dish thawed.
        // VI: Giảm giá trị băng của một đĩa đóng băng và bắn vụn băng từ đó (bắn mạnh hơn khi vỡ); trả về true khi đĩa tan băng.
        private bool DamageIce(Food dish)
        {
            if (!dish.IsFrozen)
                return false;
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

        // EN: Moves every remaining dish of the column up by one row, animated over shiftUpDuration.
        // VI: Đẩy mọi đĩa còn lại trong cột lên một hàng, có hiệu ứng trượt trong shiftUpDuration.
        private void ShiftColumnUp(int columnIndex)
        {
            Transform column = ingredientColumns[columnIndex];
            List<Food> dishes = columnDishes[columnIndex];
            if (dishes.Count == 0)
                return;
            List<KeyValuePair<Transform, Vector3>> targets = new List<KeyValuePair<Transform, Vector3>>(dishes.Count);
            foreach (Food dish in dishes)
                targets.Add(new KeyValuePair<Transform, Vector3>(dish.transform, dish.transform.localPosition + Vector3.up * rowSpacing));
            shiftTargets[column] = targets;
            if (shiftUpDuration <= 0f)
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
            while (time < shiftUpDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / shiftUpDuration));
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

        // EN: Reveals and highlights only the head dish of one column; the other dishes are dimmed and keep their lids.
        // VI: Chỉ mở nắp và hiện viền đĩa đầu của một cột; các đĩa còn lại bị làm tối và giữ nắp.
        private void RefreshColumnSelection(int columnIndex)
        {
            List<Food> dishes = columnDishes[columnIndex];
            for (int i = 0; i < dishes.Count; i++)
            {
                Food dish = dishes[i];
                if (i == 0 && dish.isHidden)
                    dish.SetHidden(false);
                if (dish.Selection != null)
                    dish.Selection.SetSelectable(i == 0 && !dish.IsFrozen);
            }
        }

        // EN: Recycles and fills the board from local Y zero, keeping every column at its scene position, then highlights the first selectable dish in each column.
        // VI: Tái sử dụng và điền board từ Y cục bộ bằng không, giữ nguyên vị trí các cột như trong scene, rồi hiện viền đĩa đầu có thể chọn ở mỗi cột.
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
            ReturnBoardDishes();
            ApplyColumnSpacing(columnCount);
            for (int c = 0; c < columns.Count; c++)
            {
                bool used = c < columnCount;
                columns[c].gameObject.SetActive(used);
                List<OrderFood> foods = used && c < board.Count ? board[c]?.foods : null;
                FillColumn(c, foods);
                RefreshColumnSelection(c);
            }
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
        // EN: Assigns default icon, ice-shell and dish-prefab assets without discovering scene columns.
        // VI: Gán asset icon, sprite lớp băng và prefab đĩa mặc định, không tự tìm cột trong scene.
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
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


