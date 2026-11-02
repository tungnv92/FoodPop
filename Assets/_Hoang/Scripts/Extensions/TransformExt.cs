using UnityEngine;

public static class TransformExt
{
    #region POSITION
    public static void ChangePositionX(this Transform transform, float x)
    {
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public static void ChangePositionY(this Transform transform, float y)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public static void ChangePositionZ(this Transform transform, float z)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, z);
    }
    #endregion

    #region LOCAL POSITION
    public static void ChangeLocalPositionX(this Transform transform, float x)
    {
        transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
    }

    public static void ChangeLocalPositionY(this Transform transform, float y)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
    }

    public static void ChangeLocalPositionZ(this Transform transform, float z)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
    }
    #endregion

    #region ROTATION
    public static void ChangeRotationX(this Transform transform, float x)
    {
        transform.eulerAngles = new Vector3(x, transform.eulerAngles.y, transform.eulerAngles.z);
    }

    public static void ChangeRotationY(this Transform transform, float y)
    {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, y, transform.eulerAngles.z);
    }

    public static void ChangeRotationZ(this Transform transform, float z)
    {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, z);
    }

    public static void ChangeLocalRotationX(this Transform transform, float x)
    {
        transform.localEulerAngles = new Vector3(x, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }

    public static void ChangeLocalRotationY(this Transform transform, float y)
    {
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, y, transform.localEulerAngles.z);
    }

    public static void ChangeLocalRotationZ(this Transform transform, float z)
    {
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, z);
    }
    #endregion

    #region SCALE
    public static void ChangeScaleX(this Transform transform, float x)
    {
        transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
    }

    public static void ChangeScaleY(this Transform transform, float y)
    {
        transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
    }

    public static void ChangeScaleZ(this Transform transform, float z)
    {
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
    }

    public static void ChangeGlobalScale(this Transform transform, Vector3 scale)
    {
        Vector3 lossyScale = transform.lossyScale;
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(scale.x / lossyScale.x, scale.y / lossyScale.y, scale.z / lossyScale.z);
    }
    #endregion

    public static void DestroyChildren(this Transform parent)
    {
        if (Application.isPlaying)
        {
            foreach (Transform child in parent)
            {
                Object.Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in parent)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}

public static class RectTransformExt
{
    #region ANCHOR POS
    public static void ChangeAnchorPosX(this RectTransform rt, float x)
    {
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }

    public static void ChangeAnchorPosY(this RectTransform rt, float y)
    {
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }
    #endregion

    #region ANCHOR MIN
    public static void ChangeAnchorMinX(this RectTransform rt, float x)
    {
        rt.anchorMin = new Vector2(x, rt.anchorMin.y);
    }

    public static void ChangeAnchorMinY(this RectTransform rt, float y)
    {
        rt.anchorMin = new Vector2(rt.anchorMin.x, y);
    }
    #endregion

    #region ANCHOR MAX
    public static void ChangeAnchorMaxX(this RectTransform rt, float x)
    {
        rt.anchorMax = new Vector2(x, rt.anchorMax.y);
    }

    public static void ChangeAnchorMaxY(this RectTransform rt, float y)
    {
        rt.anchorMax = new Vector2(rt.anchorMax.x, y);
    }
    #endregion

    #region OFFSET
    public static void ChangeLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void ChangeRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void ChangeBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    public static void ChangeTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }
    #endregion

    #region SIZE DELTA
    public static void ChangeSizeDeltaX(this RectTransform rt, float x)
    {
        rt.sizeDelta = new Vector2(x, rt.sizeDelta.y);
    }

    public static void ChangeSizeDeltaY(this RectTransform rt, float y)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, y);
    }
    #endregion
}
