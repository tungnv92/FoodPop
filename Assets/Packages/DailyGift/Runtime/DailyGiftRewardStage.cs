using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [CreateAssetMenu(fileName = "DailyGiftRewardStage", menuName = "Scriptable Objects/DailyGiftRewardConfig/DailyGiftRewardStage")]
    [System.Serializable]
    public class DailyGiftRewardStage : ScriptableObject
    {
        public int dayID;
        public List<ResourceGame> gifts;
        [PreviewField(60), HideLabel]
        public Sprite giftIcon;
    }
}