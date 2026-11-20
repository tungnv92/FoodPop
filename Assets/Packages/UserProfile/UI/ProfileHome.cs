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
    UserProfileFeature profileData;

    private void Start()
    {
        Init();
    }
    // EN: Reads the profile from UserProfileFeature and shows the current avatar and frame.
    // VI: Đọc hồ sơ từ UserProfileFeature và hiển thị avatar, khung hiện tại.
    public void Init()
    {
        profileData = UserProfileFeature.Instance;
        ReloadAvatar();
    }

    // EN: Shows the selected frame with a short pop animation, then calls onComplete.
    // VI: Hiển thị khung đã chọn kèm hiệu ứng nảy ngắn, rồi gọi onComplete.
    public void SetGoldenFrame(Action onComplete = null)
    {
        //ProfileManager.Ins.SaveData();
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
        transform.DOScale(0.9f, 0.15f).OnComplete(() =>
        {
            transform.DOScale(0.7f, 0.15f).OnComplete(() => onComplete?.Invoke());
        });
    }

    // EN: Resets the frame to index 0 through UserProfileFeature and shows it.
    // VI: Đặt lại khung về chỉ số 0 qua UserProfileFeature và hiển thị.
    public void ResetGoldenFrame()
    {
        profileData.SetFrame(0);
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
    }

    // EN: Shows the selected avatar and frame sprites.
    // VI: Hiển thị sprite avatar và khung đã chọn.
    void ReloadAvatar()
    {
        avatarImage.sprite = avatarConfig.AvatarSprites[profileData.AvatarId];
        frameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
        //goldenFrameEffect.enabled = profileData.FrameIndex == frameConfig.GoldenFrameId;
    }

    #region UI EVENTS
    public void OnClick()
    {
        var popup = UIManager.Ins.OpenPopup<PopupProfile>(PopupId.Profile);
        popup.OnClose(ReloadAvatar);
    }
    #endregion
}
