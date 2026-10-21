namespace FoodPop
{
    // EN: Preparation group of an ingredient, used to pick the chop sound and the debris effect; Auto means "use the default for this food type".
    // VI: Nhóm sơ chế của nguyên liệu, dùng để chọn tiếng dao và hiệu ứng vụn; Auto nghĩa là "dùng mặc định theo loại nguyên liệu".
    public enum FoodCategory
    {
        Auto = 0,
        Meat = 1,
        Vegetable = 2
    }

    // EN: Resolves the preparation group of a food type, honouring an override from FoodIconConfig when one is set.
    // VI: Xác định nhóm sơ chế của một loại nguyên liệu, ưu tiên giá trị ghi đè trong FoodIconConfig nếu có.
    public static class FoodCategories
    {
        // EN: Returns Meat or Vegetable for the food type: the config override when not Auto, otherwise the built-in default.
        // VI: Trả về Meat hoặc Vegetable cho loại nguyên liệu: dùng giá trị ghi đè trong config nếu khác Auto, nếu không thì dùng mặc định có sẵn.
        public static FoodCategory Get(FoodType type, FoodIconConfig config = null)
        {
            FoodCategory overrideCategory = config != null ? config.GetCategoryOverride(type) : FoodCategory.Auto;
            return overrideCategory != FoodCategory.Auto ? overrideCategory : GetDefault(type);
        }

        // EN: Built-in grouping: fish, seafood and eel count as Meat; rice, egg, cucumber, seaweed and condiments count as Vegetable.
        // VI: Nhóm mặc định: cá, hải sản và lươn là Meat (thịt); cơm, trứng, dưa leo, rong biển và gia vị là Vegetable (rau/đồ mềm).
        public static FoodCategory GetDefault(FoodType type)
        {
            switch (type)
            {
                case FoodType.Salmon:
                case FoodType.Tuna:
                case FoodType.Shrimp:
                case FoodType.CrabStick:
                case FoodType.SalmonRoe:
                case FoodType.Unagi:
                case FoodType.Octopus:
                case FoodType.Scallop:
                    return FoodCategory.Meat;
                default:
                    return FoodCategory.Vegetable;
            }
        }
    }
}
