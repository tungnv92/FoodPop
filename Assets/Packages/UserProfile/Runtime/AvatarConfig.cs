using System;
using UnityEngine;

namespace HomeFeatures.Profile
{
    [CreateAssetMenu(fileName = "Avatar Config", menuName = "Scriptable Objects/Profile/Avatar Config")]
	public class AvatarConfig : ScriptableObject
	{
		public Sprite[] AvatarSprites;
	}
}
