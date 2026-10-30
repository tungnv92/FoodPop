using Spine.Unity;
using UnityEngine;

// EN: Bridges preparation jobs to the chef's Spine animations without restarting an unchanged state.
// VI: Nối công việc sơ chế với animation Spine của đầu bếp, không phát lại khi trạng thái chưa đổi.
[DisallowMultipleComponent]
public class ChefAnims : MonoBehaviour
{
    [Tooltip("Component Spine hiển thị đầu bếp.")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [Tooltip("Tên animation nghỉ trong bộ Spine.")]
    [SerializeField] private string idleAnimation = "Defaul/Idle";
    [Tooltip("Tên animation cắt nguyên liệu.")]
    [SerializeField] private string cutAnimation = "Cut/Action";
    [Tooltip("Tên animation luộc đậu, tôm và bạch tuộc.")]
    [SerializeField] private string boilAnimation = "Boil/Action";
    [Tooltip("Tên animation nắm cơm sushi.")]
    [SerializeField] private string riceBallAnimation = "RiceBall/Action";
    [Tooltip("Tên animation pha trà.")]
    [SerializeField] private string teaAnimation = "Tea/Action";
    [Tooltip("Tên animation chiên trứng và chế biến lươn.")]
    [SerializeField] private string fryAnimation = "Fry/Action";

    // EN: Starts the chef in the idle pose after initializing the Spine component.
    // VI: Khởi tạo component Spine và cho đầu bếp bắt đầu ở trạng thái nghỉ.
    private void Start()
    {
        SetPreparing(false, FoodPop.FoodType.None);
    }

    // EN: Plays the ingredient action without restarting unchanged tracks; missing actions fall back to idle and transitions have no mix.
    // VI: Phát động tác theo nguyên liệu, không phát lại track chưa đổi; thiếu động tác thì về nghỉ và chuyển không trộn.
    public void SetPreparing(bool preparing, FoodPop.FoodType foodType)
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
            return;
        skeletonAnimation.Initialize(false);
        string animationName = preparing ? GetPreparationAnimationName(foodType) : idleAnimation;
        if (skeletonAnimation.Skeleton.Data.FindAnimation(animationName) == null)
        {
            Debug.LogWarning($"[ChefAnims] Không tìm thấy animation {animationName}.", this);
            animationName = idleAnimation;
            if (skeletonAnimation.Skeleton.Data.FindAnimation(animationName) == null)
                return;
        }
        var current = skeletonAnimation.AnimationState.GetCurrent(0);
        if (current == null || current.Animation.Name != animationName)
            skeletonAnimation.AnimationState.SetAnimation(0, animationName, true).MixDuration = 0f;
    }

    // EN: Maps ingredients to Spine actions, using the pan for all chicken cuts.
    // VI: Chọn động tác Spine theo nguyên liệu, dùng chảo cho các phần thịt gà.
    public string GetPreparationAnimationName(FoodPop.FoodType foodType)
    {
        switch (foodType)
        {
            case FoodPop.FoodType.None:
                return idleAnimation;
            case FoodPop.FoodType.SushiRice:
                return riceBallAnimation;
            case FoodPop.FoodType.TeaLeaves:
                return teaAnimation;
            case FoodPop.FoodType.Edamame:
            case FoodPop.FoodType.Shrimp:
            case FoodPop.FoodType.Octopus:
                return boilAnimation;
            case FoodPop.FoodType.Tamagoyaki:
            case FoodPop.FoodType.Unagi:
            case FoodPop.FoodType.ChickenThigh:
            case FoodPop.FoodType.ChickenBites:
            case FoodPop.FoodType.ChickenWing:
            case FoodPop.FoodType.ChickenBreast:
                return fryAnimation;
            default:
                return cutAnimation;
        }
    }

}
