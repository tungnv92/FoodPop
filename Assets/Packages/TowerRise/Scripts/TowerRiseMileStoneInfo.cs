using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace GamePackages
{
    [Serializable]
    public class TowerRiseMileStoneInfo
    {
        public int index;
        public int winAmount;

        [FormerlySerializedAs("rewardInfos")]
        public List<ResourceGame> rewards = new();

        public List<ResourceGame> rewardInfos
        {
            get => rewards;
            set => rewards = value;
        }

        public TowerRiseMileStoneInfo() { }

        public TowerRiseMileStoneInfo(int index, int winAmount, List<ResourceGame> rewards)
        {
            this.index = index;
            this.winAmount = winAmount;
            this.rewards = rewards ?? new List<ResourceGame>();
        }
    }
}
