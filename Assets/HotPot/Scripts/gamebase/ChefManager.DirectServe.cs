using System.Collections;
using UnityEngine;

namespace FoodPop
{
    public partial class ChefManager
    {
        // EN: Only an idle, enabled and unpaused chef may accept the one slot-free portion.
        // VI: Chỉ đầu bếp rảnh, đang bật và không pause mới nhận một phần không dùng slot.
        public bool CanPrepareDirect => isActiveAndEnabled && autoPrepareInStation && !BoosterPaused && !IsBusy && Time.timeScale > 0f;

        private DirectServeReservation directServe;

        // EN: Start exactly one committed overflow portion; its recipe need has already been reserved.
        // VI: Bắt đầu đúng một phần dư đã chốt; nhu cầu công thức của nó đã được giữ trước.
        public void BeginDirectPreparation(Food sourceDish, bool second, DirectServeReservation reservation)
        {
            directServe = reservation;
            SpriteRenderer source = second ? sourceDish.SecondIconRenderer : sourceDish.IconRenderer;
            Transform origin = source != null ? source.transform : sourceDish.transform;
            Sprite raw = IngameConfig.Instance.FoodIcons.GetIcon(reservation.Ingredient, FoodState.Raw);
            SetupCuttingFood(source, raw, origin.position, origin.rotation, origin.lossyScale);
            preparationRoutine = StartCoroutine(PrepareDirectFood(origin.lossyScale));
        }

        // EN: Fly one raw portion to the chef, prepare it, then dispatch to its booked order without a return plate.
        // VI: Bay một phần thô tới đầu bếp, sơ chế rồi gửi tới order đã giữ, không có đĩa trả về.
        private IEnumerator PrepareDirectFood(Vector3 sourceScale)
        {
            DirectServeReservation reservation = directServe;
            FoodType type = reservation.Ingredient;
            Vector3 start = cuttingFood.transform.position;
            Vector3 cutScale = sourceScale * Config.preparation.cuttingScale;
            float lift = GetFlightDuration(Config.preparation.liftDuration, Vector3.Distance(start, GetCuttingPosition()));
            yield return Hop(cuttingFood.transform, () => start, GetCuttingPosition, sourceScale, cutScale, lift);
            if (!reservation.IsValid) { FinishPreparation(); yield break; }
            cuttingFood.enabled = false;
            SetChopping(true);
            float duration = Mathf.Max(0.01f, ChefAnims != null ? ChefAnims.GetPreparationDuration(type) : 0f);
            yield return PlayChops(duration, type, cuttingFood, cutScale);
            SetChopping(false);
            if (!reservation.IsValid) { FinishPreparation(); yield break; }
            Sprite prepared = IngameConfig.Instance.FoodIcons.GetIcon(type, FoodState.Prepared);
            yield return PlayPreparedPoof(cuttingFood, prepared, cutScale);
            reservation.TryDeliver(prepared, cuttingFood.transform.position, cuttingFood.transform.lossyScale);
            FinishPreparation();
        }
        // EN: A normal job owns its belt arrival; a direct job owns an unchanged live recipe reservation.
        // VI: Lượt thường sở hữu lượt đáp trên băng; lượt đi thẳng sở hữu giữ công thức còn sống không đổi.
        private bool HasPreparationTarget()
        {
            return directServe != null ? directServe.IsValid : OwnsPreparationPlate();
        }

    }
}
