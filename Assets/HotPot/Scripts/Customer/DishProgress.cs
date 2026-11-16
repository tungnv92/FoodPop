using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Progress of one ordered final dish: its recipe (the first ingredient is the base that must come first, unless the dish picture allows any order), ingredients still to send, ingredients flying to it and whether it has been assembled on the tray.
    // VI: Tiến độ của một món hoàn chỉnh được order: công thức (nguyên liệu đầu tiên là đế, phải tới trước, trừ khi hình món cho phép thứ tự bất kỳ), nguyên liệu còn phải gửi, nguyên liệu đang bay tới và trạng thái đã ghép xong trên khay.
    internal sealed class DishProgress
    {
        public readonly FinalFoodView dish;
        public readonly List<FoodType> recipe;
        public readonly List<FoodType> missing;
        public readonly bool anyOrder;
        public int incoming;
        public bool baseReserved;
        public bool assembled;

        // EN: Creates the record of a dish with its optional picture prefab: every recipe ingredient starts as missing (ingredients of type None are skipped) and the prefab decides whether the base must come first.
        // VI: Tạo bản ghi của một món kèm prefab hình tùy chọn: mọi nguyên liệu trong công thức ban đầu đều đang thiếu (bỏ qua nguyên liệu None) và prefab quyết định đế có phải tới trước không.
        public DishProgress(FinalFoodView dish, IReadOnlyList<FoodType> ingredients)
        {
            this.dish = dish;
            anyOrder = dish != null && dish.AnyIngredientOrder;
            recipe = new List<FoodType>();
            foreach (FoodType ingredient in ingredients)
            {
                if (ingredient != FoodType.None)
                    recipe.Add(ingredient);
            }
            missing = new List<FoodType>(recipe);
        }

        // EN: The base ingredient (first of the recipe) that must reach the dish before any other.
        // VI: Nguyên liệu đế (đầu tiên trong công thức) phải tới món trước mọi nguyên liệu khác.
        public FoodType BaseIngredient => recipe.Count > 0 ? recipe[0] : FoodType.None;

        // EN: Accepts a missing ingredient only when this dish has no incoming flight; the base must land before other recipe ingredients can depart, unless the dish accepts its ingredients in any order.
        // VI: Chỉ nhận nguyên liệu còn thiếu khi món không có nguyên liệu đang bay tới; đế phải đáp xuống trước khi nguyên liệu khác trong công thức được rời băng chuyền, trừ khi món nhận nguyên liệu theo thứ tự bất kỳ.
        public bool CanAccept(FoodType ingredient)
        {
            if (incoming > 0 || !missing.Contains(ingredient))
                return false;
            return anyOrder || ingredient == BaseIngredient || baseReserved;
        }

        // EN: Books one ingredient for this dish as it leaves the belt.
        // VI: Giữ chỗ một nguyên liệu cho món khi nó rời băng chuyền.
        public void Reserve(FoodType ingredient)
        {
            missing.Remove(ingredient);
            incoming++;
            if (ingredient == BaseIngredient)
                baseReserved = true;
        }

        // EN: Releases a failed dispatch reservation without consuming an ingredient or keeping the slot busy.
        // VI: Bỏ giữ chỗ khi không phóng được món, không mất nguyên liệu hoặc làm ô bị khóa.
        public void CancelReservation(FoodType ingredient)
        {
            missing.Add(ingredient);
            incoming = Mathf.Max(0, incoming - 1);
            if (ingredient == BaseIngredient)
                baseReserved = false;
        }

        // EN: Records one booked ingredient landing on the dish.
        // VI: Ghi nhận một nguyên liệu đã giữ chỗ đáp xuống món.
        public void Arrive()
        {
            incoming = Mathf.Max(0, incoming - 1);
        }

        // EN: True once every ingredient has arrived (nothing missing, nothing still flying).
        // VI: True khi mọi nguyên liệu đã tới (không còn thiếu, không còn đang bay).
        public bool Done => missing.Count == 0 && incoming == 0;
    }
}
