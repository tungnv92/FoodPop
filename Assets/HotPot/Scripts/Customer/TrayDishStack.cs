using System;
using System.Collections;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{

[DisallowMultipleComponent]
[RequireComponent(typeof(ServingTrayView))]
public class TrayDishStack : MonoBehaviour
{
	[Serializable]
	private class SlotState
	{
		public string dish;

		public List<string> layers = new List<string>();

		public bool finished;
	}


	private sealed class SlotStack
	{
		public Transform slot;

		public FinalFoodType dish;

		public List<FoodType> recipe;

		public readonly List<FoodType> pieceTypes = new List<FoodType>();

		public readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();

		public SpriteRenderer finished;

		public Coroutine settle;
	}

	[Tooltip("Khay phục vụ cùng object, tạo các ô món.")]
	[SerializeField]
	private ServingTrayView tray;

	[Tooltip("Cấu hình dùng chung (xếp chồng, scale, hiệu ứng nảy); để trống dùng asset mặc định.")]
	[SerializeField]
	private IngameConfig ingameConfig;

	[Header("Trạng thái (chỉ xem khi chạy)")]
	[Tooltip("Món đang làm trên từng ô, các lớp đã có (từ dưới lên) và đã ghép xong chưa.")]
	[SerializeField]
	private List<SlotState> slotStates = new List<SlotState>();

	private readonly List<SlotStack> stacks = new List<SlotStack>();

	// EN: Shared tray-stack tuning from IngameConfig.
	// VI: Thông số xếp chồng trên khay lấy từ IngameConfig.
	private IngameConfig.TrayStackSettings Settings => IngameConfig.Resolve(ingameConfig).trayStack;
	private float stackStepRatio => Settings.stackStepRatio;
	private float foodScale => Settings.foodScale;

	// EN: Returns the linked serving tray.
	// VI: Trả về khay phục vụ được liên kết.
	public ServingTrayView Tray => tray;

	// EN: Returns the current number of dish slots.
	// VI: Trả về số ô món hiện tại.
	public int SlotCount => stacks.Count;

	// EN: Returns how many ingredient pieces are currently stacked in the slot.
	// VI: Trả về số nguyên liệu đang xếp chồng trong ô.
	public int PieceCount(int index) => index >= 0 && index < stacks.Count ? stacks[index].pieces.Count : 0;

	// EN: Returns the sorting layer used by stacked food.
	// VI: Trả về lớp vẽ của thức ăn xếp chồng.
	public int SortingLayer => (tray != null) ? tray.SortingLayerId : 0;

	// EN: Returns the first sorting order for ingredient layers.
	// VI: Trả về thứ tự vẽ đầu tiên cho các lớp nguyên liệu.
	private int BaseSortingOrder => (tray != null) ? tray.StackSortingOrder : 0;

	// EN: Prepares one tray stack per ordered dish using its recipe order.
	// VI: Chuẩn bị một chồng trên khay cho mỗi món theo thứ tự công thức.
	public void Begin(IReadOnlyList<FinalFoodType> dishes, IReadOnlyList<IReadOnlyList<FoodType>> recipes)
	{
		foreach (SlotStack old in stacks)
			StopSettle(old, snap: false);
		stacks.Clear();
		int num = dishes?.Count ?? 0;
		if (tray == null)
		{
			Debug.LogWarning("[TrayDishStack] Chưa gán ServingTrayView.", this);
			RefreshStates();
			return;
		}
		tray.SetSlotCount(num);
		IReadOnlyList<Transform> slots = tray.Slots;
		for (int i = 0; i < num && i < slots.Count; i++)
		{
			stacks.Add(new SlotStack
			{
				slot = slots[i],
				dish = dishes[i],
				recipe = new List<FoodType>(recipes[i])
			});
		}
		RefreshStates();
	}

	// EN: Checks whether the stack index refers to an existing tray slot.
	// VI: Kiểm tra chỉ số chồng có trỏ tới ô khay đang tồn tại không.
	public bool HasSlot(int index)
	{
		return index >= 0 && index < stacks.Count && stacks[index].slot != null;
	}

	// EN: Returns the world centre of a dish slot.
	// VI: Trả về tâm ô món trong tọa độ thế giới.
	public Vector3 GetSlotCenter(int index)
	{
		return stacks[index].slot.position;
	}

	// EN: Returns the completed dish order above its recipe layers.
	// VI: Trả về thứ tự vẽ món hoàn chỉnh phía trên các lớp công thức.
	public int GetDishSortingOrder(int index)
	{
		return BaseSortingOrder + stacks[index].recipe.Count + 1;
	}

	// EN: Returns the landing position: ingredients before Settings.insertFromIngredient land directly on their own layer; from that ingredient on, each lands at the insert spot just above the base (layer 2) and the bounce pushes the layers into recipe order like inserting into a row.
	// VI: Trả về vị trí đáp: các nguyên liệu trước Settings.insertFromIngredient đáp thẳng vào lớp của mình; từ nguyên liệu đó trở đi, mỗi nguyên liệu đáp vào chỗ chèn ngay trên nền (lớp thứ 2) rồi cú nảy đẩy các lớp về đúng thứ tự công thức như kiểu chèn hàng.
	public Vector3 GetLandingPosition(int index, FoodType type)
	{
		SlotStack slotStack = stacks[index];
		int layer;
		if (slotStack.pieceTypes.Count + 1 >= Settings.insertFromIngredient && LayerOf(slotStack, type) > 0)
			layer = 1;
		else
		{
			layer = 0;
			int own = LayerOf(slotStack, type);
			foreach (FoodType pieceType in slotStack.pieceTypes)
				if (LayerOf(slotStack, pieceType) < own)
					layer++;
		}
		return slotStack.slot.position + Vector3.up * ((float)layer * stackStepRatio * SlotWorldSize(slotStack));
	}

	// EN: Converts configured ingredient scale to world space for its slot.
	// VI: Đổi tỉ lệ nguyên liệu đã cấu hình sang tọa độ thế giới của ô.
	public Vector3 GetPieceWorldScale(int index, FoodType type)
	{
		Vector3 lossyScale = stacks[index].slot.lossyScale;
		return new Vector3(Mathf.Abs(lossyScale.x) * foodScale, Mathf.Abs(lossyScale.y) * foodScale, 1f);
	}

	// EN: Attaches an arriving ingredient, inserts it in recipe order and resettles its stack.
	// VI: Gắn nguyên liệu vừa tới, chèn theo thứ tự công thức và xếp lại chồng.
	public void AddPiece(int index, FoodType type, SpriteRenderer renderer)
	{
		SlotStack slotStack = stacks[index];
		renderer.transform.SetParent(slotStack.slot, worldPositionStays: true);
		renderer.transform.localRotation = Quaternion.identity;
		renderer.transform.localScale = new Vector3(foodScale, foodScale, 1f);
		int num = LayerOf(slotStack, type);
		int i;
		for (i = 0; i < slotStack.pieceTypes.Count && LayerOf(slotStack, slotStack.pieceTypes[i]) <= num; i++)
		{
		}
		slotStack.pieceTypes.Insert(i, type);
		slotStack.pieces.Insert(i, renderer);
		Restack(slotStack, animate: true);
		RefreshStates();
	}

	// EN: Transfers the ingredient renderers out of a stack for merging.
	// VI: Lấy các bộ hiển thị nguyên liệu ra khỏi chồng để ghép món.
	public List<SpriteRenderer> TakePieces(int index)
	{
		SlotStack slotStack = stacks[index];
		StopSettle(slotStack, snap: true);
		List<SpriteRenderer> result = new List<SpriteRenderer>(slotStack.pieces);
		slotStack.pieces.Clear();
		slotStack.pieceTypes.Clear();
		RefreshStates();
		return result;
	}

	// EN: Places the completed dish in its slot with the configured scale and sorting.
	// VI: Đặt món hoàn chỉnh vào ô với tỉ lệ và thứ tự vẽ đã cấu hình.
	public Vector3 SetFinishedDish(int index, SpriteRenderer renderer)
	{
		SlotStack slotStack = stacks[index];
		slotStack.finished = renderer;
		renderer.transform.SetParent(slotStack.slot, worldPositionStays: false);
		renderer.transform.localPosition = new Vector3(0f, 0f, -0.05f);
		renderer.transform.localRotation = Quaternion.identity;
		renderer.transform.localScale = new Vector3(foodScale, foodScale, 1f);
		renderer.sortingLayerID = SortingLayer;
		renderer.sortingOrder = GetDishSortingOrder(index);
		RefreshStates();
		return renderer.transform.localScale;
	}

	// EN: Releases ownership of the completed dish for serving.
	// VI: Chuyển quyền quản lý món hoàn chỉnh để phục vụ khách.
	public SpriteRenderer TakeFinishedDish(int index)
	{
		SlotStack slotStack = stacks[index];
		SpriteRenderer finished = slotStack.finished;
		slotStack.finished = null;
		RefreshStates();
		return finished;
	}

	// EN: Returns all owned food renderers and clears stack bookkeeping.
	// VI: Trả về mọi bộ hiển thị thức ăn đang quản lý và xóa dữ liệu các chồng.
	public List<SpriteRenderer> ReleaseAll()
	{
		List<SpriteRenderer> list = new List<SpriteRenderer>();
		foreach (SlotStack stack in stacks)
		{
			StopSettle(stack, snap: false);
			list.AddRange(stack.pieces);
			if (stack.finished != null)
			{
				list.Add(stack.finished);
			}
		}
		stacks.Clear();
		RefreshStates();
		return list;
	}

	// EN: Removes all dish slots from the linked tray.
	// VI: Xóa mọi ô món khỏi khay được liên kết.
	public void ClearSlots()
	{
		if (tray != null)
		{
			tray.SetSlotCount(0);
		}
	}

	// EN: Repositions ingredient layers in recipe order (the bounce only plays once the slot holds at least Settings.insertFromIngredient ingredients); when animated, the layers hop up lightly first and the draw order is rearranged at the top of the hop before they drop into place.
	// VI: Xếp lại các lớp nguyên liệu theo thứ tự công thức (chỉ nảy khi ô đã có từ Settings.insertFromIngredient nguyên liệu trở lên); khi có hiệu ứng, các lớp nảy nhẹ lên trước, thứ tự vẽ được sắp lại ở đỉnh cú nảy rồi mới rơi về vị trí.
	private void Restack(SlotStack stack, bool animate)
	{
		StopSettle(stack, snap: false);
		if (animate && Settings.bounceDuration > 0f && stack.pieces.Count >= Settings.insertFromIngredient && isActiveAndEnabled)
		{
			stack.settle = StartCoroutine(BounceRestack(stack));
			return;
		}
		SnapStack(stack);
	}

	// EN: Places every layer at its final position and applies the final draw order.
	// VI: Đặt mọi lớp vào vị trí cuối cùng và áp thứ tự vẽ cuối cùng.
	private void SnapStack(SlotStack stack)
	{
		for (int i = 0; i < stack.pieces.Count; i++)
		{
			SpriteRenderer piece = stack.pieces[i];
			if (piece == null)
				continue;
			piece.transform.localPosition = LayerLocalPosition(stack, i);
			ApplyLayerSorting(stack, i);
		}
	}

	// EN: Bounce that reorders while rising: draw orders switch to the recipe order immediately, each layer rises from where it is to its final stacked position plus i * bounceStep (layer 1 stays, layer 2 one step, layer 3 two steps...), pauses for bounceHold, then drops into place. No scale change.
	// VI: Nảy kèm sắp lớp ngay khi đi lên: thứ tự vẽ đổi sang thứ tự công thức ngay từ đầu, mỗi lớp đi từ chỗ đang đứng lên vị trí xếp chồng cuối cộng i * bounceStep (lớp 1 đứng im, lớp 2 một bước, lớp 3 hai bước...), dừng trong bounceHold, rồi rơi về chỗ. Không đổi scale.
	private IEnumerator BounceRestack(SlotStack stack)
	{
		int count = stack.pieces.Count;
		Vector3[] from = new Vector3[count];
		Vector3[] apex = new Vector3[count];
		Vector3[] scales = new Vector3[count];
		float step = Settings.bounceStep / Mathf.Max(0.0001f, Mathf.Abs(stack.slot.lossyScale.y));
		for (int i = 0; i < count; i++)
		{
			Transform piece = stack.pieces[i] != null ? stack.pieces[i].transform : null;
			from[i] = piece != null ? piece.localPosition : LayerLocalPosition(stack, i);
			scales[i] = piece != null ? piece.localScale : Vector3.one;
			apex[i] = LayerLocalPosition(stack, i) + Vector3.up * (i * step);
			if (piece != null)
				ApplyLayerSorting(stack, i);
		}

		float half = Settings.bounceDuration * 0.5f;
		for (float elapsed = 0f; elapsed < half; )
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / half);
			float ease = 1f - (1f - t) * (1f - t);
			for (int i = 0; i < count; i++)
				if (stack.pieces[i] != null)
				{
					stack.pieces[i].transform.localPosition = Vector3.LerpUnclamped(from[i], apex[i], ease);
					stack.pieces[i].transform.localScale = scales[i];
				}
			yield return null;
		}

		if (Settings.bounceHold > 0f)
			yield return new WaitForSeconds(Settings.bounceHold);

		for (float elapsed = 0f; elapsed < half; )
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / half);
			float ease = t * t;
			for (int i = 0; i < count; i++)
				if (stack.pieces[i] != null)
					stack.pieces[i].transform.localPosition = Vector3.LerpUnclamped(apex[i], LayerLocalPosition(stack, i), ease);
			yield return null;
		}
		stack.settle = null;
		SnapStack(stack);
	}

	// EN: True while the stack at the given index is still playing its bounce; the dish should only be checked and assembled after it ends.
	// VI: True khi chồng ở chỉ số đã cho vẫn đang nảy; chỉ kiểm tra và ghép món sau khi nảy xong.
	public bool IsSettling(int index)
	{
		return index >= 0 && index < stacks.Count && stacks[index].settle != null;
	}

	// EN: Stops a running settle animation; optionally snaps the layers to their final state.
	// VI: Dừng hiệu ứng xếp lại đang chạy; tùy chọn đặt ngay các lớp về trạng thái cuối.
	private void StopSettle(SlotStack stack, bool snap)
	{
		if (stack.settle != null)
		{
			StopCoroutine(stack.settle);
			stack.settle = null;
			if (snap)
				SnapStack(stack);
		}
	}

	// EN: Local resting position of the layer at the given stack index.
	// VI: Vị trí nghỉ cục bộ của lớp ở chỉ số chồng đã cho.
	private Vector3 LayerLocalPosition(SlotStack stack, int i)
	{
		float step = stackStepRatio * SlotWorldSize(stack) / Mathf.Max(0.0001f, Mathf.Abs(stack.slot.lossyScale.y));
		return new Vector3(0f, i * step, -0.01f * (i + 1));
	}

	// EN: Applies the final sorting layer and order of the layer at the given stack index.
	// VI: Áp lớp vẽ và thứ tự vẽ cuối cùng cho lớp ở chỉ số chồng đã cho.
	private void ApplyLayerSorting(SlotStack stack, int i)
	{
		SpriteRenderer piece = stack.pieces[i];
		piece.sortingLayerID = SortingLayer;
		piece.sortingOrder = BaseSortingOrder + i;
	}

	// EN: Updates Inspector state with dishes, ingredient layers and completion status.
	// VI: Cập nhật trạng thái Inspector gồm món, các lớp nguyên liệu và trạng thái hoàn thành.
	private void RefreshStates()
	{
		slotStates.Clear();
		foreach (SlotStack stack in stacks)
		{
			SlotState slotState = new SlotState
			{
				dish = stack.dish.ToString(),
				finished = (stack.finished != null)
			};
			foreach (FoodType pieceType in stack.pieceTypes)
			{
				slotState.layers.Add(pieceType.ToString());
			}
			slotStates.Add(slotState);
		}
	}

	// EN: Returns the recipe layer index, placing unknown ingredients last.
	// VI: Trả về chỉ số lớp trong công thức, đặt nguyên liệu không biết ở cuối.
	private static int LayerOf(SlotStack stack, FoodType type)
	{
		int num = stack.recipe.IndexOf(type);
		return (num >= 0) ? num : stack.recipe.Count;
	}

	// EN: Returns the slot width scale used to space ingredient layers.
	// VI: Trả về tỉ lệ chiều rộng ô dùng để tính khoảng cách các lớp nguyên liệu.
	private static float SlotWorldSize(SlotStack stack)
	{
		return Mathf.Abs(stack.slot.lossyScale.x);
	}

	// EN: Applies world scale while compensating for the parent transform.
	// VI: Áp dụng tỉ lệ thế giới và bù tỉ lệ của đối tượng cha.
	private static void SetWorldScale(Transform target, Vector3 worldScale)
	{
		Vector3 vector = ((target.parent != null) ? target.parent.lossyScale : Vector3.one);
		target.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, Mathf.Abs(vector.x)), worldScale.y / Mathf.Max(0.0001f, Mathf.Abs(vector.y)), 1f);
	}

	// EN: Resolves the companion tray component on the same object.
	// VI: Gán component khay đi cùng trên cùng đối tượng.
	private void OnValidate()
	{
		if (tray == null)
		{
			tray = GetComponent<ServingTrayView>();
		}
	}
}

}