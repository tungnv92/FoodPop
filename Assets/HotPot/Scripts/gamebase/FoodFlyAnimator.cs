using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Plays the pick animation: each portion of a picked dish jumps along an arc from the board to its conveyor slot, then lands with a squash, an optional particle burst and an optional sound.
    // VI: Chạy hoạt cảnh khi chọn đĩa: mỗi phần nguyên liệu nhảy theo vòng cung từ board lên slot băng chuyền, rồi chạm xuống với hiệu ứng co giãn, particle (tùy chọn) và âm thanh (tùy chọn).
    [DisallowMultipleComponent]
    public class FoodFlyAnimator : MonoBehaviour
    {
        [Header("Bay vòng cung")]
        [Tooltip("Thời gian (giây) một phần nguyên liệu bay từ đĩa lên slot băng chuyền.")]
        [Min(0.01f)] [SerializeField] private float flyDuration = 0.4f;
        [Tooltip("Độ cao đỉnh vòng cung (đơn vị world) so với đường thẳng nối điểm đầu và điểm cuối.")]
        [Min(0f)] [SerializeField] private float arcHeight = 1.5f;
        [Tooltip("Độ trễ (giây) giữa các phần khi một đĩa có nhiều phần.")]
        [Min(0f)] [SerializeField] private float portionDelay = 0.06f;
        [Tooltip("Đường cong tiến độ bay theo thời gian (trục ngang 0→1 là thời gian, trục dọc 0→1 là quãng đường).")]
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Hệ số phóng to ở đỉnh vòng cung so với kích thước bình thường.")]
        [Min(1f)] [SerializeField] private float peakScale = 1.25f;
        [Tooltip("Số vòng xoay trong lúc bay; 0 để không xoay.")]
        [SerializeField] private float spinTurns = 1f;
        [Tooltip("Cộng thêm vào thứ tự vẽ để món đang bay luôn nằm trên board và băng chuyền.")]
        [SerializeField] private int sortingOrderBoost = 100;

        [Header("Hiệu ứng khi chạm")]
        [Tooltip("Prefab particle (tùy chọn) bắn ra tại slot khi món chạm băng chuyền.")]
        [SerializeField] private ParticleSystem landEffectPrefab;
        [Tooltip("Âm thanh (tùy chọn) phát khi món chạm băng chuyền.")]
        [SerializeField] private AudioClip landSound;
        [Tooltip("Âm lượng âm thanh khi chạm.")]
        [Range(0f, 1f)] [SerializeField] private float landVolume = 1f;
        [Tooltip("Độ lệch cao độ ngẫu nhiên của âm thanh chạm để nghe đỡ lặp lại.")]
        [Range(0f, 0.3f)] [SerializeField] private float landPitchJitter = 0.08f;

        private sealed class Flight
        {
            public ConveyorSlot slot;
            public SpriteRenderer flyer;
        }

        private readonly List<Flight> activeFlights = new List<Flight>();
        private readonly Stack<SpriteRenderer> flyerPool = new Stack<SpriteRenderer>();
        private readonly List<ParticleSystem> effectPool = new List<ParticleSystem>();
        private Transform flyerRoot;
        private AudioSource audioSource;

        // EN: Launches one flying portion per target slot from the picked dish, hiding each slot's food until its portion lands.
        // VI: Phóng một phần bay cho mỗi slot đích từ đĩa vừa chọn, ẩn món trên slot cho tới khi phần đó chạm xuống.
        public void Launch(SpriteRenderer source, Vector3 startPosition, Sprite icon, IReadOnlyList<ConveyorSlot> slots)
        {
            if (slots == null || slots.Count == 0 || icon == null || !isActiveAndEnabled)
                return;
            Vector3 startScale = source != null ? source.transform.lossyScale : Vector3.one;
            Quaternion startRotation = source != null ? source.transform.rotation : Quaternion.identity;
            int sortingLayer = source != null ? source.sortingLayerID : 0;
            int sortingOrder = (source != null ? source.sortingOrder : 0) + sortingOrderBoost;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot == null)
                    continue;
                slot.BeginFoodArrival();
                SpriteRenderer flyer = RentFlyer(icon, sortingLayer, sortingOrder + i);
                Flight flight = new Flight { slot = slot, flyer = flyer };
                activeFlights.Add(flight);
                StartCoroutine(FlyToSlot(flight, startPosition, startScale, startRotation, portionDelay * i));
            }
        }

        // EN: Moves one portion along a parabolic arc toward its slot's current (moving) position, then lands it; cancels quietly if the slot is cleared mid-flight.
        // VI: Di chuyển một phần theo vòng cung parabol tới vị trí hiện tại (đang chạy) của slot rồi đặt xuống; tự hủy nếu slot bị làm rỗng giữa chừng.
        private IEnumerator FlyToSlot(Flight flight, Vector3 start, Vector3 startScale, Quaternion startRotation, float delay)
        {
            Transform flyerTransform = flight.flyer.transform;
            flight.flyer.gameObject.SetActive(false);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            flyerTransform.SetPositionAndRotation(start, startRotation);
            flyerTransform.localScale = startScale;
            flight.flyer.gameObject.SetActive(true);
            float time = 0f;
            while (time < flyDuration)
            {
                if (flight.slot == null || !flight.slot.IsFoodArriving)
                {
                    EndFlight(flight, false);
                    yield break;
                }
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / flyDuration);
                ApplyArcPose(flyerTransform, flight.slot, start, startScale, startRotation, t);
                yield return null;
            }
            EndFlight(flight, true);
        }

        // EN: Places the flyer at time t on the arc: eased travel, an added parabolic height, a pop in scale at the peak and an optional spin.
        // VI: Đặt món bay tại thời điểm t trên vòng cung: di chuyển theo đường cong, cộng độ cao parabol, phóng to ở đỉnh và xoay (tùy chọn).
        private void ApplyArcPose(Transform flyer, ConveyorSlot slot, Vector3 start, Vector3 startScale, Quaternion startRotation, float t)
        {
            float progress = moveCurve != null ? moveCurve.Evaluate(t) : t;
            Vector3 position = Vector3.LerpUnclamped(start, slot.LandingPosition, progress);
            position.y += arcHeight * 4f * t * (1f - t);
            float pop = 1f + (peakScale - 1f) * Mathf.Sin(t * Mathf.PI);
            Quaternion rotation = Quaternion.Slerp(startRotation, slot.LandingRotation, progress) *
                                  Quaternion.Euler(0f, 0f, -360f * spinTurns * progress);
            flyer.SetPositionAndRotation(position, rotation);
            flyer.localScale = Vector3.LerpUnclamped(startScale, slot.LandingScale, progress) * pop;
        }

        // EN: Finishes a flight: returns the flyer to the pool and, when it landed on a slot still waiting for it, reveals the food and plays the landing effects.
        // VI: Kết thúc một lượt bay: trả món bay về pool và, nếu chạm đúng slot vẫn đang chờ, hiện món trên slot và chạy hiệu ứng chạm.
        private void EndFlight(Flight flight, bool landed)
        {
            activeFlights.Remove(flight);
            ReturnFlyer(flight.flyer);
            if (!landed || flight.slot == null || !flight.slot.CompleteFoodArrival())
                return;
            PlayLandEffect(flight.slot.LandingPosition);
            PlayLandSound();
        }

        // EN: Spawns (or reuses) the optional landing particle at the given world position.
        // VI: Tạo (hoặc tái sử dụng) particle chạm (tùy chọn) tại vị trí world đã cho.
        private void PlayLandEffect(Vector3 position)
        {
            if (landEffectPrefab == null)
                return;
            ParticleSystem effect = null;
            foreach (ParticleSystem candidate in effectPool)
            {
                if (candidate != null && !candidate.IsAlive(true))
                {
                    effect = candidate;
                    break;
                }
            }
            if (effect == null)
            {
                effect = Instantiate(landEffectPrefab, GetFlyerRoot());
                effectPool.Add(effect);
            }
            effect.transform.position = position;
            effect.Play(true);
        }

        // EN: Plays the optional landing sound with a small random pitch change.
        // VI: Phát âm thanh chạm (tùy chọn) với cao độ thay đổi ngẫu nhiên một chút.
        private void PlayLandSound()
        {
            if (landSound == null)
                return;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            audioSource.pitch = 1f + Random.Range(-landPitchJitter, landPitchJitter);
            audioSource.PlayOneShot(landSound, landVolume);
        }

        // EN: Takes a flyer sprite from the pool (creating one if needed) and applies the icon and sorting.
        // VI: Lấy một sprite bay từ pool (tạo mới nếu cần) và gán icon cùng thứ tự vẽ.
        private SpriteRenderer RentFlyer(Sprite icon, int sortingLayer, int sortingOrder)
        {
            SpriteRenderer flyer = null;
            while (flyer == null && flyerPool.Count > 0)
                flyer = flyerPool.Pop();
            if (flyer == null)
            {
                GameObject go = new GameObject("Flying food");
                go.transform.SetParent(GetFlyerRoot(), false);
                flyer = go.AddComponent<SpriteRenderer>();
            }
            flyer.sprite = icon;
            flyer.sortingLayerID = sortingLayer;
            flyer.sortingOrder = sortingOrder;
            return flyer;
        }

        // EN: Hides a flyer and keeps it for reuse.
        // VI: Ẩn món bay và giữ lại để tái sử dụng.
        private void ReturnFlyer(SpriteRenderer flyer)
        {
            if (flyer == null)
                return;
            flyer.gameObject.SetActive(false);
            flyerPool.Push(flyer);
        }

        // EN: Returns an unscaled scene-root container for flyers and effects, so their world scale is not affected by the board's transform.
        // VI: Trả về đối tượng gốc không bị scale chứa món bay và hiệu ứng, để kích thước world không bị transform của board ảnh hưởng.
        private Transform GetFlyerRoot()
        {
            if (flyerRoot == null)
                flyerRoot = new GameObject("FoodFlyers").transform;
            return flyerRoot;
        }

        // EN: When disabled mid-flight, lands every pending portion immediately so no slot stays hidden.
        // VI: Khi bị tắt giữa lúc bay, đặt ngay mọi phần đang bay xuống để không slot nào bị ẩn mãi.
        private void OnDisable()
        {
            StopAllCoroutines();
            for (int i = activeFlights.Count - 1; i >= 0; i--)
            {
                Flight flight = activeFlights[i];
                activeFlights.RemoveAt(i);
                ReturnFlyer(flight.flyer);
                if (flight.slot != null)
                    flight.slot.CompleteFoodArrival();
            }
        }

        // EN: Destroys the flyer container together with this animator.
        // VI: Hủy đối tượng chứa món bay cùng với animator này.
        private void OnDestroy()
        {
            if (flyerRoot != null)
                Destroy(flyerRoot.gameObject);
        }
    }
}
