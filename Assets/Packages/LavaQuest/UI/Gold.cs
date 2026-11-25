using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gold : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI txt;

    // EN: Opens the shop directly at the Coin packs.
    // VI: Mở cửa hàng thẳng vào phần gói Coin.
    public void ButtonAdd()
    {
        FoodPop.PopupShop.ShowCoins();
    }
}
