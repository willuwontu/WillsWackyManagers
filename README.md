# Wills Wacky Managers

Provides 2 different managers for the community to use:
- CurseManager
- RerollManager

## CurseManager

This manager provides the various utilities needed for using curses.

Any added curses must utilize `RegisterCurse()` via `CustomCard.BuildCard<MyCurse>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });`.

Additionally, they should use the `curseCategory` in order to not be pickable for players.

The `curseInteractionCategory` can be used to denote cards that should only show up when a player has a curse.

<details>
<summary>Properties</summary>

### instance
```cs
CurseManager instance { get; private set; }
```
#### Description
A static reference of the class for accessibility from within static functions.

### curseCategory
```cs
CardCategory curseCategory
```
#### Description
The card category for every curse. If not utilized, curses may show up for regular picking.

### curseInteractionCategory
```cs
CardCategory curseInteractionCategory
```
#### Description
The card category for cards that interacted with cursed players. When utilized, cards with it will only show up when a player has a curse.
</details>

<details>
<summary>Functions</summary>

### RandomCurse()
```cs
CardInfo RandomCurse(Player player)
```
#### Description
Returns a random curse that is valid for the target player. Respects card rarity.

#### Parameters
- *Player* `player` the player to get the curse for.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
var curse = CurseManager.instance.RandomCurse(player);
```

### CursePlayer()
```cs
void CursePlayer(Player player)
void CursePlayer(Player player, Action<CardInfo> callback)
```
#### Description
Adds a random valid curse to the targeted player. Respects card rarity.

#### Parameters
- *Player* `player` the player to curse.
- *Action<CardInfo>* `callback` an optional action to run with the card info of the added curse.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
CurseManager.instance.CursePlayer(player, (curse) => { ModdingUtils.Utils.CardBarUtils.instance.ShowImmediate(player, curse); });
```

### RegisterCurse()
```cs
void RegisterCurse(CardInfo cardInfo)
```
#### Description
Registers a card as a curse with the curse manager. The card still needs to apply `curseCategory` on its own.

#### Parameters
- *CardInfo* `cardInfo` the card to register.

#### Example Usage
```CSHARP
CustomCard.BuildCard<MyCurse>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });
```

### HasCurse()
```cs
bool HasCurse(Player player)
```
#### Description
Returns true if a player has a curse.

#### Parameters
- *Player* `player` the player to check.

#### Example Usage
```CSHARP
var player = PlayerManager.instance.players[0];
var cursed = CurseManager.instance.HasCurse(player);
```

### IsCurse()
```cs
bool IsCurse(CardInfo cardInfo)
```
#### Description
Returns true if the card is a registered curse.

#### Parameters
- *CardInfo* `cardInfo` the card to check.

#### Example Usage
```CSHARP
var card = PlayerManager.instance.players[0].data.currentCards[0];
var isCurse = CurseManager.instance.IsCurse(card);
```
</details>


