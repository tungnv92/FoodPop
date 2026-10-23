using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: One counter lane: the customer spot, its order card lines, the waiting figures behind it and the portraits used for its customers.
    // VI: Một làn quầy: vị trí khách, thẻ order (OrderView), các khách đứng chờ phía sau và danh sách ảnh dùng cho khách của làn.
    [Serializable]
    internal class Lane
    {
        [Tooltip("Khách đang đứng ở làn này (kèm thẻ order).")]
        public CustomerView customer;
        [Tooltip("Thẻ đặt món của làn trái hoặc phải, giữ các ô món và bố cục lưới.")]
        public OrderView orderView;
        [Tooltip("Thành phần phục vụ riêng của làn, quản lý hoạt cảnh và khay của khách này.")]
        public ServingFood servingFood;
        [Tooltip("Ảnh các khách đứng chờ đặt sẵn trong scene (người gần quầy nhất trước); dùng làm vị trí hàng chờ và làm mẫu khi cần thêm người.")]
        public SpriteRenderer[] waitingFigures = new SpriteRenderer[0];
        [Tooltip("Ảnh khách dùng lần lượt cho các khách của làn (lặp lại khi hết).")]
        public Sprite[] portraits = new Sprite[0];
        [Tooltip("Hướng trượt ra/vào: -1 = mép trái màn hình, 1 = mép phải.")]
        public int slideDirection = -1;

        [NonSerialized] public List<CustomerOrder> queue;
        [NonSerialized] public int queueIndex;
        [NonSerialized] public bool ordering;
        [NonSerialized] public long arrivalStamp;
        [NonSerialized] public readonly List<DishProgress> dishes = new List<DishProgress>();
        [NonSerialized] public readonly List<SpriteRenderer> figures = new List<SpriteRenderer>();
        [NonSerialized] public readonly List<Vector3> spots = new List<Vector3>();
        [NonSerialized] public Vector3 spotStep;
        [NonSerialized] public Vector3 queueOrigin;
        [NonSerialized] public bool queueCaptured;
    }
}
