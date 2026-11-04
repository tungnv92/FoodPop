using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GamePackages;
using UnityEngine;

public class PopupCardCollectionInfo : Popup
{
    [Tooltip("Danh sách các bước hướng dẫn xuất hiện lần lượt theo thứ tự.")]
    public List<GameObject> objs = new();

    [Tooltip("Đối tượng dòng chữ 'Tap to continue' hiện ra ở cuối chuỗi hiệu ứng.")]
    public GameObject continueTmp;

    public Action OnClose = () => { };

    private bool _isShowingAnim;
    private Coroutine _showingRoutine;

    // EN: Opens the info popup, marks info as shown in CardCollectionData, and starts the sequential step animation.
    // VI: Mở popup thông tin, đánh dấu đã xem hướng dẫn trong CardCollectionData và bắt đầu chuỗi hiệu ứng từng bước.
    public override void Open(object args = null)
    {
        base.Open(args);
        if (continueTmp != null) continueTmp.SetActive(false);

        if (CardCollectionManager.Instance != null && CardCollectionManager.Instance.Data != null)
        {
            CardCollectionManager.Instance.Data.isShowedInfo = true;
            CardCollectionManager.Instance.Save();
        }

        AnimShow();
    }

    // EN: Hides all step objects initially and launches the coroutine to scale them in sequentially.
    // VI: Ẩn toàn bộ các bước hướng dẫn lúc đầu và chạy coroutine phóng to từng bước theo thứ tự.
    public void AnimShow()
    {
        if (objs != null)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                if (objs[i] != null) objs[i].SetActive(false);
            }
        }

        if (_showingRoutine != null)
        {
            StopCoroutine(_showingRoutine);
        }
        _showingRoutine = StartCoroutine(IEAnimShow());
    }

    // EN: Sequentially activates and scales in each tutorial step object followed by the continue prompt.
    // VI: Lần lượt kích hoạt và phóng to từng đối tượng hướng dẫn, sau đó hiển thị dòng nhắc tiếp tục.
    private IEnumerator IEAnimShow()
    {
        _isShowingAnim = true;
        if (objs != null)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                GameObject obj = objs[i];
                if (obj != null)
                {
                    obj.SetActive(true);
                    obj.transform.localScale = Vector3.one * 0.3f;
                    obj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }

        if (continueTmp != null)
        {
            continueTmp.SetActive(true);
            continueTmp.transform.localScale = Vector3.one * 0.3f;
            continueTmp.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        _isShowingAnim = false;
    }

    // EN: Completes the reveal animation immediately if still playing, or closes the info popup if already finished.
    // VI: Hoàn tất ngay hiệu ứng xuất hiện nếu đang chạy, hoặc đóng popup thông tin nếu hiệu ứng đã kết thúc.
    public void ButtonContinue()
    {
        if (_isShowingAnim)
        {
            _isShowingAnim = false;
            if (_showingRoutine != null) StopCoroutine(_showingRoutine);

            if (objs != null)
            {
                for (int i = 0; i < objs.Count; i++)
                {
                    if (objs[i] != null)
                    {
                        objs[i].SetActive(true);
                        objs[i].transform.DOKill();
                        objs[i].transform.localScale = Vector3.one;
                    }
                }
            }

            if (continueTmp != null)
            {
                continueTmp.SetActive(true);
                continueTmp.transform.DOKill();
                continueTmp.transform.localScale = Vector3.one;
            }
            return;
        }

        if (objs != null)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                if (objs[i] != null)
                {
                    objs[i].transform.DOScale(0f, 0.1f).SetEase(Ease.InBack);
                }
            }
        }

        if (continueTmp != null) continueTmp.SetActive(false);

        DOVirtual.DelayedCall(0.1f, () =>
        {
            Close();
            OnClose?.Invoke();
        });
    }
}

