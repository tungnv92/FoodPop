using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: A food item in play: its ingredient type and processing state (raw -> prepared).
    // VI: Một nguyên liệu trong game: loại nguyên liệu và trạng thái chế biến (thô -> đã sơ chế).
    public class Food : MonoBehaviour
    {
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

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

        private Sprite[] iceBlockSprites;
        private Vector3 iceBaseScale = Vector3.one;
        private bool iceScaleCaptured;
        private Coroutine iceHitRoutine;
        // EN: Runtime footprint shape; a depleted half keeps its occupied cell until the whole dish is empty.
        // VI: Kiểu vùng chiếm khi chạy; nửa hết nguyên liệu vẫn giữ ô đến khi cả đĩa trống.
        public DishShape Shape { get; private set; }
        // EN: Ingredient type of the second half, or None for a single dish.
        // VI: Loại nguyên liệu nửa thứ hai, hoặc None với đĩa đơn.
        public FoodType SecondFoodType { get; private set; }
        // EN: Remaining portions in the second half.
        // VI: Số phần còn lại ở nửa thứ hai.
        public int SecondAmount { get; private set; }
        // EN: Counts the portions that require conveyor slots across both halves.
        // VI: Đếm số phần cần slot băng chuyền của cả hai nửa.
        public int TotalAmount => amount + SecondAmount;
        // EN: Indicates whether the dish occupies two board cells.
        // VI: Cho biết đĩa có chiếm hai ô board hay không.
        public bool IsDouble => Shape != DishShape.Single;
        // EN: Exposes the second ingredient as the source of its pickup flight.
        // VI: Cung cấp hình nguyên liệu thứ hai làm nguồn bay khi lấy đĩa.
        public SpriteRenderer SecondIconRenderer => secondIcon;

        private SpriteRenderer secondIcon;
        private GameObject secondCount;
        private TMP_Text secondCountText;
        private Canvas secondCountCanvas;
        private bool shapeRestCaptured;
        private Vector3 plateRestPosition;
        private Vector3 plateRestScale;
        private Sprite singlePlateSprite;
        private Sprite singleLidSprite;
        private Vector3 singleLidScale;
        private Vector3 iceRestPosition;
        private Vector3 iceRestScale;
        private Vector3 blockRestPosition;
        private bool countBadgeSuppressed;

        // EN: Configures the double footprint, plate, selection ring and matching lid while leaving ingredient icons unstretched.
        // VI: Thiết lập vùng chiếm đĩa đôi, hình đĩa, vòng viền và nắp tương ứng, không kéo giãn icon nguyên liệu.
        public void ConfigureShape(DishShape shape, FoodType secondType, int secondAmount, Vector3 localOffset, Sprite doublePlate = null, Material outlineMaterial = null, Sprite selectionSprite = null, Sprite doubleLid = null)
        {
            CaptureShapeRest();
            StopIceHit();
            Shape = shape;
            SecondFoodType = IsDouble ? secondType : FoodType.None;
            SecondAmount = IsDouble ? Mathf.Max(0, secondAmount) : 0;
            ApplyShapeFootprint(localOffset, doublePlate, outlineMaterial, selectionSprite);
            ConfigureShapeLid(doubleLid);
            ConfigureSecondVisual(secondType, localOffset);
            RefreshSecondVisual();
        }

        // EN: Captures the original single plate, lid and transforms so pooling restores their appearance.
        // VI: Ghi lại đĩa đơn, nắp và transform gốc để khôi phục hình ảnh khi tái sử dụng từ pool.
        private void CaptureShapeRest()
        {
            if (!shapeRestCaptured)
            {
                plateRestPosition = plateRenderer != null ? plateRenderer.transform.localPosition : Vector3.zero;
                plateRestScale = plateRenderer != null ? plateRenderer.transform.localScale : Vector3.one;
                singlePlateSprite = plateRenderer != null ? plateRenderer.sprite : null;
                singleLidSprite = lidRenderer != null ? lidRenderer.sprite : null;
                CaptureLidRest();
                singleLidScale = lidRestScale;
                iceRestPosition = iceRenderer != null ? iceRenderer.transform.localPosition : Vector3.zero;
                iceRestScale = iceRenderer != null ? iceRenderer.transform.localScale : Vector3.one;
                blockRestPosition = blockText != null ? blockText.transform.localPosition : Vector3.zero;
                shapeRestCaptured = true;
            }
        }

        // EN: Fits an orientation-specific lid to the plate and restores the round lid for single dishes without changing reveal animations.
        // VI: Căn nắp theo hướng vào đĩa và khôi phục nắp tròn cho đĩa đơn, giữ nguyên hoạt cảnh mở nắp.
        private void ConfigureShapeLid(Sprite doubleLid)
        {
            if (lidRenderer == null)
                return;
            bool rectangular = IsDouble && doubleLid != null && plateRenderer != null && plateRenderer.sprite != null;
            lidRenderer.sprite = rectangular ? doubleLid : singleLidSprite;
            if (rectangular)
            {
                Vector3 plateSize = plateRenderer.sprite.bounds.size;
                Vector3 lidSize = doubleLid.bounds.size;
                lidRestScale = new Vector3(plateSize.x / lidSize.x, plateSize.y / lidSize.y, singleLidScale.z);
            }
            RestoreLidRest();
        }

        // EN: Fits the plate and its dedicated selection ring to two-cell dimensions while preserving the lid size.
        // VI: Căn đĩa và vòng viền chọn riêng vào kích thước hai ô, giữ nguyên kích thước nắp.
        private void ApplyShapeFootprint(Vector3 localOffset, Sprite doublePlate, Material outlineMaterial, Sprite selectionSprite)
        {
            Vector3 stretch = Vector3.one;
            if (plateRenderer != null && singlePlateSprite != null)
            {
                Vector3 size = Vector3.Scale(singlePlateSprite.bounds.size, plateRestScale);
                stretch.x += Mathf.Abs(localOffset.x) / Mathf.Max(0.001f, Mathf.Abs(size.x));
                stretch.y += Mathf.Abs(localOffset.y) / Mathf.Max(0.001f, Mathf.Abs(size.y));
                plateRenderer.transform.localPosition = plateRestPosition + localOffset * 0.5f;
                bool rectangular = IsDouble && doublePlate != null;
                plateRenderer.sprite = rectangular ? doublePlate : singlePlateSprite;
                Vector3 targetSize = Vector3.Scale(size, stretch);
                Vector3 spriteSize = plateRenderer.sprite.bounds.size;
                Vector3 plateScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, plateRestScale.z);
                plateRenderer.transform.localScale = plateScale;
                Vector3 previousScale = Vector3.Scale(plateRestScale, stretch);
                lidRestScale = Vector3.Scale(singleLidScale, new Vector3(previousScale.x / plateScale.x, previousScale.y / plateScale.y, 1f));
                RestoreLidRest();
                selection?.ConfigurePlateOutline(rectangular ? doublePlate : null, outlineMaterial, selectionSprite);
            }
            if (iceRenderer != null)
            {
                iceRenderer.transform.localPosition = iceRestPosition + localOffset * 0.5f;
                iceRenderer.transform.localScale = Vector3.Scale(iceRestScale, stretch);
                iceBaseScale = iceRenderer.transform.localScale;
                iceScaleCaptured = true;
            }
            if (blockText != null)
                blockText.transform.localPosition = blockRestPosition + localOffset * 0.5f;
        }

        // EN: Creates reusable second-half visuals and positions them without stretching their contents.
        // VI: Tạo hình nửa thứ hai để tái sử dụng và đặt đúng vị trí mà không kéo giãn nội dung.
        private void ConfigureSecondVisual(FoodType secondType, Vector3 localOffset)
        {
            if (IsDouble && iconRenderer != null)
            {
                if (secondIcon == null)
                {
                    secondIcon = Instantiate(iconRenderer.gameObject, iconRenderer.transform.parent).GetComponent<SpriteRenderer>();
                    secondIcon.name = "Second ingredient";
                }
                secondIcon.transform.localPosition = iconRenderer.transform.localPosition + localOffset;
                secondIcon.sprite = foodIconConfig != null ? foodIconConfig.GetIcon(secondType) : null;
                if (secondCount == null && canvasCount != null)
                {
                    secondCount = Instantiate(canvasCount, canvasCount.transform.parent);
                    secondCount.name = "Second quantity";
                    secondCountText = secondCount.GetComponentInChildren<TMP_Text>(true);
                    secondCountCanvas = secondCount.GetComponentInChildren<Canvas>(true);
                }
                if (secondCount != null)
                    secondCount.transform.localPosition = canvasCount.transform.localPosition + localOffset;
            }
        }

        // EN: Mirrors concealment, fading, brightness and sorting after board or booster visuals change.
        // VI: Đồng bộ che đậy, độ mờ, độ sáng và thứ tự vẽ sau khi hình ảnh board hoặc booster thay đổi.
        private void LateUpdate()
        {
            RefreshSecondVisual();
        }

        // EN: Refreshes the second ingredient and its own quantity badge.
        // VI: Cập nhật nguyên liệu thứ hai và nhãn số lượng riêng.
        private void RefreshSecondVisual()
        {
            if (secondIcon != null)
            {
                secondIcon.gameObject.SetActive(IsDouble && SecondAmount > 0 && iconRenderer != null && iconRenderer.gameObject.activeSelf);
                secondIcon.enabled = SecondAmount > 0;
                if (iconRenderer != null)
                {
                    secondIcon.color = iconRenderer.color;
                    secondIcon.sortingLayerID = iconRenderer.sortingLayerID;
                    secondIcon.sortingOrder = iconRenderer.sortingOrder;
                    secondIcon.gameObject.layer = iconRenderer.gameObject.layer;
                }
            }
            if (secondCount == null)
                return;
            secondCount.SetActive(IsDouble && !countBadgeSuppressed && !isHidden && !IsFrozen && iconRenderer != null && iconRenderer.gameObject.activeSelf);
            if (secondCountText != null)
                secondCountText.text = SecondAmount.ToString();
            if (secondCountCanvas != null && countCanvas != null)
            {
                secondCountCanvas.sortingLayerID = countCanvas.sortingLayerID;
                secondCountCanvas.sortingOrder = countCanvas.sortingOrder;
            }
        }

        // EN: Removes one requested portion from either half without discarding the other half.
        // VI: Lấy một phần nguyên liệu yêu cầu từ một trong hai nửa mà không làm mất nửa còn lại.
        public bool TakePortion(FoodType type)
        {
            if (foodType == type && amount > 0)
                SetAmount(amount - 1);
            else if (SecondFoodType == type && SecondAmount > 0)
                SecondAmount--;
            else
                return false;
            RefreshSecondVisual();
            return true;
        }
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
            SetLidAlpha(1f);
            RestoreLidRest();
            RefreshHiddenVisual();
            RefreshCountVisual();
        }

        // EN: Lifts the lid up by liftHeight (world units) and scales it to scaleUp while it fades out and the ingredient fades in over the given duration; then restores the lid transform and clears the hidden state.
        // VI: Nhấc nắp lên liftHeight (đơn vị thế giới) và phóng to tới scaleUp trong lúc nắp mờ dần và nguyên liệu hiện dần trong thời gian đã cho; sau đó trả lại vị trí/scale nắp và bỏ trạng thái ẩn.
        public IEnumerator FadeOutLid(float duration, float liftHeight = 0f, float scaleUp = 1f)
        {
            if (lidRenderer != null && duration > 0f)
            {
                Transform lid = lidRenderer.transform;
                CaptureLidRest();
                Vector3 lidPosition = lidRestPosition;
                Vector3 lidScale = lidRestScale;
                float parentScaleY = lid.parent != null ? Mathf.Max(0.0001f, Mathf.Abs(lid.parent.lossyScale.y)) : 1f;
                Vector3 liftTarget = lidPosition + Vector3.up * (liftHeight / parentScaleY);
                Color iconColor = iconRenderer != null ? iconRenderer.color : Color.white;
                if (iconRenderer != null)
                {
                    iconRenderer.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0f);
                    iconRenderer.gameObject.SetActive(true);
                }
                for (float time = 0f; time < duration; )
                {
                    time += Time.deltaTime;
                    float t = Mathf.Clamp01(time / duration);
                    float ease = 1f - (1f - t) * (1f - t);
                    lid.localPosition = Vector3.LerpUnclamped(lidPosition, liftTarget, ease);
                    lid.localScale = lidScale * Mathf.LerpUnclamped(1f, scaleUp, ease);
                    SetLidAlpha(1f - t);
                    if (iconRenderer != null)
                        iconRenderer.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * t);
                    yield return null;
                }
                if (iconRenderer != null)
                    iconRenderer.color = iconColor;
                lid.localPosition = lidPosition;
                lid.localScale = lidScale;
            }
            SetHidden(false);
        }

        private bool lidRestCaptured;
        private Vector3 lidRestPosition;
        private Vector3 lidRestScale;
        private Quaternion lidRestRotation;
        private Material lidDefaultMaterial;

        // EN: Direct reference to the lid renderer (used by boosters).
        // VI: Tham chiếu trực tiếp tới renderer nắp (booster dùng).
        public SpriteRenderer LidRenderer => lidRenderer;

        // EN: Closes the shared lid with a fade and drop, hiding both quantity badges and then both ingredient icons.
        // VI: Đóng nắp chung bằng hiệu ứng mờ và rơi, ẩn cả hai nhãn số lượng rồi ẩn cả hai hình nguyên liệu.
        public IEnumerator CloseLid(float duration, float dropHeight)
        {
            if (lidRenderer == null)
                yield break;
            CaptureLidRest();
            Transform lid = lidRenderer.transform;
            float parentScaleY = lid.parent != null ? Mathf.Max(0.0001f, Mathf.Abs(lid.parent.lossyScale.y)) : 1f;
            Vector3 from = lidRestPosition + Vector3.up * (dropHeight / parentScaleY);
            HideCountBadge();
            lid.gameObject.SetActive(true);
            if (plateRenderer != null)
            {
                lidRenderer.sortingLayerID = plateRenderer.sortingLayerID;
                lidRenderer.sortingOrder = plateRenderer.sortingOrder + 3;
            }
            for (float time = 0f; time < duration; )
            {
                time += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
                float ease = 1f - (1f - t) * (1f - t);
                lid.localPosition = Vector3.LerpUnclamped(from, lidRestPosition, ease);
                lid.localScale = lidRestScale * Mathf.LerpUnclamped(1.15f, 1f, ease);
                SetLidAlpha(t);
                yield return null;
            }
            RestoreLidRest();
            SetLidAlpha(1f);
            if (iconRenderer != null)
                iconRenderer.gameObject.SetActive(false);
        }

        // EN: Hides both quantity badges until the next count refresh during a booster animation.
        // VI: Ẩn cả hai nhãn số lượng đến lần cập nhật số lượng kế tiếp trong hoạt cảnh booster.
        public void HideCountBadge()
        {
            countBadgeSuppressed = true;
            if (canvasCount != null)
                canvasCount.SetActive(false);
            if (secondCount != null)
                secondCount.SetActive(false);
        }

        // EN: Tilts the lid by the given angle (degrees) around its resting rotation, for the shake effect.
        // VI: Nghiêng nắp theo góc đã cho (độ) quanh góc nghỉ, dùng cho hiệu ứng rung.
        public void SetLidTilt(float angle)
        {
            if (lidRenderer == null)
                return;
            CaptureLidRest();
            lidRenderer.transform.localRotation = lidRestRotation * Quaternion.Euler(0f, 0f, angle);
        }

        // EN: Swaps the lid material (null restores the original one).
        // VI: Đổi material của nắp (null để trả lại material gốc).
        public void SetLidMaterial(Material material)
        {
            if (lidRenderer == null)
                return;
            if (lidDefaultMaterial == null)
                lidDefaultMaterial = lidRenderer.sharedMaterial;
            lidRenderer.sharedMaterial = material != null ? material : lidDefaultMaterial;
        }

        // EN: Remembers the lid's resting position, scale and rotation once, so animations can always be undone.
        // VI: Ghi nhớ một lần vị trí, scale và góc nghỉ của nắp, để luôn hoàn tác được các hiệu ứng.
        private void CaptureLidRest()
        {
            if (lidRestCaptured || lidRenderer == null)
                return;
            Transform lid = lidRenderer.transform;
            lidRestPosition = lid.localPosition;
            lidRestScale = lid.localScale;
            lidRestRotation = lid.localRotation;
            lidRestCaptured = true;
        }

        // EN: Puts the lid back at its resting position, scale and rotation.
        // VI: Đặt nắp về lại vị trí, scale và góc nghỉ.
        private void RestoreLidRest()
        {
            if (!lidRestCaptured || lidRenderer == null)
                return;
            Transform lid = lidRenderer.transform;
            lid.localPosition = lidRestPosition;
            lid.localScale = lidRestScale;
            lid.localRotation = lidRestRotation;
        }

        // EN: Sets the lid's opacity, keeping its colour.
        // VI: Đặt độ trong suốt của nắp, giữ nguyên màu.
        private void SetLidAlpha(float alpha)
        {
            if (lidRenderer == null)
                return;
            Color color = lidRenderer.color;
            color.a = alpha;
            lidRenderer.color = color;
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
            while (time < Config.iceHit.iceHitDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Config.iceHit.iceHitDuration);
                float wobble = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * Config.iceHit.iceHitAmount;
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

        // EN: Configures a single food and concealment, resetting pooled double visuals and preparation state.
        // VI: Thiết lập nguyên liệu đơn và trạng thái che, đặt lại hình đĩa đôi từ pool và trạng thái sơ chế.
        public void Setup(FoodType type, int newAmount, Sprite icon, bool hidden = false)
        {
            ConfigureShape(DishShape.Single, FoodType.None, 0, Vector3.zero);
            foodType = type;
            state = FoodState.Raw;
            SetIcon(icon);
            SetAmount(newAmount);
            SetHidden(hidden);
            SetFrozen(false, 0);
        }

        // EN: Replaces one ingredient stack during shuffle without changing the plate shape, position, lid or ice.
        // VI: Thay một nhóm nguyên liệu khi xáo mà không đổi hình đĩa, vị trí, nắp hoặc băng.
        public void SetShuffleContent(bool secondHalf, FoodType type, int quantity)
        {
            if (secondHalf)
            {
                SecondFoodType = type;
                SecondAmount = Mathf.Max(0, quantity);
                if (secondIcon != null && foodIconConfig != null)
                    secondIcon.sprite = foodIconConfig.GetIcon(type, state);
                RefreshSecondVisual();
                return;
            }
            foodType = type;
            if (foodIconConfig != null)
                SetIcon(foodIconConfig.GetIcon(type, state));
            SetAmount(quantity);
        }

        // EN: Assigns the shared icon table and immediately updates the current state's sprite.
        // VI: Gán bảng icon dùng chung và cập nhật ngay sprite theo trạng thái hiện tại.
        public void SetIconConfig(FoodIconConfig config)
        {
            foodIconConfig = config;
            if (config != null)
                SetIcon(config.GetIcon(foodType, state));
        }

        // EN: Updates the first-half quantity, hides its icon when depleted and refreshes the count badge.
        // VI: Cập nhật số lượng nửa đầu, ẩn icon khi hết nguyên liệu và cập nhật nhãn số lượng.
        public void SetAmount(int newAmount)
        {
            amount = Mathf.Max(0, newAmount);
            if (iconRenderer != null)
                foreach (SpriteRenderer renderer in iconRenderer.GetComponentsInChildren<SpriteRenderer>(true))
                    renderer.enabled = amount > 0;
            if (txtAmount != null)
                txtAmount.text = amount.ToString();
            RefreshCountVisual();
        }

        // EN: Restores count visibility after data changes and shows the first badge only when its quantity exceeds one.
        // VI: Khôi phục hiển thị số lượng sau khi đổi dữ liệu, chỉ hiện nhãn đầu khi số lượng lớn hơn một.
        private void RefreshCountVisual()
        {
            countBadgeSuppressed = false;
            if (canvasCount != null)
                canvasCount.SetActive(amount > 1 && !isHidden && !IsFrozen);
            RefreshSecondVisual();
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
