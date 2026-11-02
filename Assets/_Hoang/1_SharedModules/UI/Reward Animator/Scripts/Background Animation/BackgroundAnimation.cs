using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Longvm.RewardAnimator
{
    public abstract class BackgroundAnimation : MonoBehaviour
    {
        [Button(ButtonStyle.FoldoutButton)]
        public abstract void Init();

        [Button(ButtonStyle.FoldoutButton)]
        public abstract void Open();

        [Button(ButtonStyle.FoldoutButton)]
        public abstract void Close();
    } 
}
