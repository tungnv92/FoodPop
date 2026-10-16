using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: One plate on the conveyor. It starts empty and only receives a Food when an item is picked from the supply below.
    // VI: Một đĩa trên băng chuyền. Mặc định rỗng, chỉ nhận Food khi người chơi pick đồ từ khay bên dưới lên.
    public class ConveyorSlot : MonoBehaviour
    {
        public bool isEmpty = true;
        public Food food;

        [Tooltip("Sprite (tùy chọn) hiện trên đĩa khi slot có đồ ăn (child 'Ingredient').")]
        [SerializeField] private SpriteRenderer ingredientRenderer;

        [Header("Hiệu ứng khi nhận món")]
        [Tooltip("Thời gian (giây) món nảy co giãn khi vừa chạm xuống slot.")]
        [Min(0f)] [SerializeField] private float landSquashDuration = 0.25f;
        [Tooltip("Độ co giãn tối đa khi món chạm slot (0.25 = bẹt ra 25%).")]
        [Range(0f, 0.6f)] [SerializeField] private float landSquashAmount = 0.25f;

        private Food slotFood;
        private Vector3 ingredientBaseScale = Vector3.one;
        private Coroutine squashRoutine;

        // EN: True while a picked portion is still flying toward this slot; the slot is reserved but its food is not shown yet.
        // VI: True khi một phần nguyên liệu vừa chọn vẫn đang bay tới slot; slot đã được giữ chỗ nhưng chưa hiện món.
        public bool IsFoodArriving { get; private set; }

        // EN: World position where a flying portion should land (the Ingredient child, or the slot itself).
        // VI: Vị trí world mà phần nguyên liệu bay sẽ đáp xuống (child Ingredient, hoặc chính slot).
        public Vector3 LandingPosition => ingredientRenderer != null ? ingredientRenderer.transform.position : transform.position;

        // EN: World scale a flying portion should reach on landing, matching the slot's ingredient sprite at rest.
        // VI: Kích thước world mà phần nguyên liệu bay cần đạt khi đáp, khớp với sprite nguyên liệu của slot lúc bình thường.
        public Vector3 LandingScale
        {
            get
            {
                if (ingredientRenderer == null)
                    return transform.lossyScale;
                Transform icon = ingredientRenderer.transform;
                Vector3 parentScale = icon.parent != null ? icon.parent.lossyScale : Vector3.one;
                return Vector3.Scale(parentScale, ingredientBaseScale);
            }
        }

        // EN: World rotation a flying portion should reach on landing.
        // VI: Góc xoay world mà phần nguyên liệu bay cần đạt khi đáp.
        public Quaternion LandingRotation => ingredientRenderer != null ? ingredientRenderer.transform.rotation : transform.rotation;

        // EN: Finds the Ingredient renderer when the component is added in the editor.
        // VI: Tự tìm renderer Ingredient khi component được thêm trong editor.
        private void Reset()
        {
            ResolveIngredientRenderer();
        }

        // EN: Every slot starts the game empty: no food and the ingredient icon hidden; remembers the icon's resting scale for the landing squash.
        // VI: Mọi slot bắt đầu game ở trạng thái rỗng: không có food và ẩn icon nguyên liệu; ghi lại scale gốc của icon cho hiệu ứng nảy khi chạm.
        private void Awake()
        {
            ResolveIngredientRenderer();
            if (ingredientRenderer != null)
                ingredientBaseScale = ingredientRenderer.transform.localScale;
            ClearFood();
        }

        // EN: Marks the slot as waiting for a flying portion and hides its ingredient sprite until the portion lands.
        // VI: Đánh dấu slot đang chờ phần nguyên liệu bay tới và ẩn sprite nguyên liệu cho tới khi phần đó đáp xuống.
        public void BeginFoodArrival()
        {
            StopSquash();
            IsFoodArriving = true;
            SetIngredientVisible(false);
        }

        // EN: Ends the arrival: shows the ingredient sprite and plays the landing squash; returns false when the slot was no longer waiting (e.g. cleared mid-flight).
        // VI: Kết thúc lượt chờ: hiện sprite nguyên liệu và chạy hiệu ứng nảy khi chạm; trả về false nếu slot không còn chờ (ví dụ bị làm rỗng giữa chừng).
        public bool CompleteFoodArrival()
        {
            if (!IsFoodArriving)
                return false;
            IsFoodArriving = false;
            SetIngredientVisible(true);
            if (!isEmpty && landSquashDuration > 0f && landSquashAmount > 0f && ingredientRenderer != null && isActiveAndEnabled)
                squashRoutine = StartCoroutine(PlayLandSquash());
            return true;
        }

        // EN: Squashes the ingredient wide-and-short on impact, then wobbles back to its resting scale with decaying strength.
        // VI: Làm món bẹt ra (rộng và thấp) lúc chạm, sau đó dao động tắt dần về scale gốc.
        private IEnumerator PlayLandSquash()
        {
            Transform icon = ingredientRenderer.transform;
            float time = 0f;
            while (time < landSquashDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / landSquashDuration);
                float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * landSquashAmount;
                icon.localScale = Vector3.Scale(ingredientBaseScale, new Vector3(1f + wobble, 1f - wobble, 1f));
                yield return null;
            }
            icon.localScale = ingredientBaseScale;
            squashRoutine = null;
        }

        // EN: Stops a running landing squash and restores the ingredient's resting scale.
        // VI: Dừng hiệu ứng nảy đang chạy và trả icon nguyên liệu về scale gốc.
        private void StopSquash()
        {
            if (squashRoutine != null)
                StopCoroutine(squashRoutine);
            squashRoutine = null;
            if (ingredientRenderer != null)
                ingredientRenderer.transform.localScale = ingredientBaseScale;
        }

        // EN: Shows or hides the ingredient sprite by toggling the renderer only, leaving the GameObject state to Food and ShowIcon.
        // VI: Hiện hoặc ẩn sprite nguyên liệu bằng cách bật/tắt renderer, không đụng tới trạng thái GameObject do Food và ShowIcon quản lý.
        private void SetIngredientVisible(bool visible)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.enabled = visible;
        }

        // EN: Restores the resting look when the slot is disabled, so a pooled or paused belt never keeps a half-finished squash.
        // VI: Khôi phục hình ảnh gốc khi slot bị tắt, để băng chuyền tạm dừng hoặc tái sử dụng không giữ hiệu ứng nảy dở dang.
        private void OnDisable()
        {
            StopSquash();
        }

        // EN: Puts a picked food on this slot; it becomes a child so it rides the belt. An icon sprite can also be shown on the plate.
        // VI: Đặt food vừa pick lên slot; food thành con của slot để chạy theo băng. Có thể hiện thêm icon trên đĩa.
        public bool TrySetFood(Food newFood, Sprite icon = null)
        {
            if (!isEmpty || newFood == null)
                return false;
            food = newFood;
            isEmpty = false;
            food.transform.SetParent(transform, true);
            food.transform.localPosition = Vector3.zero;
            ShowIcon(icon);
            return true;
        }

        // EN: Fills this empty slot with one raw portion of the given type, using the slot's own Food component (added on first use) so the chef can prepare it.
        // VI: Đặt một phần nguyên liệu thô loại đã cho vào slot rỗng, dùng component Food riêng của slot (tự thêm lần đầu) để đầu bếp có thể sơ chế.
        public bool TryFillFood(FoodType type, FoodIconConfig iconConfig)
        {
            if (!isEmpty || type == FoodType.None)
                return false;
            if (slotFood == null)
                slotFood = GetComponent<Food>();
            if (slotFood == null)
                slotFood = gameObject.AddComponent<Food>();
            slotFood.enabled = true;
            slotFood.foodType = type;
            slotFood.state = FoodState.Raw;
            slotFood.SetIconConfig(iconConfig);
            slotFood.Setup(type, 1, iconConfig != null ? iconConfig.GetIcon(type) : null);
            food = slotFood;
            isEmpty = false;
            return true;
        }

        // EN: Empties the slot and returns the food it held (null if it was already empty); the slot's own Food is switched off so it stops driving the icon. Also cancels any portion still flying here and any landing squash.
        // VI: Làm rỗng slot và trả về food đang giữ (null nếu slot đã rỗng); Food riêng của slot được tắt để không điều khiển icon nữa. Đồng thời hủy phần nguyên liệu đang bay tới và hiệu ứng nảy.
        public Food ClearFood()
        {
            Food removed = food;
            food = null;
            isEmpty = true;
            IsFoodArriving = false;
            StopSquash();
            SetIngredientVisible(true);
            if (slotFood != null)
            {
                slotFood.foodType = FoodType.None;
                slotFood.enabled = false;
            }
            ShowIcon(null);
            return removed;
        }

        // EN: Shows the given sprite on the Ingredient child, or hides it when the sprite is null.
        // VI: Hiện sprite trên child Ingredient, hoặc ẩn đi khi sprite là null.
        private void ShowIcon(Sprite icon)
        {
            if (ingredientRenderer == null)
                return;
            ingredientRenderer.sprite = icon;
            ingredientRenderer.gameObject.SetActive(icon != null);
        }

        // EN: Looks up the SpriteRenderer of the child named "Ingredient" when it is not assigned.
        // VI: Tìm SpriteRenderer của child tên "Ingredient" khi chưa được gán.
        private void ResolveIngredientRenderer()
        {
            if (ingredientRenderer != null)
                return;
            Transform child = transform.Find("Ingredient");
            if (child != null)
                ingredientRenderer = child.GetComponent<SpriteRenderer>();
        }
    }
}
