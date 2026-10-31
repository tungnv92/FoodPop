using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    // EN: Owns the pre-defeat revive offer and its separate accept and decline actions.
    // VI: Quản lý lời mời hồi sinh trước thất bại cùng hai hành động đồng ý và từ chối riêng.
    [DisallowMultipleComponent]
    public sealed class CanvasRevive : LevelPopupView
    {
        // EN: Opens the revive choice while keeping refusal available even if collection cannot start.
        // VI: Mở lựa chọn hồi sinh, vẫn cho từ chối kể cả khi chưa thể bắt đầu thu món.
        public void Show(UnityAction revive, UnityAction decline, bool available = true, bool animate = true)
        {
            Open(revive, decline, available, animate);
        }
    }
}
