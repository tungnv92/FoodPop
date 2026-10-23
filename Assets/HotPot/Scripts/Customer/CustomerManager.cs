using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Owns customer queues, order progress and dispatch priority, delegating serving visuals to each lane's independent ServingFood.
    // VI: Quản lý hàng khách, tiến độ order và ưu tiên phục vụ, giao hiển thị phục vụ cho ServingFood độc lập của từng làn.
    [DisallowMultipleComponent]
    public class CustomerManager : MonoBehaviour
    {
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        private static CustomerManager instance;

        // EN: Returns the scene singleton, resolving it once when Awake has not run yet.
        // VI: Trả về singleton trong scene, tìm một lần nếu Awake chưa chạy.
        public static CustomerManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<CustomerManager>();
                return instance;
            }
        }

        [Header("Tham chiếu")]
        [Tooltip("Băng chuyền chứa các slot món.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Bảng món hoàn chỉnh: ảnh món và các nguyên liệu tạo nên món.")]
        [SerializeField] private FinalFoodConfig finalFoodConfig;
        [Tooltip("Làn trái (phần tử 0) và làn phải (phần tử 1).")]
        [SerializeField] private Lane[] lanes = new Lane[2];

        private PraiseText praiseText;

        private long arrivalCounter;
        private bool allServedLogged;

        // EN: Clears the static singleton when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa singleton tĩnh khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers the singleton and removes a duplicate component.
        // VI: Đăng ký singleton và bỏ component trùng.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[CustomerManager] Scene đã có CustomerManager; bỏ component trùng.", this);
                Destroy(this);
                return;
            }
            instance = this;
        }

        // EN: Releases the registered manager and cancels its serving orders without letting duplicate components reset active trays.
        // VI: Giải phóng manager đã đăng ký và hủy order phục vụ, không để component trùng đặt lại các khay đang hoạt động.
        private void OnDestroy()
        {
            if (instance != this)
                return;
            foreach (Lane lane in lanes)
                ResetServing(lane);
            instance = null;
        }

        public bool BoosterPaused { get; set; }

        // EN: Sends prepared belt food to customers unless collection has temporarily paused new serving flights.
        // VI: Gửi food đã sơ chế trên băng cho khách trừ khi lượt thu đang tạm dừng chuyến phục vụ mới.
        private void Update()
        {
            if (BoosterPaused || conveyor == null)
                return;
            IReadOnlyList<ConveyorSlot> slots = conveyor.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (conveyor.IsInExitZone(i))
                    TryServeFromSlot(slots[i]);
        }

        // EN: Cancels each lane's serving order, reloads customer queues and brings in the first customer with fresh order progress.
        // VI: Hủy order phục vụ của từng làn, nạp lại hàng khách và đưa khách đầu tiên vào với tiến độ order mới.
        public void LoadCustomers(LevelConfig config)
        {
            if (config == null)
                return;
            StopAllCoroutines();
            praiseText?.HideAll();

            allServedLogged = false;
            for (int side = 0; side < lanes.Length; side++)
            {
                Lane lane = lanes[side];
                if (lane == null)
                    continue;
                if (lane.servingFood == null)
                    Debug.LogWarning($"[CustomerManager] Làn {(side == 0 ? "trái" : "phải")} chưa gán ServingFood; không thể nhận nguyên liệu.", this);
                lane.queue = config.GetQueue(side);
                lane.queueIndex = 0;
                lane.ordering = false;
                ResetServing(lane);
                lane.dishes.Clear();
                CaptureQueueSpots(lane);
                LayoutWaitingQueue(lane, 0f);
                if (lane.queue != null && lane.queue.Count > 0)
                    StartCoroutine(BringNextCustomer(lane));
                else if (lane.customer != null)
                    lane.customer.SetVisible(false);
            }
            CheckAllServed();
        }

        // EN: Selects the earliest eligible customer, reserves its recipe ingredient and delegates the flight to that lane's serving component.
        // VI: Chọn khách phù hợp đến sớm nhất, giữ chỗ nguyên liệu trong công thức và giao chuyến bay cho component phục vụ của làn.
        private void TryServeFromSlot(ConveyorSlot slot)
        {
            if (slot == null || slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsPrepared)
                return;
            if (!TryFindCustomer(slot.food.foodType, out Lane lane, out int dish))
                return;
            FoodType type = slot.food.foodType;
            DishProgress progress = lane.dishes[dish];
            progress.Reserve(type);
            if (!lane.servingFood.LaunchServe(slot, dish))
                progress.CancelReservation(type);
        }

        // EN: True when some ordering customer has a dish that can take this ingredient right now (ingredients already flying are counted).
        // VI: True khi có khách đang order có món nhận được nguyên liệu này ngay lúc này (đã tính nguyên liệu đang bay tới).
        public bool TryFindCustomer(FoodType type) => TryFindCustomer(type, out _, out _);

        // EN: A dish the AutoComplete booster will finish: its tray, its slot index and the ingredients it still needed (now reserved, in recipe order, base first).
        // VI: Món mà booster AutoComplete sẽ hoàn thành: khay, chỉ số ô và các nguyên liệu nó còn thiếu (đã được giữ chỗ, theo thứ tự công thức, đế trước).
        public sealed class AutoCompleteJob
        {
            public ServingFood serving;
            public int dish;
            public readonly List<FoodType> ingredients = new List<FoodType>();
        }

        // EN: True when some ordering customer has a dish the AutoComplete booster can finish right now.
        // VI: True khi có khách đang order có món mà booster AutoComplete hoàn thành được ngay lúc này.
        public bool HasAutoCompleteTarget() => FindAutoCompleteDish(out _, out _);

        // EN: Picks the dish missing the most ingredients among every ordering customer (ties: earlier customer, then earlier dish on the card), skipping dishes with an ingredient still flying in or already assembling. Reserves all of its missing ingredients so the belt no longer serves them, and returns them in recipe order.
        // VI: Chọn món còn thiếu nhiều nguyên liệu nhất trong mọi khách đang order (bằng nhau thì khách đến sớm hơn, rồi món đứng trước trên thẻ), bỏ qua món đang có nguyên liệu bay tới hoặc đang ghép. Giữ chỗ mọi nguyên liệu còn thiếu để băng chuyền không gửi tới nữa, và trả về chúng theo thứ tự công thức.
        public bool TryReserveAutoComplete(out AutoCompleteJob job)
        {
            job = null;
            if (!FindAutoCompleteDish(out Lane lane, out int dish))
                return false;
            DishProgress progress = lane.dishes[dish];
            job = new AutoCompleteJob { serving = lane.servingFood, dish = dish };
            foreach (FoodType ingredient in progress.recipe)
                if (progress.missing.Contains(ingredient))
                    job.ingredients.Add(ingredient);
            foreach (FoodType ingredient in job.ingredients)
                progress.Reserve(ingredient);
            return job.ingredients.Count > 0;
        }

        // EN: Finds the AutoComplete target dish (see TryReserveAutoComplete) without reserving anything.
        // VI: Tìm món đích của AutoComplete (xem TryReserveAutoComplete) mà không giữ chỗ gì.
        private bool FindAutoCompleteDish(out Lane best, out int bestDish)
        {
            best = null;
            bestDish = -1;
            int bestMissing = 0;
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || lane.servingFood == null)
                    continue;
                for (int d = 0; d < lane.dishes.Count; d++)
                {
                    DishProgress progress = lane.dishes[d];
                    int missing = progress.missing.Count;
                    if (progress.assembled || progress.incoming > 0 || missing == 0 || !lane.servingFood.CanServe(d))
                        continue;
                    bool better = missing > bestMissing ||
                                  (missing == bestMissing && best != null && lane != best && lane.arrivalStamp < best.arrivalStamp);
                    if (!better)
                        continue;
                    best = lane;
                    bestDish = d;
                    bestMissing = missing;
                }
            }
            return best != null;
        }

        // EN: True once every customer of the level has been served.
        // VI: True khi mọi khách của level đã được phục vụ.
        public bool AllServed => allServedLogged;

        // EN: Fills result with the ingredients still needed, in serving priority: customers by arrival (earliest first), their dishes in card order, each dish's base first and then its other missing ingredients in recipe order. Ingredients already on the conveyor (or landing on it) are taken out, earliest need first.
        // VI: Điền vào result các nguyên liệu còn cần, theo thứ tự ưu tiên phục vụ: khách theo thứ tự đến (sớm nhất trước), món theo thứ tự trên thẻ, mỗi món đế trước rồi tới các nguyên liệu còn thiếu theo công thức. Nguyên liệu đã có (hoặc đang đáp xuống) trên băng chuyền được trừ đi, trừ vào nhu cầu sớm nhất trước.
        public void GetNeededIngredients(List<FoodType> result)
        {
            result.Clear();
            List<Lane> ordered = new List<Lane>();
            foreach (Lane lane in lanes)
                if (lane != null && lane.ordering)
                    ordered.Add(lane);
            ordered.Sort((a, b) => a.arrivalStamp.CompareTo(b.arrivalStamp));
            foreach (Lane lane in ordered)
            {
                foreach (DishProgress dish in lane.dishes)
                {
                    if (dish.assembled)
                        continue;
                    foreach (FoodType ingredient in dish.recipe)
                        if (dish.missing.Contains(ingredient))
                            result.Add(ingredient);
                }
            }
            if (conveyor == null)
                return;
            IReadOnlyList<ConveyorSlot> slots = conveyor.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot == null || slot.food == null || (slot.isEmpty && !slot.IsFoodArriving))
                    continue;
                result.Remove(slot.food.foodType);
            }
        }

        // EN: Finds the ordering lane whose customer arrived first and has a dish that can take the ingredient; within a customer, the earliest dish on the card wins.
        // VI: Tìm làn đang order có khách đến sớm nhất và có món nhận được nguyên liệu; trong cùng một khách, món đứng trước trên thẻ được ưu tiên.
        private bool TryFindCustomer(FoodType type, out Lane best, out int bestDish)
        {
            best = null;
            bestDish = -1;
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || (best != null && lane.arrivalStamp >= best.arrivalStamp))
                    continue;
                for (int d = 0; d < lane.dishes.Count; d++)
                {
                    if (!lane.dishes[d].CanAccept(type) || lane.servingFood == null || !lane.servingFood.CanServe(d))
                        continue;
                    best = lane;
                    bestDish = d;
                    break;
                }
            }
            return best != null;
        }

        // EN: True when every dish of the lane's current customer has been assembled on the tray.
        // VI: True khi mọi món của khách hiện tại trong làn đã được ghép xong trên khay.
        private static bool IsOrderAssembled(Lane lane)
        {
            foreach (DishProgress dish in lane.dishes)
            {
                if (!dish.assembled)
                    return false;
            }
            return true;
        }

        // EN: Resets only the serving component assigned to this customer lane.
        // VI: Chỉ đặt lại component phục vụ được gán cho làn khách này.
        private void ResetServing(Lane lane)
        {
            if (lane != null && lane.servingFood != null)
                lane.servingFood.ResetOrder();
        }

        // EN: Rewards and slides out the customer, clears the dishes from the tray, then loads the next order or empties the tray when the lane is finished.
        // VI: Thưởng và cho khách trượt ra, dọn món khỏi khay, rồi nạp order tiếp theo hoặc làm trống khay khi làn hết khách.
        private IEnumerator FinishCustomer(Lane lane)
        {
            lane.ordering = false;
            PlayPraise(lane.customer);
            IngameSoundManager.Instance?.PlayCustomerHappy();
            if (Config.customers.leaveDelay > 0f)
                yield return new WaitForSeconds(Config.customers.leaveDelay);
            if (lane.orderView != null)
            {
                float hide = lane.orderView.PlayHide();
                if (hide > 0f)
                    yield return new WaitForSeconds(hide);
            }
            if (lane.customer != null)
                yield return lane.customer.SlideOut(lane.slideDirection * Config.customers.slideDistance, Config.customers.slideDuration);
            ResetServing(lane);
            lane.queueIndex++;
            if (lane.queueIndex < lane.queue.Count)
            {
                yield return StepQueueForward(lane);
                yield return BringNextCustomer(lane);
            }
            else
            {
                if (lane.customer != null)
                    lane.customer.SetVisible(false);
                CheckAllServed();
            }
        }

        // EN: Shows the lane's next customer with all of their dishes on the card; a customer without any valid dish is finished straight away.
        // VI: Hiện khách tiếp theo của làn cùng mọi món khách order trên thẻ; khách không có món hợp lệ nào được kết thúc ngay.
        private IEnumerator BringNextCustomer(Lane lane)
        {
            SetupOrder(lane, lane.queue[lane.queueIndex]);
            if (lane.customer != null)
            {
                lane.customer.SetPortrait(GetPortrait(lane, lane.queueIndex));
                lane.customer.SnapHome();
                lane.customer.SetVisible(true);
            }
            if (lane.orderView != null)
                lane.orderView.PlayShow();
            LayoutWaitingQueue(lane, 0f);
            yield return null;
            lane.arrivalStamp = ++arrivalCounter;
            if (lane.dishes.Count > 0)
            {
                lane.ordering = true;
                yield break;
            }
            Debug.LogWarning($"[CustomerManager] Khách {lane.queueIndex + 1} không có món hợp lệ nào; bỏ qua.", this);
            yield return FinishCustomer(lane);
        }

        // EN: Builds the dishes of the order (supported recipes only, at most one per card slot), reserves one empty tray slot per dish with its stack, then fills and lays out the order card.
        // VI: Dựng các món của order (chỉ món có công thức, tối đa mỗi ô thẻ một món), dành cho mỗi món một ô khay trống kèm chồng nguyên liệu, rồi điền và xếp thẻ order.
        private void SetupOrder(Lane lane, CustomerOrder order)
        {
            ResetServing(lane);
            lane.dishes.Clear();
            if (lane.orderView == null)
            {
                Debug.LogWarning("[CustomerManager] Làn chưa gán OrderView; chạy Tools → HotPot → Setup Customers And References.", this);
                return;
            }
            IReadOnlyList<OrderItemView> items = lane.orderView.Items;
            if (order?.finalFoods != null)
            {
                foreach (FinalFoodType type in order.finalFoods)
                {
                    IReadOnlyList<FoodType> ingredients = GetIngredients(type);
                    if (ingredients.Count == 0)
                        continue;
                    if (lane.dishes.Count >= items.Count)
                    {
                        Debug.LogWarning($"[CustomerManager] Khách order {order.finalFoods.Count} món nhưng thẻ chỉ có {items.Count} ô; bỏ bớt các món cuối.", this);
                        break;
                    }
                    lane.dishes.Add(new DishProgress(type, ingredients));
                }
            }
            BeginServing(lane);
            for (int i = 0; i < items.Count; i++)
            {
                OrderItemView item = items[i];
                if (item == null)
                    continue;
                if (i < lane.dishes.Count)
                    item.ShowDish(finalFoodConfig != null ? finalFoodConfig.GetSprite(lane.dishes[i].type) : null);
                else
                    item.Hide();
            }
            List<IReadOnlyList<FoodType>> cardRecipes = new List<IReadOnlyList<FoodType>>(lane.dishes.Count);
            foreach (DishProgress dish in lane.dishes)
                cardRecipes.Add(dish.recipe);
            lane.orderView.SetRecipes(cardRecipes);
            lane.orderView.Layout(lane.dishes.Count);
        }

        // EN: Gives the lane's serving component its order view and recipes, and routes arrival callbacks to customer progress.
        // VI: Giao thẻ order cùng công thức cho component phục vụ của làn và chuyển callback đáp món về tiến độ khách.
        private void BeginServing(Lane lane)
        {
            if (lane.servingFood == null)
                return;
            List<FinalFoodType> types = new List<FinalFoodType>(lane.dishes.Count);
            List<IReadOnlyList<FoodType>> recipes = new List<IReadOnlyList<FoodType>>(lane.dishes.Count);
            foreach (DishProgress dish in lane.dishes)
            {
                types.Add(dish.type);
                recipes.Add(dish.recipe);
            }
            lane.servingFood.BeginOrder(lane.orderView, types, recipes, finalFoodConfig,
                dish => OnIngredientDelivered(lane, dish), dish => OnDishDelivered(lane, dish));
        }

        // EN: Records one ingredient arrival and requests assembly only when the recipe has fully landed.
        // VI: Ghi nhận một nguyên liệu đáp xuống và chỉ yêu cầu ghép khi toàn bộ công thức đã tới nơi.
        private void OnIngredientDelivered(Lane lane, int dish)
        {
            if (!lane.ordering || dish < 0 || dish >= lane.dishes.Count)
                return;
            DishProgress progress = lane.dishes[dish];
            progress.Arrive();
            if (progress.Done)
                lane.servingFood.CompleteDish(dish);
        }

        // EN: Marks an order dish delivered and starts customer departure only when every dish has reached the order card.
        // VI: Đánh dấu món order đã được giao và chỉ bắt đầu cho khách rời đi khi mọi món đã tới thẻ order.
        private void OnDishDelivered(Lane lane, int dish)
        {
            if (!lane.ordering || dish < 0 || dish >= lane.dishes.Count || lane.dishes[dish].assembled)
                return;
            lane.dishes[dish].assembled = true;
            if (!IsOrderAssembled(lane))
            {
                IngameSoundManager.Instance?.PlayPrepared();
                return;
            }
            StartCoroutine(FinishCustomer(lane));
        }

        // EN: Returns the ingredients of a final dish from FinalFoodConfig, warning when the config or the recipe is missing.
        // VI: Trả về nguyên liệu của món hoàn chỉnh từ FinalFoodConfig, cảnh báo khi thiếu config hoặc công thức.
        private IReadOnlyList<FoodType> GetIngredients(FinalFoodType finalFood)
        {
            if (finalFoodConfig == null)
            {
                Debug.LogWarning("[CustomerManager] Chưa gán FinalFoodConfig; chạy Tools → HotPot → Setup Customers And References.", this);
                return Array.Empty<FoodType>();
            }
            IReadOnlyList<FoodType> ingredients = finalFoodConfig.GetIngredients(finalFood);
            if (ingredients.Count == 0)
                Debug.LogWarning($"[CustomerManager] FinalFoodConfig chưa có công thức cho {finalFood}.", this);
            return ingredients;
        }

        // EN: Remembers the queue spots from the waiting figures placed in the scene (nearest first) and the step between them, used to place any number of waiting customers.
        // VI: Ghi nhớ các vị trí hàng chờ từ khách đứng chờ đặt sẵn trong scene (gần quầy trước) và khoảng cách giữa chúng, dùng để xếp bao nhiêu khách chờ cũng được.
        private void CaptureQueueSpots(Lane lane)
        {
            if (lane.queueCaptured)
                return;
            lane.queueCaptured = true;
            foreach (SpriteRenderer figure in lane.waitingFigures)
            {
                if (figure == null)
                    continue;
                lane.figures.Add(figure);
                lane.spots.Add(figure.transform.position);
            }
            Vector3 counter = lane.customer != null && lane.customer.Portrait != null ? lane.customer.Portrait.transform.position : Vector3.zero;
            lane.queueOrigin = counter;
            if (lane.spots.Count >= 2)
                lane.spotStep = lane.spots[lane.spots.Count - 1] - lane.spots[lane.spots.Count - 2];
            else if (lane.spots.Count == 1)
                lane.spotStep = lane.spots[0] - counter;
            else
                lane.spotStep = new Vector3(lane.slideDirection * Config.customers.queueSpacing.x, Config.customers.queueSpacing.y, 0f);
        }

        // EN: World position of the k-th waiting spot (0 = right behind the counter), extending the scene spots by the last step.
        // VI: Vị trí world của chỗ chờ thứ k (0 = ngay sau quầy), nối dài các chỗ trong scene theo bước cuối cùng.
        private static Vector3 GetQueueSpot(Lane lane, int k)
        {
            if (k < lane.spots.Count)
                return lane.spots[k];
            Vector3 last = lane.spots.Count > 0 ? lane.spots[lane.spots.Count - 1] : lane.queueOrigin;
            return last + lane.spotStep * (k - lane.spots.Count + 1);
        }

        // EN: Makes sure the lane has a figure per waiting customer, creating a plain sprite only when the pool (scene figures first) is too small.
        // VI: Bảo đảm làn có đủ hình cho mỗi khách đang chờ, chỉ tạo sprite mới khi pool (ưu tiên hình đặt sẵn trong scene) không đủ.
        private void EnsureFigures(Lane lane, int count)
        {
            while (lane.figures.Count < count)
            {
                SpriteRenderer figure = new GameObject($"Waiting customer {lane.figures.Count + 1}").AddComponent<SpriteRenderer>();
                figure.transform.SetParent(transform, true);
                lane.figures.Add(figure);
            }
        }

        // EN: Places every waiting customer of the lane on its spot: smaller, darker and drawn behind the ordering customer, with each person further back drawn behind the one in front. stepT (0..1) blends from the previous spot for the walk-forward animation.
        // VI: Đặt mọi khách đang chờ của làn vào chỗ của mình: nhỏ hơn, tối hơn và vẽ sau khách đang order, người đứng sau vẽ sau người phía trước. stepT (0..1) nội suy từ chỗ cũ để tạo hoạt cảnh dồn lên.
        private void LayoutWaitingQueue(Lane lane, float stepT)
        {
            int waiting = lane.queue != null ? Mathf.Max(0, lane.queue.Count - lane.queueIndex - 1) : 0;
            EnsureFigures(lane, waiting);
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            for (int k = 0; k < lane.figures.Count; k++)
            {
                SpriteRenderer figure = lane.figures[k];
                bool used = k < waiting;
                figure.gameObject.SetActive(used);
                if (!used)
                    continue;
                Sprite portrait = GetPortrait(lane, lane.queueIndex + 1 + k);
                if (portrait != null)
                    figure.sprite = portrait;
                figure.color = Config.customers.waitingTint;
                Vector3 target = GetQueueSpot(lane, k);
                figure.transform.position = stepT > 0f ? Vector3.LerpUnclamped(GetQueueSpot(lane, k + 1), target, stepT) : target;
                if (active != null)
                {
                    SetWorldScale(figure.transform, active.transform.lossyScale * Config.customers.waitingScale);
                    figure.sortingLayerID = active.sortingLayerID;
                    figure.sortingOrder = active.sortingOrder - 1 - k;
                }
            }
        }

        // EN: Walks the queue forward one place: the first waiting customer moves to the counter while growing and brightening, the rest move up one spot.
        // VI: Cho hàng chờ dồn lên một chỗ: khách đầu hàng đi tới quầy, to dần và sáng dần, những người còn lại tiến lên một chỗ.
        private IEnumerator StepQueueForward(Lane lane)
        {
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            if (lane.figures.Count == 0 || active == null)
                yield break;
            SpriteRenderer walker = lane.figures[0];
            lane.figures.RemoveAt(0);
            lane.figures.Add(walker);
            lane.customer.SnapHome();
            lane.customer.SetVisible(false);
            Vector3 from = walker.transform.position;
            Vector3 fromScale = walker.transform.lossyScale;
            Vector3 to = active.transform.position;
            Vector3 toScale = active.transform.lossyScale;
            walker.sortingOrder = active.sortingOrder;
            float time = 0f;
            while (time < Config.customers.stepForwardDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / Config.customers.stepForwardDuration));
                walker.transform.position = Vector3.LerpUnclamped(from, to, t) + Vector3.up * (0.15f * Mathf.Sin(t * Mathf.PI));
                SetWorldScale(walker.transform, Vector3.LerpUnclamped(fromScale, toScale, t));
                walker.color = Color.Lerp(Config.customers.waitingTint, Color.white, t);
                LayoutQueueBehind(lane, t);
                yield return null;
            }
            walker.gameObject.SetActive(false);
        }

        // EN: Moves the remaining waiting customers (all but the one walking to the counter) one spot forward at progress t.
        // VI: Đưa các khách chờ còn lại (trừ người đang đi tới quầy) tiến lên một chỗ theo tiến độ t.
        private void LayoutQueueBehind(Lane lane, float t)
        {
            int waiting = lane.queue != null ? Mathf.Max(0, lane.queue.Count - lane.queueIndex - 1) : 0;
            for (int k = 0; k < waiting && k < lane.figures.Count - 1; k++)
            {
                SpriteRenderer figure = lane.figures[k];
                figure.transform.position = Vector3.LerpUnclamped(GetQueueSpot(lane, k + 1), GetQueueSpot(lane, k), t);
            }
        }

        // EN: Returns the portrait for the customer at a queue index (cycling through the lane's portraits), or null when none are set.
        // VI: Trả về ảnh của khách tại vị trí trong hàng (lặp lại danh sách ảnh của làn), hoặc null nếu chưa có ảnh.
        private static Sprite GetPortrait(Lane lane, int index)
        {
            return lane.portraits.Length > 0 ? lane.portraits[index % lane.portraits.Length] : null;
        }

        // EN: Sets a transform's local scale so its world scale matches the given value.
        // VI: Đặt scale cục bộ để scale world của transform bằng giá trị cho trước.
        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, parent.x), worldScale.y / Mathf.Max(0.0001f, parent.y), 1f);
        }

        // EN: Pops a random praise word ("Delicious!"...) above the customer, at the heart anchor.
        // VI: Bắn một chữ khen ngẫu nhiên ("Delicious!"...) phía trên khách, tại điểm neo tim của khách.
        private void PlayPraise(CustomerView customer)
        {
            if (customer == null)
                return;
            if (praiseText == null)
                praiseText = new PraiseText(transform);
            SpriteRenderer portrait = customer.Portrait;
            int sortingLayer = portrait != null ? portrait.sortingLayerID : 0;
            int sortingOrder = (portrait != null ? portrait.sortingOrder : 0) + Config.ingredientFlight.serveSortingBoost + 5;
            StartCoroutine(praiseText.Play(Config.praiseText, customer.HeartPosition, sortingLayer, sortingOrder));
        }

        // EN: Logs once when no lane has a customer left to serve.
        // VI: Ghi log một lần khi không còn làn nào có khách cần phục vụ.
        private void CheckAllServed()
        {
            if (allServedLogged)
                return;
            foreach (Lane lane in lanes)
            {
                if (lane != null && lane.queue != null && lane.queueIndex < lane.queue.Count)
                    return;
            }
            allServedLogged = true;
            Debug.Log("[CustomerManager] Đã phục vụ hết khách của level.", this);
        }

#if UNITY_EDITOR
        // EN: Links missing per-lane serving components left to right only when the scene contains exactly two trays.
        // VI: Gán component phục vụ còn thiếu theo thứ tự trái sang phải chỉ khi scene có đúng hai khay.
        private void OnValidate()
        {
            if (Application.isPlaying || lanes == null || lanes.Length < 2 ||
                (lanes[0]?.servingFood != null && lanes[1]?.servingFood != null))
                return;
            ServingFood[] services = FindObjectsByType<ServingFood>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (services.Length != 2)
                return;
            Array.Sort(services, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            for (int side = 0; side < 2; side++)
                if (lanes[side] != null && lanes[side].servingFood == null)
                    lanes[side].servingFood = services[side];
        }
#endif
    }
}
