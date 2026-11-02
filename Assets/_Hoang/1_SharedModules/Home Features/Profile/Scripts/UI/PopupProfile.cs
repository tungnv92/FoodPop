using DG.Tweening;
using GamePackages;
using HomeFeatures.Profile;
using Longvm.UIAnimator;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupProfile : Popup
{
    [SerializeField] UIAnimator openAnimator;
    [SerializeField] UIAnimator closeAnimator;
    [SerializeField] Image selectedAvatarImage;
    [SerializeField] Image selectedFrameImage;
    [SerializeField] RectTransform selectRt;
    [SerializeField] GridLayoutGroup gridLayoutGroup;
    [SerializeField] ContentSizeFitter contentSizeFitter;
    [Header("Name")]
    [SerializeField] TMP_InputField nameIf;
    //[SerializeField] TMP_InputField nameIf_Golden;
    [Header("Buttons")]
    [SerializeField] Avatar_Frame_Button avatarButton;
    [SerializeField] Avatar_Frame_Button frameButton;
    [Header("Avatar")]
    [SerializeField] GameObject avatarGrid;
    [SerializeField, ExposedScriptableObject] AvatarConfig avatarConfig;
    [SerializeField] Avatar[] avatars;
    [Header("Frame")]
    [SerializeField] GameObject frameGrid;
    [SerializeField, ExposedScriptableObject] FrameConfig frameConfig;
    [SerializeField] Frame[] frames;

    bool isInitialized;
    PlayerData profileData;

    event Action onClose;

    public override void Open(object args = null)
    {
        base.Open(args);
        profileData = ProfileManager.instance.PlayerData;
        StartCoroutine(OpenRoutine());
    }

    public override void Close()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        closeAnimator.Play(() =>
        {
            onClose?.Invoke();
            onClose = null;
            base.Close();
        });
    }

    public void OnClose(Action onClose) => this.onClose = onClose;

    IEnumerator OpenRoutine()
    {
        openAnimator.Init();
        //AudioManager.Ins.PlaySound(SoundType.PopupShow);

        yield return null;
        if (!isInitialized)
        {
            isInitialized = true;

            for (int i = 0; i < avatars.Length; i++)
            {
                avatars[i].Init(i, avatarConfig.AvatarSprites[i], this);
            }

            for (int i = 0; i < frames.Length; i++)
            {
                frames[i].Init(i, frameConfig.FrameSprites[i], this);
            }
            yield return null;
        }

        nameIf.text = profileData.playerName;
        //nameIf_Golden.text = profileData.Name;
        //nameIf.gameObject.SetActive(!MasterPassManager.Ins.Data.Activated);
        //nameIf_Golden.gameObject.SetActive(MasterPassManager.Ins.Data.Activated);
        selectedFrameImage.sprite = frameConfig.FrameSprites[profileData.frame_avatar_id];
        OpenAvatar();
        avatarButton.Select();
        frameButton.UnSelect();

        yield return null;
        openAnimator.Play(() =>
        {
            
        });
        yield return null;
        gridLayoutGroup.enabled = false;
        contentSizeFitter.enabled = false;
    }

    //public void ChangeFontKey(string key)
    //{
    //    var trackedObject = nameTmpLocalizer.TrackedObjects[0] as LocalizedAssetTableTrackedObject;
    //    trackedObject
    //}

    public void OpenAvatar()
    {
        avatarGrid.SetActive(true);
        frameGrid.SetActive(false);
        ReloadAvatar();
    }

    public void OpenFrame()
    {
        avatarGrid.SetActive(false);
        frameGrid.SetActive(true);
        ReloadFrame();
    }

    void ReloadAvatar()
    {
        int index = profileData.avatar_id;
        selectedAvatarImage.sprite = avatarConfig.AvatarSprites[index];

        selectRt.SetParent(avatars[index].MyRt);
        selectRt.localPosition = Vector3.zero;
        //selectRt.anchoredPosition += new Vector2(62, -70);
        //selectRt.localScale = Vector3.zero;
        //selectRt.DOScale(1, 0.25f).SetEase(Ease.OutBack);
    }

    void ReloadFrame()
    {
        int index = profileData.frame_avatar_id;
        selectedFrameImage.sprite = frameConfig.FrameSprites[index];

        //selectRt.localScale = Vector3.zero;
        //selectRt.anchoredPosition += new Vector2(62, -70);
        //selectRt.DOScale(1, 0.25f).SetEase(Ease.OutBack);

        for (int i = 0; i < frames.Length; i++)
        {
            frames[i].Reload();
        }

        selectRt.SetParent(frames[index].MyRt);
        selectRt.localPosition = Vector3.zero;
        //goldenFrameEffect.enabled = profileData.FrameIndex == frameConfig.GoldenFrameId;
    }

    public void OnSelectAvatar(int index)
    {
        profileData.avatar_id = index;
        ProfileManager.instance.SaveData();
        ReloadAvatar();
    }

    public void OnSelectFrame(int index)
    {
        profileData.frame_avatar_id = index;
        ProfileManager.instance.SaveData();
        ReloadFrame();
    }

    #region UI EVENTS
    public void OnClickClose()
    {
        Close();
    }

    public void OnEndEditName()
    {
        if (string.IsNullOrEmpty(nameIf.text))
        {
            nameIf.text = profileData.playerName;
            return;
        }
        profileData.playerName = nameIf.text;
        ProfileManager.instance.SaveData();
    }

    public void OnEndEditNameGold()
    {
        //if (string.IsNullOrEmpty(nameIf_Golden.text))
        //{
        //    nameIf_Golden.text = profileData.Name;
        //    return;
        //}
        //profileData.Name = nameIf_Golden.text;
        //ProfileManager.Ins.SaveData();
    }
    #endregion
}
