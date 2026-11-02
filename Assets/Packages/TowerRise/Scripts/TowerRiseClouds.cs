using System.Collections.Generic;
using UnityEngine;

public class TowerRiseClouds : MonoBehaviour
{
    public List<RectTransform> cloudRectTFs = new();
    public float minX;
    public float maxX;
    public float speed;

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var cloudRectTF in cloudRectTFs)
        {
            if (cloudRectTF == null) continue;
            cloudRectTF.anchoredPosition += (Vector2)Vector3.right * (speed * dt);
            if (cloudRectTF.anchoredPosition.x > maxX || cloudRectTF.anchoredPosition.x < minX)
            {
                cloudRectTF.anchoredPosition = new Vector2(minX, cloudRectTF.anchoredPosition.y);
            }
        }
    }
}

public class HoneyRiseClouds : TowerRiseClouds
{
}