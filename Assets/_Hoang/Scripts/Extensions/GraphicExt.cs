using UnityEngine;
using UnityEngine.UI;

public static class GraphicExt
{
    public static void ChangeAlpha(this Graphic graphic, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
