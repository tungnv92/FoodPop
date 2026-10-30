using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    // EN: Owns the confirmed defeat popup and its replay action.
    // VI: Quản lý popup thất bại đã xác nhận và hành động chơi lại.
    [DisallowMultipleComponent]
    public sealed class CanvasLose : LevelPopupView
    {
        // EN: Opens the defeat popup with a replay action for the current level.
        // VI: Mở popup thua với hành động chơi lại màn hiện tại.
        public void Show(UnityAction replay, bool animate = true)
        {
            Open(replay, null, true, animate);
        }
    }
}
