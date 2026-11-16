using System.Collections;
using UnityEngine;

namespace FoodPop
{
    public abstract class FinalFoodAnimation : MonoBehaviour
    {
        // EN: Restore transient effects whenever the dish is pooled, replayed or destroyed.
        // VI: Khôi phục hiệu ứng tạm khi món được trả pool, chơi lại hoặc hủy.
        protected virtual void OnDisable()
        {
            ResetAnimation();
        }

        // EN: Play this dish's preparation sequence; the caller waits before serving it.
        // VI: Chạy hoạt cảnh chế biến riêng của món; bên gọi chờ xong trước khi phục vụ.
        public abstract IEnumerator Play();

        // EN: Restore authored poses and hide temporary preparation props.
        // VI: Khôi phục tư thế thiết kế và ẩn đạo cụ chế biến tạm.
        public abstract void ResetAnimation();

        // EN: Keep preparation props on the same sorting layer and above the dish artwork.
        // VI: Giữ đạo cụ chế biến cùng sorting layer và phía trên hình món.
        public abstract void SetSorting(int sortingLayer, int baseOrder);
    }
}
