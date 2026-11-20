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
    UserProfileFeature profileData;

    event Action onClose;

    // EN: Opens the view and reads the profile through UserProfileFeature.
    // VI: Mở giao diện và đọc hồ sơ qua UserProfileFeature.
    public override void Open(object args = null)
    {
        base.Open(args);
        profileData = UserProfileFeature.Instance;
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

    // EN: Saves the avatar selection through UserProfileFeature and refreshes its preview.
    // VI: Lưu ảnh đại diện đã chọn qua UserProfileFeature và cập nhật xem trước.
    public void OnSelectAvatar(int index)
    {
        profileData.SetAvatar(index);
        ReloadAvatar();
    }

    // EN: Saves the frame selection through UserProfileFeature and refreshes its preview.
    // VI: Lưu khung đã chọn qua UserProfileFeature và cập nhật xem trước.
    public void OnSelectFrame(int index)
    {
        profileData.SetFrame(index);
        ReloadFrame();
    }

    // EN: Builds the avatar/frame grids once, then shows the current name, frame and avatar and plays the open animation.
    // VI: Tạo lưới avatar/khung một lần, rồi hiển thị tên, khung, avatar hiện tại và chạy hiệu ứng mở.
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

        nameIf.text = profileData.Name;
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

    // EN: Shows the selected avatar and moves the selection marker onto it.
    // VI: Hiển thị avatar đã chọn và đặt dấu chọn lên nó.
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

    // EN: Shows the selected frame, refreshes every frame item and moves the selection marker onto it.
    // VI: Hiển thị khung đã chọn, làm mới mọi ô khung và đặt dấu chọn lên nó.
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

    #region UI EVENTS
    public void OnClickClose()
    {
        Close();
    }

    // EN: Restores an empty name or saves the edited name through UserProfileFeature.
    // VI: Khôi phục tên rỗng hoặc lưu tên đã sửa qua UserProfileFeature.
    public void OnEndEditName()
    {
        if (string.IsNullOrEmpty(nameIf.text))
        {
            nameIf.text = profileData.Name;
            return;
        }
        profileData.SetName(nameIf.text);
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
