using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class TowerRiseTutorial : SingletonMonoBehaviour<TowerRiseTutorial>
{
    public Transform canvas;
    public CanvasGroup blackCanvasGroup;
    public float arrowSpacing = 100f;
    [Header("BUTTON TOWER RISE:")]
    public RectTransform cloneButtonTowerRiseRectTF;
    public RectTransform arrowButtonTowerRiseRectTF;

    private void Start()
    {
        ShowBlack(false, false);
    }

    [Button]
    public void ClearDataAndStartTut()
    {
        TowerRiseManager.Ins?.ClearAllData();
        StartTut();
    }

    [Button]
    public void StartTut()
    {
        if (!ShouldShowTutorial()) return;
        StartClickButtonTowerRise();
    }

    // EN: Coordinates ShouldShowTutorial using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ShouldShowTutorial qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool ShouldShowTutorial()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.IsInitialized) return false;

        return manager.isActive
            && manager.IsEventScheduleActive()
            && !manager.HasCompletedTutorial
            && !manager.HasStarted;
    }

    [Button]
    public void StartClickButtonTowerRise()
    {
        TowerRiseButtonHome homeBtn = FindFirstObjectByType<TowerRiseButtonHome>();
        if (homeBtn == null) return;

        UIManager.Ins.EnableBlocker(true);
        ShowBlack(true, true);

        cloneButtonTowerRiseRectTF = Instantiate(homeBtn.transform as RectTransform);
        if (canvas != null)
        {
            cloneButtonTowerRiseRectTF.transform.SetParent(canvas);
        }
        cloneButtonTowerRiseRectTF.transform.localScale = Vector3.one;
        cloneButtonTowerRiseRectTF.position = homeBtn.transform.position;

        Button btn = cloneButtonTowerRiseRectTF.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                EndClickButtonTowerRise();
                homeBtn.OnClick();
            });
        }

        DOVirtual.DelayedCall(Time.deltaTime, () =>
        {
            UIManager.Ins.EnableBlocker(false);
            if (arrowButtonTowerRiseRectTF != null && cloneButtonTowerRiseRectTF != null)
            {
                arrowButtonTowerRiseRectTF.gameObject.SetActive(true);
                arrowButtonTowerRiseRectTF.position = cloneButtonTowerRiseRectTF.position + new Vector3(arrowSpacing * 0.7f, 0, 0);
            }
        });
    }

    [Button]
    public void EndClickButtonTowerRise()
    {
        ShowBlack(false, false);
        if (cloneButtonTowerRiseRectTF != null)
            cloneButtonTowerRiseRectTF.gameObject.SetActive(false);
        if (arrowButtonTowerRiseRectTF != null)
            arrowButtonTowerRiseRectTF.gameObject.SetActive(false);
    }

    // EN: Coordinates Done using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối Done qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void Done()
    {
        var manager = TowerRiseManager.Ins;
        if (manager != null && manager.IsInitialized)
        {
            manager.CompleteTutorial();
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

public class HoneyRiseTutorial : TowerRiseTutorial
{
}
