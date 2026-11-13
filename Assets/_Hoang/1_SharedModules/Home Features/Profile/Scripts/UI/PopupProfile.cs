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
    PlayerDataManager profileData;

    event Action onClose;

    // EN: Opens the view using manager queries and commands instead of mutable save models.
    // VI: Mở giao diện bằng truy vấn và lệnh manager thay vì model lưu có thể sửa.
    public override void Open(object args = null)
    {
        base.Open(args);
        profileData = ProfileManager.instance.PlayerDataManager;
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

    // EN: Coordinates OpenRoutine using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối OpenRoutine qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
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

        nameIf.text = profileData.PlayerName;
        //nameIf_Golden.text = profileData.Name;
        //nameIf.gameObject.SetActive(!MasterPassManager.Ins.Data.Activated);
        //nameIf_Golden.gameObject.SetActive(MasterPassManager.Ins.Data.Activated);
        selectedFrameImage.sprite = frameConfig.FrameSprites[profileData.FrameId];
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

    // EN: Coordinates ReloadAvatar using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ReloadAvatar qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    void ReloadAvatar()
    {
        int index = profileData.AvatarId;
        selectedAvatarImage.sprite = avatarConfig.AvatarSprites[index];

        selectRt.SetParent(avatars[index].MyRt);
        selectRt.localPosition = Vector3.zero;
        //selectRt.anchoredPosition += new Vector2(62, -70);
        //selectRt.localScale = Vector3.zero;
        //selectRt.DOScale(1, 0.25f).SetEase(Ease.OutBack);
    }

    // EN: Coordinates ReloadFrame using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ReloadFrame qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    void ReloadFrame()
    {
        int index = profileData.FrameId;
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

    // EN: Persists the avatar selection through the player manager and refreshes its preview.
    // VI: Lưu ảnh đại diện đã chọn qua manager người chơi và cập nhật xem trước.
    public void OnSelectAvatar(int index)
    {
        profileData.SetAvatar(index);
        ReloadAvatar();
    }

    // EN: Persists the frame selection through the player manager and refreshes its preview.
    // VI: Lưu khung đã chọn qua manager người chơi và cập nhật xem trước.
    public void OnSelectFrame(int index)
    {
        profileData.SetFrame(index);
        ReloadFrame();
    }

    #region UI EVENTS
    public void OnClickClose()
    {
        Close();
    }

    // EN: Restores an empty name or saves the edited name through the player manager.
    // VI: Khôi phục tên rỗng hoặc lưu tên đã sửa qua manager người chơi.
    public void OnEndEditName()
    {
        if (string.IsNullOrEmpty(nameIf.text))
        {
            nameIf.text = profileData.PlayerName;
            return;
        }
        profileData.SetPlayerName(nameIf.text);
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
