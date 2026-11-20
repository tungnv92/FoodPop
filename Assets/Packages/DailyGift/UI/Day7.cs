using HomeFeatures.DailyLogin;
using UnityEngine;
using UnityEngine.UI;

namespace HomeFeatures.DailyLogin
{
    public class Day7 : Day
    {
        [SerializeField] Image boxButton;
        public override void Highlight()
        {
            base.Highlight();
            boxButton.raycastTarget = false;
        }
    }
}

