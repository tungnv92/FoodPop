using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    [CreateAssetMenu(fileName = "UIRef", menuName = "HotPot/UI/UI Ref")]
    public sealed class UIRef : ScriptableObject
    {
        [Tooltip("Danh sách prefab canvas, mỗi class chỉ đăng ký một prefab.")]
        public List<UICanvasPrime> uiCanvasRefList = new List<UICanvasPrime>();
    }
}
