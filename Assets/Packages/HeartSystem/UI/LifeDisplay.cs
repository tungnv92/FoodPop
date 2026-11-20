using GamePackages;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeDisplay : MonoBehaviour
{
    HeartFeature Heart => HeartFeature.Instance;
    [SerializeField] TextMeshProUGUI txtHeartValue, txtHeartTime;
    [SerializeField] GameObject objNormalHeart, objUnlimitedHeart;
    [SerializeField] GameObject btnAddHeart;
    bool isUnlimitedLives;
    bool isMaxHeart;
    // EN: Shows the heart count or unlimited state, the refill timer, and the add button when hearts can be bought.
    // VI: Hiển thị số mạng hoặc trạng thái vô hạn, thời gian hồi, và nút mua mạng khi còn mua được.
    private void Update()
    {
        isUnlimitedLives = Heart.IsUnlimitedLives();
        isMaxHeart = Heart.IsMaxHeart();
        objNormalHeart.gameObject.SetActive(!isUnlimitedLives);
        objUnlimitedHeart.gameObject.SetActive(isUnlimitedLives);
        if (isUnlimitedLives || isMaxHeart)
        {
            btnAddHeart.gameObject.SetActive(false);
        } else
        {
            btnAddHeart.gameObject.SetActive(true);
        }
        var heart = Heart.GetHeartDisplay();
        txtHeartValue.text = heart.Item1;
        txtHeartTime.text = heart.Item2;
    }
    // EN: Opens the buy-lives popup unless lives are unlimited or full.
    // VI: Mở popup mua mạng trừ khi đang vô hạn hoặc đã đầy.
    public void OpenBuy()
    {
        isUnlimitedLives = Heart.IsUnlimitedLives();
        isMaxHeart = Heart.IsMaxHeart();
        if (isUnlimitedLives || isMaxHeart) return;
        UIManager.Ins.OpenPopup(PopupId.MoreLife);
    }

}
