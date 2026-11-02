using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    [CreateAssetMenu(fileName = "LavaQuestConfig", menuName = "Scriptable Objects/LavaQuestConfig")]
    public class LavaQuestConfig : ScriptableObject
    {
        [Title("UNLOCK & STATUS")]
        public bool isFeatureEnabled = true;
        public int LevelActiveFeature = 45;
        public int unlockLevel
        {
            get => LevelActiveFeature;
            set => LevelActiveFeature = value;
        }

        [Title("REWARD & MILESTONES")]
        public int goldAmount = 10000;
        public int MAX_MILESTONE_AMOUNT = 7;
        public int initialPlayers = 100;
        public int rejoinCooldownMinutes = 30;
        public int minFinalParticipants = 9;
        public int maxFinalParticipants = 14;
    }
}
