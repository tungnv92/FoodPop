using System;
using System.Collections.Generic;
using FoodPop;
using Spine.Unity;
using UnityEngine;

// EN: Owns ingredient-to-Spine references and derives preparation timing from their animations.
// VI: Quản lý tham chiếu Spine theo nguyên liệu và lấy thời gian sơ chế từ animation.
[DisallowMultipleComponent]
public class ChefAnims : MonoBehaviour
{
    [Serializable]
    public class IngredientAnimation
    {
        public FoodType foodType;
        [Tooltip("Animation Spine dùng để sơ chế nguyên liệu này; thời gian lấy trực tiếp từ animation.")]
        public AnimationReferenceAsset animation;
    }

    [Tooltip("Component Spine hiển thị đầu bếp.")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [Tooltip("Animation nghỉ 1 (Idle1): phát lặp ngay khi đầu bếp về trạng thái nghỉ, ví dụ vừa sơ chế xong.")]
    [SerializeField] private AnimationReferenceAsset idleAnimation;
    [Tooltip("Animation nghỉ 2 (Idle2): phát lặp sau khi Idle1 chạy đủ thời gian chờ bên dưới.")]
    [SerializeField] private AnimationReferenceAsset idleLoopAnimation;
    [Tooltip("Thời gian (giây) giữ Idle1 trước khi chuyển sang Idle2.")]
    [SerializeField, Min(0f)] private float idleLoopDelay = 1.5f;
    [Tooltip("Animation sơ chế riêng cho từng nguyên liệu.")]
    [SerializeField] private List<IngredientAnimation> ingredientAnimations = new List<IngredientAnimation>();

    // EN: Initializes Spine and starts the idle animation.
    // VI: Khởi tạo Spine và bắt đầu animation nghỉ.
    private void Start()
    {
        skeletonAnimation.Initialize(false);
        SetPreparing(false, FoodType.None);
    }

    // EN: Plays one preparation cycle, or enters the idle sequence (Idle1, then Idle2 looping) without animation mixing; repeated calls keep the current animation.
    // VI: Phát một lượt sơ chế, hoặc vào chuỗi nghỉ (Idle1 rồi lặp Idle2) không trộn animation; gọi lặp lại thì giữ animation đang chạy.
    public void SetPreparing(bool preparing, FoodType foodType)
    {
        skeletonAnimation.Initialize(false);
        var current = skeletonAnimation.AnimationState.GetCurrent(0);
        if (!preparing)
        {
            if (current == null || !IsIdle(current.Animation))
                PlayIdleSequence();
            return;
        }
        AnimationReferenceAsset reference = GetPreparationAnimation(foodType);
        if (current == null || current.Animation != reference.Animation)
            skeletonAnimation.AnimationState.SetAnimation(0, reference.Animation, false).MixDuration = 0f;
    }

    // EN: Resolves the explicit reference for an ingredient and reports incomplete or incompatible configuration.
    // VI: Lấy tham chiếu được gán cho nguyên liệu và báo cấu hình thiếu hoặc không cùng bộ Spine.
    public AnimationReferenceAsset GetPreparationAnimation(FoodType foodType)
    {
        IngredientAnimation entry = ingredientAnimations.Find(item => item.foodType == foodType);
        if (entry == null || entry.animation == null || entry.animation.Animation == null || entry.animation.SkeletonDataAsset != skeletonAnimation.SkeletonDataAsset)
            throw new InvalidOperationException($"[ChefAnims] Chưa gán animation hợp lệ cùng bộ Spine cho {foodType}.");
        return entry.animation;
    }

    // EN: Returns the Spine action name for editor inspection and existing callers.
    // VI: Trả tên animation Spine để kiểm tra trong editor và các nơi gọi hiện có.
    public string GetPreparationAnimationName(FoodType foodType)
    {
        return foodType == FoodType.None ? idleAnimation.Animation.Name : GetPreparationAnimation(foodType).Animation.Name;
    }

    // EN: Uses the selected animation's native duration as preparation time, without a separate timing field.
    // VI: Dùng thời lượng gốc của animation đã chọn làm thời gian sơ chế, không có field thời gian riêng.
    public float GetPreparationDuration(FoodType foodType)
    {
        return GetPreparationAnimation(foodType).Animation.Duration;
    }

    // EN: Loops Idle1 immediately, then queues Idle2 to start idleLoopDelay seconds later and loop, both without mixing.
    // VI: Lặp Idle1 ngay, rồi xếp Idle2 bắt đầu sau idleLoopDelay giây và lặp, cả hai không trộn animation.
    private void PlayIdleSequence()
    {
        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation.Animation, true).MixDuration = 0f;
        if (idleLoopAnimation != null)
            skeletonAnimation.AnimationState.AddAnimation(0, idleLoopAnimation.Animation, true, Mathf.Max(0.0001f, idleLoopDelay)).MixDuration = 0f;
    }

    // EN: True when the given Spine animation is one of the two idle animations.
    // VI: True khi animation Spine đã cho là một trong hai animation nghỉ.
    private bool IsIdle(Spine.Animation animation)
    {
        return animation == idleAnimation.Animation || (idleLoopAnimation != null && animation == idleLoopAnimation.Animation);
    }
}
