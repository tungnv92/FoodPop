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
    PlayerData profileData;

    private void Start()
    {
        Init();
    }
    public void Init()
    {
        profileData = ProfileManager.instance.PlayerData;
        ReloadAvatar();
    }

    void ReloadAvatar()
    {
        avatarImage.sprite = avatarConfig.AvatarSprites[profileData.avatar_id];
        frameImage.sprite = frameConfig.FrameSprites[profileData.frame_avatar_id];
        //goldenFrameEffect.enabled = profileData.FrameIndex == frameConfig.GoldenFrameId;
    }

    public void SetGoldenFrame(Action onComplete = null)
    {
        //ProfileManager.Ins.SaveData();
        frameImage.sprite = frameConfig.FrameSprites[profileData.frame_avatar_id];
        transform.DOScale(0.9f, 0.15f).OnComplete(() =>
        {
            transform.DOScale(0.7f, 0.15f).OnComplete(() => onComplete?.Invoke());           
        });
    }

    public void ResetGoldenFrame()
    {
        profileData.frame_avatar_id = 0;
        ProfileManager.instance.SaveData();
        frameImage.sprite = frameConfig.FrameSprites[profileData.frame_avatar_id];
    }

    #region UI EVENTS
    public void OnClick()
    {
        var popup = UIManager.Ins.OpenPopup<PopupProfile>(PopupId.Profile);
        popup.OnClose(ReloadAvatar);
    }
    #endregion
}
