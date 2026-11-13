using System.Collections;
using UnityEngine;

namespace FoodPop
{
    public class AnimatorFinalFoodAnimation : FinalFoodAnimation
    {
        [Tooltip("Animator phát clip chế biến trên cùng object nhận Animation Event.")]
        [SerializeField] private Animator animator;
        [Tooltip("Tên state chế biến trong Animator Controller.")]
        [SerializeField] private string preparationState = "Prepare";
        [Tooltip("Nhóm đạo cụ chỉ hiện trong quá trình chế biến.")]
        [SerializeField] private GameObject animationRoot;
        [Tooltip("Các renderer đạo cụ cần vẽ trên hình món.")]
        [SerializeField] private SpriteRenderer[] props = new SpriteRenderer[0];
        [Tooltip("Các hệ hạt được bật/tắt bằng Animation Event.")]
        [SerializeField] private ParticleSystem[] particles = new ParticleSystem[0];

        private bool completed;

        // EN: Play the preparation state and wait for its completion Animation Event before serving.
        // VI: Phát state chế biến và chờ Animation Event hoàn tất trước khi phục vụ.
        public override IEnumerator Play()
        {
            completed = false;
            animator.enabled = true;
            animator.Play(preparationState, 0, 0f);
            while (!completed) yield return null;
        }

        // EN: Rebind idle, sample only on active objects and clear temporary particles when pooled or interrupted.
        // VI: Gắn lại tư thế nghỉ, chỉ lấy mẫu trên object đang bật và xóa hạt tạm khi trả pool hoặc bị ngắt.
        public override void ResetAnimation()
        {
            completed = true;
            foreach (ParticleSystem effect in particles)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            animator.Rebind();
            if (gameObject.activeInHierarchy) animator.Update(0f);
            animator.enabled = false;
            animationRoot.SetActive(false);
        }

        // EN: Sort all temporary props and particle renderers above the ingredient layers.
        // VI: Đặt mọi đạo cụ tạm và renderer hạt phía trên các lớp nguyên liệu.
        public override void SetSorting(int sortingLayer, int baseOrder)
        {
            foreach (SpriteRenderer prop in props)
            {
                prop.sortingLayerID = sortingLayer;
                prop.sortingOrder = baseOrder + 2;
            }
            foreach (ParticleSystem effect in particles)
            {
                ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = baseOrder + 3;
            }
        }

        // EN: Animation Event: start the authored ingredient particles at the chosen clip frame.
        // VI: Animation Event: phát hạt nguyên liệu tại khung hình đã chọn trong clip.
        public void PlayParticles()
        {
            foreach (ParticleSystem effect in particles) effect.Play(true);
        }

        // EN: Animation Event: stop emission while letting existing grains finish falling.
        // VI: Animation Event: dừng phát hạt và để các hạt đang có rơi hết.
        public void StopParticles()
        {
            foreach (ParticleSystem effect in particles)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // EN: Animation Event on the final frame releases the waiting serving coroutine.
        // VI: Animation Event ở khung cuối giải phóng coroutine phục vụ đang chờ.
        public void CompleteAnimation()
        {
            completed = true;
        }
    }
}
