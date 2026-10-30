using UnityEngine;

public class TowerRiseBackgroundFollow : MonoBehaviour
{
    public RectTransform rectTransform;
    public RectTransform targetRectTF;
    public float followSpeed;
    public float offsetY;

    private void Update()
    {
        if (rectTransform != null && targetRectTF != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, targetRectTF.anchoredPosition.y + offsetY) * followSpeed;
        }
    }
}

public class TowerRiseBackGroundFollow : TowerRiseBackgroundFollow
{
}

public class HoneyRiseBackgroundFollow : TowerRiseBackgroundFollow
{
}

public class HoneyRiseBackGroundFollow : TowerRiseBackgroundFollow
{
}