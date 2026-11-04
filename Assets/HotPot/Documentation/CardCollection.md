# Card Collection Event ("Halloween Treats")

## Overview

The **Card Collection** event is a 2-week (14-day) LiveOps feature that unlocks after the player wins level 40 (`unlockLevelIndex = 40`). Players collect 54 themed cards across 6 collections (9 cards per collection) by winning levels on their first attempt, spinning the Halloween Spin board, or purchasing Card Packs.

---

## Architecture & Scripts

All Card Collection runtime scripts are located under `Assets/Packages/CardCollection/Scripts/`:

- [`CardCollectionModel.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionModel.cs)
  - `CardDropResult`: Transient drop/conversion payload (`cardId`, `setIndex`, `rarity`, `isDuplicate`, `ticketsConverted`).
  - `CardCollectionData`: Persistent player state stored in `PlayerData.CardCollectionData` (unlock state, start/end timestamps, owned card IDs, claimed set indices, grand reward status, spin tickets, spin milestone counter, pity counters, and pending drop results).
- [`CardCollectionConfig.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionConfig.cs)
  - `CardRarity`: `OneStar` (1★) to `FourStar` (4★).
  - `CardRarityDropConfig`: Base drop weight and duplicate ticket conversion value per rarity.
  - `CardCollectionSetInfo` & `CardCollectionCardInfo`: Configuration for the 6 collections (*Treats*, *Pumpkins*, *Home Decor*, *Witchcraft*, *Backyard*, *Costumes*) and 9 cards per collection, plus per-collection completion rewards (`List<ItemData>`).
  - `CardSpinSlotInfo` & `CardPackShopInfo`: Configuration for the 12-slot Spin board and 4 IAP Card Packs (*Basic*, *Rare*, *Epic*, *Special*).
  - `EnsureDefaultSetup()`: Populates default 6x9 card definitions, rarity weights, spin slots, and shop packs if unassigned.
- [`CardCollectionManager.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionManager.cs)
  - Manages the 14-day lifecycle (`UnlockEvent()`, `CheckProgress()`, `ResetDataForNewCycle()`).
  - Awards 1 random card on first-try level win (`OnLevelWin(bool isFirstTry)`).
  - Implements the weighted + pity rarity drop algorithm (`RollRarity()`, `RollCardFromRarity()`, `GrantCard()`).
  - Converts duplicate cards into Spin Tickets and handles 12-slot spin execution (`TrySpin()`).
  - Grants collection completion rewards and the 54/54 Grand Reward (`TryClaimCollectionReward()`, `TryClaimGrandReward()`).
  - Includes Odin Inspector `[Button]` cheat helpers for testing in the Unity Editor.

### UI Components

1. **Main Event Popup (Picture 1)**:
   - [`PopupCardCollection.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollection.cs) & [`CardCollectionSetItemUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionSetItemUI.cs)
   - Displays grand reward items, `X/54` progress bar, remaining event timer, **Info**, **Close**, **Pack**, and **Spin** buttons, and the 6 collection items.
2. **Info Popup (Picture 2)**:
   - [`PopupCardCollectionInfo.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionInfo.cs)
   - Reuses the sequential step fade-in animation pattern from `MissionControlInfo` (`ShowStep()`).
3. **Halloween Spin Popup (Picture 3)**:
   - [`PopupCardCollectionSpin.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionSpin.cs) & [`CardCollectionSpinSlotUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionSpinSlotUI.cs)
   - Displays the 12 perimeter reward slots, `Complete set after X/12 spins` milestone header, current ticket balance, and animated slot highlight on spin (`Spin 50`).
4. **Card Pack IAP Shop Popup (Picture 4)**:
   - [`PopupCardCollectionShop.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionShop.cs) & [`CardCollectionPackItemUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionPackItemUI.cs)
   - Displays the 4 purchasable Card Packs (*Basic Pack*, *Rare Pack*, *Epic Pack*, *Special Pack*) and integrates with `IAPManager`.
5. **Collection Album Popup (Picture 5)**:
   - [`PopupCardCollectionAlbum.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionAlbum.cs), [`CardCollectionCardSlotUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionCardSlotUI.cs), & [`CardCollectionRewardUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionRewardUI.cs)
   - Displays the `3x3` card grid for a collection (`CardCollectionCardSlotUI` sets the slot image to `cardInfo.cardSprite` when owned, or the rarity-corresponding `BackCard` sprite via `CardCollectionConfig.GetBackCardSprite(rarity)` when unowned), completion rewards via `CardCollectionRewardUI.SetReward()`, `X/9` progress bar, and `< 1/6 >` page navigation.
6. **Card Reward Reveal & Home Button**:
   - [`PopupCardReward.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardReward.cs): Reveals newly earned cards and shows duplicate-to-ticket conversion.
   - [`CardCollectionButtonHome.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionButtonHome.cs): Home screen widget displaying countdown timer, notification badge, tutorial hand, and pending card drop animations on returning to Home.

---

## Core Rules & Drop Algorithm

1. **First-Try Win Requirement**:
   - In [`PopupWin.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Popup/PopupWin.cs), `CardCollectionManager.OnLevelWin(isFirstTry)` is invoked with `isFirstTry = retryCount <= 0`. Replaying or losing a level awards no card.
2. **Rarity Weights & Pity System**:
   - Default weights: `1★ = 65%`, `2★ = 25%`, `3★ = 8%`, `4★ = 2%`.
   - Pity counters guarantee at least a `3★` card after `8` wins without `3★+` and a `4★` card after `20` wins without `4★`.
   - Within the rolled rarity tier, unowned cards are favored with weight `unownedCardPreferenceChance` (`0.75`).
3. **Duplicate Conversion**:
   - Duplicate cards automatically convert into Spin Tickets (`1★ = 5`, `2★ = 10`, `3★ = 20`, `4★ = 50` tickets).

---

## Verification Status

- **Verified**: All C# scripts, data models, ScriptableObject configuration with automatic default initialization (`EnsureDefaultSetup()`), and integration hooks in [`PopupId.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Base/PopupId.cs), [`PlayerData.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/PlayerData.cs), [`GameFeatureContext.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/GameFeatureContext.cs), [`ProfileManager.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/ProfileManager.cs), [`PopupWin.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Popup/PopupWin.cs), and [`HomePanel.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Home/HomePanel.cs) have been implemented.
- **Pending Unity Editor Setup**: Creating the `CardCollectionConfig.asset` ScriptableObject and wiring the UI prefabs (`PopupCardCollection`, `PopupCardCollectionInfo`, `PopupCardCollectionSpin`, `PopupCardCollectionShop`, `PopupCardCollectionAlbum`, `PopupCardReward`, and `CardCollectionButtonHome`) in the Unity Editor scenes/prefabs.

