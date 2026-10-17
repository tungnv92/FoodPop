using System;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: A food item in play: its ingredient type and processing state (raw -> prepared).
    // VI: Một nguyên liệu trong game: loại nguyên liệu và trạng thái chế biến (thô -> đã sơ chế).
    public class Food : MonoBehaviour
    {
        public FoodType foodType;
        public int amount;
        [Tooltip("Che nguyên liệu bằng nắp; tự mở khi đĩa trở thành phần tử đầu cột.")]
        public bool isHidden;
        [Tooltip("Bắt đầu là Raw (thô); thành Prepared (đã sơ chế) sau khi sơ chế.")]
        public FoodState state = FoodState.Raw;
        [SerializeField] GameObject canvasCount;
        [SerializeField] TextMeshProUGUI txtAmount;
        [SerializeField] SpriteRenderer iconRenderer;
        [Tooltip("Bảng ảnh nguyên liệu thô và đã sơ chế.")]
        [SerializeField] FoodIconConfig foodIconConfig;
        [Tooltip("Renderer nắp tròn dùng để che nguyên liệu trên đĩa.")]
        [SerializeField] SpriteRenderer lidRenderer;
        [Tooltip("Renderer đĩa; nắp luôn được vẽ ngay trên đĩa này.")]
        [SerializeField] SpriteRenderer plateRenderer;
        [Tooltip("Viền chọn của đĩa trên board (để trống với món trên băng chuyền).")]
        [SerializeField] DishSelectionOutline selection;
        [Tooltip("Mọi SpriteRenderer của đĩa, dùng để đồng bộ layer và thứ tự vẽ khi lấy từ pool.")]
        [SerializeField] SpriteRenderer[] visualRenderers = new SpriteRenderer[0];
        [Tooltip("Canvas hiển thị số lượng của đĩa.")]
        [SerializeField] Canvas countCanvas;
        // EN: Raised whenever the state changes (e.g. to swap the sprite or play an effect).
        // VI: Phát ra mỗi khi trạng thái thay đổi (ví dụ để đổi sprite hoặc chạy hiệu ứng).
        public event Action<Food, FoodState> StateChanged;

        public bool IsRaw => state == FoodState.Raw;

        // EN: The icon table assigned to this food, used to look up its sprites and preparation group.
        // VI: Bảng icon gán cho nguyên liệu này, dùng để tra sprite và nhóm sơ chế.
        public FoodIconConfig IconConfig => foodIconConfig;
        public bool IsPrepared => state == FoodState.Prepared;

        // EN: Direct references used by managers instead of searching children at runtime.
        // VI: Các tham chiếu trực tiếp để manager dùng thay cho việc tìm child lúc chạy.
        public SpriteRenderer IconRenderer => iconRenderer;
        public SpriteRenderer PlateRenderer => plateRenderer;
        public DishSelectionOutline Selection => selection;
        public SpriteRenderer[] VisualRenderers => visualRenderers;
        public Canvas CountCanvas => countCanvas;

        // EN: Restores the hidden appearance when a scene or pooled dish becomes active.
        // VI: Khôi phục hình ảnh ẩn khi đĩa scene hoặc đĩa từ pool được bật.
        private void OnEnable()
        {
            RefreshHiddenVisual();
        }

        // EN: Reflects Inspector changes to isHidden during Play Mode without polling every frame.
        // VI: Cập nhật thay đổi isHidden trong Inspector khi đang Play mà không phải kiểm tra mỗi frame.
        private void OnValidate()
        {
            if (Application.isPlaying)
                RefreshHiddenVisual();
        }

        // EN: Changes concealment without changing ingredient type, quantity or preparation state.
        // VI: Đổi trạng thái che mà không đổi loại nguyên liệu, số lượng hoặc trạng thái sơ chế.
        public void SetHidden(bool hidden)
        {
            isHidden = hidden;
            RefreshHiddenVisual();
        }

        // EN: Covers hidden food with the lid and hides its ingredient/shadow objects while keeping quantity visible.
        // VI: Che đồ ăn ẩn bằng nắp và tắt hình nguyên liệu/bóng, vẫn giữ số lượng hiển thị.
        public void RefreshHiddenVisual()
        {
            if (iconRenderer != null && iconRenderer.gameObject.activeSelf == isHidden)
                iconRenderer.gameObject.SetActive(!isHidden);
            if (lidRenderer == null)
                return;
            if (lidRenderer.gameObject.activeSelf != isHidden)
                lidRenderer.gameObject.SetActive(isHidden);
            if (plateRenderer == null)
                return;
            lidRenderer.sortingLayerID = plateRenderer.sortingLayerID;
            lidRenderer.sortingOrder = plateRenderer.sortingOrder + 3;
            lidRenderer.gameObject.layer = plateRenderer.gameObject.layer;
        }

        // EN: Turns a raw food into a prepared one; returns false if it was already prepared.
        // VI: Chuyển nguyên liệu thô thành đã sơ chế; trả về false nếu đã sơ chế rồi.
        public bool Prepare()
        {
            if (state != FoodState.Raw)
                return false;
            SetState(FoodState.Prepared);
            return true;
        }

        // EN: Changes processing state, refreshes its sushi icon and notifies listeners.
        // VI: Đổi trạng thái chế biến, cập nhật icon sushi và thông báo cho bên lắng nghe.
        public void SetState(FoodState newState)
        {
            if (state == newState)
                return;
            state = newState;
            if (foodIconConfig != null)
                SetIcon(foodIconConfig.GetIcon(foodType, state));
            StateChanged?.Invoke(this, state);
        }

        // EN: Configures food and concealment from level data, resetting preparation state to Raw.
        // VI: Thiết lập nguyên liệu và trạng thái che từ level, đặt lại trạng thái sơ chế thành thô.
        public void Setup(FoodType type, int newAmount, Sprite icon, bool hidden = false)
        {
            foodType = type;
            state = FoodState.Raw;
            SetIcon(icon);
            SetAmount(newAmount);
            SetHidden(hidden);
        }

        // EN: Assigns the shared icon table and immediately updates the current state's sprite.
        // VI: Gán bảng icon dùng chung và cập nhật ngay sprite theo trạng thái hiện tại.
        public void SetIconConfig(FoodIconConfig config)
        {
            foodIconConfig = config;
            if (config != null)
                SetIcon(config.GetIcon(foodType, state));
        }

        // EN: Changes the amount and shows the count badge only when there is more than one.
        // VI: Đổi số lượng và chỉ hiện nhãn số khi có nhiều hơn một.
        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(0, newAmount);
            if (txtAmount != null)
                txtAmount.text = amount.ToString();
            if (canvasCount != null)
                canvasCount.SetActive(amount > 1);
        }

        // EN: Shows the food sprite on the assigned ingredient renderer.
        // VI: Hiện sprite nguyên liệu trên renderer nguyên liệu đã gán.
        private void SetIcon(Sprite icon)
        {
            if (iconRenderer != null)
                iconRenderer.sprite = icon;
        }
    }
}
