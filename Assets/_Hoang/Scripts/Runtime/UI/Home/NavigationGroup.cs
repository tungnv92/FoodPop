using DG.Tweening;
using System.Collections;
using UnityEngine;

public class NavigationGroup : MonoBehaviour
{
    [SerializeField] RectTransform _panelRectTf;
    [SerializeField] RectTransform _rectTf;
    [SerializeField] RectTransform _activeImageRectTf;
    [SerializeField] ButtonNavigator[] _buttons;
    private IEnumerator Start()
    {
        _buttons[1].OnClick();
        yield return new WaitForEndOfFrame();
        Debug.Log("Width: " + _panelRectTf.rect.width) ;
        _activeImageRectTf.ChangeSizeDeltaX(_panelRectTf.rect.width / 3f);
    }

    public void OnDragTab(Vector2 value)
    {
        float posX = value.x * (_buttons[2].RectTf.anchoredPosition.x - _buttons[0].RectTf.anchoredPosition.x) + _buttons[0].RectTf.anchoredPosition.x;
        _activeImageRectTf.anchoredPosition = new Vector2(posX, _activeImageRectTf.anchoredPosition.y);
        
        foreach (var button in _buttons)
        {
            button.OnDrag(value);
        }
    }
}
