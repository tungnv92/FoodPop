using GamePackages;
using SharedModules.ED;
using UnityEngine;

public class CoinDisplay : MonoBehaviour
{
    [SerializeField] bool initOnAwake = true;
    [SerializeField] CurrencyDisplay display;
    public CurrencyDisplay Display => display;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;

    private void Awake()
    {
        if (initOnAwake)
        {
            Init();
        }
       PlayerDataManager.RegisterResourceChange(ResourceType.Coin, OnCoinAmountChanged);
    }

    private void OnDestroy()
    {
        PlayerDataManager.RemoveResourceChange(ResourceType.Coin, OnCoinAmountChanged);
    }

    public void Init()
    {
        Debug.Log("Init Coin");
        display.Amount.InitAmount(PlayerDataManager.GetResourceValue(ResourceType.Coin));
    }

    public void OnCoinAmountChanged(int value)
    {
        display.Amount.TweenAmount(value);
    }

    public void OnClickMoreCoin()
    {
        if (LoadSceneManager.Ins.CurrentSceneId == SceneId.Home && !UIManager.Ins.IsAnyPopupOpen)
        {
            UIManager.Ins.GetPanel<HomePanel>().SnapToPage(0);
            UIManager.Ins.GetPanel<HomePanel>().Shop.ScrollToCoins();
        } else
        {
            //PopupShop shop = (PopupShop)UIManager.Ins.OpenPopup(PopupId.Shop);
            //shop.ScrollToCoins();
        }
    }
}
