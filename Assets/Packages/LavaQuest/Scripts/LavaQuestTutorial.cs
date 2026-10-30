using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class LavaQuestTutorial : SingletonMonoBehaviour<LavaQuestTutorial>
{
    public Transform canvas;
    public CanvasGroup blackCanvasGroup;
    public float arrowSpacing = 100f;

    [Header("BUTTON LAVA QUEST:")]
    public RectTransform cloneButtonLavaQuestRectTF;
    public RectTransform arrowButtonLavaQuestRectTF;

    private void Start()
    {
        ShowBlack(false, false);
    }

    [Button]
    public void StartTut()
    {
        StartClickButtonLavaQuest();
    }

    [Button]
    public void StartClickButtonLavaQuest()
    {
        LavaQuestButtonHome homeBtn = FindFirstObjectByType<LavaQuestButtonHome>();
        if (homeBtn == null) return;

        UIManager.Ins.EnableBlocker(true);
        ShowBlack(true, true);

        cloneButtonLavaQuestRectTF = Instantiate(homeBtn.transform as RectTransform);
        if (canvas != null)
        {
            cloneButtonLavaQuestRectTF.transform.SetParent(canvas);
        }
        cloneButtonLavaQuestRectTF.transform.localScale = Vector3.one;
        cloneButtonLavaQuestRectTF.position = homeBtn.transform.position;

        Button btn = cloneButtonLavaQuestRectTF.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                EndClickButtonLavaQuest();
                homeBtn.OnClick();
            });
        }

        DOVirtual.DelayedCall(Time.deltaTime, () =>
        {
            UIManager.Ins.EnableBlocker(false);
            if (arrowButtonLavaQuestRectTF != null)
            {
                arrowButtonLavaQuestRectTF.gameObject.SetActive(true);
                arrowButtonLavaQuestRectTF.position = cloneButtonLavaQuestRectTF.position + new Vector3(arrowSpacing * 0.7f, 0, 0);
            }
        });
    }

    [Button]
    public void EndClickButtonLavaQuest()
    {
        ShowBlack(false, false);
        if (cloneButtonLavaQuestRectTF != null) cloneButtonLavaQuestRectTF.gameObject.SetActive(false);
        if (arrowButtonLavaQuestRectTF != null) arrowButtonLavaQuestRectTF.gameObject.SetActive(false);
    }

    public void Done()
    {
        if (LavaQuestManager.Instance != null && LavaQuestManager.Instance.Data != null)
        {
            LavaQuestManager.Instance.Data.isDoneTutorial = true;
            LavaQuestManager.Instance.Save();
        }
    }

    public void ShowBlack(bool isShow, bool isAnim)
    {
        if (blackCanvasGroup == null) return;
        float duration = 0.1f;
        if (isShow)
        {
            blackCanvasGroup.gameObject.SetActive(true);
            if (isAnim)
            {
                blackCanvasGroup.alpha = 0;
                blackCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutSine);
            }
            else
            {
                blackCanvasGroup.alpha = 1;
            }
        }
        else
        {
            if (isAnim)
            {
                blackCanvasGroup.alpha = 1;
                blackCanvasGroup.DOFade(0f, duration).SetEase(Ease.OutSine).OnComplete(() =>
                {
                    blackCanvasGroup.gameObject.SetActive(false);
                });
            }
            else
            {
                blackCanvasGroup.alpha = 0;
                blackCanvasGroup.gameObject.SetActive(false);
            }
        }
    }
}