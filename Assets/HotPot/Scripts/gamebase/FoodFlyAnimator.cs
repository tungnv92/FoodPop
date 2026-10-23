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
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        private sealed class Flight
        {
            public ConveyorSlot slot;
            public SpriteRenderer flyer;
            public float pitch;
            public int arrivalId;
        }

        private readonly List<Flight> activeFlights = new List<Flight>();
        private readonly Stack<SpriteRenderer> flyerPool = new Stack<SpriteRenderer>();
        private Transform flyerRoot;

        // EN: Launches each portion with its slot's arrival identity so later slot reuse cannot steal its landing.
        // VI: Phóng từng phần cùng mã đáp của slot để việc dùng lại slot không chiếm lần đáp của nó.
        public void Launch(SpriteRenderer source, Vector3 startPosition, Sprite icon, IReadOnlyList<ConveyorSlot> slots)
        {
            if (slots == null || slots.Count == 0 || icon == null || !isActiveAndEnabled)
                return;
            Vector3 startScale = source != null ? source.transform.lossyScale : Vector3.one;
            Quaternion startRotation = source != null ? source.transform.rotation : Quaternion.identity;
            int sortingLayer = source != null ? source.sortingLayerID : 0;
            int sortingOrder = (source != null ? source.sortingOrder : 0) + Config.boardFlight.sortingOrderBoost;
            IngameSoundManager sound = IngameSoundManager.Instance;
            if (sound != null)
                sound.PlayDishLaunch();
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot == null)
                    continue;
                slot.BeginFoodArrival();
                SpriteRenderer flyer = RentFlyer(icon, sortingLayer, sortingOrder + i);
                Flight flight = new Flight { slot = slot, flyer = flyer, arrivalId = slot.ArrivalId, pitch = sound != null ? sound.NextPortionPitch() : 1f };
                activeFlights.Add(flight);
                StartCoroutine(FlyToSlot(flight, startPosition, startScale, startRotation, Config.boardFlight.portionDelay * i));
            }
        }

        // EN: Flies to the moving slot only while the original arrival identity remains valid.
        // VI: Bay tới slot đang chạy chỉ khi mã đáp ban đầu còn hiệu lực.
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
            while (time < Config.boardFlight.flyDuration)
            {
                if (flight.slot == null || !flight.slot.OwnsFoodArrival(flight.arrivalId))
                {
                    EndFlight(flight, false);
                    yield break;
                }
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Config.boardFlight.flyDuration);
                ApplyArcPose(flyerTransform, flight.slot, start, startScale, startRotation, t);
                yield return null;
            }
            EndFlight(flight, true);
        }

        // EN: Places the flyer at time t on the arc: eased travel, an added parabolic height, a pop in scale at the peak and an optional spin.
        // VI: Đặt món bay tại thời điểm t trên vòng cung: di chuyển theo đường cong, cộng độ cao parabol, phóng to ở đỉnh và xoay (tùy chọn).
        private void ApplyArcPose(Transform flyer, ConveyorSlot slot, Vector3 start, Vector3 startScale, Quaternion startRotation, float t)
        {
            float progress = Config.boardFlight.moveCurve != null ? Config.boardFlight.moveCurve.Evaluate(t) : t;
            Vector3 position = Vector3.LerpUnclamped(start, slot.LandingPosition, progress);
            position.y += Config.boardFlight.arcHeight * 4f * t * (1f - t);
            float pop = 1f + (Config.boardFlight.peakScale - 1f) * Mathf.Sin(t * Mathf.PI);
            Quaternion rotation = Quaternion.Slerp(startRotation, slot.LandingRotation, progress) *
                                  Quaternion.Euler(0f, 0f, -360f * Config.boardFlight.spinTurns * progress);
            flyer.SetPositionAndRotation(position, rotation);
            flyer.localScale = Vector3.LerpUnclamped(startScale, slot.LandingScale, progress) * pop;
        }

        // EN: Returns the flyer and completes only its own arrival, never a replacement portion's landing.
        // VI: Trả sprite bay và chỉ hoàn tất mã đáp của nó, không hoàn tất lần đáp của phần thay thế.
        private void EndFlight(Flight flight, bool landed)
        {
            activeFlights.Remove(flight);
            ReturnFlyer(flight.flyer);
            if (!landed || flight.slot == null || !flight.slot.CompleteFoodArrival(flight.arrivalId))
                return;
            IngameSoundManager.Instance?.PlayFoodLand(flight.pitch);
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

        // EN: Returns an unscaled scene-root container for flyers, so their world scale is not affected by the board's transform.
        // VI: Trả về đối tượng gốc không bị scale chứa món bay, để kích thước world không bị transform của board ảnh hưởng.
        private Transform GetFlyerRoot()
        {
            if (flyerRoot == null)
                flyerRoot = new GameObject("FoodFlyers").transform;
            return flyerRoot;
        }

        // EN: Lands only owned pending arrivals on disable, leaving newer slot reservations untouched.
        // VI: Chỉ đáp các lượt còn sở hữu khi tắt, giữ nguyên phần giữ chỗ mới hơn của slot.
        private void OnDisable()
        {
            StopAllCoroutines();
            for (int i = activeFlights.Count - 1; i >= 0; i--)
            {
                Flight flight = activeFlights[i];
                activeFlights.RemoveAt(i);
                ReturnFlyer(flight.flyer);
                if (flight.slot != null)
                    flight.slot.CompleteFoodArrival(flight.arrivalId);
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
