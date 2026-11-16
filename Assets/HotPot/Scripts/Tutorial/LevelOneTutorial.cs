using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class LevelOneTutorial : MonoBehaviour
    {
        [Tooltip("Controller phát sự kiện nạp màn chơi.")]
        [SerializeField] private LevelController levelController;
        [Tooltip("Bảng đĩa nhận thao tác chọn của người chơi.")]
        [SerializeField] private FoodBoardManager boardManager;
        [Tooltip("Prefab bàn tay và vùng sáng hướng dẫn chọn đĩa.")]
        [SerializeField] private BoosterTrialOverlay overlayPrefab;
        [Tooltip("Lời nhắc chọn đĩa ở màn đầu.")]
        [SerializeField] private string prompt = "Select a dish!";
        [Tooltip("Chờ đĩa xuất hiện xong trước khi đặt hướng dẫn, tính bằng giây.")]
        [SerializeField, Min(0f)] private float showDelay = 0.35f;

        [Tooltip("Manager phát sự kiện khách đến gọi món.")]
        [SerializeField] private CustomerManager customerManager;
        [Tooltip("Lời nhắc mở nguyên liệu của order.")]
        [SerializeField] private string orderPrompt = "Tap the order to see ingredients!";

        private OrderView targetOrder;
        private bool selectingDish;
        private BoosterTrialOverlay overlay;
        private Coroutine pendingShow;

        // EN: Subscribe to level, successful-pick and customer-arrival events before the initial level load.
        // VI: Đăng ký sự kiện màn, chọn đĩa thành công và khách đến trước khi nạp màn đầu.
        private void OnEnable()
        {
            levelController.LevelLoaded += OnLevelLoaded;
            levelController.StateChanged += OnStateChanged;
            boardManager.DishPicked += OnDishPicked;
            customerManager.CustomerArrived += OnCustomerArrived;
        }

        // EN: Unsubscribe and remove any pending or visible guide when leaving the scene.
        // VI: Gỡ đăng ký và xóa hướng dẫn đang chờ hoặc đang hiện khi rời scene.
        private void OnDisable()
        {
            levelController.LevelLoaded -= OnLevelLoaded;
            levelController.StateChanged -= OnStateChanged;
            boardManager.DishPicked -= OnDishPicked;
            customerManager.CustomerArrived -= OnCustomerArrived;
            ClearOrderTarget();
            HideGuide();
        }

        // EN: Reset both steps on load and start dish selection only at level one.
        // VI: Reset hai bước khi nạp màn và chỉ bắt đầu chọn đĩa ở màn một.
        private void OnLevelLoaded(LevelConfig config)
        {
            HideGuide();
            ClearOrderTarget();
            selectingDish = config.levelNumber == 1;
            if (selectingDish) pendingShow = StartCoroutine(ShowGuide());
        }

        // EN: Remove the guide when gameplay leaves the playing state.
        // VI: Xóa hướng dẫn khi gameplay rời trạng thái đang chơi.
        private void OnStateChanged(LevelController.LevelState state)
        {
            if (state == LevelController.LevelState.Playing) return;
            ClearOrderTarget();
            HideGuide();
        }

        // EN: Finish the level-one dish step after a successful pick.
        // VI: Kết thúc bước chọn đĩa màn một sau khi chọn thành công.
        private void OnDishPicked(FoodType type, IReadOnlyList<ConveyorSlot> slots)
        {
            if (!selectingDish) return;
            selectingDish = false;
            HideGuide();
        }

        // EN: Fit the spotlight to the food artwork on the front dish of column two, excluding its plate and shadow.
        // VI: Đặt vùng sáng khít hình món trên đĩa đầu cột hai, không tính đĩa và bóng.
        private IEnumerator ShowGuide()
        {
            yield return new WaitForSeconds(showDelay);
            pendingShow = null;
            IReadOnlyList<Food> dishes = boardManager.GetColumnDishes(1);
            if (dishes.Count == 0) yield break;
            Food dish = dishes[0];
            overlay = Instantiate(overlayPrefab, transform);
            Bounds iconBounds = dish.IconRenderer.bounds;
            float size = Mathf.Max(iconBounds.size.x, iconBounds.size.y);
            Bounds focusBounds = new Bounds(iconBounds.center, new Vector3(size, size, 0f));
            overlay.Show(focusBounds, Camera.main, prompt);
        }

        // EN: Show ingredient-view guidance when the first customer arrives at level two.
        // VI: Hiện hướng dẫn xem nguyên liệu khi khách đầu tiên đến ở màn hai.
        private void OnCustomerArrived(int index, OrderView order)
        {
            if (levelController.CurrentLevel.levelNumber != 2 || index != 0) return;
            targetOrder = order;
            targetOrder.IngredientsOpened += OnIngredientsOpened;
            pendingShow = StartCoroutine(ShowOrderGuide());
        }

        // EN: Center level-two order guidance on the first customer's dish picture.
        // VI: Đặt hướng dẫn order màn hai vào tâm hình món của khách đầu tiên.
        private IEnumerator ShowOrderGuide()
        {
            yield return new WaitForSeconds(showDelay);
            pendingShow = null;
            if (targetOrder == null || targetOrder.ActiveDishCount == 0) yield break;
            overlay = Instantiate(overlayPrefab, transform);
            OrderItemView item = targetOrder.Items[0];
            float size = item.DishSize * Mathf.Abs(item.transform.lossyScale.x);
            Bounds dishBounds = new Bounds(item.TargetPosition, new Vector3(size, size, 0f));
            overlay.Show(dishBounds, Camera.main, orderPrompt);
        }

        // EN: Finish the order hint when its actual ingredient details are shown.
        // VI: Kết thúc hướng dẫn order khi chi tiết nguyên liệu thật sự được mở.
        private void OnIngredientsOpened(OrderView order)
        {
            HideGuide();
            ClearOrderTarget();
        }

        // EN: Release the order event before loading, replaying or destroying its card.
        // VI: Gỡ sự kiện order trước khi nạp, chơi lại hoặc hủy thẻ.
        private void ClearOrderTarget()
        {
            if (targetOrder != null) targetOrder.IngredientsOpened -= OnIngredientsOpened;
            targetOrder = null;
        }

        // EN: Cancel the delayed opening and remove the active overlay without changing gameplay or time scale.
        // VI: Hủy lần hiện trễ và xóa lớp hướng dẫn mà không đổi gameplay hoặc tốc độ thời gian.
        private void HideGuide()
        {
            if (pendingShow != null) StopCoroutine(pendingShow);
            pendingShow = null;
            if (overlay != null)
            {
                overlay.gameObject.SetActive(false);
                Destroy(overlay.gameObject);
            }
            overlay = null;
        }
    }
}
