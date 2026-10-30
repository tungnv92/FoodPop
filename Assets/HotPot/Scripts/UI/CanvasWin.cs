using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    // EN: Owns the victory popup and its Next action while sharing common popup presentation.
    // VI: Quản lý popup chiến thắng và hành động Next, dùng chung phần hiển thị cơ bản của popup.
    [DisallowMultipleComponent]
    public sealed class CanvasWin : LevelPopupView
    {
        // EN: Opens victory with Next enabled only when another level exists.
        // VI: Mở chiến thắng và chỉ cho dùng Next khi còn màn kế tiếp.
        public void Show(UnityAction next, bool hasNextLevel = true, bool animate = true)
        {
            Open(next, null, hasNextLevel, animate);
        }
    }
}
