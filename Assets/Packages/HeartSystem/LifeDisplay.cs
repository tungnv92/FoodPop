using GamePackages;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI txtHeartValue, txtHeartTime;
    [SerializeField] GameObject objNormalHeart, objUnlimitedHeart;
    [SerializeField] GameObject btnAddHeart;
    HeartManager HeartManager => ProfileManager.instance.GameFeatureContext.HeartManager;
    bool isUnlimitedLives;
    bool isMaxHeart;
    private void Update()
    {
        isUnlimitedLives = HeartManager.IsUnlimitedLives();
        isMaxHeart = HeartManager.IsMaxHeart();
        objNormalHeart.gameObject.SetActive(!isUnlimitedLives);
        objUnlimitedHeart.gameObject.SetActive(isUnlimitedLives);
        if (isUnlimitedLives || isMaxHeart)
        {
            btnAddHeart.gameObject.SetActive(false);
        } else
        {
            btnAddHeart.gameObject.SetActive(true);
        }
        var heart = HeartManager.GetHeartDisplay();
        txtHeartValue.text = heart.Item1;
        txtHeartTime.text = heart.Item2;
    }
    public void OpenBuy()
    {
        isUnlimitedLives = HeartManager.IsUnlimitedLives();
        isMaxHeart = HeartManager.IsMaxHeart();
        if (isUnlimitedLives || isMaxHeart) return;
        UIManager.Ins.OpenPopup(PopupId.MoreLife);
    }
  
}
