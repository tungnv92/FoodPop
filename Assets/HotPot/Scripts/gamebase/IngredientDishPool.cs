using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Owns reusable ingredient dishes; only creates a new instance when the pool is exhausted.
    // VI: Quản lý đĩa nguyên liệu tái sử dụng; chỉ tạo instance mới khi pool đã hết đĩa.
    public sealed class IngredientDishPool
    {
        private readonly Food prefab;
        private readonly Transform root;
        private readonly Stack<Food> available = new Stack<Food>();
        private readonly HashSet<Food> owned = new HashSet<Food>();
        private readonly HashSet<Food> pooled = new HashSet<Food>();
        private bool disposed;

        // EN: Binds a prefab and inactive storage parent without spawning any dishes yet.
        // VI: Gán prefab và parent lưu đĩa không hoạt động, chưa tạo đĩa ngay.
        public IngredientDishPool(Food prefab, Transform root)
        {
            this.prefab = prefab;
            this.root = root;
        }

        // EN: Adopts a scene dish and returns it to storage so the initial layout seeds the pool.
        // VI: Nhận đĩa có sẵn trong scene và đưa vào kho để dùng bố cục ban đầu làm nguồn pool.
        public void Adopt(Food dish)
        {
            if (disposed || dish == null)
                return;
            owned.Add(dish);
            Release(dish);
        }

        // EN: Acquires an inactive dish, creating from the prefab only when no reusable dish remains.
        // VI: Lấy đĩa ở trạng thái tắt, chỉ tạo từ prefab khi không còn đĩa tái sử dụng.
        public Food Get(Transform parent)
        {
            if (disposed)
                return null;
            Food dish = null;
            while (available.Count > 0 && dish == null)
            {
                dish = available.Pop();
                pooled.Remove(dish);
            }
            if (dish == null)
            {
                if (prefab == null)
                    return null;
                dish = Object.Instantiate(prefab, root);
                owned.Add(dish);
            }
            dish.gameObject.SetActive(false);
            dish.transform.SetParent(parent, false);
            return dish;
        }

        // EN: Clears selection and food data before storing an owned dish, rejecting foreign or duplicate returns.
        // VI: Xóa viền chọn và dữ liệu nguyên liệu trước khi cất đĩa, từ chối đĩa ngoài pool hoặc trả trùng.
        public bool Release(Food dish)
        {
            if (disposed || dish == null || !owned.Contains(dish) || !pooled.Add(dish))
                return false;
            if (dish.Selection != null)
                dish.Selection.SetSelectable(false);
            dish.gameObject.SetActive(false);
            dish.Setup(FoodType.None, 0, null);
            dish.name = "Ingredient dish · Pooled";
            dish.transform.SetParent(root, false);
            dish.transform.localPosition = Vector3.zero;
            dish.transform.localRotation = Quaternion.identity;
            dish.transform.localScale = Vector3.one;
            available.Push(dish);
            return true;
        }

        // EN: Destroys owned instances only when the manager is disposed, including dishes moved outside its hierarchy.
        // VI: Chỉ hủy instance thuộc pool khi manager bị dọn, gồm cả đĩa đã chuyển ra ngoài hierarchy.
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            foreach (Food dish in owned)
            {
                if (dish == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(dish.gameObject);
                else
                    Object.DestroyImmediate(dish.gameObject);
            }
            available.Clear();
            pooled.Clear();
            owned.Clear();
        }
    }
}
