using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace GamePackages
{
    [CreateAssetMenu(fileName = "TowerRiseConfig", menuName = "Config/TowerRiseConfig")]
    public class TowerRiseConfig : ScriptableObject
    {
        [Title("UNLOCK & STATUS")]
        public bool isFeatureEnabled = true;
        public int LevelActiveFeature = 85;
        public int unlockLevel
        {
            get => LevelActiveFeature;
            set => LevelActiveFeature = value;
        }

        [Title("MILESTONES")]
        public List<TowerRiseMileStoneInfo> mileStoneInfos = new();

        [Button]
        public void AssignIndexes()
        {
            for (int i = 0; i < mileStoneInfos.Count; i++)
            {
                mileStoneInfos[i].index = i;
            }
        }

        [Button]
        public void AssignDefaultWinAmounts()
        {
            // Segment wins: cumulative 2,5,10,16,23,31,40,50,64,80
            int[] defaultWinAmounts = { 2, 3, 5, 6, 7, 8, 9, 10, 14, 16 };
            mileStoneInfos.Clear();
            for (int i = 0; i < defaultWinAmounts.Length; i++)
            {
                mileStoneInfos.Add(new TowerRiseMileStoneInfo
                {
                    index = i,
                    winAmount = defaultWinAmounts[i],
                    rewards = new List<ResourceGame>()
                });
            }
        }

        public int GetMileStoneCount()
        {
            return mileStoneInfos.Count;
        }

        public TowerRiseMileStoneInfo GetMileStoneInfo(int index)
        {
            if (index < 0 || index >= mileStoneInfos.Count) return null;
            return mileStoneInfos[index];
        }

        /// <summary>Tổng winAmount của toàn bộ event (cộng dồn tất cả mốc).</summary>
        public int GetTotalWinAmount()
        {
            int count = GetMileStoneCount();
            if (count <= 0) return 0;
            return GetCumulativeWinAmount(count - 1);
        }

        /// <summary>Tổng win liên tiếp cần đạt để hoàn thành mốc index (cộng dồn các đoạn).</summary>
        public int GetCumulativeWinAmount(int index)
        {
            if (index < 0) return 0;
            int sum = 0;
            for (int i = 0; i <= index && i < mileStoneInfos.Count; i++)
                sum += mileStoneInfos[i].winAmount;
            return sum;
        }
    }
}
