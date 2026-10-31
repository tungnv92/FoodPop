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

        [Tooltip("Component Food riêng của slot (cùng GameObject), dùng lại cho mọi lần nhận món.")]
        [SerializeField] private Food slotFood;
        private Vector3 ingredientBaseScale = Vector3.one;
        private Coroutine squashRoutine;
        private Transform collectionPlate;
        private Vector3 plateRestPosition;
        private Vector3 plateRestScale;
        private float collectionLift;
        private Coroutine plateDrop;
        private LineRenderer preparationTrack;
        private LineRenderer preparationArc;
        private Material preparationMaterial;

        // EN: Shows a circular progress ring attached to this plate, filling clockwise from the top.
        // VI: Hiện vòng tiến độ bám theo đĩa này, đầy dần theo chiều kim đồng hồ từ phía trên.
        public void SetPreparationProgress(float progress)
        {
            if (preparationArc == null)
                CreatePreparationRing();
            preparationTrack.gameObject.SetActive(true);
            preparationArc.gameObject.SetActive(true);
            DrawPreparationArc(preparationTrack, 1f);
            DrawPreparationArc(preparationArc, Mathf.Clamp01(progress));
        }

        // EN: Hides the preparation indicator without allocating or destroying its reusable renderers.
        // VI: Ẩn chỉ báo sơ chế mà không tạo hay hủy các renderer dùng lại.
        public void HidePreparationProgress()
        {
            if (preparationTrack != null) preparationTrack.gameObject.SetActive(false);
            if (preparationArc != null) preparationArc.gameObject.SetActive(false);
        }

        // EN: Creates a dark circular track and a green foreground arc using a runtime sprite material.
        // VI: Tạo đường vòng tối và cung tiến độ xanh bằng material sprite lúc chạy.
        private void CreatePreparationRing()
        {
            preparationMaterial = new Material(Shader.Find("Sprites/Default"));
            preparationTrack = CreatePreparationLine("Preparation Track", new Color(0.12f, 0.2f, 0.18f, 0.7f), 10);
            preparationArc = CreatePreparationLine("Preparation Progress", new Color(0.25f, 1f, 0.65f, 1f), 11);
        }

        // EN: Builds a local-space ring renderer above the plate artwork on its existing sorting layer.
        // VI: Tạo renderer vòng theo tọa độ local phía trên hình đĩa trên cùng sorting layer.
        private LineRenderer CreatePreparationLine(string objectName, Color color, int sortingOffset)
        {
            var child = new GameObject(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = preparationMaterial;
            line.startColor = line.endColor = color;
            line.numCapVertices = 4;
            line.startWidth = line.endWidth = 0.045f;
            SpriteRenderer source = ingredientRenderer != null ? ingredientRenderer : GetComponentInChildren<SpriteRenderer>();
            if (source != null)
            {
                line.sortingLayerID = source.sortingLayerID;
                line.sortingOrder = source.sortingOrder + sortingOffset;
            }
            return line;
        }

        // EN: Fits a clockwise circular arc just outside the plate sprite, following the plate's local centre.
        // VI: Đặt cung tròn chiều kim đồng hồ ngay ngoài sprite đĩa, bám tâm local của đĩa.
        private void DrawPreparationArc(LineRenderer line, float progress)
        {
            Transform plate = transform.Find("Plate");
            SpriteRenderer plateSprite = plate != null ? plate.GetComponent<SpriteRenderer>() : null;
            Vector3 centre = plate != null ? plate.localPosition : Vector3.zero;
            float radius = 0.5f;
            if (plateSprite != null && plateSprite.sprite != null)
                radius = Mathf.Max(plateSprite.sprite.bounds.extents.x * Mathf.Abs(plate.localScale.x), plateSprite.sprite.bounds.extents.y * Mathf.Abs(plate.localScale.y)) + 0.06f;
            int segments = Mathf.Max(1, Mathf.CeilToInt(progress * 64));
            line.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 0.5f - Mathf.PI * 2f * progress * i / segments;
                line.SetPosition(i, centre + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.02f));
            }
        }

        // EN: Releases the runtime ring material when this slot is destroyed.
        // VI: Giải phóng material vòng sinh lúc chạy khi slot này bị hủy.
        private void OnDestroy()
        {
            if (preparationMaterial != null)
            {
                if (Application.isPlaying) Destroy(preparationMaterial);
                else DestroyImmediate(preparationMaterial);
            }
        }

        public bool IsPlateReturning => plateDrop != null;

        // EN: Captures the plate child once so collection effects never move the spline-controlled slot root.
        // VI: Ghi nhớ child đĩa một lần để hiệu ứng thu không di chuyển root slot do spline điều khiển.
        private bool ResolveCollectionPlate()
        {
            if (collectionPlate != null)
                return true;
            collectionPlate = transform.Find("Plate");
            if (collectionPlate == null)
                return false;
            plateRestPosition = collectionPlate.localPosition;
            plateRestScale = collectionPlate.localScale;
            return true;
        }

        // EN: Raises only the plate artwork by a world-space height, following the food's preflight lift.
        // VI: Nâng riêng hình đĩa theo độ cao world, đi cùng nhịp nhấc food trước khi bay.
        public void SetCollectionLift(float height)
        {
            if (!ResolveCollectionPlate())
                return;
            if (plateDrop != null) StopCoroutine(plateDrop);
            plateDrop = null;
            ApplyPlateLift(Mathf.Max(0f, height));
        }

        // EN: Drops the empty plate onto its slot with a small impact bounce.
        // VI: Thả đĩa trống về slot với một nhịp nảy nhẹ khi chạm.
        public void DropCollectionPlate(float duration)
        {
            if (!ResolveCollectionPlate() || !isActiveAndEnabled)
                return;
            if (plateDrop != null) StopCoroutine(plateDrop);
            plateDrop = StartCoroutine(ReturnCollectionPlate(Mathf.Max(0.01f, duration)));
        }

        // EN: Applies the visual offset relative to the stationary slot, retaining its authored scale.
        // VI: Áp dụng độ dịch hình so với slot cố định, giữ scale đã thiết kế.
        private void ApplyPlateLift(float height)
        {
            collectionLift = height;
            collectionPlate.localPosition = plateRestPosition + collectionPlate.parent.InverseTransformVector(Vector3.up * height);
        }

        // EN: Accelerates the plate downward and settles it with a scaled-time bounce.
        // VI: Cho đĩa rơi nhanh dần và ổn định bằng nhịp nảy theo thời gian game.
        private IEnumerator ReturnCollectionPlate(float duration)
        {
            float height = collectionLift;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyPlateLift(height * (1f - t * t));
                yield return null;
            }
            ApplyPlateLift(0f);
            const float BounceDuration = 0.12f;
            for (float elapsed = 0f; elapsed < BounceDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / BounceDuration);
                float bounce = Mathf.Sin(t * Mathf.PI);
                ApplyPlateLift(height * 0.12f * bounce);
                collectionPlate.localScale = Vector3.Scale(plateRestScale, new Vector3(1f + 0.05f * bounce, 1f - 0.05f * bounce, 1f));
                yield return null;
            }
            RestorePlateVisual();
            plateDrop = null;
        }

        // EN: Restores the plate transform after landing without modifying its slot or food.
        // VI: Khôi phục transform đĩa sau khi chạm mà không thay đổi slot hoặc food.
        private void RestorePlateVisual()
        {
            collectionLift = 0f;
            if (collectionPlate == null)
                return;
            collectionPlate.localPosition = plateRestPosition;
            collectionPlate.localScale = plateRestScale;
        }

        // EN: Cancels any plate drop and restores its authored transform for replay, reuse or destruction.
        // VI: Hủy lượt rơi đĩa và khôi phục transform thiết kế khi chơi lại, dùng lại hoặc hủy.
        public void ResetCollectionPlate()
        {
            if (plateDrop != null) StopCoroutine(plateDrop);
            plateDrop = null;
            RestorePlateVisual();
        }

        // EN: True while a picked portion is still flying toward this slot; the slot is reserved but its food is not shown yet.
        // VI: True khi một phần nguyên liệu vừa chọn vẫn đang bay tới slot; slot đã được giữ chỗ nhưng chưa hiện món.
        public bool IsFoodArriving { get; private set; }
        public int ArrivalId { get; private set; }

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

        // EN: The sprite renderer that shows this slot's food (child "Ingredient"), used to place effects just above it.
        // VI: Sprite renderer hiển thị món của slot (child "Ingredient"), dùng để đặt hiệu ứng ngay phía trên nó.
        public SpriteRenderer IngredientRenderer => ingredientRenderer;

        // EN: World rotation a flying portion should reach on landing.
        // VI: Góc xoay world mà phần nguyên liệu bay cần đạt khi đáp.
        public Quaternion LandingRotation => ingredientRenderer != null ? ingredientRenderer.transform.rotation : transform.rotation;

        // EN: Every slot starts the game empty: no food and the ingredient icon hidden; remembers the icon's resting scale for the landing squash.
        // VI: Mọi slot bắt đầu game ở trạng thái rỗng: không có food và ẩn icon nguyên liệu; ghi lại scale gốc của icon cho hiệu ứng nảy khi chạm.
        private void Awake()
        {
            if (ingredientRenderer != null)
                ingredientBaseScale = ingredientRenderer.transform.localScale;
            ClearFood();
        }

        // EN: Creates a new arrival identity and hides food until that specific flight lands.
        // VI: Tạo mã nhận diện lượt đáp mới và ẩn food đến khi đúng chuyến bay đó chạm xuống.
        public void BeginFoodArrival()
        {
            StopSquash();
            unchecked { ArrivalId++; }
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

        // EN: Checks whether a pending arrival still belongs to the caller, including same-type slot reuse.
        // VI: Kiểm tra lượt đáp đang chờ còn thuộc nơi gọi, kể cả khi slot dùng lại cùng loại food.
        public bool OwnsFoodArrival(int arrivalId)
        {
            return IsFoodArriving && ArrivalId == arrivalId;
        }

        // EN: Completes only the matching arrival so stale flights cannot finish a newer portion.
        // VI: Chỉ hoàn tất lượt đáp có mã khớp để chuyến bay cũ không kết thúc phần food mới.
        public bool CompleteFoodArrival(int arrivalId)
        {
            return OwnsFoodArrival(arrivalId) && CompleteFoodArrival();
        }

        // EN: Clears only the caller's reservation and leaves any replacement food untouched.
        // VI: Chỉ xóa phần giữ chỗ của nơi gọi, không tác động food thay thế.
        public bool CancelFoodArrival(int arrivalId)
        {
            if (!OwnsFoodArrival(arrivalId))
                return false;
            ClearFood();
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

        // EN: Hides preparation progress and restores landing effects when the slot is disabled.
        // VI: Ẩn tiến độ sơ chế và khôi phục hiệu ứng đáp khi slot bị tắt.
        private void OnDisable()
        {
            HidePreparationProgress();
            StopSquash();
            ResetCollectionPlate();
        }

        // EN: Fills this empty slot with one raw portion of the given type, reusing the slot's own Food component (assigned in the prefab) so the chef can prepare it.
        // VI: Đặt một phần nguyên liệu thô loại đã cho vào slot rỗng, dùng lại component Food riêng của slot (gán sẵn trong prefab) để đầu bếp có thể sơ chế.
        public bool TryFillFood(FoodType type, FoodIconConfig iconConfig)
        {
            if (!isEmpty || type == FoodType.None || slotFood == null)
                return false;
            slotFood.enabled = true;
            slotFood.foodType = type;
            slotFood.state = FoodState.Raw;
            slotFood.SetIconConfig(iconConfig);
            slotFood.Setup(type, 1, iconConfig != null ? iconConfig.GetIcon(type) : null);
            food = slotFood;
            isEmpty = false;
            return true;
        }

        // EN: Clears food and progress, invalidating arrival identity so old flights cannot affect slot reuse.
        // VI: Xóa món và tiến độ, vô hiệu mã lượt đáp để chuyến bay cũ không tác động slot dùng lại.
        public Food ClearFood()
        {
            HidePreparationProgress();
            unchecked { ArrivalId++; }
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
    }
}
