# HotPotDemo — scene và vật thể world

- Scene `Assets/HotPot/Scenes/HotPotDemo.unity`, Game view dọc 945:2048.
- Vật thể gameplay (khách, đầu bếp, đĩa, món, thẻ order, quầy, băng chuyền, khay, booster) dùng Transform + SpriteRenderer ngoài Canvas. Chỉ nhãn số lượng/chữ dùng UI world-space (TextMeshProUGUI, canvas scale 0.01 = 100 px/unit).
- Băng chuyền `ConveyorStandalone`: lớp nền (order 0) và con `Front` (order 66) che lên đầu bếp (order 65). Slot băng vẽ từ `slotSortingOrder` (70). Chuyển động và số slot: xem [Conveyor.md](../../Handoff/Conveyor.md).
- Prefab dùng chung ở `Assets/HotPot/Prefabs/Ingame` và `Prefabs/Demo`. Chỉnh trực tiếp scene/prefab; không có tool Setup dựng prefab.

## SafeAreaCamera
`Scripts/UI/SafeAreaCamera.cs` trên camera scene; luật fit hai vùng: xem [UI.md](../../Handoff/UI.md#camera-và-hud).
