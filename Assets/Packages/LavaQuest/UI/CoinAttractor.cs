using UnityEngine;

public class CoinAttractor : MonoBehaviour
{
    // EN: Retains the serialized particle-finish callback without depending on the removed legacy vibration manager.
    // VI: Giữ callback hạt kết thúc đã gán trong prefab mà không phụ thuộc manager rung cũ đã xóa.
    public void OnParticleFinish()
    {
    }
}
