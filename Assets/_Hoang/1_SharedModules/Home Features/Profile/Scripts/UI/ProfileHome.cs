using System;
using DG.Tweening;
using GamePackages;
using HomeFeatures.Profile;
using UnityEngine;
using UnityEngine.UI;

public class ProfileHome : SingletonMonoBehaviour<ProfileHome>
{
    [SerializeField] Image avatarImage;
    [SerializeField] Image frameImage;
    [SerializeField, ExposedScriptableObject] AvatarConfig avatarConfig;
    [SerializeField, ExposedScriptableObject] FrameConfig frameConfig;
    PlayerDataManager profileData;

    private void Start()
    {
        Init();
    }
    // EN: Coordinates Init using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối Init qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void Init()
    {
        profileData = ProfileManager.instance.PlayerDataManager;
        ReloadAvatar();
    }

    // EN: Coordinates ReloadAvatar using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ReloadAvatar qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    void ReloadAvatar()
    {
        avatarImage.sprite = avatarConfig.AvatarSprites[profileData.AvatarId];
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
        //goldenFrameEffect.enabled = profileData.FrameIndex == frameConfig.GoldenFrameId;
    }

    // EN: Coordinates SetGoldenFrame using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối SetGoldenFrame qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void SetGoldenFrame(Action onComplete = null)
    {
        //ProfileManager.Ins.SaveData();
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
        transform.DOScale(0.9f, 0.15f).OnComplete(() =>
        {
            transform.DOScale(0.7f, 0.15f).OnComplete(() => onComplete?.Invoke());           
        });
    }

    // EN: Coordinates ResetGoldenFrame using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ResetGoldenFrame qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ResetGoldenFrame()
    {
        profileData.SetFrame(0);
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
    }

    #region UI EVENTS
    public void OnClick()
    {
        var popup = UIManager.Ins.OpenPopup<PopupProfile>(PopupId.Profile);
        popup.OnClose(ReloadAvatar);
    }
    #endregion
}
