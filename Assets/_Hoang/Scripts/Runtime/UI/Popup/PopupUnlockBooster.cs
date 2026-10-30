using FoodPop;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUnlockBooster : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;

    [SerializeField] TMP_Text titleTmp;
    [SerializeField] Image boosterIcon;
    [SerializeField] TMP_Text descTmp;

    BoosterType _boostertype;
    //BoosterConfig _boosterConfig;
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        //_boosterConfig = BoosterController.Instance.BoosterConfig;
        //_boostertype = (BoosterType)(int.Parse(args.ToString()));
        //titleTmp.text = _boosterConfig.Boosters[(int)_boostertype].Name.ToString();
        //boosterIcon.sprite = _boosterConfig.Boosters[(int)_boostertype].Icon;
        //boosterIcon.SetNativeSize();
        //descTmp.text = _boosterConfig.Boosters[(int)_boostertype].Description.ToString();
        //BoosterController.Instance.AddBooster(_boostertype, GameManager.Ins.Config.BoosterInitAmount, "popup_unlock");
    }

    public override void Close()
    {
        base.Close();
    }

    public void ClickClaimButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        _closeAnim.Play(() =>
        {
            Close();
            //RewardAnimator rewardAnimator = UIManager.Ins.GetPanel<GamePanel>().RewardAnimator;
            //rewardAnimator.Init();
            //rewardAnimator.Add((int)_boostertype + 2, GameManager.Ins.Config.BoosterInitAmount, () =>
            //{
            //    IngameBoosterUI.Ins.ShowTutBooster(_boostertype);
            //});

            //rewardAnimator.Play();
        });
    }
}
