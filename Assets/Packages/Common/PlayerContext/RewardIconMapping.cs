using GamePackages;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Reward Icon Mapping", menuName = "Scriptable Objects/Reward Icon Mapping")]
public class RewardIconMapping : ScriptableObject
{
    public RewardIconDict RewardIconDict;
}

[Serializable]
public class RewardIconDict : SerializedDictionary<ResourceType, Sprite> { }
