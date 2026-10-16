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

        private Food slotFood;

        // EN: Finds the Ingredient renderer when the component is added in the editor.
        // VI: Tự tìm renderer Ingredient khi component được thêm trong editor.
        private void Reset()
        {
            ResolveIngredientRenderer();
        }

        // EN: Every slot starts the game empty: no food and the ingredient icon hidden.
        // VI: Mọi slot bắt đầu game ở trạng thái rỗng: không có food và ẩn icon nguyên liệu.
        private void Awake()
        {
            ResolveIngredientRenderer();
            ClearFood();
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

        // EN: Empties the slot and returns the food it held (null if it was already empty); the slot's own Food is switched off so it stops driving the icon.
        // VI: Làm rỗng slot và trả về food đang giữ (null nếu slot đã rỗng); Food riêng của slot được tắt để không điều khiển icon nữa.
        public Food ClearFood()
        {
            Food removed = food;
            food = null;
            isEmpty = true;
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
