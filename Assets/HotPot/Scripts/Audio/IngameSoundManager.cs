using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Scene singleton that owns every in-game sound effect: which clip plays, how loud, at which pitch and how often. Gameplay scripts only say what happened (dish launched, food landed, chop, served...).
    // VI: Singleton của scene quản lý mọi hiệu ứng âm thanh trong game: phát clip nào, to bao nhiêu, cao độ nào và dày đặc tới đâu. Script gameplay chỉ báo chuyện gì vừa xảy ra (phóng đĩa, món chạm, chặt, phục vụ...).
    [DisallowMultipleComponent]
    public class IngameSoundManager : MonoBehaviour
    {
        private static IngameSoundManager instance;

        // EN: Returns the scene singleton, resolving it once when Awake has not run yet.
        // VI: Trả về singleton trong scene, tìm một lần nếu Awake chưa chạy.
        public static IngameSoundManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<IngameSoundManager>();
                return instance;
            }
        }

        [Tooltip("AudioSource dùng để phát mọi hiệu ứng âm thanh (gắn cùng GameObject).")]
        [SerializeField] private AudioSource source;

        [Header("Chọn đĩa từ board")]
        [Tooltip("Âm thanh khi đĩa được chọn và các phần bắt đầu bay.")]
        [SerializeField] private AudioClip dishLaunchSound;
        [Range(0f, 1f)] [SerializeField] private float dishLaunchVolume = 0.6f;
        [Tooltip("Độ lệch cao độ ngẫu nhiên của âm thanh phóng.")]
        [Range(0f, 0.3f)] [SerializeField] private float dishLaunchPitchJitter = 0.08f;

        [Header("Món rơi xuống slot")]
        [Tooltip("Âm thanh khi một phần nguyên liệu chạm slot băng chuyền.")]
        [SerializeField] private AudioClip foodLandSound;
        [Range(0f, 1f)] [SerializeField] private float foodLandVolume = 1f;
        [Tooltip("Khoảng cao độ ngẫu nhiên của tiếng chạm (1 = cao độ gốc).")]
        [SerializeField] private Vector2 foodLandPitchRange = new Vector2(0.85f, 1.2f);
        [Tooltip("Độ chênh cao độ tối thiểu giữa các phần của cùng một đĩa.")]
        [Range(0f, 0.15f)] [SerializeField] private float minPortionPitchGap = 0.06f;

        [Header("Đầu bếp chặt (tiếng riêng từng món nằm trong FoodIconConfig)")]
        [Range(0f, 1f)] [SerializeField] private float chopVolume = 0.7f;
        [Range(0f, 0.3f)] [SerializeField] private float chopPitchJitter = 0.1f;
        [Tooltip("Âm thanh khi món sơ chế xong.")]
        [SerializeField] private AudioClip preparedSound;
        [Range(0f, 1f)] [SerializeField] private float preparedVolume = 0.8f;

        [Header("Phục vụ khách")]
        [Tooltip("Âm thanh khi món chạm thẻ order của khách.")]
        [SerializeField] private AudioClip serveSound;
        [Range(0f, 1f)] [SerializeField] private float serveVolume = 0.6f;
        [Tooltip("Khoảng cách tối thiểu (giây) giữa hai lần phát tiếng phục vụ.")]
        [Min(0f)] [SerializeField] private float serveSoundInterval = 0.12f;
        [Tooltip("Các tiếng phục vụ liên tiếp trong khoảng này (giây) thành một chuỗi cao dần và nhỏ dần.")]
        [Min(0f)] [SerializeField] private float serveComboWindow = 0.5f;
        [Range(0f, 0.2f)] [SerializeField] private float serveComboPitchStep = 0.06f;
        [Tooltip("Âm thanh khách vui khi nhận đủ order.")]
        [SerializeField] private AudioClip customerHappySound;
        [Range(0f, 1f)] [SerializeField] private float customerHappyVolume = 0.9f;

        private readonly List<float> dishPitches = new List<float>();
        private float lastServeTime = -10f;
        private int serveCombo;

        // EN: Clears the static singleton when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa singleton tĩnh khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers the singleton, removes a duplicate component and makes sure an AudioSource exists.
        // VI: Đăng ký singleton, bỏ component trùng và bảo đảm có AudioSource.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[IngameSoundManager] Scene đã có IngameSoundManager; bỏ component trùng.", this);
                Destroy(this);
                return;
            }
            instance = this;
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        // EN: Releases the singleton registration.
        // VI: Giải phóng đăng ký singleton.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // EN: A dish was picked from the board: plays the launch whoosh and starts a new set of portion pitches.
        // VI: Một đĩa vừa được chọn từ board: phát tiếng phóng và bắt đầu bộ cao độ mới cho các phần của đĩa.
        public void PlayDishLaunch()
        {
            dishPitches.Clear();
            Play(dishLaunchSound, dishLaunchVolume, 1f + Random.Range(-dishLaunchPitchJitter, dishLaunchPitchJitter));
        }

        // EN: Reserves a random landing pitch for the next portion of the current dish, kept at least minPortionPitchGap away from its other portions.
        // VI: Giữ một cao độ chạm ngẫu nhiên cho phần tiếp theo của đĩa hiện tại, cách các phần khác ít nhất minPortionPitchGap.
        public float NextPortionPitch()
        {
            float pitch = 1f;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                pitch = Random.Range(foodLandPitchRange.x, foodLandPitchRange.y);
                if (IsFarFromDishPitches(pitch))
                    break;
            }
            dishPitches.Add(pitch);
            return pitch;
        }

        // EN: A portion landed on its slot: plays the light pop at the pitch reserved for it.
        // VI: Một phần nguyên liệu chạm slot: phát tiếng pop nhẹ với cao độ đã giữ cho nó.
        public void PlayFoodLand(float pitch)
        {
            Play(foodLandSound, foodLandVolume, pitch);
        }

        // EN: One knife chop on a food: plays one of the food's own chop sounds from FoodIconConfig.
        // VI: Một nhát dao lên món: phát một trong các tiếng chặt riêng của món trong FoodIconConfig.
        public void PlayChop(Food food)
        {
            if (food == null || food.IconConfig == null)
                return;
            Play(food.IconConfig.GetChopSound(food.foodType), chopVolume, 1f + Random.Range(-chopPitchJitter, chopPitchJitter));
        }

        // EN: A food finished its preparation.
        // VI: Một món vừa sơ chế xong.
        public void PlayPrepared()
        {
            Play(preparedSound, preparedVolume, 1f + Random.Range(-0.05f, 0.05f));
        }

        // EN: A food reached a customer's order card: plays at most once per serveSoundInterval, and close pops form a combo that rises in pitch and softens.
        // VI: Một món tới thẻ order của khách: phát tối đa một lần mỗi serveSoundInterval, các tiếng sát nhau thành chuỗi cao dần và nhỏ dần.
        public void PlayServe()
        {
            float now = Time.time;
            float sinceLast = now - lastServeTime;
            if (sinceLast < serveSoundInterval)
                return;
            serveCombo = sinceLast <= serveComboWindow ? Mathf.Min(serveCombo + 1, 6) : 0;
            lastServeTime = now;
            Play(serveSound, serveVolume / (1f + 0.15f * serveCombo), 1f + serveComboPitchStep * serveCombo);
        }

        // EN: A customer received the whole order.
        // VI: Một khách đã nhận đủ order.
        public void PlayCustomerHappy()
        {
            Play(customerHappySound, customerHappyVolume, 1f);
        }

        // EN: Plays one clip once at the given volume and pitch.
        // VI: Phát một clip một lần với âm lượng và cao độ đã cho.
        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || source == null)
                return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        // EN: True when the pitch differs enough from every pitch already used by the current dish.
        // VI: True khi cao độ đủ khác mọi cao độ đã dùng cho đĩa hiện tại.
        private bool IsFarFromDishPitches(float pitch)
        {
            foreach (float used in dishPitches)
            {
                if (Mathf.Abs(used - pitch) < minPortionPitchGap)
                    return false;
            }
            return true;
        }
    }
}
