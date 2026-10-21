using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Coordinates chef preparation jobs and conveyor slot availability through one scene singleton.
    // VI: Điều phối công việc sơ chế và slot băng chuyền qua một singleton trong scene.
    [DisallowMultipleComponent]
    public class ChefManager : MonoBehaviour
    {
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        private static ChefManager instance;
        private static readonly ConveyorSlot[] EmptySlots = new ConveyorSlot[0];
        private static readonly int IsChoppingId = Animator.StringToHash("IsChopping");

        [Tooltip("Animator đầu bếp với tham số điều khiển trạng thái cắt.")]
        [SerializeField] private Animator chefAnimator;
        [Tooltip("Component điều khiển đường đi và chuyển động các đĩa trên băng chuyền.")]
        [SerializeField] private ConveyorChefMovement conveyorMovement;

        [Tooltip("Bật để đầu bếp tự sơ chế món thô khi món đi vào trạm sơ chế trên băng chuyền.")]
        [SerializeField] private bool autoPrepareInStation = true;

        [Header("Bàn cắt trước mặt đầu bếp")]
        [Tooltip("Điểm món bay tới để đầu bếp cắt (thường là mặt thớt); để trống sẽ dùng vị trí đầu bếp cộng Cutting Offset.")]
        [SerializeField] private Transform cuttingPoint;
        [Tooltip("Thớt trước mặt đầu bếp (tùy chọn); rung nhẹ theo mỗi nhát chặt.")]
        [SerializeField] private Transform cuttingBoard;
        [Tooltip("Sprite bóng đổ (tùy chọn) hiện dưới món khi món nằm trên thớt.")]
        [SerializeField] private Sprite foodShadowSprite;

        [Tooltip("Độ lệch (world) từ vị trí đầu bếp tới điểm cắt khi chưa gán Cutting Point.")]
        [SerializeField] private Vector3 cuttingOffset = new Vector3(0f, -0.6f, 0f);

        [Tooltip("Prefab khói (lặp lại, tùy chọn) bốc lên từ món trong lúc cắt.")]
        [SerializeField] private ParticleEffect chopSmokePrefab;

        [Header("Vụn khi cắt")]
        [Tooltip("Prefab vụn thịt (tùy chọn) bắn ra ở mỗi nhát chặt món thịt/hải sản.")]
        [SerializeField] private ParticleEffect meatDebrisPrefab;
        [Tooltip("Prefab vụn rau (tùy chọn) bắn ra ở mỗi nhát chặt món rau/đồ mềm.")]
        [SerializeField] private ParticleEffect vegetableDebrisPrefab;

        [Header("Hiệu ứng khi sơ chế xong")]
        [Tooltip("Prefab particle (tùy chọn) chạy tại slot khi món vừa sơ chế xong.")]
        [SerializeField] private ParticleEffect preparedEffectPrefab;

        private Coroutine preparationRoutine;
        private Food preparingFood;
        private ConveyorSlot preparingSlot;
        private readonly Dictionary<ConveyorSlot, float> slotPriorities = new Dictionary<ConveyorSlot, float>();
        private SpriteRenderer cuttingFood;
        private SpriteRenderer cuttingShadow;
        private Vector3 boardBaseScale = Vector3.one;
        private ParticleEffect chopSmoke;
        private readonly Dictionary<ParticleEffect, ParticleEffect> debrisInstances = new Dictionary<ParticleEffect, ParticleEffect>();

        // EN: Returns the scene singleton, resolving it once if lifecycle registration has not run yet.
        // VI: Trả về singleton scene, tìm một lần nếu vòng đời chưa đăng ký instance.
        public static ChefManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<ChefManager>();
                return instance;
            }
        }

        // EN: Exposes the movement component's authoritative slot list without duplicating slot storage.
        // VI: Cung cấp danh sách slot gốc của component chuyển động, không lưu danh sách trùng lặp.
        public IReadOnlyList<ConveyorSlot> Slots => conveyorMovement != null ? conveyorMovement.Slots : EmptySlots;

        // EN: Indicates whether the chef currently has an unfinished preparation job.
        // VI: Cho biết đầu bếp đang có công việc sơ chế chưa hoàn thành.
        public bool IsBusy => preparationRoutine != null;

        // EN: Clears stale static state when entering Play Mode, including with domain reload disabled.
        // VI: Xóa trạng thái tĩnh cũ khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers this manager (removing only duplicate components) and remembers the cutting board's rest scale for the chop punch.
        // VI: Đăng ký manager (chỉ xóa component trùng) và ghi nhớ scale gốc của thớt cho hiệu ứng nảy khi chặt.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[ChefManager] Scene đã có manager; bỏ component trùng.", this);
                enabled = false;
                Destroy(this);
                return;
            }
            instance = this;
            if (cuttingBoard != null)
                boardBaseScale = cuttingBoard.localScale;
        }

        // EN: Cancels incomplete work when disabled so it cannot prepare food later unexpectedly.
        // VI: Hủy việc chưa hoàn thành khi tắt để tránh sơ chế nguyên liệu muộn ngoài ý muốn.
        private void OnDisable()
        {
            if (instance == this)
                CancelPreparation();
        }

        // EN: While idle, starts preparing the raw food that is furthest through the preparation station.
        // VI: Khi rảnh, bắt đầu sơ chế món thô đã đi xa nhất trong trạm sơ chế.
        private void Update()
        {
            if (!autoPrepareInStation || IsBusy || conveyorMovement == null)
                return;
            ConveyorSlot slot = FindSlotToPrepare();
            if (slot != null)
                TryPrepareSlot(slot);
        }

        // EN: Returns the slot inside the station holding raw, already landed food that is closest to leaving the station, or null.
        // VI: Trả về slot trong trạm đang chứa món thô đã đáp xuống và gần ra khỏi trạm nhất, hoặc null.
        private ConveyorSlot FindSlotToPrepare()
        {
            ConveyorSlot best = null;
            float bestProgress = -1f;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (!CanPrepare(slot))
                    continue;
                float stationProgress = conveyorMovement.GetStationProgress(i);
                if (stationProgress > bestProgress)
                {
                    bestProgress = stationProgress;
                    best = slot;
                }
            }
            return best;
        }

        // EN: Checks that a slot is active and holds raw food that has finished flying in.
        // VI: Kiểm tra slot đang hoạt động và chứa món thô đã bay tới xong.
        private static bool CanPrepare(ConveyorSlot slot)
        {
            return slot != null && slot.isActiveAndEnabled && !slot.isEmpty && !slot.IsFoodArriving &&
                   slot.food != null && slot.food.IsRaw;
        }

        // EN: Releases this manager's singleton registration and its runtime objects when its scene is destroyed.
        // VI: Giải phóng đăng ký singleton và các object tạo lúc chạy của manager khi scene bị hủy.
        private void OnDestroy()
        {
            OnDestroyRuntimeObjects();
            if (instance == this)
                instance = null;
        }

        // EN: Number of active empty conveyor slots right now (same rule TryGetNearestEmptySlots uses); lets boosters check whether a dish fits before picking it.
        // VI: Số slot rỗng đang hoạt động trên băng chuyền lúc này (cùng quy tắc với TryGetNearestEmptySlots); booster dùng để kiểm tra đĩa có vừa trước khi lấy.
        public int CountEmptySlots()
        {
            int count = 0;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                    count++;
            }
            return count;
        }

        // EN: Collects the given number of active empty slots, counted from Station End onward along the belt direction (the first free slot past the station comes first); returns false and leaves the list empty when there are not enough.
        // VI: Lấy đủ số slot rỗng đang hoạt động, tính từ vị trí Station End trở đi theo chiều chạy của băng (slot trống đầu tiên sau trạm được ưu tiên); trả về false và để danh sách rỗng khi không đủ slot.
        public bool TryGetNearestEmptySlots(Vector3 worldPosition, int count, List<ConveyorSlot> result)
        {
            result.Clear();
            slotPriorities.Clear();
            if (count <= 0)
                return false;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                {
                    result.Add(slot);
                    slotPriorities[slot] = GetSlotPriority(i, slot, worldPosition);
                }
            }
            if (result.Count < count)
            {
                result.Clear();
                return false;
            }
            result.Sort((a, b) => slotPriorities[a].CompareTo(slotPriorities[b]));
            result.RemoveRange(count, result.Count - count);
            return true;
        }

        // EN: Sort key for an empty slot: distance travelled since Station End, so the slot just past the station end comes first and slots inside the station rank last.
        // VI: Khóa sắp xếp cho slot trống: quãng đường đã đi kể từ Station End, slot vừa qua điểm cuối trạm được ưu tiên, slot đang trong trạm xếp cuối.
        private float GetSlotPriority(int index, ConveyorSlot slot, Vector3 worldPosition)
        {
            if (conveyorMovement == null)
                return (slot.transform.position - worldPosition).sqrMagnitude;
            return conveyorMovement.GetDistanceFromStationEnd(index);
        }

        // EN: Checks membership against the conveyor's slot references rather than hierarchy position.
        // VI: Kiểm tra slot thuộc băng chuyền bằng reference thay vì vị trí trong hierarchy.
        public bool ContainsSlot(ConveyorSlot slot)
        {
            if (slot == null)
                return false;
            foreach (ConveyorSlot candidate in Slots)
            {
                if (candidate == slot)
                    return true;
            }
            return false;
        }

        // EN: Starts one raw-food job on an owned active slot, rejecting concurrent or invalid work and food that is still flying in from the board.
        // VI: Bắt đầu sơ chế một nguyên liệu thô trên slot đang hoạt động thuộc manager, từ chối việc trùng, không hợp lệ hoặc món vẫn đang bay từ board lên.
        public bool TryPrepareSlot(ConveyorSlot slot)
        {
            if (!isActiveAndEnabled || IsBusy || chefAnimator == null || !ContainsSlot(slot) || !CanPrepare(slot))
                return false;
            preparingSlot = slot;
            preparingFood = slot.food;
            preparationRoutine = StartCoroutine(PrepareFood());
            return true;
        }

        // EN: Full preparation sequence: the raw food hops from its plate to the cutting point in front of the chef, the chef chops it (group-specific knife sounds, debris and smoke), it turns into the prepared food with a poof, then drops back onto its plate. Cancels quietly if the plate loses the food meanwhile.
        // VI: Toàn bộ quá trình sơ chế: món thô nhảy từ đĩa lên điểm cắt trước mặt đầu bếp, đầu bếp cắt (tiếng dao theo nhóm, vụn và khói), món biến thành món sơ chế kèm hiệu ứng phụt khói, rồi rơi lại vào đĩa. Tự hủy nếu đĩa mất món trong lúc đó.
        private IEnumerator PrepareFood()
        {
            ConveyorSlot slot = preparingSlot;
            SpriteRenderer icon = slot.IngredientRenderer;
            SpriteRenderer flyer = SetupCuttingFood(icon);
            Vector3 plateScale = slot.LandingScale;
            Vector3 cutScale = plateScale * Config.preparation.cuttingScale;
            slot.BeginFoodArrival();

            yield return Hop(flyer.transform, () => slot.LandingPosition, GetCuttingPosition, plateScale, cutScale, Config.preparation.liftDuration);
            if (!IsStillPreparing(slot))
            {
                FinishPreparation();
                yield break;
            }

            SetShadow(true, flyer, cutScale);
            SetChopping(true);
            SetSmoke(true, flyer);
            yield return PlayChops(Mathf.Max(0.01f, Config.preparation.preparationDuration), preparingFood, flyer, cutScale);
            SetSmoke(false, flyer);
            SetChopping(false);
            if (!IsStillPreparing(slot) || !preparingFood.Prepare())
            {
                FinishPreparation();
                yield break;
            }

            yield return PlayPreparedPoof(flyer, icon != null ? icon.sprite : flyer.sprite, cutScale);
            SetShadow(false, null, Vector3.one);
            yield return Hop(flyer.transform, GetCuttingPosition, () => slot.LandingPosition, cutScale, plateScale, Config.preparation.dropDuration);
            FinishPreparation();
        }

        // EN: True while the slot still holds the food this job started with (it was not cleared or reused).
        // VI: Trả về true khi slot vẫn giữ đúng món của lượt sơ chế này (chưa bị làm rỗng hay dùng lại).
        private bool IsStillPreparing(ConveyorSlot slot)
        {
            return slot != null && slot == preparingSlot && slot.isActiveAndEnabled && !slot.isEmpty &&
                   preparingFood != null && slot.food == preparingFood && slot.IsFoodArriving;
        }

        // EN: Moves a transform along an arc between two (possibly moving) positions while blending its scale, over the given duration.
        // VI: Di chuyển transform theo vòng cung giữa hai vị trí (có thể đang di chuyển) đồng thời đổi dần scale, trong thời gian đã cho.
        private IEnumerator Hop(Transform target, System.Func<Vector3> from, System.Func<Vector3> to, Vector3 fromScale, Vector3 toScale, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.LerpUnclamped(from(), to(), eased);
                position.y += Config.preparation.hopHeight * 4f * t * (1f - t);
                target.position = position;
                target.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                yield return null;
            }
            target.position = to();
            target.localScale = toScale;
        }

        // EN: Swaps the cutting food to its prepared sprite behind a poof particle and the chime, popping it from small with a slight overshoot.
        // VI: Đổi món đang cắt sang ảnh đã sơ chế phía sau particle phụt khói và tiếng "ting", cho món bật ra từ nhỏ và vượt nhẹ.
        private IEnumerator PlayPreparedPoof(SpriteRenderer flyer, Sprite preparedSprite, Vector3 restScale)
        {
            flyer.sprite = preparedSprite;
            IngameSoundManager.Instance?.PlayPrepared();
            const float Duration = 0.22f;
            float time = 0f;
            while (time < Duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Duration);
                float pop = t < 0.6f ? Mathf.Lerp(0.4f, 1.15f, t / 0.6f) : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f);
                flyer.transform.localScale = restScale * pop;
                yield return null;
            }
            flyer.transform.localScale = restScale;
        }

        // EN: Ends the job: hides the cutting food, its shadow and smoke, restores the board, shows the food on its plate again with the landing bounce (if the plate still holds it) and returns the chef to idle.
        // VI: Kết thúc lượt: ẩn món đang cắt, bóng đổ và khói, trả thớt về như cũ, hiện lại món trên đĩa kèm hiệu ứng nảy (nếu đĩa vẫn giữ món) và đưa đầu bếp về nghỉ.
        private void FinishPreparation()
        {
            if (cuttingFood != null)
                cuttingFood.gameObject.SetActive(false);
            SetShadow(false, null, Vector3.one);
            if (cuttingBoard != null)
                cuttingBoard.localScale = boardBaseScale;
            SetSmoke(false, null);
            SetChopping(false);
            if (preparingSlot != null && preparingSlot.IsFoodArriving)
                preparingSlot.CompleteFoodArrival();
            preparingSlot = null;
            preparingFood = null;
            preparationRoutine = null;
        }

        // EN: Returns the world position in front of the chef where food is cut.
        // VI: Trả về vị trí world trước mặt đầu bếp nơi món được cắt.
        private Vector3 GetCuttingPosition()
        {
            if (cuttingPoint != null)
                return cuttingPoint.position;
            Transform chef = chefAnimator != null ? chefAnimator.transform : transform;
            return chef.position + cuttingOffset;
        }

        // EN: Prepares the reusable sprite that represents the food while it is off its plate, copying the plate sprite's look and drawing it above the chef.
        // VI: Chuẩn bị sprite dùng lại đại diện cho món khi rời đĩa, sao chép hình ảnh của sprite trên đĩa và vẽ nó phía trên đầu bếp.
        private SpriteRenderer SetupCuttingFood(SpriteRenderer source)
        {
            if (cuttingFood == null)
            {
                cuttingFood = new GameObject("Cutting food").AddComponent<SpriteRenderer>();
                cuttingFood.transform.SetParent(null);
            }
            cuttingFood.sprite = source != null ? source.sprite : null;
            cuttingFood.sortingLayerID = source != null ? source.sortingLayerID : 0;
            cuttingFood.sortingOrder = (source != null ? source.sortingOrder : 0) + Config.preparation.cuttingSortingBoost;
            cuttingFood.gameObject.layer = source != null ? source.gameObject.layer : 0;
            Transform target = cuttingFood.transform;
            target.position = preparingSlot.LandingPosition;
            target.rotation = preparingSlot.LandingRotation;
            target.localScale = preparingSlot.LandingScale;
            cuttingFood.gameObject.SetActive(true);
            return cuttingFood;
        }

        // EN: Starts or stops the looping chop smoke at the cutting food, creating it from the prefab on first use and drawing it just above the food.
        // VI: Bật hoặc tắt khói lặp lại tại món đang cắt, tạo từ prefab ở lần dùng đầu và vẽ ngay phía trên món.
        private void SetSmoke(bool on, SpriteRenderer food)
        {
            if (!on)
            {
                if (chopSmoke != null)
                    chopSmoke.Stop();
                return;
            }
            if (chopSmokePrefab == null || food == null)
                return;
            if (chopSmoke == null)
                chopSmoke = Instantiate(chopSmokePrefab);
            chopSmoke.transform.position = food.transform.position;
            chopSmoke.SetSorting(food.sortingLayerID, food.sortingOrder + 1);
            chopSmoke.Play();
        }

        // EN: Waits for the given duration and, on every chop (each Config.preparation.chopInterval, starting immediately), plays the food's own knife sound, throws a few of its own debris pieces and punches the food and board; restores the food's rest scale at the end.
        // VI: Chờ trong khoảng thời gian đã cho và ở mỗi nhát chặt (mỗi Config.preparation.chopInterval, bắt đầu ngay), phát tiếng dao riêng của món, bắn vài mảnh vụn riêng và làm món cùng thớt nảy nhẹ; trả món về scale chuẩn khi kết thúc.
        private IEnumerator PlayChops(float duration, Food food, SpriteRenderer foodSprite, Vector3 restScale)
        {
            ParticleEffect debris = GetDebris(food);
            float elapsed = 0f;
            float nextChop = 0f;
            float lastChop = 0f;
            while (elapsed < duration)
            {
                if (elapsed >= nextChop)
                {
                    IngameSoundManager.Instance?.PlayChop(food);
                    EmitDebris(debris, foodSprite);
                    lastChop = nextChop;
                    nextChop += Config.preparation.chopInterval;
                }
                ApplyChopPunch(foodSprite.transform, restScale, elapsed - lastChop);
                yield return null;
                elapsed += Time.deltaTime;
            }
            ApplyChopPunch(foodSprite.transform, restScale, float.MaxValue);
        }

        // EN: Squashes the food and slightly presses the cutting board right after a chop, easing back within ~0.1 s, so each knife hit has weight.
        // VI: Làm món bẹp và thớt lún nhẹ ngay sau mỗi nhát chặt, trở lại bình thường trong khoảng 0.1 giây, để mỗi nhát dao có lực.
        private void ApplyChopPunch(Transform food, Vector3 restScale, float timeSinceChop)
        {
            const float PunchTime = 0.1f;
            float strength = timeSinceChop < PunchTime ? Config.preparation.chopPunch * (1f - timeSinceChop / PunchTime) : 0f;
            food.localScale = Vector3.Scale(restScale, new Vector3(1f + strength, 1f - strength, 1f));
            if (cuttingBoard != null)
                cuttingBoard.localScale = Vector3.Scale(boardBaseScale, new Vector3(1f + strength * 0.25f, 1f - strength * 0.4f, 1f));
        }

        // EN: Shows or hides a soft contact shadow under the food while it lies on the cutting board, drawn just below the food.
        // VI: Hiện hoặc ẩn bóng đổ mềm dưới món khi món nằm trên thớt, vẽ ngay dưới món.
        private void SetShadow(bool on, SpriteRenderer food, Vector3 foodScale)
        {
            if (!on || foodShadowSprite == null || food == null)
            {
                if (cuttingShadow != null)
                    cuttingShadow.gameObject.SetActive(false);
                return;
            }
            if (cuttingShadow == null)
                cuttingShadow = new GameObject("Cutting food shadow").AddComponent<SpriteRenderer>();
            cuttingShadow.sprite = foodShadowSprite;
            cuttingShadow.color = new Color(0f, 0f, 0f, 0.35f);
            cuttingShadow.sortingLayerID = food.sortingLayerID;
            cuttingShadow.sortingOrder = food.sortingOrder - 1;
            cuttingShadow.gameObject.layer = food.gameObject.layer;
            Bounds bounds = food.bounds;
            cuttingShadow.transform.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.15f, food.transform.position.z);
            float width = bounds.size.x * 1.1f;
            Vector2 spriteSize = foodShadowSprite.bounds.size;
            cuttingShadow.transform.localScale = new Vector3(width / Mathf.Max(0.01f, spriteSize.x), width * 0.35f / Mathf.Max(0.01f, spriteSize.y), 1f);
            cuttingShadow.gameObject.SetActive(true);
        }

        // EN: Returns the debris emitter for a food (its own prefab from FoodIconConfig, otherwise its group's prefab), creating one instance per prefab on first use.
        // VI: Trả về bộ phát vụn cho món (prefab riêng trong FoodIconConfig, nếu không có thì prefab theo nhóm), mỗi prefab tạo một bản ở lần dùng đầu.
        private ParticleEffect GetDebris(Food food)
        {
            FoodIconConfig config = food.IconConfig;
            ParticleEffect prefab = config != null ? config.GetChopDebris(food.foodType) : null;
            if (prefab == null)
                prefab = FoodCategories.Get(food.foodType, config) == FoodCategory.Meat ? meatDebrisPrefab : vegetableDebrisPrefab;
            if (prefab == null)
                return null;
            if (!debrisInstances.TryGetValue(prefab, out ParticleEffect instance) || instance == null)
            {
                instance = Instantiate(prefab);
                debrisInstances[prefab] = instance;
            }
            return instance;
        }

        // EN: Moves the debris emitter to the food, draws it just above the food and emits Config.preparation.debrisPerChop pieces from the main layer and about half as many from each extra layer (e.g. sauce drops, shell bits).
        // VI: Đưa bộ phát vụn tới vị trí món, vẽ ngay phía trên món, bắn Config.preparation.debrisPerChop mảnh ở lớp chính và khoảng một nửa số đó ở mỗi lớp phụ (ví dụ giọt sốt, mảnh vỏ).
        private void EmitDebris(ParticleEffect debris, SpriteRenderer food)
        {
            if (debris == null || food == null || Config.preparation.debrisPerChop <= 0)
                return;
            debris.transform.position = food.transform.position;
            debris.SetSorting(food.sortingLayerID, food.sortingOrder + 1);
            debris.Emit(Config.preparation.debrisPerChop, Mathf.Max(1, Config.preparation.debrisPerChop / 2));
        }

        // EN: Stops the current job without changing the food's processing state, puts the food back on its plate and returns the chef to idle.
        // VI: Dừng công việc hiện tại, giữ trạng thái nguyên liệu, trả món về đĩa và đưa đầu bếp về nghỉ.
        public void CancelPreparation()
        {
            if (preparationRoutine != null)
                StopCoroutine(preparationRoutine);
            FinishPreparation();
        }

        // EN: Destroys the runtime cutting sprite, its shadow, smoke and debris emitters together with this manager.
        // VI: Hủy sprite món đang cắt, bóng đổ, khói và bộ phát vụn tạo lúc chạy cùng với manager này.
        private void OnDestroyRuntimeObjects()
        {
            if (cuttingFood != null)
                Destroy(cuttingFood.gameObject);
            if (cuttingShadow != null)
                Destroy(cuttingShadow.gameObject);
            if (chopSmoke != null)
                Destroy(chopSmoke.gameObject);
            foreach (ParticleEffect debris in debrisInstances.Values)
            {
                if (debris != null)
                    Destroy(debris.gameObject);
            }
            debrisInstances.Clear();
        }

        // EN: Shows the cutting point in the Scene view when the manager is selected.
        // VI: Hiển thị điểm cắt trong Scene view khi chọn manager.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.DrawWireSphere(GetCuttingPosition(), 0.3f);
        }

        // EN: Updates the chef animator's existing Idle/Chop boolean parameter.
        // VI: Cập nhật tham số bật/tắt trạng thái nghỉ và cắt có sẵn của animator đầu bếp.
        private void SetChopping(bool chopping)
        {
            if (chefAnimator != null && chefAnimator.runtimeAnimatorController != null)
                chefAnimator.SetBool(IsChoppingId, chopping);
        }
    }
}
