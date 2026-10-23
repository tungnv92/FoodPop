using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Owns one customer's tray flights, assembly effects and sprite pool; each serving prefab has its own instance.
    // VI: Quản lý chuyến bay, hiệu ứng ghép và pool sprite của khay một khách; mỗi prefab phục vụ có instance riêng.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ServingTrayView), typeof(TrayDishStack))]
    public class ServingFood : MonoBehaviour
    {
        [Tooltip("Khay phục vụ thuộc đối tượng này.")]
        [SerializeField] private ServingTrayView tray;
        [Tooltip("Cấu hình thời gian và hoạt cảnh dùng chung trong màn chơi.")]
        [SerializeField] private IngameConfig ingameConfig;
        [Tooltip("Hiệu ứng khi các lớp nguyên liệu ghép thành món hoàn chỉnh.")]
        [SerializeField] private ParticleEffect dishCompleteEffect;

        private OrderView orderView;
        private FinalFoodConfig finalFoodConfig;
        private readonly List<FinalFoodType> dishTypes = new List<FinalFoodType>();
        private readonly HashSet<int> incomingDishes = new HashSet<int>();
        private readonly HashSet<int> completingDishes = new HashSet<int>();
        private readonly HashSet<int> deliveredDishes = new HashSet<int>();
        private readonly HashSet<SpriteRenderer> ownedFlyers = new HashSet<SpriteRenderer>();
        private readonly Stack<SpriteRenderer> flyerPool = new Stack<SpriteRenderer>();
        private Transform flyerRoot;
        private Action<int> ingredientDelivered;
        private Action<int> dishDelivered;

        // EN: Resolves the shared animation configuration for this tray.
        // VI: Lấy cấu hình hoạt cảnh dùng chung cho khay này.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        // EN: Returns this instance's ingredient stack without searching another customer's tray.
        // VI: Trả về chồng nguyên liệu của instance này, không tìm sang khay khách khác.
        private TrayDishStack DishStack => tray != null ? tray.DishStack : null;

        private sealed class ServeFlight
        {
            public int dish;
            public FoodType type;
            public SpriteRenderer flyer;
            public Vector3 start;
            public Vector3 startScale;
        }

        // EN: Prepares this tray for one order and binds delivery notifications to the caller's customer progress.
        // VI: Chuẩn bị khay này cho một order và nối thông báo đáp món với tiến độ khách do bên gọi quản lý.
        public void BeginOrder(OrderView view, IReadOnlyList<FinalFoodType> types,
            IReadOnlyList<IReadOnlyList<FoodType>> recipes, FinalFoodConfig foodConfig,
            Action<int> onIngredientDelivered, Action<int> onDishDelivered)
        {
            ResetOrder();
            orderView = view;
            finalFoodConfig = foodConfig;
            dishTypes.AddRange(types);
            ingredientDelivered = onIngredientDelivered;
            dishDelivered = onDishDelivered;
            if (DishStack != null)
                DishStack.Begin(types, recipes);
            else if (tray != null)
                tray.SetSlotCount(types.Count);
        }

        // EN: Allows only one ingredient flight to each live dish slot while keeping other slots independent.
        // VI: Chỉ cho một nguyên liệu bay tới mỗi ô món đang hoạt động, các ô khác vẫn độc lập.
        public bool CanServe(int dish)
        {
            return isActiveAndEnabled && orderView != null && dish >= 0 && dish < dishTypes.Count &&
                dish < orderView.Items.Count && orderView.Items[dish] != null &&
                !incomingDishes.Contains(dish) && !completingDishes.Contains(dish) && !deliveredDishes.Contains(dish);
        }

        // EN: Consumes a prepared belt ingredient and starts this tray's flight after the customer has reserved its recipe entry.
        // VI: Lấy nguyên liệu đã sơ chế khỏi băng và bắt đầu bay tới khay này sau khi khách đã giữ chỗ trong công thức.
        public bool LaunchServe(ConveyorSlot slot, int dish)
        {
            if (!CanServe(dish) || slot == null || slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsPrepared)
                return false;
            SpriteRenderer source = slot.IngredientRenderer;
            int trayOrder = tray != null ? tray.StackSortingOrder + CustomerOrder.MaxDishes * 8 : 0;
            int sortingOrder = Mathf.Max(source != null ? source.sortingOrder : 0, trayOrder) + Config.ingredientFlight.serveSortingBoost;
            ServeFlight flight = new ServeFlight
            {
                dish = dish,
                type = slot.food.foodType,
                flyer = RentFlyer(source != null ? source.sprite : null, source != null ? source.sortingLayerID : 0, sortingOrder),
                start = slot.LandingPosition,
                startScale = slot.LandingScale
            };
            incomingDishes.Add(dish);
            slot.ClearFood();
            StartCoroutine(FlyToTray(flight));
            return true;
        }

        // EN: Starts a flight for an ingredient that does not come from the belt (AutoComplete booster): it flies from start along the usual serving arc onto the dish's stack and is delivered like a normal serve. The customer must already have reserved the ingredient.
        // VI: Cho một nguyên liệu không đến từ băng chuyền (booster AutoComplete) bay từ start theo vòng cung phục vụ thường lên chồng của món và được giao như phục vụ thường. Khách phải đã giữ chỗ nguyên liệu này.
        public bool LaunchBoosterServe(int dish, FoodType type, Sprite sprite, Vector3 start, Vector3 startScale)
        {
            if (!isActiveAndEnabled || orderView == null || dish < 0 || dish >= dishTypes.Count ||
                completingDishes.Contains(dish) || deliveredDishes.Contains(dish))
                return false;
            int trayOrder = tray != null ? tray.StackSortingOrder + CustomerOrder.MaxDishes * 8 : 0;
            ServeFlight flight = new ServeFlight
            {
                dish = dish,
                type = type,
                flyer = RentFlyer(sprite, EffectSortingLayer, trayOrder + Config.ingredientFlight.serveSortingBoost),
                start = start,
                startScale = startScale
            };
            incomingDishes.Add(dish);
            StartCoroutine(FlyToTray(flight));
            return true;
        }

        // EN: True while an ingredient is still flying to the given dish slot.
        // VI: True khi còn nguyên liệu đang bay tới ô món đã cho.
        public bool IsReceiving(int dish) => incomingDishes.Contains(dish);

        // EN: World centre of a dish slot on this tray (the order card dish when there is no tray stack).
        // VI: Tâm world của ô món trên khay này (món trên thẻ order khi không có chồng trên khay).
        public Vector3 GetDishSlotCenter(int dish)
        {
            if (DishStack != null && DishStack.HasSlot(dish))
                return DishStack.GetSlotCenter(dish);
            return orderView != null && dish >= 0 && dish < orderView.Items.Count && orderView.Items[dish] != null
                ? orderView.Items[dish].TargetPosition : transform.position;
        }

        // EN: World scale an ingredient has once stacked on the dish slot (Vector3.one when there is no stack).
        // VI: Scale world của nguyên liệu khi đã nằm trên chồng của ô món (Vector3.one khi không có chồng).
        public Vector3 GetStackedScale(int dish, FoodType type)
        {
            return DishStack != null && DishStack.HasSlot(dish) ? DishStack.GetPieceWorldScale(dish, type) : Vector3.one;
        }

        // EN: Sorting layer of the food on this tray (effects drawn over the tray use it).
        // VI: Sorting layer của món trên khay này (hiệu ứng vẽ trên khay dùng nó).
        public int EffectSortingLayer => DishStack != null ? DishStack.SortingLayer : 0;

        // EN: Places an arriving ingredient on this tray and notifies customer progress only after the landing is recorded.
        // VI: Đặt nguyên liệu vừa đáp lên khay này và chỉ báo tiến độ khách sau khi ghi nhận việc đáp xuống.
        private void DeliverIngredient(ServeFlight flight)
        {
            IngameSoundManager.Instance?.PlayServe();
            orderView.Items[flight.dish].PlayIngredientArrived();
            orderView.MarkIngredientArrived(flight.dish, flight.type);
            if (DishStack != null && DishStack.HasSlot(flight.dish))
            {
                DishStack.AddPiece(flight.dish, flight.type, flight.flyer);
                StartTwinkleIfStacked(flight.dish);
            }
            else
                ReturnFlyer(flight.flyer);
            incomingDishes.Remove(flight.dish);
            ingredientDelivered?.Invoke(flight.dish);
        }

        // EN: Starts the twinkling-star effect nested in the dish's slot once its stack holds at least dishTwinkle.minLayers ingredients.
        // VI: Bật hiệu ứng sao lấp lánh lồng sẵn trong ô của món khi chồng đã có từ dishTwinkle.minLayers nguyên liệu trở lên.
        private void StartTwinkleIfStacked(int dish)
        {
            DishSlotView slot = tray != null ? tray.GetSlotView(dish) : null;
            if (slot == null || DishStack.PieceCount(dish) < Config.dishTwinkle.minLayers)
                return;
            slot.PlayTwinkle(DishStack.SortingLayer, DishStack.GetDishSortingOrder(dish) + 2);
        }

        // EN: Stops the twinkle on every slot of this tray.
        // VI: Dừng hiệu ứng lấp lánh trên mọi ô của khay này.
        private void StopAllTwinkles()
        {
            if (tray == null)
                return;
            for (int i = 0; i < tray.SlotCount; i++)
                tray.GetSlotView(i)?.StopTwinkle();
        }

        // EN: Starts assembly once for a fully delivered recipe, preventing further ingredients entering that slot.
        // VI: Bắt đầu ghép một lần cho công thức đã nhận đủ nguyên liệu, ngăn nguyên liệu khác vào ô đó.
        public void CompleteDish(int dish)
        {
            if (!CanServe(dish))
                return;
            completingDishes.Add(dish);
            StartCoroutine(AssembleDish(dish));
        }

        // EN: Waits for the stack bounce to finish, then assembles the tray dish, flies it to its order image, marks the view complete and reports delivery to the customer owner.
        // VI: Chờ chồng nguyên liệu nảy xong, rồi ghép món trên khay, cho bay tới ảnh order, đánh dấu hiển thị hoàn thành và báo đáp món cho bên quản lý khách.
        private IEnumerator AssembleDish(int dish)
        {
            while (DishStack != null && DishStack.IsSettling(dish))
                yield return null;
            if (Config.dishAssembly.assembleDelay > 0f)
                yield return new WaitForSeconds(Config.dishAssembly.assembleDelay);
            if (DishStack != null && DishStack.HasSlot(dish))
            {
                yield return MergeStack(DishStack, dish, dishTypes[dish]);
                yield return FlyDishToOrder(dish);
            }
            orderView.Items[dish].MarkDone();
            completingDishes.Remove(dish);
            deliveredDishes.Add(dish);
            dishDelivered?.Invoke(dish);
        }

        // EN: Cancels only this customer's animations and returns every owned sprite, including sprites between merge and flight stages.
        // VI: Hủy hoạt cảnh của riêng khách này và trả mọi sprite đang sở hữu, kể cả sprite ở giữa giai đoạn ghép và bay.
        public void ResetOrder()
        {
            StopAllCoroutines();
            StopAllTwinkles();
            ingredientDelivered = null;
            dishDelivered = null;
            if (DishStack != null)
                DishStack.ReleaseAll();
            foreach (SpriteRenderer flyer in new List<SpriteRenderer>(ownedFlyers))
                ReturnFlyer(flyer);
            ownedFlyers.Clear();
            incomingDishes.Clear();
            completingDishes.Clear();
            deliveredDishes.Clear();
            dishTypes.Clear();
            orderView = null;
            finalFoodConfig = null;
            if (tray != null)
                tray.SetSlotCount(0);
        }

        // EN: Releases this tray's animation resources without affecting the other customer's pool.
        // VI: Giải phóng tài nguyên hoạt cảnh của khay này mà không ảnh hưởng pool khách khác.
        private void OnDestroy()
        {
            ResetOrder();
            if (flyerRoot != null)
                Destroy(flyerRoot.gameObject);
        }

#if UNITY_EDITOR
        // EN: Links only the tray on this prefab's own root in the editor.
        // VI: Trong editor, chỉ gán khay nằm trên gốc prefab của chính component này.
        private void OnValidate()
        {
            if (tray == null)
                tray = GetComponent<ServingTrayView>();
        }
#endif

        // EN: Moves the flyer along a parabolic arc to where it will rest on the dish's stack (re-aimed every frame, so a base landing first lifts the target), shrinking to the stacked size; falls back to the order card when the lane has no tray.
        // VI: Cho nguyên liệu bay theo vòng cung parabol tới chỗ nó sẽ nằm trên chồng của món (tính lại mỗi frame, nên đế đáp trước sẽ nâng điểm đến), thu nhỏ về cỡ lớp; dùng thẻ order khi làn không có khay.
        private IEnumerator FlyToTray(ServeFlight flight)
        {
            Transform flyer = flight.flyer.transform;
            flyer.SetPositionAndRotation(flight.start, Quaternion.identity);
            flyer.localScale = flight.startScale;
            flight.flyer.gameObject.SetActive(true);
            float time = 0f;
            while (time < Config.ingredientFlight.serveFlyDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Config.ingredientFlight.serveFlyDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                GetFlightTarget(flight, out Vector3 targetPosition, out Vector3 targetScale);
                Vector3 position = Vector3.LerpUnclamped(flight.start, targetPosition, eased);
                position.y += Config.ingredientFlight.serveArcHeight * 4f * t * (1f - t);
                flyer.position = position;
                float pop = 1f + (Config.ingredientFlight.servePeakScale - 1f) * Mathf.Sin(t * Mathf.PI);
                flyer.localScale = Vector3.LerpUnclamped(flight.startScale, targetScale, eased) * pop;
                yield return null;
            }

            DeliverIngredient(flight);
        }

        // EN: Current landing point and world scale of a flight: its slot on the dish's tray stack, or the dish on the order card when there is no stack.
        // VI: Điểm đáp và scale world hiện tại của nguyên liệu đang bay: chỗ trên chồng của món trên khay, hoặc món trên thẻ order khi không có chồng.
        private void GetFlightTarget(ServeFlight flight, out Vector3 position, out Vector3 scale)
        {
            TrayDishStack tray = DishStack;
            if (tray != null && tray.HasSlot(flight.dish))
            {
                position = tray.GetLandingPosition(flight.dish, flight.type);
                scale = tray.GetPieceWorldScale(flight.dish, flight.type);
                return;
            }
            position = orderView.Items[flight.dish].TargetPosition;
            scale = flight.startScale * Config.ingredientFlight.serveEndScale;
        }

        // EN: Squeezes the stacked pieces into the slot centre, pools them, plays the poof and shows the finished dish picture with a pop.
        // VI: Chụm các lớp vào tâm ô, đưa về pool, phát hiệu ứng poof và hiện ảnh món hoàn chỉnh kèm cú nảy.
        private IEnumerator MergeStack(TrayDishStack tray, int dish, FinalFoodType type)
        {
            Vector3 center = tray.GetSlotCenter(dish);
            List<SpriteRenderer> pieces = tray.TakePieces(dish);
            List<Vector3> fromPositions = new List<Vector3>(pieces.Count);
            List<Vector3> fromScales = new List<Vector3>(pieces.Count);
            foreach (SpriteRenderer piece in pieces)
            {
                fromPositions.Add(piece.transform.position);
                fromScales.Add(piece.transform.localScale);
            }
            float time = 0f;
            while (time < Config.dishAssembly.mergeDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / Config.dishAssembly.mergeDuration));
                for (int i = 0; i < pieces.Count; i++)
                {
                    pieces[i].transform.position = Vector3.LerpUnclamped(fromPositions[i], center, t);
                    pieces[i].transform.localScale = fromScales[i] * Mathf.Lerp(1f, 0.6f, t);
                }
                yield return null;
            }
            foreach (SpriteRenderer piece in pieces)
                ReturnFlyer(piece);
            OneShotEffects.PlayParticle(dishCompleteEffect, center, tray.SortingLayer, tray.GetDishSortingOrder(dish) + 1);
            Sprite sprite = finalFoodConfig != null ? finalFoodConfig.GetSprite(type) : null;
            SpriteRenderer finished = RentFlyer(sprite, tray.SortingLayer, tray.GetDishSortingOrder(dish));
            Vector3 restScale = tray.SetFinishedDish(dish, finished);
            finished.gameObject.SetActive(true);
            yield return PopScale(finished.transform, restScale, Config.dishAssembly.dishPopDuration, Config.dishAssembly.dishPopAmount);
        }

        // EN: Flies the finished dish to its order slot with a scale swell mid-flight, settles at the exact card size and pops the card dish on landing.
        // VI: Cho món hoàn chỉnh bay tới ô order và phóng to giữa đường, thu về đúng cỡ ảnh trên thẻ rồi làm món trên thẻ nảy khi đáp xuống.
        private IEnumerator FlyDishToOrder(int dish)
        {
            if (Config.dishOrderFlight.dishFlyDelay > 0f)
                yield return new WaitForSeconds(Config.dishOrderFlight.dishFlyDelay);
            SpriteRenderer flyer = DishStack.TakeFinishedDish(dish);
            tray?.GetSlotView(dish)?.StopTwinkle();
            if (flyer == null)
                yield break;
            OrderItemView target = orderView.Items[dish];
            Transform flyerTransform = flyer.transform;
            Vector3 start = flyerTransform.position;
            Vector3 startScale = flyerTransform.lossyScale;
            flyerTransform.SetParent(EnsureFlyerRoot(), true);
            flyerTransform.localScale = startScale;
            int cardOrder = target.IngredientRenderer != null ? target.IngredientRenderer.sortingOrder : 0;
            flyer.sortingOrder = Mathf.Max(flyer.sortingOrder, cardOrder) + Config.ingredientFlight.serveSortingBoost;
            float time = 0f;
            while (time < Config.dishOrderFlight.dishFlyDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Config.dishOrderFlight.dishFlyDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.LerpUnclamped(start, target.TargetPosition, eased);
                position.y += Config.dishOrderFlight.dishFlyArcHeight * 4f * t * (1f - t);
                flyerTransform.position = position;
                float scaleBoost = 1f + (Mathf.Max(1f, Config.dishOrderFlight.dishFlyPeakScale) - 1f) * 4f * t * (1f - t);
                flyerTransform.localScale = Vector3.LerpUnclamped(startScale, target.TargetScale, eased) * scaleBoost;
                yield return null;
            }
            flyerTransform.position = target.TargetPosition;
            flyerTransform.localScale = target.TargetScale;
            ReturnFlyer(flyer);

        }

        // EN: Squash-and-stretch pop: grows the target quickly then eases back to its resting scale.
        // VI: Hiệu ứng nảy: phóng to nhanh rồi thu dần về scale nghỉ.
        private static IEnumerator PopScale(Transform target, Vector3 restScale, float duration, float amount)
        {
            float time = 0f;
            while (time < duration && target != null)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                target.localScale = restScale * (1f + amount * Mathf.Sin(t * Mathf.PI) * (1f - 0.5f * t));
                yield return null;
            }
            if (target != null)
                target.localScale = restScale;
        }

        // EN: Takes a flyer sprite from the pool (creating one only when the pool is empty) and applies the sprite and sorting.
        // VI: Lấy sprite bay từ pool (chỉ tạo mới khi pool rỗng) và gán sprite cùng thứ tự vẽ.
        private SpriteRenderer RentFlyer(Sprite sprite, int sortingLayer, int sortingOrder)
        {
            SpriteRenderer flyer = flyerPool.Count > 0 ? flyerPool.Pop() : null;
            if (flyer == null)
            {
                GameObject go = new GameObject("Served food");
                go.transform.SetParent(EnsureFlyerRoot(), false);
                flyer = go.AddComponent<SpriteRenderer>();
            }
            ownedFlyers.Add(flyer);
            flyer.sprite = sprite;
            flyer.sortingLayerID = sortingLayer;
            flyer.sortingOrder = sortingOrder;
            flyer.gameObject.SetActive(false);
            return flyer;
        }

        // EN: Hides a flyer, moves it back under the flyer container (it may have been lying on a tray slot) and keeps it for reuse.
        // VI: Ẩn sprite bay, đưa về lại object chứa (có thể nó đang nằm trên ô khay) và giữ lại để tái sử dụng.
        private void ReturnFlyer(SpriteRenderer flyer)
        {
            if (flyer == null || !ownedFlyers.Remove(flyer))
                return;
            flyer.gameObject.SetActive(false);
            flyer.transform.SetParent(EnsureFlyerRoot(), false);
            flyerPool.Push(flyer);
        }

        // EN: Returns the container of pooled flyers, creating it on first use.
        // VI: Trả về object chứa các sprite bay trong pool, tạo mới ở lần dùng đầu.
        private Transform EnsureFlyerRoot()
        {
            if (flyerRoot == null)
                flyerRoot = new GameObject($"ServeFlyers - {name}").transform;
            return flyerRoot;
        }

    }
}
