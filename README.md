# Wills Wacky Managers

Provides 2 different managers for the community to use:
- CurseManager
- RerollManager
----
### v 1.0.1
- Fixed an error in reroll's logic that would cause it to stop after rerolling a player.

----
## CurseManager

This manager provides the various utilities needed for using curses.

Any added curses must utilize `RegisterCurse()` via `CustomCard.BuildCard<MyCurse>(cardInfo => { CurseManager.instance.RegisterCurse(cardInfo); });`.

Additionally, they should use the `curseCategory` in order to not be pickable for players.

The `curseInteractionCategory` can be used to denote cards that should only show up when a player has a curse.

<details>
<summary>Properties</summary>

### instance
```cs
CurseManager instance { get;}
```
#### Description
A static reference of the class for accessibility from within static functions.

### curseCategory
```cs
CardCategory curseCategory { get;}
```
#### Description
The card category for every curse. If not utilized, curses may show up for regular picking.

### curseInteractionCategory
```cs
CardCategory curseInteractionCategory { get;}
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
```
```cs
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

### GetRaw()
```cs
CardInfo[] GetRaw()
```
#### Description
Registers a card as a curse with the curse manager. The card still needs to apply `curseCategory` on its own.

#### Parameters

#### Example Usage
```CSHARP
var curse = CurseManager.instance.GetRaw();
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


## RerollManager

This manager provides the various utilities for rerolling a player's cards.

<details>
<summary>Properties</summary>

### instance
```cs
RerollManager instance { get;}
```
#### Description
A static reference of the class for accessibility from within static functions.

### NoFlip
```cs
CardCategory NoFlip { get;}
```
#### Description
The card category for cards that should not be given out after a table flip.

### flippingPlayer
```cs
Player flippingPlayer
```
#### Description
The player responsible for the tableflip. Used to add the table flip card to the player.

### tableFlipped
```cs
bool tableFlipped
```
#### Description
When set to true, a table flip will be initiated at the next end of a player's pick. Initiate the `FlipTable()` method if you wish to flip before then.

### tableFlipCard
```cs
CardInfo tableFlipCard
```
#### Description
The table flip card itself. It's automatically given out to the flipping player after a table flip.

### rerollPlayers
```cs
List<Player> rerollPlayers
```
#### Description
A list of players to reroll when the next reroll is initiated.

### reroll
```cs
bool reroll
```
#### Description
When set to true, a reroll will be initiated at the next end of a player's pick. Initiate the `Reroll()` method if you wish to reroll before then.

### rerollCard
```cs
CardInfo rerollCard
```
#### Description
The reroll card itself. It's automatically given out to the rerolling player after a table flip.
</details>

<details>
<summary>Functions</summary>

### FlipTable()
```cs
IEnumerator FlipTable(bool addCard = true)
```
#### Description
Initiates a table flip for all players.

#### Parameters
- *bool* `addCard` whether the flipping player (if one exists) shoul be given the Table Flip Card (if it exists).

#### Example Usage
```CSHARP


```

### InitiateRerolls()
```cs
IEnumerator InitiateRerolls(bool addCard = true)
```
#### Description
Initiates any rerolls in the queue.

#### Parameters
- *bool* `addCard` whether a player should be given the Reroll card after they reroll.

#### Example Usage
```CSHARP


```

### Reroll()
```cs
IEnumerator Reroll(Player player, bool addCard = true)
```
#### Description
Initiates any rerolls in the queue.

#### Parameters
- *Player* `player` the player whose cards to reroll
- *bool* `addCard` whether the player should be given the Reroll card afterwards.

#### Example Usage
```CSHARP


```
</details>