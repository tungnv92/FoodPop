using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace FoodPop
{
    // EN: Moves the conveyor slots around the belt of Conveyor_Chef along a closed spline path.
    // VI: Di chuyển các slot băng chuyền quanh băng của Conveyor_Chef theo một đường spline khép kín.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    public class ConveyorChefMovement : MonoBehaviour
    {
        public const int DefaultSlotCount = 12;

        // Belt centre line of ConveyorStandalone.png (perspective art) in sprite-local units (centre pivot, 100 PPU), clockwise from top middle.
        // Đường tâm mặt băng của ConveyorStandalone.png theo đơn vị local của sprite (pivot giữa, 100 PPU), chiều kim đồng hồ từ giữa cạnh trên.
        private static readonly Vector2[] BeltCenterline =
        {
            new Vector2(-0.172f, 3.836f), new Vector2(0.589f, 3.836f), new Vector2(1.351f, 3.836f), new Vector2(2.113f, 3.836f),
            new Vector2(2.875f, 3.836f), new Vector2(3.636f, 3.836f), new Vector2(4.398f, 3.836f), new Vector2(5.144f, 3.719f),
            new Vector2(5.726f, 3.308f), new Vector2(5.972f, 2.716f), new Vector2(6.014f, 2.085f), new Vector2(6.057f, 1.444f),
            new Vector2(6.100f, 0.794f), new Vector2(6.144f, 0.135f), new Vector2(6.189f, -0.534f), new Vector2(6.234f, -1.213f),
            new Vector2(6.213f, -1.896f), new Vector2(5.809f, -2.472f), new Vector2(5.095f, -2.767f), new Vector2(4.278f, -2.783f),
            new Vector2(3.460f, -2.783f), new Vector2(2.641f, -2.783f), new Vector2(1.822f, -2.783f), new Vector2(1.004f, -2.783f),
            new Vector2(0.185f, -2.783f), new Vector2(-0.634f, -2.783f), new Vector2(-1.452f, -2.783f), new Vector2(-2.271f, -2.783f),
            new Vector2(-3.090f, -2.783f), new Vector2(-3.908f, -2.783f), new Vector2(-4.727f, -2.783f), new Vector2(-5.513f, -2.648f),
            new Vector2(-6.079f, -2.181f), new Vector2(-6.255f, -1.523f), new Vector2(-6.209f, -0.840f), new Vector2(-6.164f, -0.167f),
            new Vector2(-6.120f, 0.497f), new Vector2(-6.076f, 1.151f), new Vector2(-6.034f, 1.796f), new Vector2(-5.991f, 2.432f),
            new Vector2(-5.886f, 3.054f), new Vector2(-5.439f, 3.565f), new Vector2(-4.742f, 3.821f), new Vector2(-3.981f, 3.836f),
            new Vector2(-3.219f, 3.836f), new Vector2(-2.457f, 3.836f), new Vector2(-1.696f, 3.836f), new Vector2(-0.934f, 3.836f),
        };

        [Header("Path")]
        [SerializeField] private SplineContainer splineContainer;
        [Tooltip("SpriteRenderer của ConveyorStandalone; spline được dựng theo mặt băng của nó.")]
        [SerializeField] private SpriteRenderer conveyorRenderer;
        [SerializeField] private bool buildSplineIfEmpty = true;

        [Header("Path Fit (world units)")]
        [Tooltip("Khi bật, spline được dựng lại theo ảnh băng chuyền cùng các offset bên dưới mỗi khi đổi giá trị. Tắt đi để giữ spline đã chỉnh tay.")]
        [SerializeField] private bool fitSplineToConveyor = true;
        [Tooltip("Dịch toàn bộ đường chạy lên (+) hoặc xuống (-).")]
        [SerializeField] private float pathOffsetY;
        [Tooltip("Offset dọc thêm cho hàng trước (phía dưới); hai cạnh bên chuyển dần sang offset hàng sau.")]
        [SerializeField] private float frontRowOffsetY;
        [Tooltip("Offset dọc thêm cho hàng sau (phía trên); hai cạnh bên chuyển dần sang offset hàng trước.")]
        [SerializeField] private float backRowOffsetY;

        [Header("Slots")]
        [SerializeField] private ConveyorSlot slotPrefab;
        [Tooltip("Object cha chứa các slot; mặc định là child tên 'Slots', nếu không có thì dùng chính object này.")]
        [SerializeField] private Transform slotParent;
        [SerializeField, Min(1)] private int slotCount = DefaultSlotCount;
        [SerializeField] private List<ConveyorSlot> slots = new List<ConveyorSlot>();

        [Header("Movement")]
        [Tooltip("Tốc độ băng chuyền (đơn vị world mỗi giây).")]
        [SerializeField, Min(0f)] private float speed = 0.8f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private bool isMoving = true;
        [Tooltip("Vị trí chuẩn hóa (0..1) trên spline của slot 0.")]
        [SerializeField, Range(0f, 1f)] private float startOffset;

        [Header("Trạm sơ chế")]
        [Tooltip("Điểm bắt đầu của trạm sơ chế trên đường băng (0..1, tính từ giữa cạnh trên theo chiều kim đồng hồ). Món thô đi vào đoạn này sẽ được đầu bếp cắt.")]
        [SerializeField, Range(0f, 1f)] private float stationStart = 0.44f;
        [Tooltip("Điểm kết thúc của trạm sơ chế trên đường băng (0..1). Nếu nhỏ hơn điểm bắt đầu thì trạm vắt qua điểm 0.")]
        [SerializeField, Range(0f, 1f)] private float stationEnd = 0.56f;

        [Header("Sorting")]
        [Tooltip("Sorting order thấp nhất của sprite trong slot; giữ cao hơn băng chuyền (0), lớp Front (66) và đầu bếp (65).")]
        [SerializeField] private int slotSortingOrder = 70;

        private float progress;
        private float pathLength;

        public IReadOnlyList<ConveyorSlot> Slots => slots;
        public SplineContainer Path => splineContainer;
        public bool IsMoving => isMoving;

        private Transform SlotParent
        {
            get
            {
                if (slotParent == null)
                {
                    Transform named = transform.Find("Slots");
                    slotParent = named != null ? named : transform;
                }
                return slotParent;
            }
        }
        public float Speed => speed;

        // EN: Fills component references when the component is first added in the editor.
        // VI: Tự gán các tham chiếu khi component vừa được thêm trong editor.
        private void Reset()
        {
            ResolveReferences();
        }

        // EN: Prepares references, the spline path and the slot list before the first frame.
        // VI: Chuẩn bị tham chiếu, đường spline và danh sách slot trước frame đầu tiên.
        private void Awake()
        {
            ResolveReferences();
            if (buildSplineIfEmpty && splineContainer.Spline.Count < 2)
                RebuildSplineFromConveyor();
            EnsureSlots();
            ApplySlotSorting();
            RefreshPathMetrics();
            PlaceSlots();
        }

        // EN: Advances the belt and moves every slot to its new spline position.
        // VI: Cho băng chạy tiếp và đặt mọi slot vào vị trí mới trên spline.
        private void Update()
        {
            if (!isMoving || pathLength <= Mathf.Epsilon)
                return;
            AdvanceProgress(Time.deltaTime);
            PlaceSlots();
        }

        // EN: Starts or pauses the belt movement.
        // VI: Bật hoặc tạm dừng chuyển động của băng chuyền.
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        // EN: Changes the belt speed in world units per second.
        // VI: Đổi tốc độ băng chuyền theo đơn vị world mỗi giây.
        public void SetSpeed(float unitsPerSecond)
        {
            speed = Mathf.Max(0f, unitsPerSecond);
        }

        // EN: Returns the normalized spline position (0..1) currently occupied by a slot index.
        // VI: Trả về vị trí chuẩn hóa (0..1) trên spline mà slot theo chỉ số đang chiếm.
        public float GetSlotNormalizedPosition(int index)
        {
            int count = Mathf.Max(1, slots.Count);
            return Mathf.Repeat(startOffset + progress + (float)index / count, 1f);
        }

        // EN: Returns how far a slot has travelled through the preparation station along the belt direction (0 = just entered, 1 = about to leave), or -1 when it is outside the station.
        // VI: Trả về mức slot đã đi qua trạm sơ chế theo chiều chạy của băng (0 = vừa vào, 1 = sắp ra), hoặc -1 khi slot nằm ngoài trạm.
        public float GetStationProgress(int index)
        {
            float length = Mathf.Repeat(stationEnd - stationStart, 1f);
            if (length <= Mathf.Epsilon)
                return -1f;
            float offset = Mathf.Repeat(GetSlotNormalizedPosition(index) - stationStart, 1f);
            if (offset > length)
                return -1f;
            float progressInStation = offset / length;
            return clockwise ? progressInStation : 1f - progressInStation;
        }

        // EN: Returns true while the slot at the given index is inside the preparation station.
        // VI: Trả về true khi slot theo chỉ số đang nằm trong trạm sơ chế.
        public bool IsSlotInStation(int index)
        {
            return GetStationProgress(index) >= 0f;
        }

        // EN: Finds the SplineContainer and the ConveyorStandalone renderer when they are not assigned.
        // VI: Tìm SplineContainer và renderer của ConveyorStandalone khi chưa được gán.
        public void ResolveReferences()
        {
            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();
            if (conveyorRenderer == null)
                conveyorRenderer = FindConveyorRenderer();
        }

        // EN: Replaces the spline with a closed, smooth loop following the belt centre of the conveyor sprite, shifted by the Y offsets.
        // VI: Thay spline bằng vòng kín, mượt theo tâm mặt băng của sprite băng chuyền, có cộng thêm các offset Y.
        public void RebuildSplineFromConveyor()
        {
            ResolveReferences();
            Transform source = conveyorRenderer != null ? conveyorRenderer.transform : transform;
            GetCenterlineYRange(out float minY, out float maxY);
            Spline spline = splineContainer.Spline;
            spline.Clear();
            foreach (Vector2 point in BeltCenterline)
            {
                Vector3 world = source.TransformPoint(point);
                world.y += GetOffsetY(point.y, minY, maxY);
                Vector3 local = splineContainer.transform.InverseTransformPoint(world);
                spline.Add(new BezierKnot(new float3(local.x, local.y, 0f)), TangentMode.AutoSmooth);
            }
            spline.Closed = true;
            RefreshPathMetrics();
        }

        // EN: Returns the world Y offset for a centre-line point: the global offset plus a front/back blend by its height.
        // VI: Trả về offset Y (world) cho một điểm đường tâm: offset chung cộng phần pha trộn trước/sau theo độ cao của điểm.
        private float GetOffsetY(float pointY, float minY, float maxY)
        {
            float backWeight = Mathf.InverseLerp(minY, maxY, pointY);
            return pathOffsetY + Mathf.Lerp(frontRowOffsetY, backRowOffsetY, backWeight);
        }

        // EN: Finds the lowest (front) and highest (back) Y of the built-in belt centre line.
        // VI: Tìm Y thấp nhất (hàng trước) và cao nhất (hàng sau) của đường tâm băng có sẵn.
        private static void GetCenterlineYRange(out float minY, out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            foreach (Vector2 point in BeltCenterline)
            {
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }
        }

#if UNITY_EDITOR
        // EN: In the editor, refits the spline and snaps the slots right after an Inspector value changes (when fitting is enabled).
        // VI: Trong editor, dựng lại spline và đặt lại slot ngay khi đổi giá trị trong Inspector (khi bật fit theo băng chuyền).
        private void OnValidate()
        {
            if (!fitSplineToConveyor)
                return;
            UnityEditor.EditorApplication.delayCall -= RefitFromInspector;
            UnityEditor.EditorApplication.delayCall += RefitFromInspector;
        }

        // EN: Deferred refit used by OnValidate: rebuilds the spline, re-places the slots and marks the scene objects dirty.
        // VI: Hàm dựng lại trì hoãn dùng cho OnValidate: dựng lại spline, đặt lại slot và đánh dấu object đã thay đổi.
        private void RefitFromInspector()
        {
            if (this == null || !fitSplineToConveyor)
                return;
            ResolveReferences();
            if (splineContainer == null)
                return;
            RebuildSplineFromConveyor();
            PlaceSlots();
            UnityEditor.EditorUtility.SetDirty(splineContainer);
            foreach (ConveyorSlot slot in slots)
            {
                if (slot != null)
                    UnityEditor.EditorUtility.SetDirty(slot.transform);
            }
        }
#endif

        // EN: Collects existing child slots and, when a prefab is set, creates missing ones up to slotCount.
        // VI: Gom các slot con có sẵn và, nếu có prefab, tạo thêm cho đủ slotCount.
        public void EnsureSlots(System.Func<ConveyorSlot, Transform, ConveyorSlot> createSlot = null)
        {
            CollectChildSlots();
            if (slotPrefab == null)
                return;
            while (slots.Count < slotCount)
            {
                Transform parent = SlotParent;
                ConveyorSlot slot = createSlot != null ? createSlot(slotPrefab, parent) : Instantiate(slotPrefab, parent);
                slot.name = $"Conveyor_Slot_{slots.Count + 1:00}";
                slots.Add(slot);
            }
        }

        // EN: Recomputes the world length of the spline path.
        // VI: Tính lại độ dài world của đường spline.
        public void RefreshPathMetrics()
        {
            ResolveReferences();
            if (splineContainer.Spline.Count < 2)
            {
                pathLength = 0f;
                return;
            }
            pathLength = splineContainer.CalculateLength();
        }

        // EN: Puts every slot at its evenly spaced spline position.
        // VI: Đặt mọi slot vào vị trí chia đều trên spline.
        public void PlaceSlots()
        {
            if (splineContainer == null || splineContainer.Spline.Count < 2)
                return;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot == null)
                    continue;
                float3 position = splineContainer.EvaluatePosition(GetSlotNormalizedPosition(i));
                Vector3 current = slot.transform.position;
                slot.transform.position = new Vector3(position.x, position.y, current.z);
            }
        }

        // EN: Moves the shared belt progress forward by the distance travelled this frame.
        // VI: Tăng tiến độ chung của băng theo quãng đường đi được trong frame này.
        private void AdvanceProgress(float deltaTime)
        {
            float direction = clockwise ? 1f : -1f;
            progress = Mathf.Repeat(progress + direction * speed * deltaTime / pathLength, 1f);
        }

        // EN: Rebuilds the slot list from ConveyorSlot components directly under the slot parent, keeping hierarchy order.
        // VI: Dựng lại danh sách slot từ các ConveyorSlot là con trực tiếp của slot parent, giữ theo thứ tự hierarchy.
        private void CollectChildSlots()
        {
            slots.RemoveAll(slot => slot == null);
            foreach (Transform child in SlotParent)
            {
                ConveyorSlot slot = child.GetComponent<ConveyorSlot>();
                if (slot != null && !slots.Contains(slot))
                    slots.Add(slot);
            }
        }

        // EN: Puts every slot sprite on the conveyor's sorting layer and object layer, ordered from slotSortingOrder upward so slots draw above the belt.
        // VI: Đưa mọi sprite của slot về cùng sorting layer và layer object với băng chuyền, xếp order từ slotSortingOrder trở lên để slot nằm trên băng.
        public void ApplySlotSorting()
        {
            ResolveReferences();
            foreach (ConveyorSlot slot in slots)
            {
                if (slot != null)
                    ApplySortingToSlot(slot);
            }
        }

        // EN: Re-numbers one slot's renderers from slotSortingOrder upward, keeping their current stacking (shadow < plate < ingredient).
        // VI: Đánh số lại renderer của một slot từ slotSortingOrder trở lên, giữ thứ tự chồng hiện tại (bóng < đĩa < nguyên liệu).
        private void ApplySortingToSlot(ConveyorSlot slot)
        {
            SpriteRenderer[] renderers = slot.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
                return;
            List<int> hierarchyIndex = new List<int>();
            for (int i = 0; i < renderers.Length; i++)
                hierarchyIndex.Add(i);
            hierarchyIndex.Sort((a, b) =>
            {
                int byOrder = renderers[a].sortingOrder.CompareTo(renderers[b].sortingOrder);
                return byOrder != 0 ? byOrder : a.CompareTo(b);
            });
            for (int rank = 0; rank < hierarchyIndex.Count; rank++)
            {
                SpriteRenderer renderer = renderers[hierarchyIndex[rank]];
                renderer.sortingOrder = slotSortingOrder + rank;
                if (conveyorRenderer != null)
                    renderer.sortingLayerID = conveyorRenderer.sortingLayerID;
            }
            if (conveyorRenderer != null)
                SetLayerRecursively(slot.transform, conveyorRenderer.gameObject.layer);
        }

        // EN: Assigns a GameObject layer to a transform and all of its children so the camera culls them like the conveyor.
        // VI: Gán layer GameObject cho transform và toàn bộ con để camera hiển thị chúng giống băng chuyền.
        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
                SetLayerRecursively(child, layer);
        }

        // EN: Looks for the ConveyorStandalone sprite renderer under this object, then in its parent.
        // VI: Tìm sprite renderer ConveyorStandalone trong object này, sau đó trong object cha.
        private SpriteRenderer FindConveyorRenderer()
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            foreach (SpriteRenderer candidate in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (candidate.sprite != null && candidate.sprite.name.StartsWith("ConveyorStandalone") &&
                    !candidate.sprite.name.Contains("Front"))
                    return candidate;
            }
            return null;
        }

        // EN: Draws the slot spacing and the preparation station (orange segment with end markers) on the spline in the Scene view.
        // VI: Vẽ khoảng chia slot và trạm sơ chế (đoạn màu cam có vạch hai đầu) trên spline trong Scene view.
        private void OnDrawGizmosSelected()
        {
            if (splineContainer == null || splineContainer.Spline.Count < 2)
                return;
            Gizmos.color = Color.cyan;
            int count = Mathf.Max(1, slotCount);
            for (int i = 0; i < count; i++)
            {
                float3 position = splineContainer.EvaluatePosition(Mathf.Repeat(startOffset + progress + (float)i / count, 1f));
                Gizmos.DrawWireSphere(position, 0.12f);
            }
            DrawStationGizmo();
        }

        // EN: Draws the station segment as a thick orange polyline along the spline, with spheres at its start and end.
        // VI: Vẽ đoạn trạm thành đường gấp khúc màu cam dọc spline, có hình cầu ở điểm đầu và điểm cuối.
        private void DrawStationGizmo()
        {
            float length = Mathf.Repeat(stationEnd - stationStart, 1f);
            if (length <= Mathf.Epsilon)
                return;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 1f);
            const int Steps = 24;
            Vector3 previous = splineContainer.EvaluatePosition(stationStart);
            for (int i = 1; i <= Steps; i++)
            {
                Vector3 next = splineContainer.EvaluatePosition(Mathf.Repeat(stationStart + length * i / Steps, 1f));
                for (int k = -2; k <= 2; k++)
                {
                    Vector3 offset = Vector3.up * (k * 0.02f);
                    Gizmos.DrawLine(previous + offset, next + offset);
                }
                previous = next;
            }
            Gizmos.DrawSphere((Vector3)splineContainer.EvaluatePosition(stationStart), 0.15f);
            Gizmos.DrawSphere((Vector3)splineContainer.EvaluatePosition(stationEnd), 0.15f);
        }
    }
}

