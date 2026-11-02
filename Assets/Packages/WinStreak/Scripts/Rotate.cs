using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float speed = 50f;

    private void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}
