using System;

namespace HomeFeatures.Profile
{
    [Serializable]
	public class ProfileData
	{
		public int AvatarIndex;
		public int FrameIndex;
		public string Name;

		public ProfileData()
		{
			Name = "User6819";
		}
	}
}
