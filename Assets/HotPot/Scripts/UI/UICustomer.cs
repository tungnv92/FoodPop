using TMPro;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class UICustomer : MonoBehaviour
    {
        [Tooltip("Chữ hiển thị số khách còn lại của màn chơi.")]
        [SerializeField] private TMP_Text amountLabel;
        [Tooltip("Manager khách hàng của màn chơi.")]
        [SerializeField] private CustomerManager customerManager;

        // EN: Subscribes to customer progress and reads the current remaining count.
        // VI: Đăng ký tiến độ khách hàng và đọc số khách còn lại hiện tại.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            customerManager.RemainingCustomersChanged += ShowAmount;
            ShowAmount(customerManager.RemainingCustomers);
        }

        // EN: Stops listening when this display is hidden.
        // VI: Ngừng nhận sự kiện khi phần hiển thị bị ẩn.
        private void OnDisable()
        {
            if (Application.isPlaying) customerManager.RemainingCustomersChanged -= ShowAmount;
        }

        // EN: Shows customers still waiting, being served or departing.
        // VI: Hiện số khách còn chờ, đang phục vụ hoặc chưa rời quầy xong.
        private void ShowAmount(int remaining)
        {
            amountLabel.text = remaining.ToString();
        }
    }
}
