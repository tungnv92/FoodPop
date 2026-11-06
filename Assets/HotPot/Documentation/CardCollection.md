# Card Collection Event ("Halloween Treats")

## Overview

The **Card Collection** event is a 2-week (14-day) LiveOps feature that unlocks after the player wins level 40 (`unlockLevelIndex = 40`). Players collect 54 themed cards across 6 collections (9 cards per collection) by winning levels on their first attempt, spinning the Halloween Spin board, or purchasing Card Packs.

---

## Architecture & Scripts

All Card Collection runtime scripts are located under `Assets/Packages/CardCollection/Scripts/`:

- [`CardCollectionModel.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionModel.cs)
  - `CardDropResult`: Transient drop/conversion payload (`cardId`, `setIndex`, `rarity`, `isDuplicate`, `ticketsConverted`).
  - `CardCollectionData`: Persistent player state stored in `PlayerData.CardCollectionData` (unlock state, start/end timestamps, owned card IDs, claimed set indices, grand reward status, spin tickets, spin milestone counter, `currentSpinBoardIndex` (`0..3`), `setDrawsWithoutNewCard` (`6` per-set pity counters), and pending drop results).
- [`CardCollectionConfig.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionConfig.cs)
  - `CardRarity`: `OneStar` (1★) to `FourStar` (4★).
  - `CardRarityDropConfig`: Base drop weight (`60, 30, 10, 0`) and duplicate ticket conversion value (`10, 30, 80, 200`) per rarity.
  - `CardSetGachaParam` & `CardGachaSlotRule`: Per-set dynamic weight parameters (`baseWeight`, `earlyStrength`, `finishStrength`, `finishCurve`, `missPerDraw`, `newCardMultiplier`, `guaranteeDraws`) and rarity weights (`NormalGachaRule` + per-slot IAP Pack rules).
  - `CardCollectionSetInfo` & `CardCollectionCardInfo`: Configuration for the 6 collections (*Treats*, *Pumpkins*, *Home Decor*, *Witchcraft*, *Backyard*, *Costumes*) and 9 cards per collection (`Treats: 6×1★, 3×2★`; `Pumpkins: 3×1★, 6×2★`; `Home Decor: 6×1★, 3×3★`; `Witchcraft/Backyard/Costumes: 3×1★, 3×2★, 2×3★, 1×4★`), plus per-collection completion rewards (`List<ItemData>`).
  - `CardSpinSlotInfo`, `CardSpinBoardInfo` & `CardPackShopInfo`: Configuration for the 4 Lucky Spin boards (`spinBoards`), escalating spin costs (`spinStepCosts`: `50 → 600`), `subPool` (`1..5`), `specificCardId`, and 4 IAP Card Packs (*Basic*, *Rare*, *Epic*, *Special*).
  - `EnsureDefaultSetup()`: Populates default 6x9 card definitions, Gacha parameters, 4 spin boards, escalating spin costs, and shop packs if unassigned.
