using System;
using UnityEngine;

namespace Longvm.RewardAnimator
{
	[Serializable]
	public class AmountFormatter
	{
		public string format = "x{0}";

        public virtual string Format(int amount) => string.Format(format, amount);
    } 
}
