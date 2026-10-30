using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class LavaQuestFakeUser : MonoBehaviour
{
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Image frame;
    public Image avatar;
    public RectTransform virtualRectTF;
    public AnimationCurve jumpCurve;
    public float jumpDuration;
    public float jumpHeight;
    public float jumpOffsetY;
    public float fallDuration;
    public float fallHeight;

    private LavaQuest Quest => LavaQuest.Instance != null ? LavaQuest.Instance : (UIManager.Ins != null ? UIManager.Ins.GetPopup<LavaQuest>(PopupId.LavaQuest) : null);

    public void OnInitt(int frameID, int avatarID)
    {
        if (frame != null && ProfileManager.instance != null)
            frame.sprite = ProfileManager.instance.GetFrameSprite(frameID);
        if (avatar != null && ProfileManager.instance != null)
            avatar.sprite = ProfileManager.instance.GetAvatarSprite(avatarID);
        if (canvasGroup != null)
            canvasGroup.alpha = 1;
        if (rectTransform != null)
            rectTransform.localRotation = Quaternion.identity;
    }

    public void RandomFrameAndAvatar()
    {
        if (frame != null && ProfileManager.instance != null)
            frame.sprite = ProfileManager.instance.GetRandomFrameSprite();
        if (avatar != null && ProfileManager.instance != null)
            avatar.sprite = ProfileManager.instance.GetRandomAvatarSprite();
    }

    [Button]
    public void JumpToMileStoneIndex(int mileStoneIndex, bool isRandom)
    {
        if (Quest == null || Quest.lavaQuestMileStones == null || Quest.lavaQuestMileStones.Count == 0)
        {
            Debug.LogError("No mile stones found in LavaQuest.");
            return;
        }
        mileStoneIndex = Mathf.Clamp(mileStoneIndex, 0, Quest.lavaQuestMileStones.Count - 1);
        LavaQuestMileStone lavaQuestMileStone = Quest.lavaQuestMileStones[mileStoneIndex];
        if (lavaQuestMileStone != null)
        {
            Jump(lavaQuestMileStone.targetJumpRectTF, isRandom);
        }
    }

    [Button]
    public void Jump(RectTransform targetJumpRectTF, bool isRandom)
    {
        if (targetJumpRectTF == null) return;
        float randomX = isRandom && Quest != null ? Quest.randomX : 0;
        float randomY = isRandom && Quest != null ? Quest.randomY : 0;
        virtualRectTF.transform.SetParent(this.transform.parent);
        virtualRectTF.position = targetJumpRectTF.position;
        Vector3 center = (virtualRectTF.anchoredPosition + this.rectTransform.anchoredPosition) / 2;
        this.rectTransform.DOAnchorPosX(virtualRectTF.anchoredPosition.x + UnityEngine.Random.Range(-randomX, randomX), jumpDuration).SetEase(Ease.OutSine);
        this.rectTransform.DOAnchorPosY(center.y + jumpHeight + jumpOffsetY, jumpDuration / 2).SetEase(Ease.OutSine).OnComplete(() =>
        {
            this.rectTransform.DOAnchorPosY(virtualRectTF.anchoredPosition.y + jumpOffsetY + UnityEngine.Random.Range(0, randomY), jumpDuration / 2).SetEase(Ease.InSine).OnComplete(() =>
            {
                virtualRectTF.transform.SetParent(this.transform);
                this.rectTransform.DOPunchRotation(new Vector3(0, 0, 5), 0.15f);
            });
        });
    }

    [Button]
    public void SpawnAt(RectTransform targetJumpRectTF, bool isRandom)
    {
        if (targetJumpRectTF == null) return;
        float randomX = isRandom && Quest != null ? Quest.randomX : 0;
        float randomY = isRandom && Quest != null ? Quest.randomY : 0;
        virtualRectTF.transform.SetParent(this.transform.parent);
        virtualRectTF.position = targetJumpRectTF.position;
        this.rectTransform.anchoredPosition = virtualRectTF.anchoredPosition + new Vector2(UnityEngine.Random.Range(-randomX, randomX), UnityEngine.Random.Range(0, randomY) + jumpOffsetY);
        virtualRectTF.transform.SetParent(this.transform);
    }

    [Button]
    public void Fall(bool isRight)
    {
        float targetRotationZ = 0;
        if (isRight)
        {
            virtualRectTF.anchoredPosition = new Vector2(200, -120);
            targetRotationZ = -60;
        }
        else
        {
            virtualRectTF.anchoredPosition = new Vector2(-200, -120);
            targetRotationZ = 60;
        }
        virtualRectTF.transform.SetParent(this.transform.parent);
        Vector3 center = (virtualRectTF.anchoredPosition + this.rectTransform.anchoredPosition) / 2;
        this.rectTransform.DOAnchorPosX(virtualRectTF.anchoredPosition.x, fallDuration).SetEase(Ease.OutSine);
        this.rectTransform.DOAnchorPosY(center.y + fallHeight, fallDuration / 2).SetEase(Ease.OutSine).OnComplete(() => {
            this.rectTransform.DOAnchorPosY(virtualRectTF.anchoredPosition.y, fallDuration / 2).SetEase(Ease.InSine).OnComplete(() =>
            {
                virtualRectTF.transform.SetParent(this.transform);
                this.rectTransform.DOPunchRotation(new Vector3(0, 0, 5), 0.15f);
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, 1.3f).OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
                }
                else
                {
                    gameObject.SetActive(false);
                }
            });
        });
        this.rectTransform.DORotate(new Vector3(0, 0, targetRotationZ), fallDuration).SetEase(Ease.Linear);
    }
}