- [`CardCollectionManager.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionManager.cs)
  - Manages the 14-day lifecycle (`UnlockEvent()`, `CheckProgress()`, `ResetDataForNewCycle()`).
  - Awards 1 random card on first-try level win (`OnLevelWin(bool isFirstTry)`) using `RollCardByGachaRule(config.normalGachaRule)`.
  - Implements the 5-step Gacha algorithm (`RollCardByGachaRule()`, `TryRollHardGuaranteedCard()`, `ComputeDynamicSetWeight()`, `RollCardInCollectionAndRarity()`, `UpdateSetGachaProgressOnDraw()`, `GrantCard()`).
  - Converts duplicate cards into Spin Tickets and handles 4-board, 12-slot spin execution with `subPool` `1→5` ordering (`TryExecuteSpin()`, `RollSpinSlotIndex()`, `AdvanceSpinBoardIfCompleted()`).
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
   - Displays the 12 perimeter reward slots for the active board (`Manager.GetCurrentSpinSlots()`), `Complete set after X/12 spins` progress header, current ticket balance, and escalating spin cost (`50 → 100 → 150 → ... → 600`).
   - Spin rolls strictly from the lowest remaining `subPool` (`1 → 5`) among unclaimed slots on the active board (`CardCollectionManager.RollSpinSlotIndex()`). After each spin, the rewarded slot is marked as claimed in `CardCollectionData.claimedSpinSlots`, visually disabled (`CardCollectionSpinSlotUI.SetDisabled(true)`), and skipped in all subsequent spins on that board.
   - Boards 1–3 contain the 9 specific cards of Sets 4 (*Witchcraft* `#28–#36`), 5 (*Backyard* `#37–#45`), and 6 (*Costumes* `#46–#54`) plus 3 resource slots; Board 4 contains 12 resource slots and repeats indefinitely (`CardCollectionManager.AdvanceSpinBoardIfCompleted()`).
4. **Card Pack IAP Shop Popup (Picture 4)**:
   - [`PopupCardCollectionShop.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionShop.cs) & [`CardCollectionPackItemUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionPackItemUI.cs)
   - Displays the 4 purchasable Card Packs (*Basic Pack* — 2 cards, *Rare Pack* — 3 cards, *Epic Pack* — 4 cards, *Special Pack* — 5 cards) and integrates with `IAPManager`.
5. **Collection Album Popup (Picture 5)**:
   - [`PopupCardCollectionAlbum.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardCollectionAlbum.cs), [`CardCollectionCardSlotUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionCardSlotUI.cs), & [`CardCollectionRewardUI.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionRewardUI.cs)
   - Displays the `3x3` card grid for a collection (`CardCollectionCardSlotUI` sets the slot image to `cardInfo.cardSprite` when owned, or the rarity-corresponding `BackCard` sprite via `CardCollectionConfig.GetBackCardSprite(rarity)` when unowned), completion rewards via `CardCollectionRewardUI.SetReward()`, `X/9` progress bar, and `< 1/6 >` page navigation.
6. **Card Reward Reveal & Home Button**:
   - [`PopupCardReward.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/PopupCardReward.cs): Keeps the top-right ticket balance holder (`ticketHolderObject`) inactive by default (`SetTicketHolderActive(false)`), displays the rarity-matching `BackCard` sprite at the center first (`CardCollectionConfig.GetBackCardSprite(rarity)`), and plays a horizontal flip animation (`PlayCardFlipAndDuplicateTransition()`) to reveal `cardInfo.cardSprite`. If the card is a duplicate, it animates the card shifting left and scaling down while popping in the arrow and converted ticket amount on the right (`AppendDuplicateLayoutTransition()`), then activates the ticket balance holder (`SetTicketHolderActive(true)`) and automatically plays the `ParticleImage` (`ticketParticleImage`) attraction effect flying to the ticket holder (`PlayTicketAttractionEffect()`). `FirstParticleFinished()` tweens `ticketHolderAmountText` upward to the new ticket balance (and punches `ticketHolderIconTf`), and `LastParticleFinished()` activates `continueButton`.
   - [`CardCollectionButtonHome.cs`](file:///d:/Projects/FoodPop/Assets/Packages/CardCollection/Scripts/CardCollectionButtonHome.cs): Home screen widget displaying countdown timer, notification badge, tutorial hand, and pending card drop animations on returning to Home.

---

## Core Rules & Drop Algorithm (`FoodHunt_CardCollection (2).xlsx`)

1. **First-Try Win Requirement**:
   - In [`PopupWin.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Popup/PopupWin.cs), `CardCollectionManager.OnLevelWin(isFirstTry)` is invoked with `isFirstTry = retryCount <= 0`. Replaying or losing a level awards no card.
2. **5-Step Gacha Algorithm (`NormalGachaRule` & Card Packs)**:
   - **Step 0 (Hard Guarantee / Pity)**: Checks incomplete sets (`0→5`); if `setDrawsWithoutNewCard[set] >= guaranteeDraws` (`1, 2, 35, 40, 30, 45`), forces an unowned card from that set (rolling rarity restricted to rarities with missing cards in that set).
   - **Step 1 (Roll Rarity)**: Rolls rarity from the active `CardGachaSlotRule` (`NormalGachaRule`: `1★ = 60%`, `2★ = 30%`, `3★ = 10%`, `4★ = 0%`).
   - **Step 2 (Filter Sets)**: Filters sets that contain at least one card of the rolled rarity.
   - **Step 3 (Roll Set by Dynamic Weight)**:
     $$\text{weight} = \text{baseWeight} \times \text{earlyFactor} \times \text{finishFactor} \times \text{missFactor}$$
     where $\text{progress} = \text{owned}/9$, $\text{earlyFactor} = 1 + (1 - \text{progress}) \times \text{earlyStrength}$, $\text{finishFactor} = 1 + \text{progress}^{\text{finishCurve}} \times \text{finishStrength}$ (when $\text{owned} < 9$, else $1$), and $\text{missFactor} = 1 + \text{drawsWithoutNewCard} \times \text{missPerDraw}$.
   - **Step 4 (Roll Card in Set & Rarity)**: Unowned cards in `(set, rarity)` use weight `newCardMultiplier` (`2.0` for Sets 1, 2, 3, 5; `1.5` for Sets 4, 6), while owned cards use weight `1.0`.
   - **Step 5 (Update Progress)**: Receiving a new card resets `setDrawsWithoutNewCard[set] = 0`; receiving a duplicate increments `setDrawsWithoutNewCard` for all incomplete sets (`owned < 9`) by `1`.
3. **Duplicate Conversion & Lucky Spin**:
   - Duplicate cards automatically convert into Spin Tokens (`1★ = 10`, `2★ = 30`, `3★ = 80`, `4★ = 200` tokens).
   - Lucky Spin costs `50 → 100 → 150 → 200 → 250 → 300 → 350 → 400 → 450 → 500 → 550 → 600` tokens across the 12 spins of each board (`3,900` tokens per board), rolling strictly in `subPool` `1 → 5` order.

---

## Verification Status

- **Verified**: All C# scripts, data models, ScriptableObject configuration with automatic default initialization (`EnsureDefaultSetup()`), and integration hooks in [`PopupId.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Base/PopupId.cs), [`PlayerData.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/PlayerData.cs), [`GameFeatureContext.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/GameFeatureContext.cs), [`ProfileManager.cs`](file:///d:/Projects/FoodPop/Assets/Packages/Common/PlayerContext/ProfileManager.cs), [`PopupWin.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Popup/PopupWin.cs), and [`HomePanel.cs`](file:///d:/Projects/FoodPop/Assets/_Hoang/Scripts/Runtime/UI/Home/HomePanel.cs) have been implemented.
- **Pending Unity Editor Setup**: Creating the `CardCollectionConfig.asset` ScriptableObject and wiring the UI prefabs (`PopupCardCollection`, `PopupCardCollectionInfo`, `PopupCardCollectionSpin`, `PopupCardCollectionShop`, `PopupCardCollectionAlbum`, `PopupCardReward`, and `CardCollectionButtonHome`) in the Unity Editor scenes/prefabs.

