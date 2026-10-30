using Longvm.UIAnimator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupNewTrap : Popup
{
    //[SerializeField] UIAnimator _openAnim;
    //[SerializeField] UIAnimator _closeAnim;
    //[SerializeField] ItemUnlockConfig _config;

    //[SerializeField] Image _itemIcon;
    //[SerializeField] TMP_Text _itemNameTmp;
    //[SerializeField] TMP_Text _itemDescTmp;

    //public override void Open(object args = null)
    //{
    //    base.Open(args);
    //    _openAnim.InitAndPlay();
    //    int itemId = int.Parse(args.ToString());
    //    GameItem item = _config.Items[itemId];

    //    _itemIcon.sprite = item.ItemIcon;
    //    _itemIcon.SetNativeSize();
    //    _itemNameTmp.text = item.ItemName;
    //    _itemDescTmp.text = item.ItemDescription;

    //    AudioManager.Ins.PlaySound(SoundType.UnlockItem);
    //}

    //public override void Close()
    //{
    //    _closeAnim.Play(base.Close);
    //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
    //}

    //public void ClickContinue()
    //{
    //    Close();
    //}
}
