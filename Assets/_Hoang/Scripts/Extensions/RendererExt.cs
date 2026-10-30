using DG.Tweening;
using UnityEngine;

public static class RendererExt
{
    public static void ChangeAlpha(this SpriteRenderer sr, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}
