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
        [Tooltip("Đóng băng: không chọn được cho tới khi blockValue về 0.")]
        public bool isFrozen;
        [Tooltip("Giá trị khối băng còn lại; giảm 1 mỗi khi người chơi chọn được một đĩa.")]
        public int blockValue;
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
        [Tooltip("Renderer lớp băng phủ lên đĩa khi đóng băng.")]
        [SerializeField] SpriteRenderer iceRenderer;
        [Tooltip("Text hiển thị giá trị khối băng còn lại.")]
        [SerializeField] TextMeshPro blockText;
        [Tooltip("Thời gian (giây) khối băng nảy mỗi khi bị trừ giá trị.")]
        [Min(0.01f)] [SerializeField] float iceHitDuration = 0.22f;
        [Tooltip("Độ nảy tối đa của khối băng khi bị trừ giá trị (0.12 = phồng 12%).")]
        [Range(0f, 0.5f)] [SerializeField] float iceHitAmount = 0.12f;

        private Sprite[] iceBlockSprites;
        private Vector3 iceBaseScale = Vector3.one;
        private bool iceScaleCaptured;
        private Coroutine iceHitRoutine;
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
        public bool IsFrozen => isFrozen && blockValue > 0;
        public SpriteRenderer IceRenderer => iceRenderer;

        // EN: Restores the hidden appearance when a scene or pooled dish becomes active.
        // VI: Khôi phục hình ảnh ẩn khi đĩa scene hoặc đĩa từ pool được bật.
        private void OnEnable()
        {
            RefreshHiddenVisual();
            RefreshFrozenVisual();
        }

        // EN: Changes concealment without changing ingredient type, quantity or preparation state.
        // VI: Đổi trạng thái che mà không đổi loại nguyên liệu, số lượng hoặc trạng thái sơ chế.
        public void SetHidden(bool hidden)
        {
            isHidden = hidden;
            RefreshHiddenVisual();
            RefreshCountVisual();
        }

        // EN: Covers hidden food with the lid and hides its ingredient/shadow objects.
        // VI: Che đồ ăn ẩn bằng nắp và tắt hình nguyên liệu/bóng.
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

        // EN: Freezes the dish with the given block value, or unfreezes it when frozen is false or the value is not positive.
        // VI: Đóng băng đĩa với giá trị khối băng đã cho, hoặc bỏ đóng băng khi frozen là false hoặc giá trị không dương.
        public void SetFrozen(bool frozen, int value)
        {
            isFrozen = frozen && value > 0;
            blockValue = isFrozen ? value : 0;
            StopIceHit();
            RefreshFrozenVisual();
            RefreshCountVisual();
        }

        // EN: Lowers the block value by one; returns true when this thawed the dish.
        // VI: Giảm giá trị khối băng đi một; trả về true khi lần giảm này làm đĩa tan băng.
        public bool ReduceBlock()
        {
            if (!IsFrozen)
                return false;
            blockValue--;
            if (blockValue <= 0)
                isFrozen = false;
            RefreshFrozenVisual();
            if (isFrozen)
                PlayIceHit();
            RefreshCountVisual();
            return !isFrozen;
        }

        // EN: Shows the ice layer and the remaining block value while frozen, drawn above the plate, food and lid.
        // VI: Hiện lớp băng và giá trị khối băng còn lại khi đóng băng, vẽ trên đĩa, món và nắp.
        public void RefreshFrozenVisual()
        {
            bool frozen = IsFrozen;
            if (iceRenderer != null)
            {
                iceRenderer.gameObject.SetActive(frozen);
                if (plateRenderer != null)
                {
                    iceRenderer.sortingLayerID = plateRenderer.sortingLayerID;
                    iceRenderer.sortingOrder = plateRenderer.sortingOrder + 5;
                }
                ApplyIceStage();
            }
            if (blockText == null)
                return;
            blockText.gameObject.SetActive(frozen);
            blockText.text = blockValue.ToString();
            if (plateRenderer != null)
            {
                blockText.sortingLayerID = plateRenderer.sortingLayerID;
                blockText.sortingOrder = plateRenderer.sortingOrder + 6;
            }
        }

        // EN: Receives the shared ice sprites from the board, ordered from about-to-thaw (block value 1) to thickest.
        // VI: Nhận bộ sprite băng dùng chung từ board, xếp từ sắp tan (giá trị băng 1) tới dày nhất.
        public void SetIceBlockSprites(Sprite[] sprites)
        {
            iceBlockSprites = sprites;
            RefreshFrozenVisual();
        }

        // EN: Shows the ice sprite for the remaining block value: value 1 uses the first sprite, higher values use thicker ones (capped at the last).
        // VI: Hiện sprite băng theo giá trị băng còn lại: giá trị 1 dùng sprite đầu, giá trị lớn hơn dùng sprite dày hơn (tối đa sprite cuối).
        private void ApplyIceStage()
        {
            if (iceBlockSprites == null || iceBlockSprites.Length == 0)
                return;
            int index = Mathf.Clamp(blockValue - 1, 0, iceBlockSprites.Length - 1);
            if (iceBlockSprites[index] != null)
                iceRenderer.sprite = iceBlockSprites[index];
        }

        // EN: Gives the ice block a quick squash-and-pop when it loses a point.
        // VI: Làm khối băng nảy nhanh (bẹp rồi bật lại) khi bị trừ một điểm.
        private void PlayIceHit()
        {
            if (iceRenderer == null || !isActiveAndEnabled)
                return;
            StopIceHit();
            iceHitRoutine = StartCoroutine(IceHit());
        }

        // EN: Animates the ice scale: a short squash followed by a decaying wobble back to rest.
        // VI: Hoạt cảnh scale của băng: bẹp nhanh rồi dao động tắt dần về trạng thái nghỉ.
        private System.Collections.IEnumerator IceHit()
        {
            Transform ice = iceRenderer.transform;
            if (!iceScaleCaptured)
            {
                iceBaseScale = ice.localScale;
                iceScaleCaptured = true;
            }
            float time = 0f;
            while (time < iceHitDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / iceHitDuration);
                float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * iceHitAmount;
                ice.localScale = Vector3.Scale(iceBaseScale, new Vector3(1f + wobble, 1f - wobble, 1f));
                yield return null;
            }
            ice.localScale = iceBaseScale;
            iceHitRoutine = null;
        }

        // EN: Stops a running ice wobble and restores the ice's resting scale.
        // VI: Dừng dao động băng đang chạy và trả băng về scale gốc.
        private void StopIceHit()
        {
            if (iceHitRoutine != null)
                StopCoroutine(iceHitRoutine);
            iceHitRoutine = null;
            if (iceScaleCaptured && iceRenderer != null)
                iceRenderer.transform.localScale = iceBaseScale;
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
            SetFrozen(false, 0);
        }

        // EN: Assigns the shared icon table and immediately updates the current state's sprite.
        // VI: Gán bảng icon dùng chung và cập nhật ngay sprite theo trạng thái hiện tại.
        public void SetIconConfig(FoodIconConfig config)
        {
            foodIconConfig = config;
            if (config != null)
                SetIcon(config.GetIcon(foodType, state));
        }

        // EN: Changes the amount and refreshes the count badge.
        // VI: Đổi số lượng và cập nhật nhãn số lượng.
        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(0, newAmount);
            if (txtAmount != null)
                txtAmount.text = amount.ToString();
            RefreshCountVisual();
        }

        // EN: Shows the quantity badge only for visible, unfrozen dishes holding more than one portion.
        // VI: Chỉ hiện nhãn số lượng cho đĩa không bị ẩn, không đóng băng và có nhiều hơn một phần.
        private void RefreshCountVisual()
        {
            if (canvasCount != null)
                canvasCount.SetActive(amount > 1 && !isHidden && !IsFrozen);
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
