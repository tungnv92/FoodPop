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

        // EN: Raised after a head dish is picked: gives its food type and the conveyor slots that received its portions.
        // VI: Phát ra sau khi chọn đĩa đầu cột: cho biết loại nguyên liệu và các slot băng chuyền đã nhận từng phần của đĩa.
        public event Action<FoodType, IReadOnlyList<ConveyorSlot>> DishPicked;

        private readonly List<ConveyorSlot> targetSlots = new List<ConveyorSlot>();

        private readonly Dictionary<Transform, Coroutine> columnShifts = new Dictionary<Transform, Coroutine>();
        private readonly Dictionary<Transform, List<KeyValuePair<Transform, Vector3>>> shiftTargets = new Dictionary<Transform, List<KeyValuePair<Transform, Vector3>>>();

        private IngredientDishPool dishPool;
        private Vector3 dishLocalScale;
        private Quaternion dishLocalRotation;
        private int dishLayer;
        private readonly Dictionary<string, int> dishSortingOrders = new Dictionary<string, int>();
        private int dishSortingLayer;
        private int quantitySortingOrder;

        // EN: Reports newly created dishes so pool growth can be observed independently of reused scene dishes.
        // VI: Cho biết số đĩa tạo mới để theo dõi pool tăng thêm, không tính đĩa scene tái sử dụng.
        public int CreatedDishCount => dishPool != null ? dishPool.CreatedCount : 0;

        // EN: Reports the number of inactive dishes ready for reuse.
        // VI: Cho biết số đĩa không hoạt động đang sẵn sàng tái sử dụng.
        public int AvailableDishCount => dishPool != null ? dishPool.AvailableCount : 0;

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

        // EN: Picks the selectable head dish whose plate contains the given world point, if any.
        // VI: Chọn đĩa đầu cột có vùng đĩa chứa điểm world đã cho, nếu có.
        public bool TryPickAt(Vector2 worldPoint)
        {
            if (ingredientColumns == null)
                return false;
            foreach (Transform column in ingredientColumns)
            {
                if (column == null || !column.gameObject.activeInHierarchy)
                    continue;
                Food head = GetColumnHead(column);
                if (head != null && ContainsPoint(head, worldPoint))
                    return TryPickDish(head);
            }
            return false;
        }

        // EN: Picks a column's head dish: puts one portion per amount onto the nearest empty conveyor slots, removes the dish from the column, slides the rest up one row and raises DishPicked. Fails without changes when there are not enough empty slots.
        // VI: Chọn đĩa đầu cột: đặt mỗi phần (theo amount) vào các slot rỗng gần đĩa nhất trên băng chuyền, gỡ đĩa khỏi cột, đẩy các đĩa còn lại lên một hàng và phát DishPicked. Không thay đổi gì nếu không đủ slot rỗng.
        public bool TryPickDish(Food dish)
        {
            if (dish == null || dish.transform.parent == null)
                return false;
            Transform column = dish.transform.parent;
            if (GetColumnHead(column) != dish)
                return false;
            FoodType type = dish.foodType;
            if (!TryFillConveyorSlots(type, dish.amount, dish.transform.position))
                return false;
            FinishColumnShift(column);
            RemoveFromBoard(dish);
            ShiftColumnUp(column);
            RefreshSelectableDishes();
            DishPicked?.Invoke(type, targetSlots);
            return true;
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

        // EN: Takes a picked dish off the board, returning it to the pool (or just hiding it when it is not pooled).
        // VI: Gỡ đĩa đã chọn khỏi bảng, trả về pool (hoặc chỉ ẩn đi nếu đĩa không thuộc pool).
        private void RemoveFromBoard(Food dish)
        {
            if (dishPool == null || !dishPool.Release(dish))
                dish.gameObject.SetActive(false);
        }

        // EN: Returns the highest active nonempty dish of a column (its current head), or null.
        // VI: Trả về đĩa có đồ ăn đang hoạt động cao nhất của cột (đầu cột hiện tại), hoặc null.
        private static Food GetColumnHead(Transform column)
        {
            Food first = null;
            foreach (Transform child in column)
            {
                Food food = child.GetComponent<Food>();
                if (food == null || !food.gameObject.activeInHierarchy || food.foodType == FoodType.None || food.amount <= 0)
                    continue;
                if (first == null || child.localPosition.y > first.transform.localPosition.y)
                    first = food;
            }
            return first;
        }

        // EN: Hit-tests a point against the dish's plate sprite bounds (falls back to any sprite of the dish).
        // VI: Kiểm tra điểm có nằm trong vùng sprite đĩa không (nếu không có Plate thì dùng sprite bất kỳ của đĩa).
        private static bool ContainsPoint(Food dish, Vector2 worldPoint)
        {
            SpriteRenderer area = null;
            foreach (SpriteRenderer renderer in dish.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.name == "Plate")
                {
                    area = renderer;
                    break;
                }
                if (area == null)
                    area = renderer;
            }
            if (area == null)
                return false;
            Bounds bounds = area.bounds;
            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x &&
                   worldPoint.y >= bounds.min.y && worldPoint.y <= bounds.max.y;
        }

        // EN: Moves every remaining active dish of the column up by one row, animated over shiftUpDuration.
        // VI: Đẩy mọi đĩa còn hoạt động trong cột lên một hàng, có hiệu ứng trượt trong shiftUpDuration.
        private void ShiftColumnUp(Transform column)
        {
            List<KeyValuePair<Transform, Vector3>> targets = new List<KeyValuePair<Transform, Vector3>>();
            foreach (Transform child in column)
            {
                if (!child.gameObject.activeSelf || child.GetComponent<Food>() == null)
                    continue;
                targets.Add(new KeyValuePair<Transform, Vector3>(child, child.localPosition + Vector3.up * rowSpacing));
            }
            if (targets.Count == 0)
                return;
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

        // EN: Tracks removal, reparenting and depletion so only each column's current head stays highlighted.
        // VI: Theo dõi đĩa bị lấy, đổi parent hoặc hết số lượng để chỉ phần tử đầu mỗi cột có viền.
        private void LateUpdate()
        {
            RefreshSelectableDishes();
        }

        // EN: Reveals and highlights only the highest active nonempty dish in each column; lower hidden dishes stay covered.
        // VI: Chỉ mở nắp và hiện viền đĩa có đồ ăn đang hoạt động cao nhất mỗi cột; các đĩa ẩn phía dưới giữ nắp.
        public void RefreshSelectableDishes()
        {
            if (ingredientColumns == null)
                return;
            foreach (Transform column in ingredientColumns)
            {
                if (column == null)
                    continue;
                Food first = null;
                foreach (Transform child in column)
                {
                    Food food = child.GetComponent<Food>();
                    if (food == null || !food.gameObject.activeInHierarchy || food.foodType == FoodType.None || food.amount <= 0)
                        continue;
                    if (first == null || child.localPosition.y > first.transform.localPosition.y)
                        first = food;
                }
                if (first != null && first.isHidden)
                    first.SetHidden(false);
                foreach (Transform child in column)
                {
                    DishSelectionOutline selection = child.GetComponent<DishSelectionOutline>();
                    if (selection != null)
                        selection.SetSelectable(first != null && child == first.transform);
                }
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
            if (!EnsureDishPool(columns))
                return;
            int upperLimit = Mathf.Min(MaxColumns, columns.Count);
            int columnCount = Mathf.Clamp(board.Count, Mathf.Min(MinColumns, upperLimit), upperLimit);
            if (board.Count < MinColumns || board.Count > columnCount)
                Debug.LogWarning($"[FoodBoardManager] Bảng có {board.Count} cột, chỉ hỗ trợ {MinColumns}-{MaxColumns} cột (scene có {columns.Count}); dùng {columnCount} cột.", this);

            foreach (Transform column in columns)
                FinishColumnShift(column);
            ReturnBoardDishes(columns);
            for (int c = 0; c < columns.Count; c++)
            {
                bool used = c < columnCount;
                columns[c].gameObject.SetActive(used);
                if (used)
                {
                    List<OrderFood> foods = c < board.Count ? board[c]?.foods : null;
                    FillColumn(columns[c], foods);
                }
            }
            RefreshSelectableDishes();
        }

        // EN: Rents cells at local row positions and restores the level's hidden flag before head selection reveals the top dish.
        // VI: Lấy đĩa theo tọa độ hàng cục bộ và gán cờ ẩn từ level trước khi đĩa đầu được mở nắp.
        private void FillColumn(Transform column, List<OrderFood> foods)
        {
            if (foods == null)
                return;
            for (int r = 0; r < foods.Count; r++)
            {
                OrderFood cell = foods[r];
                if (cell == null || cell.foodType == FoodType.None)
                    continue;
                Food dish = dishPool.Get(column);
                if (dish == null)
                    continue;
                dish.transform.localScale = dishLocalScale;
                dish.transform.localRotation = dishLocalRotation;
                dish.transform.localPosition = new Vector3(0f, -rowSpacing * r, 0f);
                dish.Setup(cell.foodType, cell.requiredCount, GetIcon(cell.foodType), cell.isHidden);
                dish.SetIconConfig(foodIconConfig);
                dish.name = $"Ingredient dish · {cell.foodType}";
                ApplyDishVisuals(dish);
                dish.gameObject.SetActive(true);
            }
        }

        // EN: Captures dish visuals before adopting scene dishes into the reusable pool; row spacing comes from the inspector.
        // VI: Ghi lại hình ảnh đĩa trước khi đưa đĩa scene vào pool tái sử dụng; khoảng cách hàng lấy từ inspector.
        private bool EnsureDishPool(List<Transform> columns)
        {
            if (dishPool != null)
                return true;
            if (ingredientDishPrefab == null)
            {
                Debug.LogWarning("[FoodBoardManager] Chưa gán prefab đĩa nguyên liệu cho pool.", this);
                return false;
            }
            Food template = FindDishTemplate(columns);
            if (template == null)
                template = ingredientDishPrefab;
            dishLocalScale = template.transform.localScale;
            dishLocalRotation = template.transform.localRotation;
            dishLayer = template.gameObject.layer;
            foreach (SpriteRenderer renderer in template.GetComponentsInChildren<SpriteRenderer>(true))
            {
                dishSortingOrders[renderer.name] = renderer.sortingOrder;
                dishSortingLayer = renderer.sortingLayerID;
            }
            Canvas quantityCanvas = template.GetComponentInChildren<Canvas>(true);
            quantitySortingOrder = quantityCanvas != null ? quantityCanvas.sortingOrder : 0;
            if (dishPoolRoot == null)
            {
                dishPoolRoot = new GameObject("IngredientDishPool").transform;
                dishPoolRoot.SetParent(transform, false);
            }
            dishPoolRoot.gameObject.SetActive(false);
            dishPool = new IngredientDishPool(ingredientDishPrefab, dishPoolRoot);
            foreach (Transform column in columns)
                foreach (Food dish in GetDishes(column))
                    dishPool.Adopt(dish);
            return true;
        }

        // EN: Returns dishes still on the board; food already moved to the conveyor is left with its current owner.
        // VI: Trả đĩa còn trên board; nguyên liệu đã chuyển lên băng chuyền được giữ cho bên đang quản lý.
        private void ReturnBoardDishes(List<Transform> columns)
        {
            foreach (Transform column in columns)
                foreach (Food dish in GetDishes(column))
                    dishPool.Adopt(dish);
        }

        // EN: Returns an owned dish to the pool and immediately promotes the next selectable dish.
        // VI: Trả đĩa thuộc pool và cập nhật ngay đĩa kế tiếp được chọn.
        public bool ReleaseDish(Food dish)
        {
            bool released = dishPool != null && dishPool.Release(dish);
            if (released)
                RefreshSelectableDishes();
            return released;
        }

        // EN: Restores the board's renderer layers and sorting so newly created prefab dishes match scene dishes.
        // VI: Khôi phục layer và thứ tự vẽ để đĩa prefab tạo mới giống đĩa hiện có trong scene.
        private void ApplyDishVisuals(Food dish)
        {
            foreach (Transform child in dish.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = dishLayer;
            foreach (SpriteRenderer renderer in dish.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerID = dishSortingLayer;
                if (dishSortingOrders.TryGetValue(renderer.name, out int order))
                    renderer.sortingOrder = order;
            }
            foreach (Canvas canvas in dish.GetComponentsInChildren<Canvas>(true))
                canvas.sortingOrder = quantitySortingOrder;
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

        // EN: Returns the Food dishes that are direct children of a column, in hierarchy order (top to bottom).
        // VI: Trả về các đĩa Food là con trực tiếp của một cột, theo thứ tự hierarchy (trên xuống dưới).
        private static List<Food> GetDishes(Transform column)
        {
            List<Food> dishes = new List<Food>();
            foreach (Transform child in column)
            {
                Food food = child.GetComponent<Food>();
                if (food != null)
                    dishes.Add(food);
            }
            return dishes;
        }

        // EN: Uses the first dish found in the columns as the template for cloning new dishes.
        // VI: Dùng đĩa đầu tiên tìm thấy trong các cột làm mẫu để nhân bản đĩa mới.
        private static Food FindDishTemplate(List<Transform> columns)
        {
            foreach (Transform column in columns)
            {
                List<Food> dishes = GetDishes(column);
                if (dishes.Count > 0)
                    return dishes[0];
            }
            return null;
        }

        // EN: Returns the icon of a food type from the FoodIconConfig, or null.
        // VI: Lấy icon của một loại nguyên liệu từ FoodIconConfig, hoặc null.
        private Sprite GetIcon(FoodType type)
        {
            return foodIconConfig != null ? foodIconConfig.GetIcon(type) : null;
        }

#if UNITY_EDITOR
        // EN: Assigns default icon and dish-prefab assets without discovering scene columns.
        // VI: Gán asset icon và prefab đĩa mặc định, không tự tìm cột trong scene.
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (foodIconConfig == null)
                foodIconConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<FoodIconConfig>("Assets/HotPot/Config/FoodIconConfig.asset");
            if (ingredientDishPrefab == null)
                ingredientDishPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Food>("Assets/HotPot/Prefabs/Ingame/IngredientDish.prefab");
        }
#endif
    }
}


